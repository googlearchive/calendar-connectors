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

using Google.GData.Calendar;
using Google.GData.Client;
using Google.GData.Extensions;
using Google.GCalExchangeSync.Library.Util;
using Google.GCalExchangeSync.Library.Scheduling;

using TZ4Net;
using log4net;

namespace Google.GCalExchangeSync.Library
{
    /// <summary>
    /// Synchonization of Google Calendar and Exchange accounts
    /// </summary>
    public class GCalSyncProcess
    {
        /// <summary>
        /// Logger for GCalSyncProcess
        /// </summary>
        protected static readonly log4net.ILog log =
          log4net.LogManager.GetLogger( typeof( GCalSyncProcess ) );

        private ExchangeService exchangeGateway;
        private GCalGateway gcalGateway;
        private List<ExchangeUser> exchangeUsers;
        private IFreeBusyWriter freeBusyWriter = null;

        private int errorCount = 0;
        private int errorThreshold = 0;
        private object lockObject = new object();

        private ModifiedDateUtil modifiedDateUtil;

        private string googleAppsLogin;
        private string googleAppsPassword;
        private string googleAppsDomain;
        private string exchangeServer;
        private string networkLogin;
        private string networkPassword;

        /// <summary>
        /// Ctor for a GCalSyncProcess - all configuration comes from the .config file settings
        /// </summary>
        public GCalSyncProcess() : this(
            ConfigCache.GoogleAppsLogin,
            ConfigCache.GoogleAppsPassword,
            ConfigCache.GoogleAppsDomain,
            ConfigCache.ExchangeServerUrl,
            ConfigCache.ExchangeAdminLogin,
            ConfigCache.ExchangeAdminPassword )
        {
        }

        /// <summary>
        /// Ctor for a GCalSyncProcess with the given settings
        /// </summary>
        /// <param name="googleAppsLogin">Google Apps login</param>
        /// <param name="googleAppsPassword">Google Apps password</param>
        /// <param name="googleAppsDomain">Google Apps domain to sync</param>
        /// <param name="exchangeServer">Exchange server URL</param>
        /// <param name="networkLogin">Exchange server admin login</param>
        /// <param name="networkPassword">Exchange server admin password</param>
        public GCalSyncProcess( string googleAppsLogin, string googleAppsPassword,
            string googleAppsDomain, string exchangeServer, string networkLogin, string networkPassword )
        {
            this.googleAppsLogin = googleAppsLogin;
            this.googleAppsPassword = googleAppsPassword;
            this.googleAppsDomain = googleAppsDomain;
            this.exchangeServer = exchangeServer;
            this.networkLogin = networkLogin;
            this.networkPassword = networkPassword;
        }

        /// <summary>
        /// Executes the sync from GCalender to Exchange
        /// </summary>
        public void RunSyncProcess()
        {
            RunSyncProcess( ConfigCache.ServiceThreadCount );
        }

        /// <summary>
        /// Run the sync process from Google Calendar to Exchange
        /// </summary>
        /// <param name="threadCount">Number of threads to use to sync</param>
        public void RunSyncProcess( int threadCount )
        {
            log.Info( "Exchange synchronization process started." );

            using ( BlockTimer bt = new BlockTimer( "RunSyncProcess" ) )
            {
                gcalGateway =
                    new GCalGateway(googleAppsLogin, googleAppsPassword, googleAppsDomain);
                exchangeGateway =
                    new ExchangeService(exchangeServer, networkLogin, networkPassword );

                ExchangeUserDict users;

                using ( BlockTimer loadUserTimer = new BlockTimer( "LoadUserList" ) )
                {
                    users = QueryExchangeForValidUsers();
                }

                exchangeUsers = new List<ExchangeUser>();

                freeBusyWriter = FreeBusyWriterFactory.GetWriter( exchangeUsers );

                foreach ( ExchangeUser user in users.Values )
                {
                    exchangeUsers.Add( user );
                }

                if ( exchangeUsers.Count == 0 )
                {
                    log.Warn( "No eligible users found for synchronization.  Aborting sync process." );
                    return;
                }

                if ( threadCount < 1 )
                    threadCount = 1;

                modifiedDateUtil = new ModifiedDateUtil( ConfigCache.ServiceModifiedXmlFileName );
                modifiedDateUtil.LoadModifiedTimes();

                if ( threadCount == 1 )
                {
                    SyncUsers();
                }
                else
                {
                    StartSyncUserThreads( threadCount );
                }

                modifiedDateUtil.PersistModifiedTimes();

                gcalGateway = null;
                exchangeGateway = null;
                freeBusyWriter = null;
                modifiedDateUtil = null;
                exchangeUsers = null;

                System.GC.Collect();
                log.DebugFormat("Memory after sync: {0}", System.GC.GetTotalMemory(false));
            }
        }

        private void StartSyncUserThreads( int threadCount )
        {
            log.InfoFormat(
                "Starting multithreaded sync process.  [ThreadCount={0}]", threadCount );

            System.Net.ServicePointManager.DefaultConnectionLimit = 2 * threadCount;

            Thread[] workers = new Thread[threadCount];

            for ( int i = 0; i < threadCount; i++ )
            {
                workers[i] = new Thread( SyncUsers );
                workers[i].Name = "GCalSync.Worker." + i.ToString();
                workers[i].Start();
            }

            for ( int i = 0; i < threadCount; i++ )
            {
                workers[i].Join();
            }
        }

        /// <summary>
        /// Perform sync of users from Google Calendar to Exchnage until there are no more
        /// users to sync. This can be called from a worker thread.
        /// </summary>
        public void SyncUsers()
        {
            ExchangeUser user;
            int userCount = 0;
            string login = string.Empty;

            errorThreshold = ConfigCache.ServiceErrorCountThreshold;
            errorCount = 0;

            while ( exchangeUsers.Count > 0 )
            {
                lock ( lockObject )
                {
                    if ( exchangeUsers.Count == 0 )
                    {
                        break;
                    }
                    else
                    {
                        user = exchangeUsers[0];
                        exchangeUsers.RemoveAt( 0 );
                    }
                }

                try
                {
                    userCount++;

                    login = user.Email.ToLower();

                    DateTime modifiedDate = modifiedDateUtil.GetModifiedDateForUser(login);
                    DateTime currentDate = DateUtil.NowUtc;

                    // Pick a window to synchronize for:
                    //
                    // [-N, +N] days where N is settable in the config file
                    //
                    // Scanning back in time is necessary so that we pickup changes to meetings and events that were
                    // made invisible.
                    //
                    // TODO: The window we're syncing for should also be used when we modify events in Exchange
                    // so we only modify events in the window.

                    DateTime start = currentDate.AddDays(-ConfigCache.GCalSyncWindow);
                    DateTime end = currentDate.AddDays(ConfigCache.GCalSyncWindow);
                    DateTimeRange syncWindow = new DateTimeRange(start, end);

                    log.InfoFormat("Processing user {0} for {1}", login, syncWindow);

                    EventFeed feed = gcalGateway.QueryGCal(
                        user.Email,
                        GCalVisibility.Private,
                        GCalProjection.Full,
                        modifiedDate,
                        syncWindow);

                    // if feed is null, then that means no calendar items changed for the user since we last queried.
                    if ( feed == null )
                    {
                        log.DebugFormat(
                            "Calendar has not changed for user {0}.  User will not be synced this round.",
                            login );

                        continue;
                    }

                    if ( !ValidateFeed( feed ) )
                    {
                        /* No Google App Feed was returned,  skip to next user */
                        log.WarnFormat(
                            "GCal feed could not be read for '{0}'.  This user may not have activated their account or may be inactive.",
                            login );

                        continue;
                    }

                    log.InfoFormat("Calendar Query returned {0} events", feed.Entries.Count);

                    using (BlockTimer freeBusyTimer = new BlockTimer("WriteFreeBusy"))
                    {
                        /* User and feed retrieval was succesful, merge the two datasources down into Exchange */
                        freeBusyWriter.SyncUser( user, feed, exchangeGateway, syncWindow );
                    }

                    // Only update the modified time if we sync'd
                    modifiedDateUtil.UpdateUserModifiedTime(login, currentDate);
                }
                catch ( Exception ex )
                {
                    Interlocked.Increment( ref errorCount );

                    log.Error( string.Format(
                        "Error occured while executing sync process for user '{0}'. [running error count={1}]", login, errorCount ),
                        ex );
                }

                if ( errorCount > errorThreshold )
                    throw new GCalExchangeException(
                        GCalExchangeErrorCode.GenericError,
                        "Error threshold has been surpassed, aborting sync process." );

            }

            log.InfoFormat( "User synchronization complete.  {0} users processed.", userCount );
        }

        private bool ValidateFeed( EventFeed feed )
        {
            bool isValid = true;

            if ( feed == null )
            {
                isValid = false;
            }
            else
            {
                if ( feed.TimeZone == null )
                {
                    isValid = false;
                }
            }

            return isValid;
        }

        private ExchangeUserDict QueryExchangeForValidUsers()
        {
            ExchangeUserDict users = null;

            if ( string.IsNullOrEmpty( ConfigCache.ServiceLDAPUserFilter ) )
            {
                users = exchangeGateway.RetrieveAllExchangeUsers();
            }
            else
            {
                users = exchangeGateway.QueryActiveDirectory( ConfigCache.ServiceLDAPUserFilter );
            }

            return users;
        }
    }
}
