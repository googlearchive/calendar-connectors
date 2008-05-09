using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Google.GCalExchangeSync.Library.Scheduling
{
    public abstract class IWorker
    {
        string _threadID;

        public delegate void OnWorkCompleteEventHandler();
        public delegate void OnWorkStartEventHandler();
        public delegate void OnWorkFailedEventHandler(Exception ex);

        public event OnWorkCompleteEventHandler OnWorkComplete;
        public event OnWorkStartEventHandler OnWorkStart;
        public event OnWorkFailedEventHandler OnWorkFailed;

        public IWorker()
        {
            
        }


        void IWorker_OnWorkFailed(Exception ex)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        private void IWorker_OnWorkComplete()
        {
            
        }

        private void IWorker_OnWorkStart()
        {
            
        }

        private void StartWork()
        {

            _threadID = String.Format("{0}", Thread.CurrentThread.GetHashCode());
            Sentinel.AddActiveThread(_threadID);

        }

        public void DoWork()
        {

            try
            {
                OnWorkStart();
                this.StartWork();
                this.Execute();
                OnWorkComplete();
            }
            catch (Exception ex)
            {
                OnWorkFailed(ex);
            }
            finally
            {
                this.FinishWork();
            }

        }

        protected abstract void Execute();

        private void FinishWork()
        {
            Sentinel.RemoveActiveThread(_threadID);
        }
    }
}
