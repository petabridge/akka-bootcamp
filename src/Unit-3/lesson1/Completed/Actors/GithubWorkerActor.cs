using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Octokit;

namespace GithubActors.Actors
{
    /// <summary>
    /// Individual actor responsible for querying the Github API
    /// </summary>
    public class GithubWorkerActor : ReceiveActor
    {
        #region Message classes

        public class QueryStarrers
        {
            public QueryStarrers(RepoKey key)
            {
                Key = key;
            }

            public RepoKey Key { get; private set; }
        }

        /// <summary>
        /// Query an individual starrer
        /// </summary>
        public class QueryStarrer
        {
            public QueryStarrer(string login)
            {
                Login = login;
            }

            public string Login { get; private set; }
        }

        public class StarredReposForUser
        {
            public StarredReposForUser(string login, IEnumerable<Repository> repos)
            {
                Repos = repos;
                Login = login;
            }

            public string Login { get; private set; }

            public IEnumerable<Repository> Repos { get; private set; }
        }

        #endregion

        private IGitHubClient _gitHubClient;
        private readonly Func<IGitHubClient> _gitHubClientFactory;

        public GithubWorkerActor(Func<IGitHubClient> gitHubClientFactory)
        {
            _gitHubClientFactory = gitHubClientFactory;
            InitialReceives();
        }

        protected override void PreStart()
        {
            _gitHubClient = _gitHubClientFactory();
        }

        private void InitialReceives()
        {
            //query an individual starrer
            Receive<RetryableQuery>(query => query.Query is QueryStarrer, query =>
            {
                // ReSharper disable once PossibleNullReferenceException (we know from the previous IS statement that this is not null)
                var starrer = (query.Query as QueryStarrer).Login;
                try
                {
                    var getStarrer = _gitHubClient.Activity.Starring.GetAllForUser(starrer);

                    //ewww
                    getStarrer.Wait();
                    var starredRepos = getStarrer.Result;
                    Sender.Tell(new StarredReposForUser(starrer, starredRepos));
                }
                catch (Exception ex)
                {
                    //operation failed - let the parent know
                    Sender.Tell(query.NextTry());
                }
            });

            //query all starrers for a repository
            Receive<RetryableQuery>(query => query.Query is QueryStarrers, query =>
            {
                // ReSharper disable once PossibleNullReferenceException (we know from the previous IS statement that this is not null)
                var starrers = (query.Query as QueryStarrers).Key;
                try
                {
                    var getStars = _gitHubClient.Activity.Starring.GetAllStargazers(starrers.Owner, starrers.Repo);

                    //ewww
                    getStars.Wait();
                    var stars = getStars.Result;
                    Sender.Tell(stars.ToArray());
                }
                catch (Exception ex)
                {
                    //operation failed - let the parent know
                    Sender.Tell(query.NextTry());
                }
            });
        }
    }
}
