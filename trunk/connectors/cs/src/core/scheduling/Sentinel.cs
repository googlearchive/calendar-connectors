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
using System.Threading;
using System.Diagnostics;
using System.Timers;
using System.Collections;

using log4net;

namespace Google.GCalExchangeSync.Library.Scheduling
{
   	/// <summary>
	/// The Sentinel will watch over a given target, with a given frequency.  On finding a target, it will
	/// instantiate a worker process.
	/// </summary>
	public class Sentinel
	{
        #region members
        private static readonly ILog _log = LogManager.GetLogger( typeof(Sentinel) );
   
        private System.Timers.Timer _timer;

        /// <summary>
        /// Array of active worker threads maintained by the Sentinel
        /// </summary>
        public static ArrayList ActiveThreads = new ArrayList();

        /// <summary>
        /// Semaphore used to enable / disable Sentinel target searching
        /// </summary>
        public static bool Enabled = true;

        /// <summary>
        /// Semaphore used to enable / disable multithreaded worker spawning
        /// </summary>
        public static bool MultiThreaded = true;

        private Type _workerType; //the specific type of IWorker to instantianate in SpawnWorker
        #endregion

        #region thread control
        /// <summary>
        /// Add the thread as an active thread
        /// </summary>
        /// <param name="threadIdentifier">thread to add</param>
        public static void AddActiveThread( string threadIdentifier ) 
        {
            lock ( Sentinel.ActiveThreads ) 
            {
                ActiveThreads.Add( threadIdentifier );  
                if(_log.IsDebugEnabled) 
                { 
                    _log.Debug( String.Format(
                        "Start Thread {0} on Line {1}", 
                        threadIdentifier, ActiveThreads.Count)); 
                }
            }
        }

        /// <summary>
        /// Remove a currently active thread
        /// </summary>
        /// <param name="threadIdentifier">Thread to remove</param>
        public static void RemoveActiveThread( string threadIdentifier )
        {
            lock ( Sentinel.ActiveThreads ) 
            {
                ActiveThreads.Remove( threadIdentifier );     
                _log.Debug( String.Format(
                    "Finish Thread {0} on Line {1}", 
                    threadIdentifier, ActiveThreads.Count));
            }
        } 
        #endregion

        #region constructors
        /// <summary>
        /// Create a new sentinel
        /// </summary>
        /// <param name="frequency">nterval in seconds between running the task</param>
        /// <param name="workerType">Type of worker to use</param>
        public Sentinel(int frequency, Type workerType)
        {

            _timer = new System.Timers.Timer(frequency);
            _timer.Elapsed += new ElapsedEventHandler(Timer_Elapsed);
            _workerType = workerType;
        }
        #endregion

        #region event related
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //check if the sentinel is still enabled
            if ( Sentinel.Enabled ) 
            {
                if (_log.IsDebugEnabled)
                    _log.Debug("Sentinel is Enabled");

                _timer.Stop();

                if (_log.IsDebugEnabled)
                    _log.Debug("Timer stopped, spawning process");

                //log.Info("Timer elapsed, looking for targets." + System.DateTime.Now.ToLongTimeString() );
                SpawnWorker();

                if (_log.IsDebugEnabled)
                    _log.Debug("Work complete, starting timer");

                _timer.Start();

                if (_log.IsDebugEnabled)
                    _log.Debug("Timer started");
            }
            else
                _timer.Stop(); //stop the timer, spawn no more threads
        }

        /// <summary>
        /// Handle spawning a new worker
        /// </summary>
        /// <param name="sender">event sender</param>
        public delegate void ThreadPoolSpawnWorkerHandler( object sender );
        #endregion

        private void SpawnWorker() 
        {
            try
            {
                if (Sentinel.MultiThreaded)
                    ThreadPool.QueueUserWorkItem(new WaitCallback(this.SpawnProcess)); //anonymous method would be nice here
                else
                    this.SpawnProcess(new object());
            }
            catch (Exception ex)
            {
                _log.Error(String.Format("Error occured while spawning worker process of type {0}",_workerType.Name), ex);
            }
        }

        private void SpawnProcess(object sender)
        {
            if (_log.IsInfoEnabled)
            {
                _log.Info(String.Format("Spawning process of type {0}", _workerType.Name));
            }

            BaseWorker worker = System.Activator.CreateInstance(_workerType) as BaseWorker;

            worker.DoWork();
        }


        /// <summary>
        /// Start a sentinel
        /// </summary>
        public void Start()
        {
            _timer.Enabled = true;
            Sentinel.Enabled = true;

            if (_log.IsInfoEnabled) { _log.Info("Sentinel started"); }

            Timer_Elapsed( this, null );
        }

        /// <summary>
        /// Stop a currently executing sentinel
        /// </summary>
        public void Stop()
        {
            _timer.Enabled = false;
            Sentinel.Enabled = false;

            if (_log.IsInfoEnabled) { _log.Info("Sentinel stopped"); }
        }
    }
}
