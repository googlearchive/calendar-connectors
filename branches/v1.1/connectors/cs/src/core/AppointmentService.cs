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

using Google.GCalExchangeSync.Library.Util;
using Google.GCalExchangeSync.Library.WebDav;

namespace Google.GCalExchangeSync.Library
{
    /// <summary>
    /// Handle reading / writing appointments to exchange user mailboxes
    /// </summary>
    public class AppointmentService
    {
        /// <summary>
        /// Logger for AppointmentService
        /// </summary>
        protected static readonly log4net.ILog log =
           log4net.LogManager.GetLogger(typeof(AppointmentService));

        private readonly string exchangeServerUrl;
        private WebDavQuery webDavQuery;

        /// <summary>
        /// Constructor for an Appointment Gateway
        /// </summary>
        /// <param name="exchangeServer">Exchange server address for exchange searches</param>
        /// <param name="webdav">WebDAV query service</param>
        public AppointmentService(string exchangeServer, WebDavQuery webdav)
        {
            exchangeServerUrl = exchangeServer;
            webDavQuery = webdav;
        }

        /// <summary>
        /// Returns appointments for the specified exchange user
        /// </summary>
        /// <param name="user">The user which appointments will be looked up for</param>
        /// <param name="window">The event window to return appointments for</param>
        /// <returns>The event appointments that were looked up</returns>
        public List<Appointment> Lookup(ExchangeUser user, DateTimeRange window)
        {
            /* Create a holder for appointments */
            List<Appointment> appointments = new List<Appointment>();
            if (!ConfigCache.EnableAppointmentLookup)
            {
                if (log.IsDebugEnabled)
                    log.DebugFormat("Appointment lookup supressed for {0}", user.Email );
                return appointments;
            }

            try
            {
                /* Attempt to retrieve the Exchanger user's appointments
                 * This may fail if there is a permissions issue for this user's calendar */
                string calendarUrl = ExchangeUtil.GetDefaultCalendarUrl(exchangeServerUrl, user);

                appointments = webDavQuery.LoadAppointments(calendarUrl, window.Start, window.End);
                user.AccessLevel = GCalAccessLevel.ReadAccess;
                user.HaveAppointmentDetail = true;

                log.InfoFormat(
                    "Appointments read succesfully for '{0}', setting access level to ReadAccess.",
                    user.Email );
            }
            catch (WebException ex)
            {
                log.InfoFormat("Appointment access denied for {0} - {1}", user.Email, ex);
            }
            catch (Exception ex)
            {
                string errorMessage = string.Format(
                    "Error occured while retrieving appointments for user '{0}'", user.Email );

                throw new GCalExchangeException(
                    GCalExchangeErrorCode.ExchangeUnreachable,
                    errorMessage,
                    ex);
            }

            return appointments;
        }

        /// <summary>
        /// Write appointments to an exchange server mailbox
        /// </summary>
        /// <param name="user">The user mailbox to write appointments for</param>
        /// <param name="appointments">The Appointments to write</param>
        public virtual void WriteAppointments(ExchangeUser user, List<Appointment> appointments)
        {
            string calendarUrl = ExchangeUtil.GetDefaultCalendarUrl(
                exchangeServerUrl, user );

            try
            {
                foreach (Appointment appt in appointments)
                {
                    webDavQuery.CreateAppointment(calendarUrl, appt);
                }
            }
            catch (Exception ex)
            {
                throw new GCalExchangeException(
                    GCalExchangeErrorCode.ExchangeUnreachable, "Error writing appointment", ex);
            }
        }

        /// <summary>
        /// Update appointments in an exchange user mailbox
        /// </summary>
        /// <param name="user">The user mailbox to update events in</param>
        /// <param name="appointments">The appointments to update</param>
        public virtual void UpdateAppointments(ExchangeUser user, List<Appointment> appointments)
        {
            string userMailbox = ExchangeUtil.GetMailboxUrl(exchangeServerUrl, user.AccountName);

            try
            {
                foreach (Appointment appt in appointments)
                {
                    webDavQuery.UpdateAppointment( userMailbox, appt );
                }
            }
            catch (Exception ex)
            {
                throw new GCalExchangeException(
                    GCalExchangeErrorCode.ExchangeUnreachable, "Error updating appointment", ex);
            }
            
        }

        /// <summary>
        /// Delete appointments from an exchange user mailbox
        /// </summary>
        /// <param name="user">The user mailbox to remove events from</param>
        /// <param name="appointments">The events to remove</param>
        public virtual void DeleteAppointments(ExchangeUser user, List<Appointment> appointments)
        {
            try
            {
                foreach (Appointment appt in appointments)
                {
                    webDavQuery.Delete(appt.HRef);
                }
            }
            catch (Exception ex)
            {
                throw new GCalExchangeException(
                    GCalExchangeErrorCode.ExchangeUnreachable, "Error deleting appointment", ex);
            }
        }
    }
}