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

namespace Google.GCalExchangeSync.Library
{
    /// <summary>
    /// Google Calendar Access Level
    /// </summary>
    public enum GCalAccessLevel
    {
        /// <summary>No access</summary>
        NoAccess        = 0,

        /// <summary>Access to free busy info</summary>
        FreeBusyAccess  = 10,

        /// <summary>Read only access</summary>
        ReadAccess      = 20,

        /// <summary>Owner access</summary>
        Owner           = 70
    }

    /// <summary>
    /// Google Calendar Busy Status
    /// </summary>
    public enum GCalBusyStatus
    {
        /// <summary>Busy</summary>
        Busy,

        /// <summary>Out of the office</summary>
        OutOfOffice,

        /// <summary>Tentative</summary>
        Tentative
    }

    /// <summary>
    /// Google Calendar projection
    /// </summary>
    public enum GCalProjection
    {
        /// <summary>Full</summary>
        Full,

        /// <summary>Full / No Attendees</summary>
        FullNoAttendees,

        /// <summary>Composite</summary>
        Composite,

        /// <summary>Attendees Only</summary>
        AttendeesOnly,

        /// <summary>Free Busy</summary>
        FreeBusy,

        /// <summary>Basic</summary>
        Basic
    }

    /// <summary>
    /// Google Calendar Visibility
    /// </summary>
    public enum GCalVisibility
    {
        /// <summary>Public</summary>
        Public,

        /// <summary>Private</summary>
        Private
    }
}