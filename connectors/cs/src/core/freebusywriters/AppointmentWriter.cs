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
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

using Google.GData.Apps;
using Google.GData.Calendar;
using Google.GData.Client;
using Google.GData.Extensions;
using Google.GCalExchangeSync.Library.Util;
using Google.GCalExchangeSync.Library.Scheduling;
using Google.GCalExchangeSync.Library.WebDav;

using TZ4Net;
using log4net;

namespace Google.GCalExchangeSync.Library
{
    /// <summary>
    /// Implement Free / Busy writing by adding events to an Exchange Users
    /// mailbox. This requires an actual Exchange user rather than a contact
    /// but will not affect any of the existing events in the mailbox.
    /// </summary>
    public class AppointmentWriter : IFreeBusyWriter
    {
        private static readonly log4net.ILog log =
         log4net.LogManager.GetLogger( typeof( AppointmentWriter ) );

        private static readonly string GCalenderTag = "GCalenderAppointment";

        /// <summary>
        /// Validates that the appointment passed in was created through a GCalExchange sync
        /// </summary>
        /// <param name="appt">The appointment to validate ownership for</param>
        /// <returns>true if the event was written by AppointmentWriter</returns>
        protected bool ValidateOwnership( Appointment appt )
        {
            return ( appt.Comment == GCalenderTag );
        }

        /// <summary>
        /// Tags an appointment with a ownership flag so that the appointment can be validated 
        /// against
        /// </summary>
        /// <param name="appt">The appointment to set ownership for</param>
        protected void AssignOwnership( Appointment appt )
        {
            appt.Comment = GCalenderTag;
        }

        /// <summary>
        /// Merges a users appointment schedule from with appointments generated from a 
        /// GoogleApps feed
        /// </summary>
        /// <param name="user">User to update with Google Apps information</param>
        /// <param name="googleAppsFeed">Source feed to generate appointment information</param>
        /// <param name="exchangeGateway">Gateway to sync Appointments with</param>
        /// <param name="window">DateRange to sync for</param>
        public void SyncUser( 
            ExchangeUser user, 
            EventFeed googleAppsFeed, 
            ExchangeService exchangeGateway, 
            DateTimeRange window)
        {
            exchangeGateway.GetCalendarInfoForUser(user, window);
            if (!user.HaveAppointmentDetail)
            {
                // Cannot sync if there is no appointment detail
                log.InfoFormat("Skipped Sync of {0} due to missing appointment lookup failure", user.Email);
                return;
            }

            List<Appointment> toUpdate = new List<Appointment>();
            List<Appointment> toDelete = new List<Appointment>();
            List<Appointment> toCreate = new List<Appointment>();

            OlsonTimeZone feedTimeZone = OlsonUtil.GetTimeZone(googleAppsFeed.TimeZone.Value);
            IntervalTree<Appointment> gcalApptTree = 
                CreateAppointments(user, feedTimeZone, googleAppsFeed);

            /* Iterate through each Free/Busy time block for the user */
            foreach (FreeBusyTimeBlock fbtb in user.BusyTimes.Values)
            {
                /* Iterate through each appointment for the Free/Busy time block */
                foreach (Appointment appt in fbtb.Appointments)
                {
                    log.Debug(String.Format("Exchange @ '{0} {1}'",
                               appt.Range,
                               ValidateOwnership(appt)));
                    /* Validate that this is a GCalender appoint */
                    if ( ValidateOwnership( appt ) )
                    {
                        /* If the GCalender appointments do not contain an 
                         * appointment for this period, add it for deletion */
                        if (gcalApptTree.FindExact(appt.Range) == null)
                        {
                            toDelete.Add( appt );
                        }
                    }
                }
            }

            /* Iterate through each Google Apps appointment */
            AppointmentCollection appointments = user.BusyTimes.Appointments;
            List<Appointment> gcalApptList = gcalApptTree.GetNodeList();
            
            foreach (Appointment newAppt in gcalApptList)
            {
                // If the meeting was cancelled
                log.DebugFormat("Looking @ {0} {1}", newAppt.Range, newAppt.Range.Start.Kind);

                if ( newAppt.MeetingStatus == MeetingStatus.Cancelled )
                {
                    // Check if there is an existing appointment that matches
                    List<Appointment> matches = appointments.Get(newAppt.Range);
                    foreach (Appointment a in matches)
                    {
                        if (ValidateOwnership(a))
                        {
                            toDelete.Add(a);
                        }
                    }

                    // Work is done for this appointment, continue to next entry
                    continue;
                }

                bool updatedAppointment = false;

                List<Appointment> apptList = appointments.Get(newAppt.Range);
                log.DebugFormat("Looking up preexisting event: {0} {1}", newAppt.Range, newAppt.Range.Start.Kind);
                log.DebugFormat("Found {0} matching items", apptList.Count);
                    
                // Check that there is a free busy block that correlates with this appointment
                foreach ( Appointment existingAppt in apptList )
                {
                    if (ValidateOwnership(existingAppt) && !updatedAppointment)
                    {
                        UpdateAppointmentInfo(existingAppt, newAppt);
                        toUpdate.Add( existingAppt );
                        updatedAppointment = true;
                    }
                }

                if (!updatedAppointment)
                {
                    toCreate.Add( newAppt );
                    log.DebugFormat("ADDING '{0}' - Not an update",
                            newAppt.Range);
                }
            }

            if (log.IsInfoEnabled)
            {
                log.InfoFormat( 
                    "AppointmentWriter for '{0}'.  [{1} deleted, {2} updated, {3} new]",
                    user.Email,
                    toDelete.Count,
                    toUpdate.Count,
                    toCreate.Count);
            }

            exchangeGateway.Appointments.DeleteAppointments(user, toDelete);
            // TODO: Updates are not currently published
            // exchangeGateway.Appointments.UpdateAppointments( user, updateAppointments );
            exchangeGateway.Appointments.WriteAppointments(user, toCreate);
        }

        /// <summary>
        /// Returns a set of appointments from a GoogleApps Feed
        /// </summary>
        /// <param name="user">The exchange user to get apointments for</param>
        /// <param name="srcTimeZone">The time zone to use</param>
        /// <param name="googleAppsFeed">Source feed to create array from</param>
        /// <returns></returns>
        private IntervalTree<Appointment> CreateAppointments(
            ExchangeUser user, 
            OlsonTimeZone srcTimeZone, 
            EventFeed googleAppsFeed)
        {
            IntervalTree<Appointment> result = new IntervalTree<Appointment>();

            foreach ( EventEntry eventEntry in googleAppsFeed.Entries )
            {
                try
                {
                    CreateAppointment(result, user, srcTimeZone, eventEntry);
                }
                catch ( GCalExchangeException ex )
                {
                    if ( ex.ErrorCode == GCalExchangeErrorCode.OlsonTZError )
                    {
                        log.Error( "Error creating appointment (TimeZone issue)", ex );
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return result;
        }

        private void CreateAppointment( 
            IntervalTree<Appointment> result,
            ExchangeUser user, 
            OlsonTimeZone srcTimeZone, 
            EventEntry googleAppsEvent)
        {
            Appointment appt = new Appointment();

            if ( googleAppsEvent.Times != null && googleAppsEvent.Times.Count > 0 )
            {
                When eventTime = googleAppsEvent.Times[0];
                appt.StartDate = OlsonUtil.ConvertToTimeZone(eventTime.StartTime, srcTimeZone, user.TimeZone);
                appt.EndDate = OlsonUtil.ConvertToTimeZone(eventTime.EndTime, srcTimeZone, user.TimeZone);

                log.DebugFormat("Create - [{0}] {1}", appt.Range, appt.Range.Start.Kind);

                appt.AllDayEvent = googleAppsEvent.Times[0].AllDay;
            }
            if ( googleAppsEvent.Locations != null && googleAppsEvent.Locations.Count > 0 )
            {
                appt.Location = googleAppsEvent.Locations[0].ValueString;
            }
            else
            {
                appt.Location = "";
            }

            appt.Subject = ConfigCache.PlaceHolderMessage;

            if ( googleAppsEvent.Status != null )
            {
                appt.MeetingStatus = 
                    ConversionsUtil.ConvertGoogleEventStatus(googleAppsEvent.Status);
            }
            else
            {
                appt.MeetingStatus = MeetingStatus.Confirmed;
            }

            appt.Created = DateTime.Now;
            appt.InstanceType = InstanceType.Single;
            appt.BusyStatus = BusyStatus.Busy;

            AssignOwnership( appt );
            result.Insert(appt.Range, appt);
        }

        /// <summary>
        /// Updates appointment information between appointments
        /// </summary>
        /// <param name="existingAppt">The existing appointment to update</param>
        /// <param name="newAppt">Appointment information to update from</param>
        /// <returns></returns>
        private Appointment UpdateAppointmentInfo( Appointment existingAppt, Appointment newAppt )
        {
            existingAppt.Body = newAppt.Body;
            existingAppt.Subject = newAppt.Subject;
            existingAppt.StartDate = newAppt.StartDate;
            existingAppt.EndDate = newAppt.EndDate;
            existingAppt.AllDayEvent = newAppt.AllDayEvent;
            existingAppt.MeetingStatus = newAppt.MeetingStatus;
            existingAppt.Location = newAppt.Location;
            existingAppt.InstanceType = newAppt.InstanceType;
            existingAppt.Comment = newAppt.Comment;
            existingAppt.BusyStatus = newAppt.BusyStatus;

            return existingAppt;
        }

        /// <summary>
        /// Initialize the free busy writer
        /// </summary>
        /// <param name="exchangeGateway">The exchange server to use</param>
        public void Initialize(ExchangeService exchangeGateway)
        {
        }
    }
}
