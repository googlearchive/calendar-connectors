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

namespace Google.GCalExchangeSync.Library.WebDav
{
    /// <summary>
    /// Exchange BusyStatus
    /// http://msdn2.microsoft.com/en-us/library/aa487198.aspx
    /// </summary>
    public enum BusyStatus
    {
        /// <summary>
        /// Busy
        /// </summary>
        Busy,

        /// <summary>
        /// Free
        /// </summary>
        Free,

        /// <summary>
        /// Out of the office
        /// </summary>
        OutOfOffice,

        /// <summary>
        /// Accepted tentatively
        /// </summary>
        Tentative
    }

    /// <summary>
    /// Instance type of the event
    /// http://msdn2.microsoft.com/en-us/library/ms870457.aspx
    /// </summary>
    public enum InstanceType
    {
        /// <summary>
        /// Exception
        /// </summary>
        Exception = 3,

        /// <summary>
        /// Instance
        /// </summary>
        Instance = 2,

        /// <summary>
        /// Master
        /// </summary>
        Master = 1,

        /// <summary>
        /// Single
        /// </summary>
        Single = 0
    }

    /// <summary>
    /// Meeting Status
    /// http://msdn2.microsoft.com/en-us/library/ms991449.asp
    /// </summary>
    public enum MeetingStatus
    {
        /// <summary>
        /// Cancelled
        /// </summary>
        Cancelled,

        /// <summary>
        /// Confirmed
        /// </summary>
        Confirmed,

        /// <summary>
        /// Tentative
        /// </summary>
        Tentative
    }

    /// <summary>
    /// Response status (undocumented)
    /// </summary>
    public enum ResponseStatus
    {
        /// <summary>
        /// Declined
        /// </summary>
        Declined     = 4,

        /// <summary>
        /// Accepted
        /// </summary>
        Accepted     = 3,

        /// <summary>
        /// None
        /// </summary>
        None         = 0,

        /// <summary>
        /// No Reponse
        /// </summary>
        NotResponded = 5,

        /// <summary>
        /// Organized
        /// </summary>
        Organized    = 1,

        /// <summary>
        /// Tentative
        /// </summary>
        Tentative    = 2
    }
}
