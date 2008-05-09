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
using System.Configuration;
using System.Data;
using System.DirectoryServices;
using System.Net;
using System.Text;

using Google.GCalExchangeSync.Library.Util;

using log4net;

namespace Google.GCalExchangeSync.Library
{
    /// <summary>
    /// Class for locating exchange users in Active Directory
    /// </summary>
    public class ActiveDirectoryService : IDisposable
    {
        /// <summary>
        /// Logger for ActiveDirectoryService
        /// </summary>
        protected static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(typeof(ActiveDirectoryService));
        private static readonly int LDAP_PAGE_SIZE = 500;
        private DirectoryEntry activeDirectory;

        /// <summary>The Active Directory server URL</summary>
        public string LDAPServerUrl
        {
            get { return String.Format(ConfigCache.DomainController); }
        }

        /// <summary>
        /// Constructor for the ActiveDirectoryService.
        /// </summary>
        public ActiveDirectoryService()
        {
            activeDirectory = GetDirectoryEntry( LDAPServerUrl );
        }

        /// <summary>
        /// Get the directory entry associated with path
        /// </summary>
        /// <param name="path">Active directory path</param>
        /// <returns>DirectoryEntry coresponding to the user</returns>
        private DirectoryEntry GetDirectoryEntry(string path)
        {
            return new DirectoryEntry(
                path,
                ConfigCache.DomainUserLogin,
                ConfigCache.DomainUserPassword);
        }

        /// <summary>
        /// Returns an ArrayList of ActiveDirectory users for matching mailboxes
        /// </summary>
        /// <param name="attribute">ActiveDirectory attr to search for</param>
        /// <param name="terms">Array of seach terms</param>
        /// <returns>An active directoru result set</returns>
        public SearchResultCollection SearchDirectoryByAttribute(
            string attribute, params string[] terms)
        {
            if (log.IsInfoEnabled)
            {
                log.InfoFormat(
                    "Searching Active Directory [Server={0}, User={1}]",
                    this.LDAPServerUrl,
                    ConfigCache.DomainUserLogin );
            }

            StringBuilder sb = new StringBuilder("(");
            
            // Build the LDAP query
            if (terms.Length > 0)
            {
                if (terms.Length > 1)
                {
                    sb.Append( "|" );
                    foreach (string searchTerm in terms)
                    {
                        sb.AppendFormat("({0}={1})", attribute, searchTerm);
                    }
                }
                else
                {
                    sb.AppendFormat("{0}={1}", attribute, terms[0]);
                }
            }
            else
            {
                sb.AppendFormat("{0}=*", attribute);
            }

            sb.Append(")");
            
            try
            {
                return SearchDirectory(sb.ToString());
            }
            catch (Exception ex)
            {
                throw new GCalExchangeException(
                    GCalExchangeErrorCode.ActiveDirectoryError,
                    "Error while querying Active Directory for exchange users.",
                    ex);
            }
        }

        /// <summary>
        /// Search ActiveDirectory using an LDAP filter expression
        /// </summary>
        /// <param name="ldapFilter">The LDAP filter expression to use</param>
        /// <returns>The ActiveDirectory result set for the query</returns>
        public SearchResultCollection SearchDirectory( string ldapFilter )
        {
            StringBuilder sb = new StringBuilder();

            if (!ldapFilter.StartsWith( "(" ))
            {
                sb.Append("(");
            }

            sb.Append(ldapFilter);

            if (!ldapFilter.EndsWith(")"))
            {
                sb.Append(")");
            }

            string fullQuery = String.Format(
                "(&{0}(|(objectcategory=user)(objectcategory=contact)))", 
                sb.ToString());

            if (log.IsInfoEnabled)
            {
                log.InfoFormat("Issuing LDAP query '{0}'", fullQuery);
            }
            
            // Create the directory searcher and set the scope
            DirectorySearcher searcher = new DirectorySearcher( fullQuery ); 
            searcher.SearchRoot = activeDirectory;
            searcher.SearchScope = SearchScope.Subtree;
            searcher.PageSize = LDAP_PAGE_SIZE;

            SearchResultCollection matches = searcher.FindAll();

            if ( log.IsInfoEnabled )
            {
                log.InfoFormat(
                    "Found {0} users in Active Directory.", matches.Count);
            }
            
            return matches;
        }

        /// <summary>
        /// Validate if a user exists in the AD by email ID
        /// </summary>
        /// <param name="email">Mail ID to check for</param>
        /// <returns>True if the user exists, false otherwise</returns>
        public bool UserExists(string email)
        {
            SearchResultCollection results = 
                SearchDirectoryByAttribute(email, "mail");
            return (results != null && results.Count != 0);
        }

        /// <summary>
        /// Get a user from Active directory - a GCalExchangeException is thrown
        /// if the user does not exist
        /// </summary>
        /// <param name="userName">Mail ID to check for</param>
        /// <returns>DirectoryEntry for the user</returns>
        public DirectoryEntry GetUser(string userName)
        {
            SearchResultCollection results = 
                SearchDirectoryByAttribute(userName, "mail");

            if (results == null || results.Count != 1)
            {
                string errorMessage = string.Format(
                    "Unable to find user '{0}' in Active Directory.",
                    userName );
                
                throw new GCalExchangeException( 
                    GCalExchangeErrorCode.ActiveDirectoryError, 
                    errorMessage );
            }
            else
            {
                return results[0].GetDirectoryEntry();
            }
        }

        /// <summary>
        /// Dispose method of IDisposable
        /// </summary>
        public void Dispose()
        {
            if (activeDirectory != null)
            {
                activeDirectory.Close();
            }
        }
    }
}
