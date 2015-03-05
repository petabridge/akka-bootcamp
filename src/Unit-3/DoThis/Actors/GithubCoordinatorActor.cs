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
        private RepoKey currentRepo;
        private HashSet<ActorRef> _subscribers;
        private Stack<User> _starredUsers;
        private CancellationTokenSource _publishTimer;
        private readonly GithubProgressStats _githubProgressStats = new GithubProgressStats();
    }
}
