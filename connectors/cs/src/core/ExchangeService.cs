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
using System.Configuration;
using System.DirectoryServices;
using System.Net;
using System.Text;

using log4net;

using Google.GCalExchangeSync.Library.Util;
using Google.GCalExchangeSync.Library.WebDav;

namespace Google.GCalExchangeSync.Library
{
    /// <summary>
    /// This class handles read / write requests for Exchange
    /// Free / Busy and Appointment data.
    /// </summary>
    public class ExchangeService
    {
        /// <summary>
        /// Logger for ExchangeService
        /// </summary>
        protected static readonly ILog log =
           LogManager.GetLogger(typeof(ExchangeService));

        private string exchangeServerUrl;

        private AppointmentService appointments;
        private FreeBusyService freebusy;
        private WebDavQuery webDavQuery;

        /// <summary>
        /// Return the appointments gateway
        /// </summary>
        public virtual AppointmentService Appointments
        {
            get { return appointments; }
        }

        /// <summary>
        /// return the FreeBusyService
        /// </summary>
        public virtual FreeBusyService FreeBusy
        {
            get { return freebusy; }
        }

        /// <summary>
        /// The URL of the Exchange server
        /// </summary>
        public string ExchangeServerUrl
        {
            get { return exchangeServerUrl; }
        }

        /// <summary>
        /// The WebDAVQuery service
        /// </summary>
        public WebDavQuery WebDavQuery
        {
            get { return webDavQuery; }
        }

        /// <summary>
        /// Constructor for an Exchange Gateway
        /// </summary>
        /// <param name="exchangeServer">Exchange server address for exchange searches</param>
        /// <param name="networkLogin">Username for an exchange user</param>
        /// <param name="networkPassword">Credentail for an exchange user</param>
        public ExchangeService(string exchangeServer, string networkLogin, string networkPassword)
        {
            exchangeServerUrl = exchangeServer;

            // Set the default connection limit
            ServicePointManager.DefaultConnectionLimit = ConfigCache.ExchangeMaxConnections;

            ICredentials credentials = new NetworkCredential(networkLogin, networkPassword);
            webDavQuery = new WebDavQuery(credentials, "Exchange");
            appointments = new AppointmentService(exchangeServerUrl, webDavQuery);
            freebusy = new FreeBusyService(exchangeServerUrl, webDavQuery, null);
        }

        /// <summary>
        /// Returns a list of Exchange users matching an email address, provides
        /// free busy times and appointments if available within the date range.
        /// </summary>
        /// <param name="utcRange">DateRange for search</param>
        /// <param name="searchTerms">Search terms to search exchange for</param>
        /// <returns></returns>
        public ExchangeUserDict SearchByEmail(
            DateTimeRange utcRange, params string[] searchTerms)
        {
            return Search("mail", utcRange, searchTerms);
        }

        /// <summary>
        /// Returns a list of Exchange users, provides free busy times and
        /// appointments if available within the date range.
        /// </summary>
        /// <param name="ldapAttribute">Parameter to search upon</param>
        /// <param name="utcRange">DateRange for search</param>
        /// <param name="searchTerms">Search terms to search exchange for</param>
        /// <returns></returns>
        public ExchangeUserDict Search(
            string ldapAttribute, DateTimeRange utcRange, params string[] searchTerms)
        {
            if(utcRange.Equals(DateTimeRange.Full))
            {
                throw new Exception("Must specify a time range");
            }

            /* Create the network credentials and WebDav resources needed for user */
            ExchangeUserDict activeDirectoryResults = QueryActiveDirectoryByAttribute(ldapAttribute, searchTerms);

            /* Assign user free busy times if any users were returned */
            if ( activeDirectoryResults != null && activeDirectoryResults.Count > 0 )
            {
                GetCalendarInfoForUsers( activeDirectoryResults, utcRange );
            }
            return activeDirectoryResults;
        }

        /// <summary>
        /// Returns a list of Exchange users, provides free busy times and appointments if available
        /// </summary>
        /// <returns></returns>
        public ExchangeUserDict RetrieveAllExchangeUsers()
        {
            /* Create the network credentials and WebDav resources needed for user */
            return QueryActiveDirectoryByAttribute( "mail" );
        }

        private ExchangeUserDict QueryActiveDirectoryByAttribute(
            string ldapAttribute, params string[] searchTerms )
        {
            SearchResultCollection searchResults = null;

            /* Perform the mailbox search */
            using (ActiveDirectoryService ad = new ActiveDirectoryService())
            {
                searchResults = ad.SearchDirectoryByAttribute( ldapAttribute, searchTerms);
            }

            ExchangeUserDict userCollection = CreateExchangeUserCollection( searchResults );

            LogQueryResults( userCollection, searchTerms, ldapAttribute );

            return userCollection;
        }

        private ExchangeUserDict CreateExchangeUserCollection( SearchResultCollection searchResults )
        {
            ExchangeUserDict userCollection = new ExchangeUserDict();

            if ( searchResults != null )
            {
                /* For each result set in the result set */
                foreach ( System.DirectoryServices.SearchResult result in searchResults )
                {
                    /* Extract the property collection and create a new exchange user with it
                     * Add the new user to the result set and use the account name as the index for
                     * the dictionary collection */
                    ResultPropertyCollection property = result.Properties;
                    ExchangeUser user = new ExchangeUser( property );

                    if ( !user.IsValid )
                    {
                        log.WarnFormat( "User '{0}' is invalid and will not be synchronized.", user.CommonName );
                    }
                    else if ( userCollection.ContainsKey( user.Email.ToLower() ) )
                    {
                        log.WarnFormat( "User '{0}' was returned multiple times in the LDAP query. " +
                            "Only the first instance was added.", user.Email );
                    }
                    else
                    {
                        userCollection.Add( user.Email.ToLower(), user );
                        log.InfoFormat("Found and added '{0}' as an ExchangeUser.", user.Email);
                    }

                    log.DebugFormat("LDAP object debug info: {0}", user);
                }
            }

            return userCollection;
        }

        /// <summary>
        /// Query Active Directory and return all users matching the LDAP query
        /// </summary>
        /// <param name="ldapFilter">An LDAP query to match users against</param>
        /// <returns>The set of users matching the query</returns>
        public ExchangeUserDict QueryActiveDirectory( string ldapFilter )
        {
            SearchResultCollection searchResults = null;

            /* Perform the mailbox search */
            using (ActiveDirectoryService ad = new ActiveDirectoryService())
            {
                searchResults = ad.SearchDirectory( ldapFilter );
            }

            ExchangeUserDict exchangeUsers = CreateExchangeUserCollection( searchResults );

            return exchangeUsers;
        }

        private void LogQueryResults(
            ExchangeUserDict userCollection, string[] searchTerms, string ldapAttribute )
        {
            if ( userCollection.Count != searchTerms.Length )
            {
                if ( ldapAttribute == "sAMAccountName" )
                {
                    foreach ( string term in searchTerms )
                    {
                        if ( !userCollection.ContainsKey( term.ToLower() ) )
                        {
                            log.InfoFormat( "Unable to find Active Directory user where '{0}'='{1}'.", ldapAttribute, term );
                        }
                    }
                }
                else if ( ldapAttribute == "mail" )
                {
                    foreach ( string email in searchTerms )
                    {
                        string login = email.Split( '@' )[ 0 ];

                        if ( !userCollection.ContainsKey( login.ToLower() ) )
                        {
                            log.InfoFormat( "Unable to find Active Directory user where '{0}'='{1}'.", ldapAttribute, email );
                        }
                    }
                }
                else
                {
                    log.InfoFormat(
                        "Unable to find all users in Active Directory.  [Users={0}]",
                        string.Join( ";", searchTerms ) );
                }
            }
        }

        /// <summary>
        /// Assigns free busy times to the exchange users that are passed into the method
        /// </summary>
        public virtual void GetCalendarInfoForUser( ExchangeUser exchangeUser )
        {
            GetCalendarInfoForUser( exchangeUser, DateTimeRange.Full );
        }

        /// <summary>
        /// Assigns free busy times to the exchange users that are passed into the method
        /// </summary>
        public virtual void GetCalendarInfoForUser(ExchangeUser user, DateTimeRange window)
        {
            ExchangeUserDict users = new ExchangeUserDict();
            users.Add(user.Email, user);

            GetCalendarInfoForUsers(users, window);
        }

        /// <summary>
        /// Assigns free busy times to the exchange users that are passed into the method
        /// </summary>
        public virtual void GetCalendarInfoForUsers(ExchangeUserDict users, DateTimeRange window)
        {
            // Perform  appointment lookup in parallel with FB lookup
            using (AppointmentLookupFuture future =
                new AppointmentLookupFuture(this, users, window))
            {
                Dictionary<ExchangeUser, FreeBusy> freeBusyBlocks =
                    freebusy.LookupFreeBusyTimes(users, window);

                foreach (ExchangeUser user in users.Values)
                {
                    /* Retrieve the free busy blocks */
                    FreeBusy freeBusy = freeBusyBlocks[user];

                    user.AccessLevel = GCalAccessLevel.FreeBusyAccess;

                    List<Appointment> appointments = future.getResult(user);

                    MergeFreeBusyWithAppointments(
                        user,
                        freeBusy,
                        appointments,
                        window.Start,
                        window.End);
                }
            }
        }

        /// <summary>
        /// Combines the free busy and appointment blocks supplied to the exchange user object
        /// If no appointments are supplied the user will still have free busy time blocks assigned
        /// to them, with a null appointment assigned to the free busy time.
        /// </summary>
        /// <param name="exchangeUser">Exchange users to apply freeBusy and appointments</param>
        /// <param name="freeBusy">The collection of FreeBusy blocks to assign to exchangeUser</param>
        /// <param name="appointments">The collection of appointment blocks to assign to exchangeUser</param>
        /// <param name="startDate">Start of the date window to merge for</param>
        /// <param name="endDate">End of the date window to merge for</param>
        protected void MergeFreeBusyWithAppointments(
            ExchangeUser exchangeUser,
            FreeBusy freeBusy,
            List<Appointment> appointments,
            DateTime startDate,
            DateTime endDate)
        {
            using (BlockTimer bt = new BlockTimer("MergeFreeBusyWithAppointments"))
            {
                IntervalTree<FreeBusyTimeBlock> freeBusyIntervals =
                    new IntervalTree<FreeBusyTimeBlock>();
                FreeBusyCollection busyTimes = new FreeBusyCollection();
                int appointmentsCount = 0;

                /* Add the date ranges from each collection in the FreeBusy object */
                foreach (DateTimeRange range in freeBusy.All)
                {
                    /* If the free busy date is between the start and end dates request */
                    if (DateUtil.IsWithinRange(range.Start, startDate, endDate) ||
                        DateUtil.IsWithinRange(range.End, startDate, endDate))
                    {
                        /* Add the a new FreeBusyTimeBlock to the collection, with a status of Busy,
                         * Set the key to the start date of the block */
                        FreeBusyTimeBlock block = new FreeBusyTimeBlock(range);

                        if (!busyTimes.ContainsKey(range.Start))
                        {
                            busyTimes.Add(range.Start, block);
                            freeBusyIntervals.Insert(range, block);
                        }
                    }
                }

                if (appointments != null && appointments.Count > 0)
                {
                    appointmentsCount = appointments.Count;
                    foreach (Appointment appt in appointments)
                    {
                        DateTimeRange range = new DateTimeRange(appt.StartDate, appt.EndDate);
                        List<FreeBusyTimeBlock> result = freeBusyIntervals.FindAll(range);

                        log.DebugFormat("Appt added: {0} {1}", appt.Range, appt.StartDate.Kind);
                        log.DebugFormat("Found {0} in [{1}] with {2} FB", result.Count, range, result.Count);
                        foreach (FreeBusyTimeBlock block in result)
                        {
                            log.DebugFormat("FB: {0} {1}", block.Range, block.StartDate.Kind);
                            block.Appointments.Add(appt);
                        }

                        busyTimes.Appointments.Add(appt);
                    }
                }

                log.InfoFormat("Merge Result of FB {0} + Appointment {1} -> {2}",
                               freeBusy.All.Count,
                               appointmentsCount,
                               busyTimes.Count);

                /* Assign the data structure to the exchange user */
                exchangeUser.BusyTimes = busyTimes;
            }
        }

        /// <summary>
        /// Determine if the URL exists
        /// </summary>
        /// <param name="url">The URL to check</param>
        /// <returns>True if the URL exists</returns>
        public bool DoesUrlExist( string url )
        {
            bool urlExists = true;

            try
            {
                webDavQuery.IssueRequestIgnoreResponse(url, Method.GET, string.Empty);
            }
            catch ( WebException we )
            {
                HttpWebResponse response = (HttpWebResponse)we.Response;

                if ( response.StatusCode == HttpStatusCode.NotFound )
                {
                    urlExists = false;
                }
                else
                {
                    throw;
                }
            }

            return urlExists;
        }
    }
}
