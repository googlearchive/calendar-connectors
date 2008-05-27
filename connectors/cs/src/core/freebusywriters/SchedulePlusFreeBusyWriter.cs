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
            }

            string userFreeBusyUrl = FreeBusyUrl.GenerateUrlFromDN(_exchangeServerUrl,
                                                                   user.LegacyExchangeDN);

            // Get F/B properties

            OlsonTimeZone feedTimeZone = OlsonUtil.GetTimeZone(googleAppsFeed.TimeZone.Value);

            DateTime minStartDate = DateTime.MinValue;
            DateTime maxEndDate = DateTime.MinValue;
            DateTime startDate = DateTime.MinValue;
            DateTime endDate = DateTime.MinValue;
            DateTime utcStartDate = DateTime.MinValue;
            DateTime utcEndDate = DateTime.MinValue;

            List<DateTimeRange> busyTimes = new List<DateTimeRange>();
            List<DateTimeRange> tentativeTimes = new List<DateTimeRange>();

            /*    Iterate through the start and end dates
             * -  Convert everything to UTC
             * -  Clean up any problem dates
             * -  Get the min start date and the max end date */
            foreach (EventEntry googleAppsEvent in googleAppsFeed.Entries)
            {
                BusyStatus userStatus = BusyStatus.Free;
                userStatus = ConversionsUtil.GetUserStatusForEvent(user, googleAppsEvent);

                startDate = googleAppsEvent.Times[0].StartTime;
                utcStartDate = DateTime.SpecifyKind(startDate.ToUniversalTime(),
                                                    DateTimeKind.Unspecified);
                endDate = googleAppsEvent.Times[0].EndTime;
                utcEndDate = DateTime.SpecifyKind(endDate.ToUniversalTime(),
                                                  DateTimeKind.Unspecified);

                if (minStartDate == DateTime.MinValue || utcStartDate < minStartDate)
                {
                    minStartDate = utcStartDate;
                }

                if (maxEndDate == DateTime.MinValue || utcEndDate > maxEndDate)
                {
                    maxEndDate = utcEndDate;
                }

                if (_log.IsDebugEnabled)
                {
                    _log.DebugFormat("Read GData FB event {0} - {1} in {2}",
                                     startDate,
                                     endDate,
                                     googleAppsFeed.TimeZone.Value);
                    _log.DebugFormat("Write FB event {0} - {1} in UTC", utcStartDate, utcEndDate);
                    _log.DebugFormat("The FB status is {0}", userStatus.ToString());
                }

                // If the user is free, do not put this meeting in the free busy
                if (userStatus == BusyStatus.Free)
                {
                    continue;
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

            CondenseFreeBusyTimes(busyTimes);
            CondenseFreeBusyTimes(tentativeTimes);

            List<string> busyMonthValues = new List<string>();
            List<string> busyBase64Data = new List<string>();
            List<string> tentativeMonthValues = new List<string>();
            List<string> tentativeBase64Data = new List<string>();

            FreeBusyConverter.ConvertDateTimeBlocksToBase64String(minStartDate,
                                                                  maxEndDate,
                                                                  busyTimes,
                                                                  busyMonthValues,
                                                                  busyBase64Data);

            FreeBusyConverter.ConvertDateTimeBlocksToBase64String(minStartDate,
                                                                  maxEndDate,
                                                                  tentativeTimes,
                                                                  tentativeMonthValues,
                                                                  tentativeBase64Data);

            string stringStartDate = FreeBusyConverter.ConvertToSysTime(minStartDate).ToString();
            string stringEndDate = FreeBusyConverter.ConvertToSysTime(maxEndDate).ToString();


            exchangeGateway.FreeBusy.CreateFreeBusyMessage(userFreeBusyUrl,
                                                           user.FreeBusyCommonName,
                                                           busyMonthValues,
                                                           busyBase64Data,
                                                           tentativeMonthValues,
                                                           tentativeBase64Data,
                                                           stringStartDate,
                                                           stringEndDate);

            if ( _log.IsInfoEnabled )
            {
                _log.Info( "Free/Busy message with the right properties created successfully." );
            }
        }

        private static int CompareRangesByStartThenEnd(
            DateTimeRange x,
            DateTimeRange y)
        {
            if ((x == null) && (y == null))
            {
                // If x is null and y is null, they are equal.
                return 0;
            }

            if (x == null)
            {
                // If x is null and y is not null, y is greater.
                return -1;
            }

            if (y == null)
            {
                // If x is not null and y is null, x is greater.
                return 1;
            }

            int result = x.Start.CompareTo(y.Start);

            if (result == 0)
            {
                result = x.End.CompareTo(y.End);
            }

            #if DEBUG
                Debug.Assert((result == 0) == (x.CompareTo(y) == 0) &&
                             (result > 0) == (x.CompareTo(y) > 0));
            #endif

            return result;
        }

        private static bool IsMarkedToBeDeleted(
            DateTimeRange range)
        {
            return (range.Start == DateTime.MinValue) && (range.End == DateTime.MinValue);
        }

        private static void CondenseFreeBusyTimes(
            List<DateTimeRange> freeBusyTimes)
        {
            freeBusyTimes.Sort(CompareRangesByStartThenEnd);

            IEnumerable<DateTimeRange> enumerable = freeBusyTimes as IEnumerable<DateTimeRange>;
            IEnumerator<DateTimeRange> enumerator = enumerable.GetEnumerator();
            DateTimeRange previous = null;
            DateTimeRange current = null;
            int markedToBeDeleted = 0;
            int deleted = 0;

            if (!enumerator.MoveNext())
            {
                // The list is empty
                return;
            }

            previous = enumerator.Current;

            while (enumerator.MoveNext())
            {
                current = enumerator.Current;

                #if DEBUG
                    Debug.Assert(previous.Start <= previous.End);
                    Debug.Assert(current.Start <= current.End);
                    Debug.Assert(previous.Start <= current.End);
                #endif

                // If the events touch or overlap
                if (previous.End >= current.Start)
                {
                    // Make the current the union of both
                    #if DEBUG
                        Debug.Assert(previous.Start <= current.Start);
                    #endif
                    current.Start = previous.Start;

                    if (current.End < previous.End)
                    {
                        current.End = previous.End;
                    }
                    #if DEBUG
                        Debug.Assert(current.End >= previous.End);
                    #endif


                    // Mark the previous to be deleted
                    previous.Start = DateTime.MinValue;
                    previous.End = DateTime.MinValue;
                    markedToBeDeleted++;
                }

                previous = current;
            }

            deleted = freeBusyTimes.RemoveAll(IsMarkedToBeDeleted);
            #if DEBUG
                Debug.Assert(markedToBeDeleted == deleted);
            #endif
        }
    }
}
