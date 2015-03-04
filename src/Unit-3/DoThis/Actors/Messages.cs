namespace GithubActors.Actors
{
    /// <summary>
    /// Begin processing a new Github repository for analysis
    /// </summary>
    public class ProcessRepo
    {
        public ProcessRepo(string repoUri)
        {
            RepoUri = repoUri;
        }

        public string RepoUri { get; private set; }
    }
}
