using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Akka.Actor;
using Akka.Util.Internal;
using ChartApp.Actors;

namespace ChartApp
{
    public partial class Main : Form
    {
        private ActorRef chartActor;
        private AtomicCounter counter = new AtomicCounter(1);

        public Main()
        {
            InitializeComponent();
        }

        #region Initialization


        private void Main_Load(object sender, EventArgs e)
        {
            chartActor = Program.ChartActors.ActorOf(Props.Create(() => new ChartingActor(sysChart)), "charting");
            var series = ChartDataHelper.RandomSeries("FakeSeries" + counter.GetAndIncrement());
            chartActor.Tell(new ChartingActor.InitializeChart(new Dictionary<string, Series>()
            {
                {series.Name, series}
            }));
        }

        #endregion

        #region Button handlers

        private void button1_Click(object sender, EventArgs e)
        {
            var series = ChartDataHelper.RandomSeries("FakeSeries" + counter.GetAndIncrement());
            chartActor.Tell(new ChartingActor.AddSeries(series));
        }

        #endregion

    }
}
