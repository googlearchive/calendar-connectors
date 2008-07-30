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
using System.Threading;
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
    /// Interface to throttle connections made to Google, if failures occur.
    /// </summary>
    public interface IConnectionThrottle
    {
        /// <summary>
        /// Report a success to the throttler. Should be called after successfully getting a feed.
        /// </summary>
        /// <returns>void</returns>
        void ReportSuccess();

        /// <summary>
        /// Report a failure to the throttler. Should be called after failing to get a feed.
        /// </summary>
        /// <returns>void</returns>
        void ReportFailure();

        /// <summary>
        /// Ask the throttler to wait as necessary before new connection.
        /// Should be called before asking for a feed.
        /// </summary>
        /// <returns>void</returns>
        void WaitBeforeNewConnection();
    };

    /// <summary>
    /// Class to throttle connections made to Google, if failures occur.
    /// </summary>
    public class ConnectionThrottle : IConnectionThrottle
    {
        private static readonly int kMaxDeviation = 33;
        private static readonly int kMaxMaxDelay = 0x5555555; // This is approximately 24 hours.
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(typeof(ConnectionThrottle));

        private int delay = 0;
        private int minDelay = 1000;
        private int maxDelay = 30 * 65536;
        private int multiplier = 2;
        private object lockObject = new object();
        private Random random = new Random();

        /// <summary>
        /// Create a throttler with default settings.
        /// </summary>
        public ConnectionThrottle()
        {
        }

        /// <summary>
        /// Create a throttler with specific settings.
        /// </summary>
        /// <param name="desiredMinDelay">The minimal time in milliseconds to wait when throttling</param>
        /// <param name="desiredMaxDelay">The maximum time in milliseconds to wait when throttling</param>
        /// <param name="desiredMultiplier">The multiplier used to increase the wait upon more failures</param>
        public ConnectionThrottle(
            int desiredMinDelay,
            int desiredMaxDelay,
            int desiredMultiplier)
        {
            if (desiredMinDelay > 0 && desiredMinDelay < desiredMaxDelay)
            {
                minDelay = desiredMinDelay;
            }
            if (desiredMaxDelay >= this.minDelay && desiredMaxDelay < kMaxMaxDelay)
            {
                maxDelay = desiredMaxDelay;
            }
            if (desiredMultiplier > 0)
            {
                multiplier = desiredMultiplier;
            }
        }

        /// <summary>
        /// Report a success to the throttler. Should be called after successfully getting a feed.
        /// </summary>
        /// <returns>void</returns>
        public void ReportSuccess()
        {
            log.DebugFormat("Success reported, resetting the delay to 0");
            lock (lockObject)
            {
                delay = 0;
            }
        }

        /// <summary>
        /// Report a failure to the throttler. Should be called after failing to get a feed.
        /// </summary>
        /// <returns>void</returns>
        public void ReportFailure()
        {
            log.DebugFormat("Failure reported, increasing the delay as appropriate");
            int deviation = GenerateDeviation();

            lock (lockObject)
            {
                if (delay == 0)
                {
                    delay = minDelay;
                }
                else
                {
                    delay *= multiplier;
                    // The long cast is necessary for max delays bigger than 6 hours,
                    // but better safe than sorry.
                    delay = (int)(((long)delay * deviation) / 100);
                    if (delay > maxDelay)
                    {
                        delay = maxDelay;
                    }
                    else
                    {
                        // With multiplier of 1, it is possible to deviate below the minimum,
                        // which should be prevented.
                        if (delay < minDelay)
                        {
                            delay = minDelay;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Ask the throttler to wait as necessary before new connection.
        /// Should be called before asking for a feed.
        /// </summary>
        /// <returns>void</returns>
        public void WaitBeforeNewConnection()
        {
            int currentDelay = 0;

            lock (lockObject)
            {
                currentDelay = delay;
            }

            if (currentDelay != 0)
            {
                log.DebugFormat("Sleeping for {0} ms", currentDelay);
                Thread.Sleep(currentDelay);
            }
        }

        private int GenerateDeviation()
        {
            int number = random.Next(2 * kMaxDeviation + 1);

            // Number is in [0, 2 * kMaxDeviation] shift it to [-kMaxDeviation, kMaxDeviation]
            number -= kMaxDeviation;

            // For pure paranoia or if someone changes kMaxDeviation, make sure number is in (-100, 100]
            if (number < -99)
            {
                number = -99;
            }
            else
            {
                if (number > 100)
                {
                    number = 100;
                }
            }

            // Shift to [100 - kMaxDeviation, 100 + kMaxDeviation], so multiplying works
            number += 100;

            log.DebugFormat("Generated deviation of {0}%", number);

            return number;
        }
    };

    /// <summary>
    /// Wrapper for behaviour about Google Calendar
    /// </summary>
    public class GCalGateway
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(typeof(GCalGateway));

        private static readonly IConnectionThrottle defaultConnectionThrottler = new ConnectionThrottle();

        private string googleAppsLogin;
        private string googleAppsPassword;
        private string googleAppsDomain;
        private string logDirectory;
        private CalendarService service;
        private static IConnectionThrottle connectionThrottler;

        private static readonly string AgentIdentifier = "Google Calendar Connector Sync/1.0.0";

        /// <summary>
        /// Create a gateway for talking to Google Calendar
        /// </summary>
        /// <param name="login">User login to use</param>
        /// <param name="password">User credential to use</param>
        /// <param name="domain">Google Apps Domain to user</param>
        /// <param name="throttler">Optional connection throttler to use</param>
        public GCalGateway(
            string login,
            string password,
            string domain,
            IConnectionThrottle throttler)
        {
            this.googleAppsLogin = login;
            this.googleAppsPassword = password;
            this.googleAppsDomain = domain;
            if (throttler != null)
            {
                connectionThrottler = throttler;
            }
            else
            {
                connectionThrottler = defaultConnectionThrottler;
            }

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
            {
                sb.Append("/");
            }

            sb.AppendFormat("feeds/{0}/", user);

            switch (visibility)
            {
                case GCalVisibility.Public:
                    sb.Append("public/");
                    break;

                case GCalVisibility.Private:
                default:
                    sb.Append("private/");
                    break;
            }

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

            if (window.End != DateTime.MaxValue)
            {
                query.EndTime = window.End;
            }

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
                    throw;
                }
            }

        }

        private EventFeed QueryGCal(FeedQuery query, string userName, DateTime modifiedSince)
        {
            if (log.IsInfoEnabled)
                log.Info(String.Format("Querying GCal for User '{0}': {1}", userName, query.Uri));

            EventFeed feed = null;

            // Wait as necessary before making new request.

            connectionThrottler.WaitBeforeNewConnection();

            try
            {
                using (BlockTimer bt = new BlockTimer("QueryGCal"))
                {
                    feed = service.Query(query, modifiedSince) as EventFeed;
                }

            }
            catch (GDataNotModifiedException e)
            {
                // Content was not modified
                log.InfoFormat("NotModified: {0}", e.Message);
            }
            catch (Exception)
            {
                // Report a failure, regardless of the exception, as long as it is not GDataNotModifiedException.
                // This could be a bit overly aggressive, but it is hard to make bets
                // what exception was caught and rethrown in GData and what was let to fly.
                connectionThrottler.ReportFailure();

                throw;
            }

            // Everything went well, report it.
            // Note this is valid even when we caught GDataNotModifiedException.
            connectionThrottler.ReportSuccess();

            LogResponse(feed, userName);

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
