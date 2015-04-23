using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Akka.Actor;
using Akka.Routing;
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
            public SubscribeToProgressUpdates(IActorRef subscriber)
            {
                Subscriber = subscriber;
            }

            public IActorRef Subscriber { get; private set; }
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

        /// <summary>
        /// Let the subscribers know we failed
        /// </summary>
        public class JobFailed
        {
            public JobFailed(RepoKey repo)
            {
                Repo = repo;
            }

            public RepoKey Repo { get; private set; }
        }

        #endregion

        private IActorRef _githubWorker;

        private RepoKey _currentRepo;
        private Dictionary<string, SimilarRepo> _similarRepos;
        private HashSet<IActorRef> _subscribers;
        private ICancelable _publishTimer;
        private GithubProgressStats _githubProgressStats;

        private bool _receivedInitialUsers = false;

        public GithubCoordinatorActor()
        {
            Waiting();
        }

        protected override void PreStart()
        {
            _githubWorker = Context.ActorOf(Props.Create(() => new GithubWorkerActor(GithubClientFactory.GetClient))
                .WithRouter(new RoundRobinPool(10)));
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
            _receivedInitialUsers = false;
            _currentRepo = repo;
            _subscribers = new HashSet<IActorRef>();
            _similarRepos = new Dictionary<string, SimilarRepo>();
            _publishTimer = new Cancelable(Context.System.Scheduler);
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
            Receive<GithubWorkerActor.StarredReposForUser>(user =>
            {
                _githubProgressStats = _githubProgressStats.UserQueriesFinished();
                foreach (var repo in user.Repos)
                {
                    if (!_similarRepos.ContainsKey(repo.HtmlUrl))
                    {
                        _similarRepos[repo.HtmlUrl] = new SimilarRepo(repo);
                    }

                    //increment the number of people who starred this repo
                    _similarRepos[repo.HtmlUrl].SharedStarrers++;
                }
            });

            Receive<PublishUpdate>(update =>
            {
                //check to see if the job is done
                if (_receivedInitialUsers && _githubProgressStats.IsFinished)
                {
                    _githubProgressStats = _githubProgressStats.Finish();
                    
                    //all repos minus forks of the current one
                    var sortedSimilarRepos = _similarRepos.Values
                        .Where(x => x.Repo.Name != _currentRepo.Repo).OrderByDescending(x => x.SharedStarrers).ToList();
                    foreach (var subscriber in _subscribers)
                    {
                        subscriber.Tell(sortedSimilarRepos);
                    }
                    BecomeWaiting();
                }

                foreach (var subscriber in _subscribers)
                {
                    subscriber.Tell(_githubProgressStats);
                }
            });

            //completed our initial job - we now know how many users we need to query
            Receive<User[]>(users =>
            {
                _receivedInitialUsers = true;
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
                    Context.System.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100),
                        Self, PublishUpdate.Instance, Self, _publishTimer);
                }

                _subscribers.Add(updates.Subscriber);
            });

            //query failed, but can be retried
            Receive<RetryableQuery>(query => query.CanRetry, query => _githubWorker.Tell(query));

            //query failed, can't be retried, and it's a QueryStarrers operation - means the entire job failed
            Receive<RetryableQuery>(query => !query.CanRetry && query.Query is GithubWorkerActor.QueryStarrers, query =>
            {
                _receivedInitialUsers = true;
                foreach (var subscriber in _subscribers)
                {
                    subscriber.Tell(new JobFailed(_currentRepo));
                }
                BecomeWaiting();
            });

            //query failed, can't be retried, and it's a QueryStarrers operation - means individual operation failed
            Receive<RetryableQuery>(query => !query.CanRetry && query.Query is GithubWorkerActor.QueryStarrer, query => _githubProgressStats.IncrementFailures());
        }
    }
}
