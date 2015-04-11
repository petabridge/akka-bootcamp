using System.Drawing;
using System.Windows.Forms;
using Akka.Actor;

namespace GithubActors.Actors
{
    /// <summary>
    /// Actor that runs on the UI thread and handles
    /// UI events for <see cref="LauncherForm"/>
    /// </summary>
    public class MainFormActor : ReceiveActor, IWithUnboundedStash
    {
        #region Messages

        public class LaunchRepoResultsWindow
        {
            public LaunchRepoResultsWindow(RepoKey repo, IActorRef coordinator)
            {
                Repo = repo;
                Coordinator = coordinator;
            }

            public RepoKey Repo { get; private set; }

            public IActorRef Coordinator { get; private set; }
        }

        #endregion

        private readonly Label _validationLabel;

        public MainFormActor(Label validationLabel)
        {
            _validationLabel = validationLabel;
            Ready();
        }

        /// <summary>
        /// State for when we're able to accept new jobs
        /// </summary>
        private void Ready()
        {
            Receive<ProcessRepo>(repo =>
            {
                Context.ActorSelection(ActorPaths.GithubValidatorActor.Path).Tell(new GithubValidatorActor.ValidateRepo(repo.RepoUri));
                BecomeBusy(repo.RepoUri);
            });

            //launch the window
            Receive<LaunchRepoResultsWindow>(window =>
            {
                var form = new RepoResultsForm(window.Coordinator, window.Repo);
                form.Show();
            });
        }

        /// <summary>
        /// Make any necessary URI updates, then switch our state to busy
        /// </summary>
        private void BecomeBusy(string repoUrl)
        {
            _validationLabel.Visible = true;
            _validationLabel.Text = string.Format("Validating {0}...", repoUrl);
            _validationLabel.ForeColor = Color.Gold;
            Become(Busy);
        }

        /// <summary>
        /// State for when we're currently processing a job
        /// </summary>
        private void Busy()
        {
            Receive<GithubValidatorActor.RepoIsValid>(valid => BecomeReady("Valid!"));
            Receive<GithubValidatorActor.InvalidRepo>(invalid => BecomeReady(invalid.Reason, false));
            //yes
            Receive<GithubCommanderActor.UnableToAcceptJob>(job => BecomeReady(string.Format("{0}/{1} is a valid repo, but system can't accept additional jobs", job.Repo.Owner, job.Repo.Repo), false));

            //no
            Receive<GithubCommanderActor.AbleToAcceptJob>(job => BecomeReady(string.Format("{0}/{1} is a valid repo - starting job!", job.Repo.Owner, job.Repo.Repo)));
            Receive<LaunchRepoResultsWindow>(window => Stash.Stash());
        }

        private void BecomeReady(string message, bool isValid = true)
        {
            _validationLabel.Text = message;
            _validationLabel.ForeColor = isValid ? Color.Green : Color.Red;
            Stash.UnstashAll();
            Become(Ready);
        }

        public IStash Stash { get; set; }
    }
}
