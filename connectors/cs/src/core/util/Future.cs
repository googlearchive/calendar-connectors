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
using System.Collections;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Threading;

namespace Google.GCalExchangeSync.Library.Util
{
    /// <summary>
    /// Support for Future / Promise pattern
    /// </summary>
    public abstract class Future : IDisposable
    {
        private Thread taskThread;

        /// <summary>
        /// Initiate the task to be completed and return immediately
        /// </summary>
        public void start()
        {
            taskThread = new Thread(doTask);
            taskThread.Name = this.TaskName;
            taskThread.Start();
        }

        /// <summary>
        /// Override this method with the operation to be completed
        /// </summary>
        protected abstract void doTask();

        /// <summary>
        /// Override this method to provide a unique ID for the task
        /// </summary>
        protected abstract string TaskName
        {
            get;
        }

        /// <summary>
        /// Block for the result to be available - this function should be called from a method in
        /// the subclass that returns results
        /// </summary>
        public void waitForCompletion()
        {
            taskThread.Join();
        }

        /// <summary>
        /// Dispose of the future
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    };
}
