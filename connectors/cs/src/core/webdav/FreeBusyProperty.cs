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
    /// Properties specific to Free Busy information
    /// </summary>
    public class FreeBusyProperty : Property
    {
        /// <summary>
        /// Disable full fidelity (appeats that it should be always set to true/1).
        /// </summary>
        public static readonly FreeBusyProperty DisableFullFidelity =
            new FreeBusyProperty("x10F2000B", "http://schemas.microsoft.com/mapi/proptag/", "boolean");

        /// <summary>
        /// The id (Windows LCID) of the message's locale (for example 1033).
        /// </summary>
        public static readonly FreeBusyProperty MessageLocaleId =
            new FreeBusyProperty("x3FF10003", "http://schemas.microsoft.com/mapi/proptag/", "int");

        /// <summary>
        /// The id (Windows LCID) of the locale for system generated messages (for example 1033).
        /// </summary>
        public static readonly FreeBusyProperty LocaleId =
            new FreeBusyProperty("x66A10003", "http://schemas.microsoft.com/mapi/proptag/", "int");

        /// <summary>
        /// Free busy Schedule Info Resource Type (should be always set to zero).
        /// </summary>
        public static readonly FreeBusyProperty ScheduleInfoResourceType =
            new FreeBusyProperty("x68410003", "http://schemas.microsoft.com/mapi/proptag/", "int");

        /// <summary>
        /// Free busy start of publisher range
        /// </summary>
        public static readonly FreeBusyProperty StartOfPublishedRange =
            new FreeBusyProperty("x68470003", "http://schemas.microsoft.com/mapi/proptag/", "int");

        /// <summary>
        /// Free busy end of publisher range
        /// </summary>
        public static readonly FreeBusyProperty EndOfPublishedRange =
            new FreeBusyProperty("x68480003", "http://schemas.microsoft.com/mapi/proptag/", "int");

        /// <summary>
        /// FreeBusy merged (busy + OOF) months
        /// </summary>
        public static readonly FreeBusyProperty MergedMonths =
            new FreeBusyProperty("x684f1003", "http://schemas.microsoft.com/mapi/proptag/", "int");

        /// <summary>
        /// FreeBusy merged (busy + OOF) events
        /// </summary>
        public static readonly FreeBusyProperty MergedEvents =
            new FreeBusyProperty("x68501102", "http://schemas.microsoft.com/mapi/proptag/", "bin.base64");

        /// <summary>
        /// FreeBusy tentative months
        /// </summary>
        public static readonly FreeBusyProperty TentativeMonths =
            new FreeBusyProperty("x68511003", "http://schemas.microsoft.com/mapi/proptag/", "int");

        /// <summary>
        /// FreeBusy tentative events
        /// </summary>
        public static readonly FreeBusyProperty TentativeEvents =
            new FreeBusyProperty("x68521102", "http://schemas.microsoft.com/mapi/proptag/", "bin.base64");

        /// <summary>
        /// FreeBusy busy months
        /// </summary>
        public static readonly FreeBusyProperty BusyMonths =
            new FreeBusyProperty( "x68531003", "http://schemas.microsoft.com/mapi/proptag/", "int" );

        /// <summary>
        /// FreeBusy busy events
        /// </summary>
        public static readonly FreeBusyProperty BusyEvents =
            new FreeBusyProperty( "x68541102", "http://schemas.microsoft.com/mapi/proptag/", "bin.base64" );

        /// <summary>
        /// FreeBusy OOF months
        /// </summary>
        public static readonly FreeBusyProperty OutOfOfficeMonths =
            new FreeBusyProperty("x68551003", "http://schemas.microsoft.com/mapi/proptag/", "int" );

        /// <summary>
        /// FreeBusy OOF events
        /// </summary>
        public static readonly FreeBusyProperty OutOfOfficeEvents =
            new FreeBusyProperty("x68561102", "http://schemas.microsoft.com/mapi/proptag/", "bin.base64" );

        /// <summary>
        /// The time when the free busy information was set, in UTC.
        /// </summary>
        public static readonly FreeBusyProperty FreeBusyRangeTimestamp =
            new FreeBusyProperty("x68680040", "http://schemas.microsoft.com/mapi/proptag/", "dateTime.tz");

        private string _type = string.Empty;

        /// <summary>
        /// The property type
        /// </summary>
        public string Type
        {
            get { return this._type; }
        }

        /// <summary>
        /// Ctor for a new FreeBusyProperty
        /// </summary>
        /// <param name="name">The name of the property</param>
        /// <param name="nameSpace">The namespace for the property</param>
        /// <param name="type">The tpe of the propery</param>
        public FreeBusyProperty( string name, string nameSpace, string type )
            : base( name, nameSpace )
        {
            this._type = type;
        }
    }
}
