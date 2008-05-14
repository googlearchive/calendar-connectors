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
using Google.GData.Calendar;
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
            DateTimeRange window )
        {
            if (_log.IsInfoEnabled)
            {
                _log.InfoFormat("Creating F/B message.  [User={0}]", user.Email);
            }

            string userFreeBusyUrl = FreeBusyUrl.GenerateUrlFromDN(
                _exchangeServerUrl, user.LegacyExchangeDN );

            // Get F/B properties

            OlsonTimeZone feedTimeZone = OlsonUtil.GetTimeZone( googleAppsFeed.TimeZone.Value );

            DateTime minStartDate = DateTime.MinValue, maxEndDate = DateTime.MinValue;
            DateTime startDate = DateTime.MinValue, endDate = DateTime.MinValue;
            DateTime utcStartDate = DateTime.MinValue, utcEndDate = DateTime.MinValue;

            List<DateTimeRange> freeBusyTimes = new List<DateTimeRange>();

            /*    Iterate through the start and end dates
             * -  Convert everything to UTC
             * -  Clean up any problem dates
             * -  Get the min start date and the max end date */
            foreach ( EventEntry googleAppsEvent in googleAppsFeed.Entries )
            {
                startDate = googleAppsEvent.Times[0].StartTime;
                utcStartDate = DateTime.SpecifyKind(startDate.ToUniversalTime(),
                    DateTimeKind.Unspecified);
                endDate = googleAppsEvent.Times[0].EndTime;
                utcEndDate = DateTime.SpecifyKind(endDate.ToUniversalTime(),
                    DateTimeKind.Unspecified);

                if ( minStartDate == DateTime.MinValue || utcStartDate < minStartDate )
                    minStartDate = utcStartDate;

                if ( maxEndDate == DateTime.MinValue || utcEndDate > maxEndDate )
                    maxEndDate = utcEndDate;

                if (_log.IsDebugEnabled)
                {
                    _log.DebugFormat("Read GData FB event {0} - {1} in {2}",
                        startDate, endDate, googleAppsFeed.TimeZone.Value);
                    _log.DebugFormat("Write FB event {0} - {1} in UTC", utcStartDate, utcEndDate);
                }

                // If the start UTC start date and UTC end date of the block are on different months,
                // the block needs to be split up so that there is a free busy block in both the
                // start and end month
                if ( utcStartDate.Month == utcEndDate.Month )
                {
                    freeBusyTimes.Add(new DateTimeRange(utcStartDate, utcEndDate));
                }
                else
                {
                    // Create an artificial end date for the portion of the block in the first
                    // day by creating it with the year, month and day from the startDate, and
                    //ending that day at 11:59:59 PM
                    freeBusyTimes.Add(new DateTimeRange(
                        utcStartDate,
                        new DateTime(utcStartDate.Year,
                                     utcStartDate.Month,
                                     utcStartDate.Day,
                                     23, 59, 59))) ;

                    // Create an artificial start date for the portion of the block in the second
                    // day by creating it with the year, month and day from the endDate, and
                    // starting that day at 12:00:00 AM
                    freeBusyTimes.Add( new DateTimeRange(
                        new DateTime( utcEndDate.Year, utcEndDate.Month, utcEndDate.Day, 0, 0, 0 ),
                        utcEndDate ) );
                }
            }

            freeBusyTimes = CondenseFreeBusyTimes( freeBusyTimes );

            List<string> monthValues = new List<string>();
            List<string> base64FreeBusyData = new List<string>();

            FreeBusyConverter.ConvertDateTimeBlocksToBase64String(
                freeBusyTimes, monthValues, base64FreeBusyData);

            string stringStartDate = FreeBusyConverter.ConvertToSysTime(minStartDate).ToString();
            string stringEndDate = FreeBusyConverter.ConvertToSysTime(maxEndDate).ToString();

            exchangeGateway.FreeBusy.CreateFreeBusyMessage(userFreeBusyUrl,
                                                           user.FreeBusyCommonName,
                                                           monthValues,
                                                           base64FreeBusyData,
                                                           stringStartDate,
                                                           stringEndDate);

            if ( _log.IsInfoEnabled )
            {
                _log.Info( "Free/Busy message with the right properties created successfully." );
            }
        }


        private List<DateTimeRange> CondenseFreeBusyTimes(List<DateTimeRange> freeBusyTimes)
        {
            List<DateTimeRange> condensedFreeBusyTimes = new List<DateTimeRange>();

            freeBusyTimes.Sort();

            foreach (DateTimeRange existingTime in freeBusyTimes)
            {
                AddFreeBusyTime(condensedFreeBusyTimes, existingTime);
            }

            return condensedFreeBusyTimes;
        }

        private void AddFreeBusyTime(List<DateTimeRange> freeBusyTimes, DateTimeRange newEntry)
        {
            int i = 0;
            DateTime minStartDate, maxEndDate;

            bool updatedExistingRange = false;

            foreach ( DateTimeRange existingTime in freeBusyTimes )
            {
                if ( DateUtil.IsWithinRange( newEntry.Start, existingTime.Start, existingTime.End ) ||
                      DateUtil.IsWithinRange( newEntry.End, existingTime.Start, existingTime.End ) )
                {
                    minStartDate = ( newEntry.Start < existingTime.Start ) ?
                        newEntry.Start : existingTime.Start;
                    maxEndDate = ( newEntry.End > existingTime.End ) ?
                        newEntry.End : existingTime.End;

                    existingTime.Start = minStartDate;
                    existingTime.End = maxEndDate;

                    updatedExistingRange = true;

                    i++;
                }
            }

            if ( !updatedExistingRange )
            {
                freeBusyTimes.Add( newEntry );
            }
        }
    }
}
