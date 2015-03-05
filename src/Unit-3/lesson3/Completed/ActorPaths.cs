using Akka.Actor;

namespace GithubActors
{
    /// <summary>
    /// Static helper class used to define paths to fixed-name actors
    /// (helps eliminate errors when using <see cref="ActorSelection"/>)
    /// </summary>
    public static class ActorPaths
    {
        public static readonly ActorMetaData GithubAuthenticatorActor = new ActorMetaData("authenticator", "/user/authenticator");
        public static readonly ActorMetaData MainFormActor = new ActorMetaData("mainform", "/user/mainform");
        public static readonly ActorMetaData GithubValidatorActor = new ActorMetaData("validator", "/user/validator");
        public static readonly ActorMetaData GithubCommanderActor = new ActorMetaData("commander", "/user/commander");
        public static readonly ActorMetaData GithubCoordinatorActor = new ActorMetaData("coordinator", "/user/commander/coordinator");
    }

    /// <summary>
    /// Meta-data class
    /// </summary>
    public class ActorMetaData
    {
        public ActorMetaData(string name, string path)
        {
            Name = name;
            Path = path;
        }

        public string Name { get; private set; }

        public string Path { get; private set; }
    }
}
