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
using Google.GCalExchangeSync.Library;

namespace Google.GCalExchangeSync.Library
{
    /// <summary>
    /// Factory for a FreeBusyWriter
    /// </summary>
    public class FreeBusyWriterFactory
    {
        /// <summary>
        /// Get a free busy writer for the set of users
        /// </summary>
        /// <param name="users">List of the users to write f/b info for</param>
        /// <returns>A FreeBusyWriter</returns>
        public static IFreeBusyWriter GetWriter(List<ExchangeUser> users)
        {
            IFreeBusyWriter writer = null;

            switch ( ConfigCache.FreeBusyWriter )
            {
                default:
                case "SchedulePlus":
                    writer = new SchedulePlusFreeBusyWriter();
                    break;
                case "Appointment":
                    writer = new AppointmentWriter();
                    break;
            }

            return writer;
        }
    }
}
