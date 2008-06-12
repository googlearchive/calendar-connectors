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
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using Google.GData.Client;
using Google.GData.Calendar;
using Google.GData.Extensions;
using Google.GCalExchangeSync.Library.Util;
using Google.GCalExchangeSync.Library.WebDav;
using TZ4Net;

namespace Google.GCalExchangeSync.Library
{
    /// <summary>
    /// A free busy writer for SchedulePlus - free busy information is written to
    /// the Exchange Public Folders.
    /// </summary>
    public class SchedulePlusFreeBusyWriter : IFreeBusyWriter
    {
        /// <summary>
        /// Logger for SchedulePlusFreeBusyWriter
        /// </summary>
        protected static readonly log4net.ILog _log =
          log4net.LogManager.GetLogger( typeof( SchedulePlusFreeBusyWriter ) );

        private string _adminGroup;
        private string _exchangeServerUrl;

        /// <summary>
        /// Create the FreeBusy writer
        /// </summary>
        public SchedulePlusFreeBusyWriter()
        {
            _adminGroup = ConfigCache.AdminServerGroup;
            _exchangeServerUrl = ConfigCache.ExchangeFreeBusyServerUrl;
        }

        /// <summary>
        /// The schedule plus writer needs the feed to expand the recurring events, so return true.
        /// </summary>
        public bool RequiresEventExpansion()
        {
            return true;
        }

        /// <summary>
        /// Sync a users free busy information between Google Calendar and the
        /// SchedulePlus Public Folder store
        /// </summary>
        /// <param name="user">The user to synchronize</param>
        /// <param name="googleAppsFeed">The Google Calendar events for the user</param>
        /// <param name="exchangeGateway">The Exchange Gateway to use</param>
        /// <param name="window">The DateTimeRange to synchronize for</param>
        public void SyncUser(
            ExchangeUser user,
            EventFeed googleAppsFeed,
            ExchangeService exchangeGateway,
            DateTimeRange window)
        {
            if (_log.IsInfoEnabled)
            {
                _log.InfoFormat("Creating F/B message.  [User={0}]", user.Email);
                _log.DebugFormat("The feed time zone is {0}", googleAppsFeed.TimeZone.Value);
            }

            string userFreeBusyUrl = FreeBusyUrl.GenerateUrlFromDN(_exchangeServerUrl,
                                                                   user.LegacyExchangeDN);

            DateTimeRange coveredRange = new DateTimeRange(window.Start, window.End);
            List<string> busyMonthValues = new List<string>();
            List<string> busyBase64Data = new List<string>();
            List<string> tentativeMonthValues = new List<string>();
            List<string> tentativeBase64Data = new List<string>();

            ConvertEventsToFreeBusy(user,
                                    googleAppsFeed.Entries,
                                    coveredRange,
                                    busyMonthValues,
                                    busyBase64Data,
                                    tentativeMonthValues,
                                    tentativeBase64Data);



            string startDate = FreeBusyConverter.ConvertToSysTime(coveredRange.Start).ToString();
            string endDate = FreeBusyConverter.ConvertToSysTime(coveredRange.End).ToString();

            exchangeGateway.FreeBusy.CreateFreeBusyMessage(userFreeBusyUrl,
                                                           user.FreeBusyCommonName,
                                                           busyMonthValues,
                                                           busyBase64Data,
                                                           tentativeMonthValues,
                                                           tentativeBase64Data,
                                                           startDate,
                                                           endDate);

            if ( _log.IsInfoEnabled )
            {
                _log.Info( "Free/Busy message with the right properties created successfully." );
            }
        }

        private static void ConvertEventsToFreeBusy(
            ExchangeUser user,
            AtomEntryCollection entries,
            DateTimeRange coveredRange,
            List<string> busyMonthValues,
            List<string> busyBase64Data,
            List<string> tentativeMonthValues,
            List<string> tentativeBase64Data)
        {
            List<DateTimeRange> busyTimes = new List<DateTimeRange>();
            List<DateTimeRange> tentativeTimes = new List<DateTimeRange>();

            foreach (EventEntry googleAppsEvent in entries)
            {
                ConvertEventToFreeBusy(user, googleAppsEvent, coveredRange, busyTimes, tentativeTimes);
            }

            FreeBusyConverter.CondenseFreeBusyTimes(busyTimes);
            FreeBusyConverter.CondenseFreeBusyTimes(tentativeTimes);

            FreeBusyConverter.ConvertDateTimeBlocksToBase64String(coveredRange.Start,
                                                                  coveredRange.End,
                                                                  busyTimes,
                                                                  busyMonthValues,
                                                                  busyBase64Data);

            FreeBusyConverter.ConvertDateTimeBlocksToBase64String(coveredRange.Start,
                                                                  coveredRange.End,
                                                                  tentativeTimes,
                                                                  tentativeMonthValues,
                                                                  tentativeBase64Data);
        }

        private static void ConvertEventToFreeBusy(
            ExchangeUser user,
            EventEntry googleAppsEvent,
            DateTimeRange coveredRange,
            List<DateTimeRange> busyTimes,
            List<DateTimeRange> tentativeTimes)
        {
            DateTime startDate = DateTime.MinValue;
            DateTime endDate = DateTime.MinValue;
            DateTime utcStartDate = DateTime.MinValue;
            DateTime utcEndDate = DateTime.MinValue;
            BusyStatus userStatus = BusyStatus.Free;

            userStatus = ConversionsUtil.GetUserStatusForEvent(user, googleAppsEvent);

            // If the user is free, do not put this meeting in the free busy
            if (userStatus == BusyStatus.Free)
            {
                return;
            }

            startDate = googleAppsEvent.Times[0].StartTime;
            utcStartDate = DateTime.SpecifyKind(startDate.ToUniversalTime(),
                                                DateTimeKind.Unspecified);
            endDate = googleAppsEvent.Times[0].EndTime;
            utcEndDate = DateTime.SpecifyKind(endDate.ToUniversalTime(),
                                              DateTimeKind.Unspecified);

            if (utcStartDate < coveredRange.Start)
            {
                coveredRange.Start = utcStartDate;
            }

            if (utcEndDate > coveredRange.End)
            {
                coveredRange.End = utcEndDate;
            }

            if (_log.IsDebugEnabled)
            {
                _log.DebugFormat("Read GData FB event {0} - {1}", startDate, endDate);
                _log.DebugFormat("Write FB event {0} - {1} in UTC", utcStartDate, utcEndDate);
                _log.DebugFormat("The FB status is {0}", userStatus.ToString());
            }

            if (userStatus == BusyStatus.Tentative)
            {
                tentativeTimes.Add(new DateTimeRange(utcStartDate, utcEndDate));
            }
            else
            {
                Debug.Assert(userStatus == BusyStatus.Busy);
                busyTimes.Add(new DateTimeRange(utcStartDate, utcEndDate));
            }
        }
    }
}
