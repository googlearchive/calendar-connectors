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
using System.Configuration;
using System.Text;

using Google.GCalExchangeSync.Library;
using Google.GCalExchangeSync.Library.Scheduling;

namespace Google.GCalExchangeSync.Service
{
    public class GCalSyncProcessHost : BaseWorker
    {
        private GCalSyncProcess _gcalSyncProcess;

        private static bool _isConfigLoaded = false;

        public GCalSyncProcessHost()
        {
            CheckConfig();
            InitializeComponent();
        }

        private void CheckConfig()
        {
            if ( !_isConfigLoaded )
            {
                _isConfigLoaded = true;

                Configuration config = 
                    ConfigurationManager.OpenExeConfiguration( ConfigurationUserLevel.None );

                ConfigCache.LoadConfiguration( config );
            }
        }

        protected override void Execute()
        {
            _gcalSyncProcess.RunSyncProcess();
        }

        private void InitializeComponent()
        {
            _gcalSyncProcess = new GCalSyncProcess();

            this.OnWorkComplete += new OnWorkCompleteEventHandler(GCalSyncProcess_OnWorkComplete);
            this.OnWorkStart += new OnWorkStartEventHandler(GCalSyncProcess_OnWorkStart);
            this.OnWorkFailed += new OnWorkFailedEventHandler(GCalSyncProcess_OnWorkFailed);
        }

        private void GCalSyncProcess_OnWorkFailed(Exception ex)
        {
            if (_log.IsErrorEnabled)
                _log.Error("GCalSync process failed", ex);
        }

        private void GCalSyncProcess_OnWorkStart()
        {
            if (_log.IsInfoEnabled)
                _log.Info("Starting GCalSyncProcess");
        }

        private void GCalSyncProcess_OnWorkComplete()
        {
            if (_log.IsInfoEnabled)
                _log.Info("GCalSyncProcess finished succesfully");
        }
    }
}
