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
using System.Web;

namespace Google.GCalExchangeSync.Library.Util
{
    /// <summary>
    /// Tools for Generating Free Busy URLs for Exchange
    /// </summary>
    public class FreeBusyUrl
    {
        /// <summary>
        /// Generate a URL for the users free busy appointments
        /// </summary>
        /// <param name="exchangeServerUrl">The Exchange Server to use</param>
        /// <param name="adminGroup">The group the user is in</param>
        /// <param name="userAlias">Alias for the user</param>
        /// <returns>The URL for the users appointments</returns>
        public static string GenerateUrl( string exchangeServerUrl, string adminGroup, string userAlias )
        {
            return string.Format(
                "{0}/public/NON_IPM_SUBTREE/SCHEDULE%2B%20FREE%20BUSY/EX:{1}/USER-{2}.EML",
                exchangeServerUrl,
                ExchangeEncode(adminGroup),
                ExchangeEncode(string.Format("/cn=RECIPIENTS/cn={0}", userAlias)));
        }

        /// <summary>
        /// Generate a Free Busy URL from the legacy Exchange distinguished name - legacy
        /// exchange names are of the form:
        ///     /o={ORG}/ou={ORG-UNIT}/cn={CATEGORY}/cn={USERS CN}
        /// </summary>
        /// <param name="exchangeServerUrl">The exchange server to use</param>
        /// <param name="legacyExchangeDN">The legacy exchange distingished name</param>
        /// <returns>The URL for the users appointments</returns>
        public static string GenerateUrlFromDN( string exchangeServerUrl, string legacyExchangeDN )
        {
            string adminGroup = 
                legacyExchangeDN.Substring( 0, legacyExchangeDN.IndexOf( "/cn" ) );
            
            string userAlias = 
                legacyExchangeDN.Substring( legacyExchangeDN.IndexOf( "/cn" ) );

            return string.Format(
                "{0}/public/NON_IPM_SUBTREE/SCHEDULE%2B%20FREE%20BUSY/EX:{1}/USER-{2}.EML",
                exchangeServerUrl,
                ExchangeEncode( adminGroup ),
                ExchangeEncode( userAlias ) );
        }

        /// <summary>
        /// Generate a URL for the appointments within an Admin group
        /// </summary>
        /// <param name="exchangeServerUrl">The exchange server to use</param>
        /// <param name="adminGroup">The admin group</param>
        /// <returns>The URL to use to get the appointments for an admin group</returns>
        public static string GenerateAdminGroupUrl( string exchangeServerUrl, string adminGroup )
        {
            return string.Format(
                "{0}/public/NON_IPM_SUBTREE/SCHEDULE%2B%20FREE%20BUSY/EX:{1}/",
                exchangeServerUrl,
                ExchangeEncode( adminGroup ) );
        }

        /// <summary>
        /// Generate a URL for the appointments within an Admin group
        /// </summary>
        /// <param name="exchangeServerUrl">The exchange server to use</param>
        /// <param name="legacyExchangeDN">Get the legacy exchange DN for the group</param>
        /// <returns>The URL to use to get the appointments for an admin group</returns>
        public static string GenerateAdminGroupUrlFromDN(string exchangeServerUrl, string legacyExchangeDN)
        {
            string adminGroup = 
                legacyExchangeDN.Substring( 0, legacyExchangeDN.IndexOf( "/cn" ) );

            return GenerateAdminGroupUrl( exchangeServerUrl, adminGroup );
        }

        /// <summary>
        /// Generate a URL for the new Free Busy Lookup format which is a replacement
        /// for public folders - documented here: http://support.microsoft.com/kb/813268
        /// </summary>
        /// <param name="exchangeServer">The exchange server to use</param>
        /// <param name="users">The set of users to lookup</param>
        /// <param name="range">The datetime range to lookup for</param>
        /// <param name="interval">The time interval to use</param>
        /// <returns>The URL to obtain FB info for the set of users</returns>
        public static string GenerateFreeBusyLookupUrl(
            string exchangeServer, 
            ExchangeUserDict users, 
            DateTimeRange range, 
            int interval)
        {
            string userParams = string.Empty;
            foreach(ExchangeUser user in users.Values)
            {
                userParams += string.Format("&u={0}", user.Email);
            }

            return string.Format(
                "{0}/public/?cmd=freebusy&start={1}&end={2}&interval={3}{4}",
                exchangeServer,
                DateUtil.FormatDateForISO8601(range.Start),
                DateUtil.FormatDateForISO8601(range.End),
                interval,
                userParams);
        }

        private static string ExchangeEncode( string element )
        {
            return HttpUtility.UrlPathEncode( element.Replace( @"/", "_xF8FF_" ) );
        }
    }
}
