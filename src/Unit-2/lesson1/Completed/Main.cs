using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Akka.Actor;
using ChartApp.Actors;

namespace ChartApp
{
    public partial class Main : Form
    {
        private ActorRef chartActor;

        public Main()
        {
            InitializeComponent();
        }

        #region Initialization


        private void Main_Load(object sender, EventArgs e)
        {
            chartActor = Program.ChartActors.ActorOf(Props.Create(() => new ChartingActor(sysChart)), "charting");
            var series = ChartDataHelper.RandomSeries("FakeSeries");
            chartActor.Tell(new ChartingActor.InitializeChart(new Dictionary<string, Series>()
            {
                {series.Name, series}
            }));
        }

        #endregion

    }
}
