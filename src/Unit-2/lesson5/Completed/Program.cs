using System;
using System.Configuration;
using System.Windows.Forms;
using Akka.Actor;
using Akka.Configuration;

namespace ChartApp
{
    static class Program
    {
        /// <summary>
        /// ActorSystem we'll be using to publish data to charts
        /// and subscribe from performance counters
        /// </summary>
        public static ActorSystem ChartActors;

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
                            #used to configure our ChartingActor
                            /charting{
                                dispatcher = akka.actor.synchronized-dispatcher #causes ChartingActor to run on the UI thread for WinForms
                            }
                        }
                    }
                }");
            ChartActors = ActorSystem.Create("ChartActors", config);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Main());
        }
    }
}
