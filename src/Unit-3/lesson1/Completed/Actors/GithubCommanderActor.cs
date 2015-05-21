using System;
using Akka.Actor;
using Akka.Routing;

namespace GithubActors.Actors
{
    /// <summary>
    /// Top-level actor responsible for coordinating and launching repo-processing jobs
    /// </summary>
    public class GithubCommanderActor : ReceiveActor, IWithUnboundedStash
    {
        #region Message classes

        public class CanAcceptJob
        {
            public CanAcceptJob(RepoKey repo)
            {
                Repo = repo;
            }

            public RepoKey Repo { get; private set; }
        }

        public class AbleToAcceptJob
        {
            public AbleToAcceptJob(RepoKey repo)
            {
                Repo = repo;
            }

            public RepoKey Repo { get; private set; }
        }

        public class UnableToAcceptJob
        {
            public UnableToAcceptJob(RepoKey repo)
            {
                Repo = repo;
            }

            public RepoKey Repo { get; private set; }
        }

        #endregion

        private IActorRef _coordinator;
        private IActorRef _canAcceptJobSender;
        private int pendingJobReplies;

        public GithubCommanderActor()
        {
           Ready();
        }

        private void Ready()
        {
            Receive<CanAcceptJob>(request =>
            {
                // Pass this job request to a coordinator...
                _coordinator.Tell(request);

                // ...and wait to see if someone accepts the job.
                BecomeWaitingForJobResponse();
            });
        }

        private void BecomeWaitingForJobResponse()
        {
            _canAcceptJobSender = Sender;
            pendingJobReplies = 3; //the number of routees
            Become(WaitingForJobResponse);
        }

        private void WaitingForJobResponse()
        {
            // Anyone else asks for work to be done while we're
            // assigning this one, stash it for later.
            Receive<CanAcceptJob>(request => Stash.Stash());

            Receive<UnableToAcceptJob>(response =>
            {
                pendingJobReplies--;
                if (pendingJobReplies == 0)
                {
                    // All three workers said, "No," so report
                    // this back to the Actor that originally
                    // originally made the request.
                    _canAcceptJobSender.Tell(response);
                    BecomeReady();
                }
            });

            Receive<AbleToAcceptJob>(response =>
            {
                // Someone took our job! Tell the Actor that
                // originally requested it.
                _canAcceptJobSender.Tell(response);

                // Tell the Actor that said they could do the
                // job to actually start doing it.
                Sender.Tell(new GithubCoordinatorActor.BeginJob(response.Repo));

                // Launch the new window to view results of the processing
                Context.ActorSelection(ActorPaths.MainFormActor.Path).Tell(new MainFormActor.LaunchRepoResultsWindow(response.Repo, Sender));

                BecomeReady();
            });
        }

        private void BecomeReady()
        {
            Become(Ready);
            Stash.UnstashAll();
        }

        protected override void PreStart()
        {
            //create three GithubCoordinatorActor instances
            var c1 = Context.ActorOf(Props.Create(() => new GithubCoordinatorActor()), ActorPaths.GithubCoordinatorActor.Name + "1");
            var c2 = Context.ActorOf(Props.Create(() => new GithubCoordinatorActor()), ActorPaths.GithubCoordinatorActor.Name + "2");
            var c3 = Context.ActorOf(Props.Create(() => new GithubCoordinatorActor()), ActorPaths.GithubCoordinatorActor.Name + "3");

            //create a broadcast router who will ask all if them if they're available for work
            _coordinator =
                Context.ActorOf(Props.Empty.WithRouter(new BroadcastGroup(ActorPaths.GithubCoordinatorActor.Path + "1",
                    ActorPaths.GithubCoordinatorActor.Path + "2", ActorPaths.GithubCoordinatorActor.Path + "3")));
            base.PreStart();
        }

        protected override void PreRestart(Exception reason, object message)
        {
            //kill off the old coordinator so we can recreate it from scratch
            _coordinator.Tell(PoisonPill.Instance);
            base.PreRestart(reason, message);
        }

        public IStash Stash { get; set; }
    }
}
