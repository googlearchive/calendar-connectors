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
        private string _templateUrl;
        private string _templateUsername;

        /// <summary>
        /// Create the FreeBusy writer
        /// </summary>
        public SchedulePlusFreeBusyWriter()
        {
            _adminGroup = ConfigCache.AdminServerGroup;
            _exchangeServerUrl = ConfigCache.ExchangeFreeBusyServerUrl;
            _templateUsername = ConfigCache.TemplateUserName;
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
            // 1. Copy template F/B message
            if ( _log.IsInfoEnabled )
                _log.InfoFormat( "Creating F/B message.  [User={0}]", user.Email );

            string userFreeBusyUrl = FreeBusyUrl.GenerateUrlFromDN( 
                _exchangeServerUrl, user.LegacyExchangeDN );
            
            exchangeGateway.FreeBusy.CopyFreeBusyMessage( 
                _templateUrl, userFreeBusyUrl, user.FreeBusyCommonName );

            if ( _log.IsInfoEnabled )
                _log.InfoFormat( "F/B template copied successfully. [User={0}]", user.Email );

            // 2. Update F/B properties

            OlsonTimeZone feedTimeZone = OlsonUtil.GetTimeZone( googleAppsFeed.TimeZone.Value );
            
            DateTime minStartDate = DateTime.MinValue, maxEndDate = DateTime.MinValue;
            DateTime startDate = DateTime.MinValue, endDate = DateTime.MinValue;
            DateTime utcStartDate = DateTime.MinValue, utcEndDate = DateTime.MinValue;

            List<DateTimeRange> freeBusyTimes = new List<DateTimeRange>();
 
            /* 3. Iterate through the start and end dates
             * -  Convert everything to UTC
             * -  Clean up any problem dates
             * -  Get the min start date and the max end date */
            foreach ( EventEntry googleAppsEvent in googleAppsFeed.Entries )
            {
                startDate = googleAppsEvent.Times[0].StartTime;
                utcStartDate = OlsonUtil.ConvertToUTC( startDate, feedTimeZone );

                endDate = googleAppsEvent.Times[0].EndTime;
                utcEndDate = OlsonUtil.ConvertToUTC( endDate, feedTimeZone );

                if ( minStartDate == DateTime.MinValue || utcStartDate < minStartDate )
                    minStartDate = utcStartDate;

                if ( maxEndDate == DateTime.MinValue || utcEndDate > maxEndDate )
                    maxEndDate = utcEndDate;

                if (_log.IsDebugEnabled)
                {
                    _log.DebugFormat("Read GData FB event {0} - {1} in {2}", startDate, endDate, googleAppsFeed.TimeZone.Value);
                    _log.DebugFormat("Write FB event {0} - {1} in UTC", utcStartDate, utcEndDate);
                }

                /* If the start UTC start date and UTC end date of the block are on different days, the block needs to be
                     * split up so that there is a free busy block in both the start and end month 
                     * 
                     * Not certain if this is how Exchange handles this situation internally. */
                if ( utcStartDate.Month == utcEndDate.Month )
                {
                    freeBusyTimes.Add(new DateTimeRange(utcStartDate, utcEndDate));
                }
                else
                {
                    /* Create an artificial end date for the portion of the block in the first day by
                     * creating it with the year, month and day from the startDate, and ending that day at 11:59:59 PM */
                    freeBusyTimes.Add( new DateTimeRange(
                        utcStartDate,
                        new DateTime( utcStartDate.Year, utcStartDate.Month, utcStartDate.Day, 23, 59, 59 ) ) ) ;

                    /* Create an artificial start date for the portion of the block in the second day by
                     * creating it with the year, month and day from the endDate, and starting that day at 12:00:00 AM */
                    freeBusyTimes.Add( new DateTimeRange(
                        new DateTime( utcEndDate.Year, utcEndDate.Month, utcEndDate.Day, 0, 0, 0 ),
                        utcEndDate ) );
                }
            }

            freeBusyTimes = CondenseFreeBusyTimes( freeBusyTimes );
            
            List<string> monthValues = new List<string>();
            List<string> base64FreeBusyData = new List<string>();

            /* Pass the resulting start and end dates into the converter, through 
             * ref parameters the month values and date time data for free busy  blocks is returned */
            FreeBusyConverter.ConvertDateTimeBlocksToBase64String(
                freeBusyTimes, monthValues, base64FreeBusyData);

            exchangeGateway.FreeBusy.SetFreeBusyProperties(
                userFreeBusyUrl,
                monthValues, 
                base64FreeBusyData,
                FreeBusyConverter.ConvertToSysTime(minStartDate).ToString(),
                FreeBusyConverter.ConvertToSysTime(maxEndDate).ToString()
                );


            if ( _log.IsInfoEnabled )
            {
                _log.Info( "Free/Busy properties set successfully." );
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

        /// <summary>
        /// Initialize the FreeBusy writer
        /// </summary>
        /// <param name="exchangeGateway">The exchange gateway to use</param>
        public void Initialize( ExchangeService exchangeGateway )
        {
            string query = string.Format("cn={0}", _templateUsername);
            ExchangeUserDict results = exchangeGateway.QueryActiveDirectory(query);
            if (results.Count != 1)
            {
                throw new GCalExchangeException(
                    GCalExchangeErrorCode.GenericError,
                    "Couldn't find FB Template User in Active Directory.  [user=" + _templateUsername + "]");
            }

            foreach (ExchangeUser user in results.Values)
            {
                _templateUrl = FreeBusyUrl.GenerateUrlFromDN( 
                    _exchangeServerUrl, user.LegacyExchangeDN );

                GuaranteeTemplateMessage( exchangeGateway ); 
            }
        }

        // creates the template message if it doesn't exist
        private void GuaranteeTemplateMessage( ExchangeService exchangeGateway ) 
        {
            _log.InfoFormat( "Using '{0}' as the Free/Busy template user.", _templateUsername );

            if ( exchangeGateway.DoesUrlExist( _templateUrl ) )
            {
                _log.InfoFormat( "Template message found. [URL={0}]", _templateUrl );
            }
            else
            {
                // template F/B message not found, so let's create one by finding an existing
                // F/B message and copying it to the template.
                string folderUrl = FreeBusyUrl.GenerateAdminGroupUrl( 
                    _exchangeServerUrl, _adminGroup );

                List<string> freeBusyMessageUrls = 
                    exchangeGateway.WebDavQuery.FindItemUrls( folderUrl, "message" );
                
                if ( freeBusyMessageUrls.Count == 0 )
                {
                    throw new GCalExchangeException( 
                        GCalExchangeErrorCode.GenericError,
                        "No free/busy messages found, unable to create F/B template.  [folder=" + folderUrl + "]" );
                }

                string firstBusyMessageUrl = freeBusyMessageUrls[ 0 ];

                exchangeGateway.FreeBusy.CopyFreeBusyMessage( 
                    firstBusyMessageUrl, _templateUrl, _templateUsername );

                exchangeGateway.FreeBusy.CleanFreeBusyProperties( _templateUrl );

                _log.InfoFormat( "Free/busy template message created. [URL={0}]", _templateUrl );
            }
        }
    }
}
