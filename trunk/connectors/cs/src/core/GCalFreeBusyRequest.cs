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
using Google.GCalExchangeSync.Library.Util;
using TZ4Net;

namespace Google.GCalExchangeSync.Library
{
    /// <summary>
    /// Request handler for a free Busy lookup
    /// </summary>
    public class GCalFreeBusyRequest
    {
        /// <summary>
        /// Logger for GCalFreeBusyRequest
        /// </summary>
        protected static readonly log4net.ILog log =
           log4net.LogManager.GetLogger(typeof(GCalFreeBusyRequest));

        private static readonly int expectedRequestItems = 6;

        #region Public Property / Private Field pairs

        private string versionNumber;

        /// <summary>
        /// Version # of the request
        /// </summary>
        public string VersionNumber
        {
            get { return versionNumber; }
        }

        private string messageId;

        /// <summary>
        /// Message ID of the request
        /// </summary>
        public string MessageId
        {
            get { return messageId; }
        }

        private DateTime startDate;

        /// <summary>
        /// Start date of the free busy lookup
        /// </summary>
        public DateTime StartDate
        {
            get { return startDate; }
        }

        private DateTime endDate;

        /// <summary>
        /// End date of the free busy lookup
        /// </summary>
        public DateTime EndDate
        {
            get { return endDate; }
        }

        private DateTime utcStartDate;

        /// <summary>
        ///  Start date of the free busy lookup in UTC
        /// </summary>
        public DateTime UTCStartDate
        {
            get { return utcStartDate; }
        }

        private DateTime utcEndDate;

        /// <summary>
        ///  End date of the free busy lookup in UTC
        /// </summary>
        public DateTime UTCEndDate
        {
            get { return utcEndDate; }
        }

        private OlsonTimeZone timeZone;

        /// <summary>
        ///  Timezone of the request
        /// </summary>
        public OlsonTimeZone TimeZone
        {
            get { return timeZone; }
        }

        private string rawRequest;

        private string[] exchangeUsers;

        /// <summary>
        /// Users in the request
        /// </summary>
        public string[] ExchangeUsers
        {
            get { return exchangeUsers; }
        }

        private DateTime since;

        /// <summary>
        /// Give results since this time
        /// </summary>
        public DateTime Since
        {
            get { return since; }
        }

        #endregion

        /// <summary>
        /// Create a new request given the query
        /// </summary>
        /// <param name="rawInput">The query</param>
        public GCalFreeBusyRequest(string rawInput)
        {
            rawRequest = rawInput;

            this.Parse(rawInput);
            this.ValidateRequest();
        }

        /// <summary>
        /// Parses the incoming Exchange request from GCal. The requests are of the form:
        /// [ version #, ID, [list of emails], startdate/enddate, sincedata, timezone]
        /// /// </summary>
        /// <param name="rawInput">The incoming GCal request string</param>
        private void Parse(string rawInput)
        {
            if ( rawInput != null )
                rawInput = rawInput.Trim();

            /* Test that the request is not null or empty */
            if (string.IsNullOrEmpty(rawInput))
            {
                throw new GCalExchangeException(GCalExchangeErrorCode.MalformedRequest,
                    "GCalRequest is null or empty.");
            }

            log.InfoFormat( "Request received from GCal. [body={0}]", rawInput );

            /* Test that the request has starting and ending brackets */
            if (!rawInput.StartsWith("[") || !rawInput.EndsWith("]"))
            {
                throw new GCalExchangeException(GCalExchangeErrorCode.MalformedRequest,
                    String.Format("GCalRequest does start and end in brackets: [rawInput:{0}]", rawInput));
            }

            /* Remove the start and end brackets */
            string requestContent = rawInput.Remove(0, 1);
            requestContent = requestContent.Remove(requestContent.Length - 1, 1);
            /* Request is cleaned to have no ending brackets */

            /* Test that the request has an inner bracket pair which (should) contains the usernames */
            if (!(requestContent.Contains("[") && requestContent.IndexOf("]") > requestContent.IndexOf("[")))
            {
                throw new GCalExchangeException( GCalExchangeErrorCode.MalformedRequest,
                    string.Format( "GCalRequest exchange users section is not properly formatted: [rawInput:{0}]", rawInput ) );
            }

            /* Get the indexes of the start and end username brackets */
            int usersStartIndex = requestContent.IndexOf("[");
            int usersEndIndex = requestContent.IndexOf("]");
            int usersLength = usersEndIndex - usersStartIndex + 1;

            /* Get the usernames string from the request */
            string usersString = requestContent.Substring(usersStartIndex, usersLength);

            /* Remove it from the rest of the request */
            requestContent = requestContent.Remove(usersStartIndex, usersLength);

            /* Remove the brackets from the start and end of the username string */
            usersString = usersString.Remove(0, 1);
            usersString = usersString.Remove(usersString.Length - 1, 1);

            /* Split the usernames by comma, store them in the request object */
            exchangeUsers = usersString.Split(',');

            // Apply any domain mappings to the user names
            for (int i = 0; i < exchangeUsers.Length; i++)
            {
                string user = exchangeUsers[i].Trim();
                exchangeUsers[i] = ConfigCache.MapToLocalDomain(user);
            }

            /* Split up the rest of the request */
            string[] requestItems = requestContent.Split(',');

            /* Test that the proper amount of variables remain in the string */
            if (requestItems.Length != expectedRequestItems)
            {
                throw new GCalExchangeException(GCalExchangeErrorCode.MalformedRequest,
                    String.Format("GCalRequest does not contain the proper amount of variables; Supplied - {0}, Expected - {1}",
                    requestItems.Length,
                    expectedRequestItems));
            }

            /* Retrieve the version and message ids */
            versionNumber = requestItems[0].Trim();
            messageId = requestItems[1].Trim();

            /* Get the start and end date from the request, the two dates are separated by '/' */
            string dateString = requestItems[3].Trim();
            string[] dateArray = dateString.Split('/');
            if (dateArray.Length != 2)
            {
                throw new GCalExchangeException(GCalExchangeErrorCode.MalformedRequest,
                    "GCalRequest does not contain sufficient date information, both a start and end date must be supplied");
            }

            startDate = DateUtil.ParseGoogleDate(dateArray[0].Trim());
            endDate = DateUtil.ParseGoogleDate(dateArray[1].Trim());

            string requestItemSince = requestItems[4].Trim();
            /* Get the since field from the request */
            try
            {
                since = DateUtil.ParseGoogleDate(requestItemSince);
            }
            catch (GCalExchangeException ex)
            {
                // We don't really use this param anyway and in some cases
                // we've seen an invalid date
                log.Warn(String.Format("Ignoring incorrect since request parameter {0}",
                                        requestItemSince),
                          ex);
                since = new DateTime();
            }

            /* Get the current time zone name */
            timeZone = OlsonUtil.GetTimeZone(requestItems[5].Trim());

            utcStartDate = OlsonUtil.ConvertToUTC(startDate, timeZone);
            utcEndDate = OlsonUtil.ConvertToUTC(endDate, timeZone);
        }

        private void ValidateRequest()
        {
            if ( versionNumber != "1")
            {
                throw new GCalExchangeException(
                    GCalExchangeErrorCode.UnsupportedVersion,
                    String.Format("Version: '{0}' is not supported", versionNumber));
            }
        }
    }

}
