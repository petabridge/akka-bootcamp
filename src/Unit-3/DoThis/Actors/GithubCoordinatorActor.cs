using System;
using System.Collections.Generic;
using System.Threading;
using Akka.Actor;
using Octokit;

namespace GithubActors.Actors
{
    /// <summary>
    /// Actor responsible for publishing data about the results
    /// of a github operation
    /// </summary>
    public class GithubCoordinatorActor : ReceiveActor
    {
        #region Message classes

       

        public class BeginJob
        {
            public BeginJob(RepoKey repo)
            {
                Repo = repo;
            }

            public RepoKey Repo { get; private set; }
        }

        public class SubscribeToProgressUpdates
        {
            public SubscribeToProgressUpdates(ActorRef subscriber)
            {
                Subscriber = subscriber;
            }

            public ActorRef Subscriber { get; private set; }
        }

        public class PublishUpdate
        {
            private PublishUpdate() { }
            private static readonly PublishUpdate _instance = new PublishUpdate();

            public static PublishUpdate Instance
            {
                get { return _instance; }
            }
        }

        #endregion

        private ActorRef _githubWorker;

        private RepoKey _currentRepo;
        private HashSet<ActorRef> _subscribers;
        private List<User> _starredUsers;
        private CancellationTokenSource _publishTimer;
        private GithubProgressStats _githubProgressStats;

        public GithubCoordinatorActor()
        {
            Waiting();
        }

        protected override void PreStart()
        {
            _githubWorker = Context.ActorOf(Props.Create(() => new GithubWorkerActor(GithubClientFactory.GetClient)));
        }

        private void Waiting()
        {
            Receive<GithubCommanderActor.CanAcceptJob>(job => Sender.Tell(new GithubCommanderActor.AbleToAcceptJob(job.Repo)));
            Receive<BeginJob>(job =>
            {
                BecomeWorking(job.Repo);

                //kick off the job to query the repo's list of starrers
                _githubWorker.Tell(new RetryableQuery(new GithubWorkerActor.QueryStarrers(job.Repo), 4));
            });
        }

        private void BecomeWorking(RepoKey repo)
        {
            _currentRepo = repo;
            _subscribers = new HashSet<ActorRef>();
            _starredUsers = new List<User>();
            _publishTimer = new CancellationTokenSource();
            _githubProgressStats = new GithubProgressStats();
            Become(Working);
        }

        private void BecomeWaiting()
        {
            //stop publishing
            _publishTimer.Cancel();
            Become(Waiting);
        }

        private void Working()
        {
            //received a downloaded user back from the github worker
            Receive<User>(user =>
            {
                _starredUsers.Add(user);
                _githubProgressStats = _githubProgressStats.UserQueriesFinished();
            });

            Receive<PublishUpdate>(update =>
            {
                //check to see if the job is done
                if (_githubProgressStats.IsFinished)
                {
                    _githubProgressStats = _githubProgressStats.Finish();
                    BecomeWaiting();
                }

                foreach (var subscriber in _subscribers)
                {
                    subscriber.Tell(_githubProgressStats);
                    subscriber.Tell(_starredUsers.ToArray());
                }

                //drain the list each time we publish so the subscribers only get updates
                _starredUsers = new List<User>(); 
            });

            //completed our initial job - we now know how many users we need to query
            Receive<User[]>(users =>
            {
                _githubProgressStats = _githubProgressStats.SetExpectedUserCount(users.Length);

                //queue up all of the jobs
                foreach (var user in users)
                    _githubWorker.Tell(new RetryableQuery(new GithubWorkerActor.QueryStarrer(user.Login), 3));
            });

            Receive<GithubCommanderActor.CanAcceptJob>(job => Sender.Tell(new GithubCommanderActor.UnableToAcceptJob(job.Repo)));

            Receive<SubscribeToProgressUpdates>(updates =>
            {
                //this is our first subscriber, which means we need to turn publishing on
                if (_subscribers.Count == 0)
                { 
                    Context.System.Scheduler.Schedule(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100),
                        Self, PublishUpdate.Instance, _publishTimer.Token);
                }

                _subscribers.Add(updates.Subscriber);
            });

            //query failed, but can be retried
            Receive<RetryableQuery>(query => query.CanRetry, query => _githubWorker.Tell(query));

            //query failed, can't be retried, and it's a QueryStarrers operation - means the entire job failed
            Receive<RetryableQuery>(query => !query.CanRetry && query.Query is GithubWorkerActor.QueryStarrers, query =>
            {
                //TODO: REPORT ERROR TO SUBSCRIBERS
                BecomeWaiting();
            });

            //query failed, can't be retried, and it's a QueryStarrers operation - means individual operation failed
            Receive<RetryableQuery>(query => !query.CanRetry && query.Query is GithubWorkerActor.QueryStarrer, query => _githubProgressStats.IncrementFailures());
        }
    }
}
