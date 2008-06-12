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
using System.DirectoryServices;

using Google.GCalExchangeSync.Library.Util;
using TZ4Net;

namespace Google.GCalExchangeSync.Library
{
    /// <summary>
    /// Represent an Exchange User
    /// </summary>
    public class ExchangeUser
    {
        /// <summary>
        /// Loggr for the ExchangeUser
        /// </summary>
        protected static readonly log4net.ILog log =
           log4net.LogManager.GetLogger(typeof(ExchangeService));

        private string mailnickname = "";
        private string proxyAddresses = "";
        private string email = "";
        private string accountName = "";
        private string commonName = "";
        private string legacyExchangeDN = "";
        private string freeBusyCommonName = "";
        private string DN = "";
        private string objectClasses = "";
        private string displayName = "";
        private bool isValid = true;
        private GCalAccessLevel accessLevel = 0;
        private FreeBusyCollection busyTimesBlocks;
        private FreeBusyCollection tentativeTimesBlocks;
        private bool haveAppointmentInfo = false;

        /// <summary>
        /// Nickname for the Exchange User
        /// </summary>
        public string MailNickname
        {
            get { return this.mailnickname; }
            set { this.mailnickname = value; }
        }

        /// <summary>
        /// ProxyAddress
        /// </summary>
        public string ProxyAddresses
        {
            get { return this.proxyAddresses; }
            set { this.proxyAddresses = value; }
        }

        /// <summary>
        /// Email address for the user
        /// </summary>
        public string Email
        {
            get { return this.email; }
            set { this.email = value; }
        }

        /// <summary>
        /// Account name
        /// </summary>
        public string AccountName
        {
            get { return this.accountName; }
            set { this.accountName = value; }
        }

        /// <summary>
        /// Common Name
        /// </summary>
        public string CommonName
        {
            get { return this.commonName; }
            set { this.commonName = value; }
        }

        /// <summary>
        /// Access level
        /// </summary>
        public GCalAccessLevel AccessLevel
        {
            get { return this.accessLevel; }
            set { this.accessLevel = value; }
        }

        /// <summary>
        /// Busy times for this user
        /// </summary>
        public FreeBusyCollection BusyTimes
        {
            get { return this.busyTimesBlocks; }
            set { this.busyTimesBlocks = value; }
        }

        /// <summary>
        /// Tentative times for this user
        /// </summary>
        public FreeBusyCollection TentativeTimes
        {
            get { return this.tentativeTimesBlocks; }
            set { this.tentativeTimesBlocks = value; }
        }

        /// <summary>
        /// Legacy exchange distinguished name
        /// </summary>
        public string LegacyExchangeDN
        {
            get { return this.legacyExchangeDN; }
            set { this.legacyExchangeDN = value; }
        }

        /// <summary>
        /// Free Busy Common Name
        /// </summary>
        public string FreeBusyCommonName
        {
            get { return this.freeBusyCommonName; }
        }

        /// <summary>
        /// Display name
        /// </summary>
        public string DisplayName
        {
            get { return this.displayName; }
        }

        /// <summary>
        /// Is the user valid?
        /// </summary>
        public bool IsValid
        {
            get { return this.isValid; }
        }


        /// <summary>
        /// Have appointment detail for this user
        /// </summary>
        public bool HaveAppointmentDetail
        {
            get { return this.haveAppointmentInfo; }
            set { this.haveAppointmentInfo = value; }
        }

        /// <summary>
        /// Ctor for an empty Exchange User
        /// </summary>
        public ExchangeUser()
        {
        }

        /// <summary>
        /// Ctor for an Exchange user from an Active Directory ResultSet
        /// </summary>
        /// <param name="properties">Active Directory propertes</param>
        public ExchangeUser(ResultPropertyCollection properties)
        {
            this.accountName = GetFirstString(properties["sAMAccountName"]);
            this.mailnickname = GetFirstString(properties["mailnickname"]);
            this.proxyAddresses = GetPrimarySmtpAddress( properties );
            this.email = GetFirstString(properties["mail"]);
            this.commonName = GetFirstString(properties["CN"]);
            this.legacyExchangeDN = GetFirstString( properties[ "legacyExchangeDN" ] );
            this.DN = GetFirstString(properties["distinguishedName"]);
            this.objectClasses = GetStringList(properties["objectclass"]);
            this.displayName = GetFirstString(properties["displayName"]);

            this.freeBusyCommonName = parseFreeBusyCommonName(legacyExchangeDN);
            this.busyTimesBlocks = new FreeBusyCollection();
            this.tentativeTimesBlocks = new FreeBusyCollection();

            AdjustAccountName();
            Validate();
        }

        private string parseFreeBusyCommonName(string legacyDN)
        {
            // The legacyDN is of the form:
            //
            // /o=<Org>/ou=<Org Unit>/cn=<Group common name>/cn=<User Common Name>
            //
            // The Free Busy Common name is the following:
            //
            // USER-/cn=<Group common name>/cn=<User Common Name>

            try
            {
                string[] parts = legacyDN.ToUpper().Split('/');
                int partsLength = parts.Length;

                if (partsLength < 2)
                {
                    log.DebugFormat("Cannot parse legacyDN = \"{0}\"", legacyDN);
                    return string.Empty;
                }

                return string.Format("USER-/{0}/{1}", parts[partsLength - 2], parts[partsLength - 1]);
            }
            catch (Exception e)
            {
                log.DebugFormat("EXCEPTION parseFreeBusyCommonName {0} - {1}", legacyDN, e.Message);
                return string.Empty;
            }
        }

        private void AdjustAccountName()
        {
            if ( string.IsNullOrEmpty( accountName ) )
            {
                accountName = email.Split( '@' )[ 0 ];
            }
        }

        private void Validate()
        {
            if ( string.IsNullOrEmpty( this.accountName ) )
            {
                log.WarnFormat("Invalid user - null or empty sAMAccountName attribute.");

                isValid = false;
            }

            if ( string.IsNullOrEmpty( this.proxyAddresses ) )
            {
                log.WarnFormat("Invalid user - null or empty ProxyAddresses attribute.");

                isValid = false;
            }

            if ( string.IsNullOrEmpty( this.email ) )
            {
                log.WarnFormat("Invalid user - null or empty email attribute.");

                isValid = false;
            }

            if ( string.IsNullOrEmpty( this.legacyExchangeDN ) )
            {
                log.WarnFormat("Invalid user - null or empty legacyExchangeDN attribute.");

                isValid = false;
            }

            if (!isValid)
            {
                log.WarnFormat(
                    "Invalid user LDAP attributes: sAMAccountName='{0}', email='{1}', ProxyAddresses='{2}'",
                    accountName ?? "(null)",
                    email ?? "(null)",
                    proxyAddresses ?? "(null)");
            }
        }

        private string GetFirstString(ResultPropertyValueCollection coll)
        {
            if (coll != null && coll.Count > 0 && coll[0] != null)
                return coll[0].ToString();
            else
                return "";
        }

        private string GetStringList(ResultPropertyValueCollection coll)
        {
          string result = "";
          foreach (string prop in coll)
          {
            if (result.Length > 0)
              result += string.Format(", {0}", prop);
            else result += prop;
          }

          return string.Format("[ {0} ]", result); ;
        }

        private string GetPrimarySmtpAddress(ResultPropertyCollection coll)
        {
            if ( coll == null || coll.Count == 0 )
                return null;

            foreach( string address in coll[ "ProxyAddresses" ] )
            {
                if ( address.StartsWith( "SMTP:" ) )
                    return address;
            }

            return null;
        }

        /// <summary>
        /// Return a string representation of the exchange user
        /// </summary>
        /// <returns>A string representation of the user</returns>
        public override string ToString()
        {
            return string.Format(
                "[ProxyAddresses={0}, sAMAccountName={1}, mailnickname={2}, mail={3}, CN={4}, legacyExchangeDN={5}, DN={6}, objectClass={7}]",
                this.proxyAddresses?? "(null)",
                this.accountName ?? "(null)",
                this.mailnickname ?? "(null)",
                this.email ?? "(null)",
                this.commonName ?? "(null)",
                this.legacyExchangeDN ?? "(null)",
                this.DN ?? "(null)",
                this.objectClasses ?? "(null)"
                );
        }

        /// <summary>
        /// Provide a unique id for the instance
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.CommonName.GetHashCode();
        }

        /// <summary>
        /// Equality test based on Active Directory CN
        /// </summary>
        /// <param name="obj">instance to compare</param>
        /// <returns>true if the two instances are equal</returns>
        public override bool Equals(Object obj)
        {
            ExchangeUser user = obj as ExchangeUser;
            return this.CommonName.Equals(user.CommonName);
        }
    }
}
