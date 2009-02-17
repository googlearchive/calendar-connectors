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

using Google.GCalExchangeSync.Library.Util;

namespace Google.GCalExchangeSync.Library.WebDav
{
    /// <summary>
    /// An Exchange Appointment
    /// </summary>
    public class Appointment
    {
        /// <summary>
        /// Creation time of the appointment
        /// </summary>
        public DateTime Created;

        /// <summary>
        /// Start time of the appointment
        /// </summary>
        public DateTime StartDate
        {
            get { return range.Start; }
            set { range.Start = value; }
        }

        /// <summary>
        /// End time of the appointment
        /// </summary>
        public DateTime EndDate
        {
            get { return range.End; }
            set { range.End = value; }
        }

        /// <summary>
        /// DateTimeRange for the event
        /// </summary>
        public DateTimeRange Range
        {
            get { return range; }
        }

        /// <summary>
        /// Appointment body
        /// </summary>
        public string Body;

        /// <summary>
        /// Appointment subject
        /// </summary>
        public string Subject;

        /// <summary>
        /// Appointment location
        /// </summary>
        public string Location;

        /// <summary>
        /// Appointment comment
        /// </summary>
        public string Comment;

        /// <summary>
        /// Appointment organizer
        /// </summary>
        public string Organizer;

        /// <summary>
        /// Appointment busy status
        /// </summary>
        public BusyStatus BusyStatus;

        /// <summary>
        /// Appointment response status
        /// </summary>
        public ResponseStatus ResponseStatus;

        /// <summary>
        /// Appointment instance type
        /// </summary>
        public InstanceType InstanceType;

        /// <summary>
        /// Appointment meeting status
        /// </summary>
        public MeetingStatus MeetingStatus;

        /// <summary>
        /// Appointment is private?
        /// </summary>
        public bool IsPrivate;

        /// <summary>
        /// Appointment is an all day event
        /// </summary>
        public bool AllDayEvent;

        /// <summary>
        /// Appointment href
        /// </summary>
        public string HRef;

        private DateTimeRange range;

        /// <summary>
        /// Create a new appointment
        /// </summary>
        public Appointment()
        {
            Created = DateUtil.NowUtc;
            range = new DateTimeRange(Created, Created);

            Body = string.Empty;
            Subject = string.Empty;
            Location = string.Empty;
            Comment = string.Empty;
            Organizer = string.Empty;

            BusyStatus = BusyStatus.Free;
            ResponseStatus = ResponseStatus.None;
            InstanceType = InstanceType.Single;
            MeetingStatus = MeetingStatus.Tentative;

            IsPrivate = false;
            AllDayEvent = false;

            HRef = string.Empty;
        }

        /// <summary>
        /// Convert an appointment to a string representation
        /// </summary>
        /// <returns>Printable string with appointment details</returns>
        public override string ToString()
        {
            return string.Format(
                "Appointment [{0}-{1}]\nSubject: {2}\nLocation: {3}\nComment: {4}\nOrganizer: {5}\nBusyStatus: {6}\nMeetingStatus: {7}\nIsPrivate: {8}\nAllDayEvent: {9}",
                StartDate,
                EndDate,
                Subject,
                Location,
                Comment,
                Organizer,
                BusyStatus,
                MeetingStatus,
                IsPrivate,
                AllDayEvent);
        }
    }
}
