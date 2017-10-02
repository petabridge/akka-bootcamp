namespace GithubActors

open System
open Akka.Actor

[<AutoOpen>]
module GeneralTypes =

    type RepoKey = { Owner: string; Repo: string }
    
    // RetryableQuery section
    type RetryableQuery = {
        Query: obj
        AllowableTries: int
        CurrentAttempt: int
    } with
        member this.RemainingTries = this.AllowableTries - this.CurrentAttempt
        member this.CanRetry = this.RemainingTries > 0

    let nextTry (retryableQuery: RetryableQuery) =
        { retryableQuery with CurrentAttempt = retryableQuery.CurrentAttempt + 1 }

    // SimilarRepo section
    type SimilarRepo = {
        Repo: Octokit.Repository
        SharedStarrers: int
    }

    let increaseSharedStarrers similarRepo =
        { similarRepo with SharedStarrers = similarRepo.SharedStarrers + 1 }

    // GithubProgressStats section
    type GithubProgressStats = {
        ExpectedUsers: int
        UsersThusFar: int
        QueryFailures: int
        StartTime: DateTime
        EndTime: DateTime option
    } with
        member this.Elapsed = 
            match this.EndTime with
            | Some endTime -> endTime - this.StartTime
            | None -> DateTime.UtcNow - this.StartTime
        member this.IsFinished =
            this.ExpectedUsers = (this.UsersThusFar + this.QueryFailures)

    let getDefaultStats () =
        {
            ExpectedUsers = 0
            UsersThusFar = 0
            QueryFailures = 0
            StartTime = DateTime.UtcNow
            EndTime = None
        }

    let userQueriesFinished stats delta =
        { stats with UsersThusFar = stats.UsersThusFar + delta }

    let setExpectedUserCount stats totalExpectedUsers =
        { stats with ExpectedUsers = totalExpectedUsers }

    let incrementFailures stats delta =
        { stats with QueryFailures = stats.QueryFailures + delta }

    let finish stats =
        { stats with EndTime = Some DateTime.UtcNow }

    // WorkerSettings section
    type WorkerSettings = {
        ReceivedInitialUsers: bool
        CurrentRepo: RepoKey
        Subscribers: System.Collections.Generic.HashSet<IActorRef>
        SimilarRepos: System.Collections.Generic.Dictionary<string, SimilarRepo>
        GithubProgressStats: GithubProgressStats
        PublishTimer: Cancelable
    }
    
[<AutoOpen>]
module Messages =
    
    type AuthenticationMessage =
        | Authenticate of oauthToken: string
        | AuthenticationFailed
        | AuthenticationCancelled
        | AuthenticationSuccess

    type GithubActorMessage =
        // Job-related
        | AbleToAcceptJob of repoKey: RepoKey
        | UnableToAcceptJob of repoKey: RepoKey
        | CanAcceptJob of repoKey: RepoKey
        | BeginJob of repoKey: RepoKey
        | JobFailed of repoKey: RepoKey
        // Repo-related
        | ValidateRepo of repoUri: string
        | ValidRepo of repo: Octokit.Repository
        | InvalidRepo of repoUri: string * reason: string
        | ProcessRepo of repoUri: string
        // Query-related
        | LaunchRepoResultsWindow of repoKey: RepoKey * coordinator: IActorRef
        | StarredReposForUser of login: string * repos: Octokit.Repository seq
        | PublishUpdate        
        | SubscribeToProgressUpdates of subscriber: IActorRef
        | RetryableQuery of query: RetryableQuery
        | QueryStarrers of repoKey: RepoKey
        | QueryStarrer of login: string
        | UsersToQuery of users: Octokit.User array
        | GithubProgressStats of stats: GithubProgressStats
        | SimilarRepos of repos: SimilarRepo seq