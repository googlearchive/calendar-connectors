/* Copyright (c) 2008 Google Inc. All Rights Reserved
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Google.GCalExchangeSync.Library.Scheduling
{
    /// <summary>
    /// Worker task
    /// </summary>
    public abstract class BaseWorker
    {
        /// <summary>
        /// Logger for BaseWorker
        /// </summary>
        protected static readonly log4net.ILog _log =
          log4net.LogManager.GetLogger(typeof(BaseWorker));

        string _threadID;

        /// <summary>
        /// Handler for when the worker completes the task
        /// </summary>
        public delegate void OnWorkCompleteEventHandler();

        /// <summary>
        /// Handler for when the worker starts the tasks
        /// </summary>
        public delegate void OnWorkStartEventHandler();

        /// <summary>
        /// Handler for when the worker task fails
        /// </summary>
        /// <param name="ex">Exception that caused task failure</param>
        public delegate void OnWorkFailedEventHandler(Exception ex);

        /// <summary>The worker on complete handler</summary>
        public event OnWorkCompleteEventHandler OnWorkComplete;

        /// <summary>The worker on start handler</summary>
        public event OnWorkStartEventHandler OnWorkStart;

        /// <summary>The worker on fail handler</summary>
        public event OnWorkFailedEventHandler OnWorkFailed;

        private void StartWork()
        {
            _threadID = String.Format("{0}", Thread.CurrentThread.GetHashCode());
            Sentinel.AddActiveThread(_threadID);
        }

        /// <summary>
        /// Perform the worker task
        /// </summary>
        public void DoWork()
        {
            try
            {
                this.OnWorkStart();
                this.StartWork();
                this.Execute();
                this.OnWorkComplete();
            }
            catch (Exception ex)
            {
                this.OnWorkFailed(ex);
            }
            finally
            {
                this.FinishWork();
            }
        }

        /// <summary>
        /// Execute the task
        /// </summary>
        protected abstract void Execute();

        private void FinishWork()
        {
            Sentinel.RemoveActiveThread(_threadID);
        }
    }
}
