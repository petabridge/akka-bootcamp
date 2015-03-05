using System.Windows.Forms;
using Akka.Actor;
using Octokit;

namespace GithubActors.Actors
{
    /// <summary>
    /// Actor responsible for printing the results and progress from a <see cref="GithubCoordinatorActor"/>
    /// onto a <see cref="RepoResultsForm"/> (runs on the UI thread)
    /// </summary>
    public class RepoResultsActor : ReceiveActor
    {
        private DataGridView _userDg;
        private ToolStripStatusLabel _statusLabel;
        private ToolStripProgressBar _progressBar;

        private bool _hasSetProgress = false;

        public RepoResultsActor(DataGridView userDg, ToolStripStatusLabel statusLabel, ToolStripProgressBar progressBar)
        {
            _userDg = userDg;
            _statusLabel = statusLabel;
            _progressBar = progressBar;
            InitialReceives();
        }

        private void InitialReceives()
        {
            //progress update
            Receive<GithubProgressStats>(stats =>
            {
                //time to start animating the progress bar
                if (!_hasSetProgress && stats.ExpectedUsers > 0)
                {
                    _progressBar.Minimum = 0;
                    _progressBar.Step = 1;
                    _progressBar.Maximum = stats.ExpectedUsers;
                    _progressBar.Value = stats.UsersThusFar;
                    _progressBar.Visible = true;
                }

                _statusLabel.Text = string.Format("{0} out of {1} users ({2} failures) [{3} elapsed]",
                    stats.UsersThusFar, stats.ExpectedUsers, stats.QueryFailures, stats.Elapsed);
                _progressBar.Value = stats.UsersThusFar + stats.QueryFailures;
            });

            //user update
            Receive<User[]>(users =>
            {
                foreach (var user in users)
                {
                    var row = new DataGridViewRow();
                    row.CreateCells(_userDg);
                    row.Cells[0].Value = user.Login;
                    row.Cells[1].Value = user.HtmlUrl;
                    row.Cells[2].Value = user.PublicRepos;
                    row.Cells[3].Value = user.Followers;
                    row.Cells[4].Value = user.Following;
                    _userDg.Rows.Add(row);
                }
            });
        }
    }
}
