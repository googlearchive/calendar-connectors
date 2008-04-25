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

namespace Google.GCalExchangeSync.Library.Util
{
    /// <summary>
    /// Date Time Range
    /// </summary>
    public class DateTimeRange : IComparable
    {
        /// <summary>
        /// Date time range from [MIN_TIME, MAX_TIME]
        /// </summary>
        public static readonly DateTimeRange Full =
            new DateTimeRange(DateTime.MinValue, DateTime.MaxValue);

        private DateTime start = DateTime.MinValue;
        private DateTime end = DateTime.MinValue;

        /// <summary>
        /// Create a new date time range [start, end]
        /// </summary>
        /// <param name="start">start time of the range</param>
        /// <param name="end">end time of the range</param>
        public DateTimeRange(DateTime start, DateTime end)
        {
            this.start = start;
            this.end = end;
        }

        /// <summary>
        /// Compare to date time ranges
        /// </summary>
        /// <param name="obj">Other date time range</param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            DateTimeRange r = obj as DateTimeRange;

            return start.CompareTo(r.Start) & end.CompareTo(r.End);
        }

        /// <summary>
        /// End of the Date Time range
        /// </summary>
        public DateTime End
        {
            get { return this.end; }
            set { this.end = value; }
        }

        /// <summary>
        /// Start of the Date Time range
        /// </summary>
        public DateTime Start
        {
            get { return this.start; }
            set { this.start = value; }
        }

        /// <summary>
        /// Convert the date time range to a string
        /// </summary>
        /// <returns>a string representation of the date time range</returns>
        public override string ToString()
        {
            return string.Format("[{0} - {1}]", start, end);
        }

        /// <summary>
        /// Get a hashcode for comparison / sorting purposes
        /// </summary>
        /// <returns>A unique hashcode for the date time range</returns>
        public override int GetHashCode()
        {
            return Start.GetHashCode() | End.GetHashCode();
        }

        /// <summary>
        /// Compare two date time ranges - they are equals if the start and end times
        /// are equal.
        /// </summary>
        /// <param name="obj">The datetime range to compare to</param>
        /// <returns>True if the two datetime ranges are equal</returns>
        public override bool Equals(object obj)
        {
            DateTimeRange r = obj as DateTimeRange;
            return Start.Equals(r.Start) && End.Equals(r.End);
        }

        /// <summary>
        /// Determine if two ranges overlap
        /// </summary>
        /// <param name="range">The range to test for overlap</param>
        /// <returns>True if the two ranges overlap</returns>
        public bool Overlaps(DateTimeRange range)
        {
            return InRange(range.Start) || InRange(range.End);
        }

        /// <summary>
        /// Determine if a range is contained within this range
        /// </summary>
        /// <param name="range">Range to test if contained</param>
        /// <returns>True if range is contained within this DateTimeRange</returns>
        public bool Contains(DateTimeRange range)
        {
            return InRange(range.Start) && InRange(range.End);
        }

        /// <summary>
        /// Determine if the datetime is contained within this range
        /// </summary>
        /// <param name="t">DateTime to test if in the range</param>
        /// <returns>True if t is within this DateTimeRange</returns>
        public bool InRange(DateTime t)
        {
            return start <= t && t <= end;
        }
    }
}
