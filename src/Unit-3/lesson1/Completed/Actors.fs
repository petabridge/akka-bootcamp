namespace GithubActors

open System
open System.Windows.Forms
open System.Drawing
open Akka.FSharp
open Akka.Actor
open Akka.Routing

[<AutoOpen>]
module Actors =

    // make a pipe-friendly version of Akka.NET PipeTo for handling async computations
    let pipeToWithSender recipient sender asyncComp = pipeTo asyncComp recipient sender

    // Helper functions to check the type of query received
    let isWorkerMessage (someType: obj) = someType.GetType().IsSubclassOf(typeof<GithubActorMessage>)

    let isQueryStarrers (someType: obj) =
        if isWorkerMessage someType then
            match someType :?> GithubActorMessage with
            | QueryStarrers _ -> true
            | _ -> false
        else
            false

    let isQueryStarrer (someType: obj) =
        if isWorkerMessage someType then
            match someType :?> GithubActorMessage with
            | QueryStarrer _ -> true
            | _ -> false
        else
            false

    // Actors
    let githubAuthenticationActor (statusLabel: Label) (githubAuthForm: Form) (launcherForm: Form) (mailbox: Actor<_>) =

        let cannotAuthenticate reason =
            statusLabel.ForeColor <- Color.Red
            statusLabel.Text <- reason

        let showAuthenticatingStatus () =
            statusLabel.Visible <- true
            statusLabel.ForeColor <- Color.Orange
            statusLabel.Text <- "Authenticating..."

        let rec unauthenticated () =
            actor {
                let! message = mailbox.Receive ()

                match message with
                | Authenticate token ->
                    showAuthenticatingStatus ()
                    let client = GithubClientFactory.getUnauthenticatedClient ()
                    client.Credentials <- Octokit.Credentials token
                    
                    let continuation (task: System.Threading.Tasks.Task<Octokit.User>) : AuthenticationMessage =
                        match task.IsFaulted with
                        | true -> AuthenticationFailed
                        | false ->
                            match task.IsCanceled with
                            | true -> AuthenticationCancelled
                            | false ->
                                GithubClientFactory.setOauthToken token
                                AuthenticationSuccess
                        
                    client.User.Current().ContinueWith continuation
                    |> Async.AwaitTask
                    |!> mailbox.Self

                    return! authenticating ()
                | _ -> return! unauthenticated ()
            }
        and authenticating () =
            actor {
                let! message = mailbox.Receive ()

                match message with
                | AuthenticationFailed ->
                    cannotAuthenticate "Authentication failed."
                    return! unauthenticated ()
                | AuthenticationCancelled ->
                    cannotAuthenticate "Authentication timed out."
                    return! unauthenticated ()
                | AuthenticationSuccess ->
                    githubAuthForm.Hide ()
                    launcherForm.Show ()
                | _ -> return! authenticating ()
            }
            
        unauthenticated ()


    let mainFormActor (isValidLabel: Label) (createRepoResultsForm) (mailbox: Actor<_>) =

        let updateLabel message isValid =
            isValidLabel.Text <- message
            if isValid then isValidLabel.ForeColor <- Color.Green else isValidLabel.ForeColor <- Color.Red
            mailbox.UnstashAll ()

        let rec ready () =
            actor {
                let! message = mailbox.Receive ()

                match message with
                | ProcessRepo uri ->
                    select "akka://GithubActors/user/validator" mailbox.Context.System <! ValidateRepo uri
                    isValidLabel.Visible <- true
                    isValidLabel.Text <- sprintf "Validating %s..." uri
                    isValidLabel.ForeColor <- Color.Orange
                    return! busy ()
                | LaunchRepoResultsWindow (repoKey, coordinator) ->
                    let repoResultsForm: Form = createRepoResultsForm repoKey coordinator
                    repoResultsForm.Show ()
                    return! ready ()
                | _ -> return! ready ()
            }
        and busy () =
            actor {
                let! message = mailbox.Receive ()

                match message with
                | ValidRepo _ ->
                    updateLabel "Valid!" true
                    return! ready ()
                | InvalidRepo (uri, reason) ->
                    updateLabel reason false
                    return! ready ()
                | UnableToAcceptJob job ->
                    updateLabel (sprintf "%s/%s is a valid repo, but the system cannot accept additional jobs" job.Owner job.Repo) false
                    return! ready ()
                | AbleToAcceptJob job ->
                    updateLabel (sprintf "%s/%s is a valid repo - starting job!" job.Owner job.Repo) true
                    return! ready ()
                | LaunchRepoResultsWindow (_, _) ->
                    mailbox.Stash ()
                    return! busy ()
                | _ -> return! busy ()
            }

        ready ()


    let githubValidatorActor (getGithubClient: unit -> Octokit.GitHubClient) (mailbox: Actor<_>) =

        let splitIntoOwnerAndRepo repoUri =
            let results = Uri(repoUri, UriKind.Absolute).PathAndQuery.TrimEnd('/').Split('/') |> Array.rev
            (results.[1], results.[0]) // User, Repo
            
        let rec processMessage () = actor {
            let! message = mailbox.Receive ()
                
            match message with
            // outright invalid URLs
            | ValidateRepo uri when uri |> String.IsNullOrEmpty || not (Uri.IsWellFormedUriString(uri, UriKind.Absolute)) ->
                mailbox.Context.Sender <! InvalidRepo(uri, "Not a valid absolute URI")
            // repos that at least have a valid absolute URL
            | ValidateRepo uri ->
                let continuation (task: System.Threading.Tasks.Task<Octokit.Repository>) : GithubActorMessage =
                    match task.IsCanceled with
                    | true -> InvalidRepo(uri, "Repo lookup timed out")
                    | false ->
                        match task.IsFaulted with
                        | true -> InvalidRepo(uri, "Not a valid absolute URI")
                        | false -> ValidRepo task.Result
                
                let (user, repo) = splitIntoOwnerAndRepo uri
                let githubClient = getGithubClient ()
                githubClient.Repository.Get(user, repo).ContinueWith continuation
                |> Async.AwaitTask
                |> pipeToWithSender mailbox.Self mailbox.Context.Sender // send the message back to ourselves but pass the real sender through
            | InvalidRepo (uri, reason) ->
                InvalidRepo(uri, reason) |> mailbox.Context.Sender.Forward
            | ValidRepo repo ->
                mailbox.Context.ActorSelection("akka://GithubActors/user/commander") <! CanAcceptJob({ Owner = repo.Owner.Login; Repo = repo.Name })
            | UnableToAcceptJob key ->
                mailbox.Context.ActorSelection("akka://GithubActors/user/mainform") <! UnableToAcceptJob key
            | AbleToAcceptJob key ->
                mailbox.Context.ActorSelection("akka://GithubActors/user/mainform") <! AbleToAcceptJob key
            | _ -> return! processMessage ()

            return! processMessage ()
        }

        processMessage ()

    let githubWorkerActor (mailbox: Actor<_>) =
        
        let githubClient = lazy (GithubClientFactory.getClient ())

        let rec processMessage () = actor {
            let! message = mailbox.Receive ()

            match message with
            | RetryableQuery query when isQueryStarrer query.Query || isQueryStarrers query.Query ->
                match query.Query :?> GithubActorMessage with
                | QueryStarrer login ->
                    try
                        let starredRepos =
                            githubClient.Value.Activity.Starring.GetAllForUser (login)
                            |> Async.AwaitTask
                            |> Async.RunSynchronously

                        mailbox.Context.Sender <! StarredReposForUser(login, starredRepos)
                    with
                    | ex -> mailbox.Context.Sender <! nextTry query // operation failed - let the parent know
                | QueryStarrers repoKey ->
                    try
                        let users =
                            githubClient.Value.Activity.Starring.GetAllStargazers (repoKey.Owner, repoKey.Repo)
                            |> Async.AwaitTask
                            |> Async.RunSynchronously
                            |> Seq.toArray

                        mailbox.Context.Sender <! UsersToQuery users
                    with
                    | ex -> mailbox.Context.Sender <! nextTry query // operation failed - let the parent know
                | _ -> () // never reached
            | _ -> ()

            return! processMessage ()
        }

        processMessage ()


    let githubCoordinatorActor (mailbox: Actor<_>) =
                  
        let startWorking repoKey (scheduler: IScheduler) =
            {
                ReceivedInitialUsers = false
                CurrentRepo = repoKey
                Subscribers = System.Collections.Generic.HashSet<IActorRef> ()
                SimilarRepos = System.Collections.Generic.Dictionary<string, SimilarRepo> ()
                GithubProgressStats = getDefaultStats ()
                PublishTimer = new Cancelable (scheduler)
            }      

        // pre-start
        let githubWorker = spawn mailbox.Context "worker" (githubWorkerActor)

        let rec waiting () =
            actor {
                let! message = mailbox.Receive ()
                
                match message with
                | CanAcceptJob repoKey ->
                    mailbox.Context.Sender <! AbleToAcceptJob repoKey
                | BeginJob repoKey ->
                    githubWorker <! RetryableQuery { Query = QueryStarrers repoKey; AllowableTries = 4; CurrentAttempt = 0 }
                    let newSettings = startWorking repoKey mailbox.Context.System.Scheduler
                    return! working newSettings
                | _ -> return! waiting ()

                return! waiting ()
            }
        and working (settings: WorkerSettings) =
            actor {
                let! message = mailbox.Receive ()
                
                match message with
                // received a downloaded user back from the github worker
                | StarredReposForUser (login, repos) ->
                    repos
                    |> Seq.iter (fun repo -> 
                        if not <| settings.SimilarRepos.ContainsKey repo.HtmlUrl then
                            settings.SimilarRepos.[repo.HtmlUrl] <- { SimilarRepo.Repo = repo; SharedStarrers = 1 }
                        else
                            settings.SimilarRepos.[repo.HtmlUrl] <- increaseSharedStarrers settings.SimilarRepos.[repo.HtmlUrl]
                    )
                        
                    return! working {settings with GithubProgressStats = userQueriesFinished settings.GithubProgressStats 1 }
                | PublishUpdate ->
                    // Check to see if the job has fully completed
                    match settings.ReceivedInitialUsers && settings.GithubProgressStats.IsFinished with
                    | true ->
                        let finishStats = finish settings.GithubProgressStats

                        // All repos minus forks of the current one
                        let sortedSimilarRepos =
                            settings.SimilarRepos.Values
                            |> Seq.filter (fun repo -> repo.Repo.Name <> settings.CurrentRepo.Repo)
                            |> Seq.sortBy (fun repo -> -repo.SharedStarrers)

                        // Update progress (both repos and users)
                        settings.Subscribers
                        |> Seq.iter (fun subscriber ->
                            subscriber <! SimilarRepos sortedSimilarRepos
                            subscriber <! GithubProgressStats finishStats)

                        settings.PublishTimer.Cancel ()
                        return! waiting ()
                    | false ->
                        settings.Subscribers
                        |> Seq.iter (fun subscriber -> subscriber <! GithubProgressStats settings.GithubProgressStats)
                | UsersToQuery users ->
                    // queue all the jobs
                    users |> Seq.iter (fun user -> githubWorker <! RetryableQuery { Query = QueryStarrer user.Login; AllowableTries = 3; CurrentAttempt = 0 })
                    return! working {settings with GithubProgressStats = setExpectedUserCount settings.GithubProgressStats users.Length; ReceivedInitialUsers = true }
                // the actor is currently busy, cannot handle the job now
                | CanAcceptJob repoKey ->
                    mailbox.Context.Sender <! UnableToAcceptJob repoKey
                | SubscribeToProgressUpdates subscriber ->
                    // this is our first subscriber, which means we need to turn publishing on
                    if settings.Subscribers.Count = 0 then
                        mailbox.Context.System.Scheduler.ScheduleTellRepeatedly(
                            TimeSpan.FromMilliseconds 100., TimeSpan.FromMilliseconds 30.,
                            mailbox.Self, PublishUpdate, mailbox.Self, settings.PublishTimer)
                    settings.Subscribers.Add subscriber |> ignore

                // query failed, but can be retried
                | RetryableQuery query when query.CanRetry ->
                    githubWorker <! RetryableQuery query
                // query failed, can't be retried, and it's a QueryStarrers operation - meaning that the entire job failed
                | RetryableQuery query when not query.CanRetry && isQueryStarrers query.Query ->
                    settings.Subscribers
                    |> Seq.iter (fun subscriber -> subscriber <! JobFailed settings.CurrentRepo)

                    settings.PublishTimer.Cancel ()
                    return! waiting ()
                // query failed, can't be retried, and it's a QueryStarrer operation - meaning that an individual operation failed
                | RetryableQuery query when not query.CanRetry && isQueryStarrer query.Query ->
                    return! working {settings with GithubProgressStats = incrementFailures settings.GithubProgressStats 1 }
                | _ -> return! working settings

                return! working settings
            }

        waiting ()


    let githubCommanderActor (mailbox: Actor<_>) =

        // pre-start
        let c1 = spawn mailbox.Context "coordinator1" (githubCoordinatorActor)
        let c2 = spawn mailbox.Context "coordinator2" (githubCoordinatorActor)
        let c3 = spawn mailbox.Context "coordinator3" (githubCoordinatorActor)

        //create a broadcast router who will ask all of the coordinators if they are available for work
        let coordinatorPaths = [| string c1.Path; string c2.Path; string c3.Path |]
        let coordinator = mailbox.Context.ActorOf(Props.Empty.WithRouter(BroadcastGroup(coordinatorPaths)))

        // post-stop, kill off the old coordinator so we can recreate it from scratch
        mailbox.Defer (fun _ -> coordinator <! PoisonPill.Instance)

        // pass around the actor that sent the CanAcceptJob message as well as the current number of pending jobs
        let rec ready canAcceptJobSender pendingJobReplies =
            actor {
                let! message = mailbox.Receive ()

                match message with
                | CanAcceptJob repoKey ->
                    coordinator <! CanAcceptJob repoKey
                    return! asking mailbox.Context.Sender 3 // 3 pending job replies
                | _ -> return! ready canAcceptJobSender pendingJobReplies
            }
        // pass around the actor that sent the CanAcceptJob message as well as the current number of pending jobs
        and asking canAcceptJobSender pendingJobReplies =
            actor {
                let! message = mailbox.Receive ()

                match message with
                | CanAcceptJob repoKey ->
                    mailbox.Stash ()
                    return! asking canAcceptJobSender pendingJobReplies
                | UnableToAcceptJob repoKey ->
                    let currentPendingJobReplies = pendingJobReplies - 1
                    if currentPendingJobReplies = 0 then
                        canAcceptJobSender <! UnableToAcceptJob repoKey
                        mailbox.UnstashAll ()
                        return! ready canAcceptJobSender currentPendingJobReplies
                    else
                        return! asking canAcceptJobSender currentPendingJobReplies
                | AbleToAcceptJob repoKey ->
                    canAcceptJobSender <! AbleToAcceptJob repoKey
                    mailbox.Context.Sender <! BeginJob repoKey // start processing messages
                    mailbox.Context.ActorSelection "akka://GithubActors/user/mainform" <! LaunchRepoResultsWindow(repoKey, mailbox.Context.Sender) // launch the new window to view results of the processing
                    mailbox.UnstashAll ()
                    return! ready canAcceptJobSender pendingJobReplies
                | _ -> return! asking canAcceptJobSender pendingJobReplies
            }

        ready null 0        


    let repoResultsActor (usersGrid: DataGridView) (statusLabel: ToolStripStatusLabel) (progressBar: ToolStripProgressBar) (mailbox: Actor<_>) =
        let startProgress stats =
            progressBar.Minimum <- 0
            progressBar.Step <- 1
            progressBar.Maximum <- stats.ExpectedUsers
            progressBar.Value <- stats.UsersThusFar
            progressBar.Visible <- true
            statusLabel.Visible <- true

        let displayProgress stats =
            statusLabel.Text <- sprintf "%i out of %i users (%i failures) [%A elapsed]" stats.UsersThusFar stats.ExpectedUsers stats.QueryFailures stats.Elapsed

        let stopProgress repo =
            progressBar.Visible <- true
            progressBar.ForeColor <- Color.Red
            progressBar.Maximum <- 1
            progressBar.Value <- 1
            statusLabel.Visible <- true
            statusLabel.Text <- sprintf "Failed to gather date for GitHub repository %s / %s" repo.Owner repo.Repo
        
        let displayRepo similarRepo =
            let repo = similarRepo.Repo
            let row = new DataGridViewRow()
            row.CreateCells usersGrid
            row.Cells.[0].Value <- repo.Owner.Login
            row.Cells.[1].Value <- repo.Owner.Name
            row.Cells.[2].Value <- repo.Owner.HtmlUrl
            row.Cells.[3].Value <- similarRepo.SharedStarrers
            row.Cells.[4].Value <- repo.OpenIssuesCount
            row.Cells.[5].Value <- repo.StargazersCount
            row.Cells.[6].Value <- repo.ForksCount
            usersGrid.Rows.Add row |> ignore

        let mutable hasSetProgress = false
        let rec processMessage () = actor {
            let! message = mailbox.Receive ()
                
            match message with
            | GithubProgressStats stats -> // progress update
                if not hasSetProgress && stats.ExpectedUsers > 0 then
                    startProgress stats
                    hasSetProgress <- true
                displayProgress stats
                progressBar.Value <- stats.UsersThusFar + stats.QueryFailures
            | SimilarRepos repos -> // user update
                repos |> Seq.iter displayRepo
            | JobFailed repoKey -> // critical failure, like not being able to connect to Github
                stopProgress repoKey
            | _ -> ()

            return! processMessage ()
        }

        processMessage ()