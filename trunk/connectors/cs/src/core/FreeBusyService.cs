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
    /// This class handles read / write requests for Exchange Free / Busy and Appointment data.
    /// </summary>
    public class FreeBusyService
    {
        private static readonly log4net.ILog log =
           log4net.LogManager.GetLogger(typeof(FreeBusyService));

        private string exchangeServerUrl;
        private WebDavQuery webDavQuery;

        /// <summary>
        /// Constructor for an Exchange Gateway
        /// </summary>
        /// <param name="exchangeServer">Exchange server address for exchange searches</param>
        /// <param name="webdav">WebDAV query service</param>
        public FreeBusyService(string exchangeServer, WebDavQuery webdav)
        {
            exchangeServerUrl = exchangeServer;
            webDavQuery = webdav;
        }

        /// <summary>
        /// Returns the free busy times for the specified exchange users
        /// </summary>
        /// <param name="users">The user which free/busy blocks will be looked up for</param>
        /// <param name="window">The time period to look up FB info for</param>
        /// <returns></returns>
        public Dictionary<ExchangeUser, FreeBusy> LookupFreeBusyTimes(
            ExchangeUserDict users, 
            DateTimeRange window )
        {
            /* Create an array of mailboxes to retrieve from exchange */
            Dictionary<ExchangeUser, FreeBusy> result = new Dictionary<ExchangeUser, FreeBusy>();

            try
            {
                using (BlockTimer bt = new BlockTimer("LookupFreeBusyTimes"))
                {
                    /* Perform the retrieval of free busy times through WebDav */
                    result = webDavQuery.LoadFreeBusy(exchangeServerUrl, users, window);
                }
            }
            catch (Exception ex)
            {
                throw new GCalExchangeException(
                    GCalExchangeErrorCode.ExchangeUnreachable,
                   "Error occured while retrieving free busy ranges",
                   ex);
            }

            return result;
        }
        
        /// <summary>
        /// Returns the free busy times for the specified exchange users
        /// </summary>
        /// <param name="user">The user which free/busy blocks will be looked up for</param>
        /// <param name="window">The date range to do the lookup</param>
        /// <returns>FreeBusy data for user in the daterange</returns>
        public FreeBusy LookupFreeBusyTimes( ExchangeUser user, DateTimeRange window )
        {
            ExchangeUserDict users = new ExchangeUserDict();
            users.Add( user.Email, user );

            Dictionary<ExchangeUser, FreeBusy> result = LookupFreeBusyTimes ( users, window );

            return result[user];
        }

        /// <summary>
        /// Copy a Public Folder FreeBusy message on the server from one URL to the next
        /// </summary>
        /// <param name="sourceUrl">Source URL to copy from</param>
        /// <param name="targetUrl">Destination URL to copy to</param>
        /// <param name="targetUsername">The new user to copy the FB template as</param>
        public void CopyFreeBusyMessage(string sourceUrl, string targetUrl, string targetUsername)
        {
            log.DebugFormat("Copy Template free/busy from: {0} to {1}", sourceUrl, targetUrl);

            webDavQuery.Copy(sourceUrl, targetUrl);
            webDavQuery.UpdateProperty(targetUrl, MessageProperty.Subject, targetUsername);

            // TODO: BUG: fix "conversation" message attribute (the line below doesn't seem to be working)
            webDavQuery.UpdateProperty(targetUrl, MessageProperty.ThreadTopic, targetUsername);
        }

        /// <summary>
        /// Set a property on an existing free busy message
        /// </summary>
        /// <param name="messageUrl">The URL of the free busy message to modify</param>
        /// <param name="months"></param>
        /// <param name="dailyData"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        public void SetFreeBusyProperties(
            string messageUrl, 
            List<string> months, 
            List<string> dailyData, 
            string startDate, 
            string endDate )
        {
            // TODO: OPTIMIZATION: all these requests should be combined into a single WebDAV request
            if ( months.Count == 0 )
            {
                webDavQuery.RemoveProperty(
                    messageUrl, 
                    FreeBusyProperty.BusyMonths.Name, 
                    FreeBusyProperty.BusyMonths.NameSpace );
            }
            else
            {
                webDavQuery.UpdateFreeBusyProperty(
                    messageUrl, 
                    FreeBusyProperty.BusyMonths, 
                    months );
            }

            if ( dailyData.Count == 0 )
            {
                webDavQuery.RemoveProperty(
                    messageUrl, 
                    FreeBusyProperty.BusyEvents.Name, 
                    FreeBusyProperty.BusyEvents.NameSpace );
            }
            else
            {
                webDavQuery.UpdateFreeBusyProperty(
                    messageUrl, FreeBusyProperty.BusyEvents, dailyData );
            }

            webDavQuery.UpdateFreeBusyProperty( 
                messageUrl, FreeBusyProperty.StartOfPublishedRange, startDate );

            webDavQuery.UpdateFreeBusyProperty( 
                messageUrl, FreeBusyProperty.EndOfPublishedRange, endDate );

            // TODO: should we should touch the message created date/time here?
        }

        /// <summary>
        /// Remove the properties set on a free busy message
        /// </summary>
        /// <param name="messageUrl"></param>
        public void CleanFreeBusyProperties( string messageUrl )
        {
            FreeBusyProperty[] props = new FreeBusyProperty[]
            {
                FreeBusyProperty.BusyEvents,
                FreeBusyProperty.BusyMonths,
                FreeBusyProperty.OutOfOfficeEvents,
                FreeBusyProperty.OutOfOfficeMonths,
                FreeBusyProperty.TentativeEvents,
                FreeBusyProperty.TentativeMonths
            };

            foreach ( FreeBusyProperty prop in props )
            {
                webDavQuery.RemoveProperty( 
                    messageUrl, prop.Name, prop.NameSpace );
            }
        }
    }
}
