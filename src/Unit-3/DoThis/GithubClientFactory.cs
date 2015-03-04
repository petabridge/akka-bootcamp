using Octokit;

namespace GithubActors
{
    /// <summary>
    /// Creates <see cref="GitHubClient"/> instances.
    /// </summary>
    public static class GithubClientFactory
    {
        public static GitHubClient GetClient()
        {
            return new GitHubClient(new ProductHeaderValue("AkkaBootcamp-Unit3"));
        }
    }
}
