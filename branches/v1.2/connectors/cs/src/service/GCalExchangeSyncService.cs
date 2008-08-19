using System;
using System.ServiceProcess;
using System.Threading;
using Google.GCalExchangeSync.Library;
using Google.GCalExchangeSync.Library.Scheduling;
using log4net;

namespace Google.GCalExchangeSync.Service
{
    public class GCalExchangeSyncService : ServiceBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(GCalExchangeSyncService));

        private Thread thread;

        public GCalExchangeSyncService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            SentinelFactory sf;

            /* Configuration is stored as minutes, multiple my 60 seconds and multiply
             * by 1000 to get milliseconds */
            int refreshTime = ConfigCache.ServiceRefreshMinutes * 60 * 1000;

            if (_log.IsDebugEnabled)
            {
                _log.Debug(String.Format("Service refresh time set to {0} ms.", refreshTime));
            }

            //create a sentinel with a 10 second watch frequency
            sf = new SentinelFactory(refreshTime, typeof(GCalSyncProcessHost));

            //Do not use Multithreaded implementation
            Sentinel.MultiThreaded = false;

            if (_log.IsDebugEnabled)
            {
                _log.Debug(String.Format("Service service set to multithreaded: {0}", Sentinel.MultiThreaded));
            }

            thread = new Thread(new ThreadStart( sf.StartSentinel ));

            thread.Name = "Sentinel Thread";

            if (_log.IsDebugEnabled)
            {
                _log.Debug("Starting sentinel process");
            }

            thread.Start();
        }

        protected override void OnStop()
        {
        }

        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
          if (disposing && (components != null))
          {
            components.Dispose();
          }
          base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
          components = new System.ComponentModel.Container();
          this.ServiceName = "GCalExchangeSync";
        }
    }
}
