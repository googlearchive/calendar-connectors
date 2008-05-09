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
    /// Represent a free busy result
    /// </summary>
    public class FreeBusy
    {
        private List<DateTimeRange> _all = new List<DateTimeRange>();
        private List<DateTimeRange> _busy = new List<DateTimeRange>();
        private List<DateTimeRange> _outOfOffice = new List<DateTimeRange>();
        private List<DateTimeRange> _tentative = new List<DateTimeRange>();
        private ExchangeUser _user;

        /// <summary>
        /// The Exchange user the free busy infor is for
        /// </summary>
        public ExchangeUser User
        {
            get { return _user; }
            set { _user = value; }
        }

        /// <summary>
        /// Set of date ranges in the free busy result
        /// </summary>
        public List<DateTimeRange> All
        {
            get { return _all; }
            set { _all = value; }
        }

        /// <summary>
        /// Set of date ranges for busy times in the free busy result
        /// </summary>
        public List<DateTimeRange> Busy
        {
            get { return _busy; }
            set { _busy = value; }
        }

        /// <summary>
        /// Set of date rnages for Out of the office times in the free busy result
        /// </summary>
        public List<DateTimeRange> OutOfOffice
        {
            get { return _outOfOffice; }
            set { _outOfOffice = value; }
        }

        /// <summary>
        /// Set of date ranges for tentative times in the free busy result
        /// </summary>
        public List<DateTimeRange> Tentative
        {
            get { return _tentative; }
            set { _tentative = value; }
        }
    }
}
