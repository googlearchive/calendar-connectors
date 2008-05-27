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
using System.Collections.Specialized;
using System.Data;
using System.Configuration;
using System.Text;
using System.Xml;
using System.IO;
using System.Web;
using System.Web.Security;

using Google.GCalExchangeSync.Library.Util;
using Google.GData.AccessControl;
using Google.GData.Calendar;
using Google.GData.Client;
using Google.GData.Extensions;

using log4net;

namespace Google.GCalExchangeSync.Library
{
    /// <summary>
    /// Wrapper for behaviour about Google Calendar
    /// </summary>
    public class GCalGateway
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(typeof(GCalGateway));

        private string googleAppsLogin;
        private string googleAppsPassword;
        private string googleAppsDomain;
        private string logDirectory;
        private CalendarService service;

        private static readonly string AgentIdentifier = "Google Calendar Connector Sync/1.0.0";

        /// <summary>
        /// Create a gateway for talking to Google Calendar
        /// </summary>
        /// <param name="login">User login to use</param>
        /// <param name="password">User credential to use</param>
        /// <param name="domain">Google Apps Domain to user</param>
        public GCalGateway(string login, string password, string domain)
        {
            this.googleAppsLogin = login;
            this.googleAppsPassword = password;
            this.googleAppsDomain   = domain;

            InitializeCalendarService();

            logDirectory = ConfigCache.GCalLogDirectory;
        }

        private void InitializeCalendarService()
        {
            service = new CalendarService( AgentIdentifier );
            service.setUserCredentials(String.Format("{0}@{1}", googleAppsLogin, googleAppsDomain), googleAppsPassword);

            // Forcekeep alive connections on
            ((GDataRequestFactory)service.RequestFactory).KeepAlive = ConfigCache.EnableKeepAlive;
        }

        /// <summary>
        /// Query Google Calendar for a user and return the event feed
        /// </summary>
        /// <param name="email">Email address of the user to query</param>
        /// <param name="visibility">Feed Visibility (Public/Private) to query for</param>
        /// <param name="projection">Feed projection - type of feed to get</param>
        /// <param name="modifiedSince">Last modified time from last check</param>
        /// <param name="window">DateTime range to query between</param>
        /// <returns>An event feed for the user</returns>
        public EventFeed QueryGCal(
            string email,
            GCalVisibility visibility,
            GCalProjection projection,
            DateTime modifiedSince,
            DateTimeRange window)
        {
            // Perform mapping on the username if necessary
            string user = ConfigCache.MapToExternalDomain(email);

            if (log.IsDebugEnabled)
            {
                log.InfoFormat(
                    "FeedQuery with parameters: {0}, {1}, {2}, {3} [{4}]",
                    user,
                    visibility,
                    projection,
                    modifiedSince,
                    window);
            }

            StringBuilder sb = new StringBuilder(ConfigCache.GCalAddress);
            if (!ConfigCache.GCalAddress.EndsWith("/"))
                sb.Append("/");

            sb.AppendFormat("feeds/{0}/", user);

            switch (visibility)
            {
                case GCalVisibility.Public:
                    sb.Append("public");
                    break;
                case GCalVisibility.Private:
                default:
                    sb.Append("private");
                    break;
            }

            sb.Append("/");

            switch (projection)
            {
                case GCalProjection.Full:
                    sb.Append("full");
                    break;
                case GCalProjection.FullNoAttendees:
                    sb.Append("full-noattendees");
                    break;
                case GCalProjection.Composite:
                    sb.Append("composite");
                    break;
                case GCalProjection.AttendeesOnly:
                    sb.Append("attendees-only");
                    break;
                case GCalProjection.FreeBusy:
                    sb.Append("free-busy");
                    break;
                case GCalProjection.Basic:
                default:
                    sb.Append("basic");
                    break;
            }

            EventQuery query = new EventQuery(sb.ToString());
            if (projection != GCalProjection.FreeBusy)
            {
                query.SingleEvents = true;
            }

            GDataRequestFactory f = (GDataRequestFactory)service.RequestFactory;
            f.UseGZip = ConfigCache.EnableHttpCompression;

            if (window.Start != DateTime.MinValue)
            {
                query.StartTime = window.Start;
            }

            if(window.End != DateTime.MaxValue)
                query.EndTime = window.End;

            query.NumberToRetrieve = int.MaxValue; // Make sure we get everything

            try
            {
                return QueryGCal(query, user, modifiedSince);
            }
            catch (System.IO.IOException e)
            {
                // Known problem with .NET 2.0 - Sometimes keep-alive connection is
                // closed by a proxy and we need to re-attemp the connection
                //
                // http://code.google.com/p/google-gdata/wiki/KeepAliveAndUnderlyingConnectionIsClosed

                if (e.InnerException.GetType().ToString().Equals("System.Net.Sockets.SocketException"))
                {
                    log.Info(String.Format("Attempt Retry Query after keep-alive termination"));
                    // One shot retry i case the keep-alive was closed
                    return QueryGCal(query, user, modifiedSince);
                }
                else
                {
                    throw e;
                }
            }

        }

        private EventFeed QueryGCal(FeedQuery query, string userName, DateTime modifiedSince)
        {
            if (log.IsInfoEnabled)
                log.Info(String.Format("Querying GCal for User '{0}': {1}", userName, query.Uri));

            EventFeed feed = null;

            try
            {
              using (BlockTimer bt = new BlockTimer("QueryGCal"))
              {
                feed = service.Query(query, modifiedSince) as EventFeed;
              }

              LogResponse(feed, userName);
            }
            catch (GDataNotModifiedException e)
            {
              // Content was not modified
              log.InfoFormat(
                  "NotModified: {0}",
                  e.Message);
            }

            return feed;
        }

        private void LogResponse(EventFeed feed, string userName)
        {
            if ( !string.IsNullOrEmpty(logDirectory) && feed != null )
            {
                string fileName = string.Format(
                    "{0}{1}-{2}.log",
                    logDirectory,
                    DateTime.Now.ToString( "yyyyMMdd'.'HHmmss" ),
                    userName );

                using ( FileStream fs = new FileStream( fileName, FileMode.OpenOrCreate ) )
                {
                    feed.SaveToXml( fs );
                }
            }
        }
    }
}
