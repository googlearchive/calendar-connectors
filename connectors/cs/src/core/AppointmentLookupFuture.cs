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
using System.Configuration;
using System.DirectoryServices;
using System.Net;
using System.Text;

using log4net;

using Google.GCalExchangeSync.Library.Util;
using Google.GCalExchangeSync.Library.WebDav;

namespace Google.GCalExchangeSync.Library
{
    /// <summary>
    /// Future to do Appointment lookup
    /// </summary>
    class AppointmentLookupFuture : Future
    {
        private Dictionary<ExchangeUser, List<Appointment>> result =
            new Dictionary<ExchangeUser, List<Appointment>>();
        private readonly ExchangeUserDict users;
        private readonly DateTimeRange syncWindow;
        private readonly ExchangeService exchange;

        protected static readonly ILog log =
           LogManager.GetLogger(typeof(AppointmentLookupFuture));

        public AppointmentLookupFuture(
            ExchangeService exchange, 
            ExchangeUserDict users, 
            DateTimeRange window)
        {
            this.exchange = exchange;
            this.users = users;
            this.syncWindow = window;

            // Only do this if appointment 
            // lookup is enabled
            if (ConfigCache.EnableAppointmentLookup)
            {
                this.start();
            }
        }

        protected override void doTask()
        {
            // This method runs from the task thread
            // We don't currently have a way to get 
            // appointment data for multiple users
            foreach (ExchangeUser user in users.Values)
            {
                result[user] = 
                    exchange.Appointments.Lookup(user, syncWindow);
            }
        }

        protected override string TaskName
        {
            get { return "AppointmentLookup"; }
        }

        public List<Appointment> getResult(ExchangeUser user)
        {
            // This method runs from the main thread
            if (ConfigCache.EnableAppointmentLookup)
            {
                waitForCompletion();
                return result[user];
            }
            else
            {
                return new List<Appointment>();
            }
        }
    };
}
