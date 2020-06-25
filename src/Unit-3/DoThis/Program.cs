using System;
using System.Configuration;
using System.Windows.Forms;
using Akka.Actor;
using Akka.Configuration;

namespace GithubActors
{
    static class Program
    {
        /// <summary>
        /// ActorSystem we'llbe using to collect and process data
        /// from Github using their official .NET SDK, Octokit
        /// </summary>
        public static ActorSystem GithubActors;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var config = ConfigurationFactory.ParseString(@"
                akka {
                    actor{
                        deployment{
                            #used to configure our MainFormActor
                            /mainform{
                                dispatcher = akka.actor.synchronized-dispatcher #causes MainFormActor to run on the UI thread for WinForms
                            }
                            /authenticator{
                                dispatcher = akka.actor.synchronized-dispatcher
                            }
                        }
                    }
                }");
            GithubActors = ActorSystem.Create("GithubActors", config);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GithubAuth());
        }
    }
}
