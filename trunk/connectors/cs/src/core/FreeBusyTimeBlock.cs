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
using System.Text;

using Google.GCalExchangeSync.Library.WebDav;
using Google.GCalExchangeSync.Library.Util;

namespace Google.GCalExchangeSync.Library
{
    /// <summary>
    /// Container for a FreeBusy TimeBlock
    /// </summary>
    public class FreeBusyTimeBlock : IComparable
    {
        /// <summary>
        /// Appointments associated with the time block
        /// </summary>
        public List<Appointment> Appointments
        {
            get { return appointments; }
        }

        /// <summary>
        /// Start datetime for the time block
        /// </summary>
        public DateTime StartDate
        {
            get { return range.Start; }
        }

        /// <summary>
        /// End datetime for the time block
        /// </summary>
        public DateTime EndDate
        {
            get { return range.End; }
        }

        /// <summary>
        /// DateTimeRange for the time block
        /// </summary>
        public DateTimeRange Range
        {
            get { return range; }
        }

        private List<Appointment> appointments;
        private DateTimeRange range;

        /// <summary>
        /// Create a new Free Busy time block given the time range
        /// </summary>
        /// <param name="r">Time range of the free busy block</param>
        public FreeBusyTimeBlock(DateTimeRange r)
        {
            this.range = r;
            appointments = new List<Appointment>();
        }

        /// <summary>
        /// Create a new Free busy time block given a start / end time
        /// </summary>
        /// <param name="start">start time for the block</param>
        /// <param name="end">end time for the block</param>
        public FreeBusyTimeBlock(DateTime start, DateTime end)
        {
            this.range = new DateTimeRange(start, end);
            appointments = new List<Appointment>();
        }

        /// <summary>
        /// Compare two free busy time blocks
        /// </summary>
        /// <param name="obj">The free busy time block to compare to</param>
        /// <returns>Comparison value</returns>
        public int CompareTo(object obj)
        {
            FreeBusyTimeBlock block = (FreeBusyTimeBlock)obj;

            return StartDate.CompareTo(block.StartDate);
        }
    }
}
