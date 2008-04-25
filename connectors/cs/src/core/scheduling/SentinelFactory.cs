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
using log4net;

namespace Google.GCalExchangeSync.Library.Scheduling
{
    /// <summary>
    /// Factory for creating a sentinel
    /// </summary>
    public class SentinelFactory
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SentinelFactory));

        private int _frequency;
        private Type _workerType;

        /// <summary>
        /// Create a SentinelFactory
        /// </summary>
        /// <param name="frequency">Interval in seconds to execute task</param>
        /// <param name="workerType">Type of worker to execute</param>
        public SentinelFactory(int frequency, Type workerType)
        {
            _frequency = frequency;
            _workerType = workerType;
        }

        /// <summary>
        /// Start the sentinel
        /// </summary>
        public void StartSentinel()
        {
            Sentinel s = new Sentinel(_frequency, _workerType);
            s.Start();
        }
    }
}
