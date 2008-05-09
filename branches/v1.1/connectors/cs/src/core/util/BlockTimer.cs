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
using System.IO;
using System.Text;
using System.Threading;

namespace Google.GCalExchangeSync.Library.Util
{
    /// <summary>
    /// Print the time in milliseconds that the block took to execute.
    /// Should be used in a using block: e.g
    /// 
    /// using(BlockTimer bt = new BlockTimes("label")
    /// {
    ///   // Events to time
    /// }
    /// </summary>
    public class BlockTimer : IDisposable
    {
        /// <summary>
        /// Logger for BlockTimer
        /// </summary>
        protected static readonly log4net.ILog log =
           log4net.LogManager.GetLogger(typeof(BlockTimer));

        private string name;
        private long   start;

        /// <summary>
        ///  Create a blocktimer
        /// </summary>
        /// <param name="name">Name for the timer</param>
        public BlockTimer( string name )
        {
            this.name = name;
            this.start = DateTime.Now.Ticks;
        }

        /// <summary>
        /// Called when the block timer is disposed - i.e. at the
        /// end of the using block
        /// </summary>
        public void Dispose()
        {
            long totalTime = DateTime.Now.Ticks - start;
            TimeSpan ts = new TimeSpan( totalTime );
            
            int timeSpanMillseconds = Convert.ToInt32( ts.TotalMilliseconds );
            
            string info = string.Format(
                "[Timer] - {0} - Total Execution Time: {1} ms.",
                name,
                timeSpanMillseconds );

            log.Info( info );
        }
    }
}
