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
using Google.GCalExchangeSync.Library.WebDav;

using log4net;

namespace Google.GCalExchangeSync.Library
{
    /// <summary>
    /// Free Busy Response
    /// </summary>
    public class GCalFreeBusyResponse
    {
        /// <summary>
        /// Logger for GCalFreeBusyResponse
        /// </summary>
        protected static readonly log4net.ILog log =
           log4net.LogManager.GetLogger(typeof(GCalFreeBusyResponse));

        private GCalFreeBusyRequest request;
        private StringBuilder escapedSubject;
        private StringBuilder escapedLocation;
        private StringBuilder escapedOrganizer;
        private StringBuilder escapedCommonName;

        /// <summary>
        /// Request to generate response for
        /// </summary>
        public GCalFreeBusyRequest Request
        {
            get { return request; }
        }

        /// <summary>
        /// Constructor accepts a GCalRequest from which to generate a response
        /// </summary>
        /// <param name="request">Incoming request from which to generate a response</param>
        public GCalFreeBusyResponse(GCalFreeBusyRequest request)
        {
            escapedSubject = new StringBuilder(256);
            escapedLocation = new StringBuilder(1024);
            escapedOrganizer = new StringBuilder(256);
            escapedCommonName = new StringBuilder(256);
            this.request = request;
        }

        /// <summary>
        /// Generates a response from a GCalRequest
        /// </summary>
        /// <returns></returns>
        public string GenerateResponse()
        {
            /* Create a string builder to hold the text for the response */
            StringBuilder result = new StringBuilder(4096);

            /* Create an exchange provider */
            ExchangeService gateway = new ExchangeService(
                ConfigCache.ExchangeServerUrl,
                ConfigCache.ExchangeUserLogin,
                ConfigCache.ExchangeUserPassword);

            /* Return the exchangers from the GCal Request that was passed in */
            DateTimeRange range = new DateTimeRange(request.UTCStartDate, request.UTCEndDate);
            ExchangeUserDict exchangeUsers = gateway.SearchByEmail(range, request.ExchangeUsers);

            /* Create the header of the request */
            result.AppendFormat("['{0}','{1}',", request.VersionNumber, request.MessageId);

            result.AppendFormat("['_ME_AddData','{0}/{1}','{2}'",
                                DateUtil.FormatDateForGoogle(request.StartDate),
                                DateUtil.FormatDateForGoogle(request.EndDate),
                                DateUtil.FormatDateTimeForGoogle(request.Since));

            /* Flag for inserting commas */
            bool firstUser = true;

            result.Append(",[");

            foreach (ExchangeUser user in exchangeUsers.Values)
            {
                /* Don't add a comma if this is the first user */
                if (!firstUser)
                {
                    result.Append(",");
                }

                /* Add the user's credentials */
                string email = ConfigCache.MapToExternalDomain(user.Email);
                result.AppendFormat("'{0}','{1}','{2}',[",
                                    user.DisplayName,
                                    email,
                                    (int)user.AccessLevel);

                GenerateResponseForTimeBlocks(user,
                                              result);

                result.Append("]");
                firstUser = false;
            }

            result.Append("]");
            result.Append("]");
            result.Append("]");

            log.Info("GCal Free/Busy response successfully generated.");
            log.DebugFormat("Response = {0}", result);

            return result.ToString();
        }

        private void GenerateResponseForTimeBlocks(
            ExchangeUser user,
            StringBuilder result)
        {
            /* If a user has time no blocks associate with him / her */
            if ((user.BusyTimes == null || user.BusyTimes.Count == 0) &&
                (user.TentativeTimes == null && user.TentativeTimes.Count == 0))
            {
                return;
            }

            /* Flag for inserting commas */
            bool firstAppointment = true;
            IEnumerable<FreeBusyTimeBlock> busyEnumerable =
                user.BusyTimes.Values as IEnumerable<FreeBusyTimeBlock>;
            IEnumerable<FreeBusyTimeBlock> tentativeEnumerable =
                user.TentativeTimes.Values as IEnumerable<FreeBusyTimeBlock>;
            IEnumerator<FreeBusyTimeBlock> busyEnumerator =
                busyEnumerable.GetEnumerator();
            IEnumerator<FreeBusyTimeBlock> tentativeEnumerator =
                tentativeEnumerable.GetEnumerator();
            bool moreTentative = tentativeEnumerator.MoveNext();
            bool moreBusy = busyEnumerator.MoveNext();

            while (moreBusy && moreTentative)
            {
                FreeBusyTimeBlock busyBlock = busyEnumerator.Current;
                FreeBusyTimeBlock tentativeBlock = tentativeEnumerator.Current;

                FreeBusyTimeBlock timeBlock = null;
                BusyStatus busyStatus = BusyStatus.Busy;

                if (busyBlock.Range.Start < tentativeBlock.Range.Start)
                {
                    timeBlock = busyBlock;
                    moreBusy = busyEnumerator.MoveNext();
                }
                else
                {
                    timeBlock = tentativeBlock;
                    busyStatus = BusyStatus.Tentative;
                    moreTentative = tentativeEnumerator.MoveNext();
                }

                GenerateResponseForTimeBlock(user,
                                             timeBlock,
                                             busyStatus,
                                             firstAppointment,
                                             result);
                firstAppointment = false;
            }

            while (moreBusy)
            {
                GenerateResponseForTimeBlock(user,
                                             busyEnumerator.Current,
                                             BusyStatus.Busy,
                                             firstAppointment,
                                             result);
                moreBusy = busyEnumerator.MoveNext();
                firstAppointment = false;
            }

            while (moreTentative)
            {
                GenerateResponseForTimeBlock(user,
                                             tentativeEnumerator.Current,
                                             BusyStatus.Tentative,
                                             firstAppointment,
                                             result);
                moreTentative = tentativeEnumerator.MoveNext();
                firstAppointment = false;
            }
        }

        private void GenerateResponseForTimeBlock(
            ExchangeUser user,
            FreeBusyTimeBlock timeBlock,
            BusyStatus busyStatus,
            bool firstAppointment,
            StringBuilder result)
        {
            if (timeBlock.Appointments == null || timeBlock.Appointments.Count == 0)
            {
                if (!firstAppointment)
                {
                    result.Append(",");
                }

                AppendPrivateFreeBusyEntry(timeBlock.StartDate,
                                           timeBlock.EndDate,
                                           user.CommonName,
                                           busyStatus,
                                           result);
                return;
            }

            foreach (Appointment appt in timeBlock.Appointments)
            {
                if (!firstAppointment)
                {
                    result.Append(",");
                }

                if (!appt.IsPrivate)
                {
                    AppendFreeBusyEntry(appt.StartDate,
                                        appt.EndDate,
                                        appt.Subject,
                                        appt.Location,
                                        appt.Organizer,
                                        appt.BusyStatus,
                                        result);
                }
                else
                {
                    AppendPrivateFreeBusyEntry(appt.StartDate,
                                               appt.EndDate,
                                               user.CommonName,
                                               appt.BusyStatus,
                                               result);
                }

                firstAppointment = false;
            }
        }

        private void AppendFreeBusyEntry(
            DateTime startUtc,
            DateTime endUtc,
            string subject,
            string location,
            string organizer,
            BusyStatus busyStatus,
            StringBuilder result)
        {
            DateTime startLocal = OlsonUtil.ConvertFromUTC(startUtc, Request.TimeZone);
            DateTime endLocal = OlsonUtil.ConvertFromUTC(endUtc, Request.TimeZone);
            int status = ConversionsUtil.ConvertBusyStatusToGoogleResponse(busyStatus);

            ConversionsUtil.EscapeNonAlphaNumeric(subject, escapedSubject);
            ConversionsUtil.EscapeNonAlphaNumeric(location, escapedLocation);
            ConversionsUtil.EscapeNonAlphaNumeric(organizer, escapedOrganizer);

            result.AppendFormat("['{0}','{1}','{2}','{3}','{4}',{5}]",
                                escapedSubject,
                                DateUtil.FormatDateTimeForGoogle(startLocal),
                                DateUtil.FormatDateTimeForGoogle(endLocal),
                                escapedLocation,
                                escapedOrganizer,
                                status);
        }

        private void AppendPrivateFreeBusyEntry(
            DateTime startUtc,
            DateTime endUtc,
            string commonName,
            BusyStatus busyStatus,
            StringBuilder result)
        {
            AppendFreeBusyEntry(startUtc,
                                endUtc,
                                "",
                                "",
                                commonName,
                                busyStatus,
                                result);
        }
    }

    /// <summary>
    /// Response generator for errors during processing
    /// </summary>
    public class GCalErrorResponse
    {
        private string versionId = "0";
        private string messageId = "0";
        private string errorMessage = string.Empty;
        private int    errorId;

        /// <summary>
        /// Generate an error response from a request
        /// </summary>
        /// <param name="request">request that generated error</param>
        /// <param name="exception">The exception from the error</param>
        public GCalErrorResponse(GCalFreeBusyRequest request, GCalExchangeException exception)
        {
            if (request != null)
            {
                versionId = request.VersionNumber;
                messageId = request.MessageId;
            }

            errorMessage = exception.Message;
            errorId = (int)(exception.ErrorCode);
        }

        /// <summary>
        /// Generate an error response based on the exception
        /// </summary>
        /// <param name="exception"></param>
        public GCalErrorResponse(Exception exception)
        {
            errorMessage = exception.Message;
        }

        /// <summary>
        /// Generate the response for the request
        /// </summary>
        /// <returns>The response</returns>
        public string GenerateResponse()
        {
            StringBuilder escapedError = new StringBuilder(errorMessage.Length);

            ConversionsUtil.EscapeNonAlphaNumeric(errorMessage, escapedError);

            // GCal expects errors in the following format:
            // [VERSION NUMBER, MESSAGE ID, ERROR ID, ERROR STRING]
            return string.Format(
                "['{0}','{1}','{2}','{3}']",
                versionId,
                messageId,
                errorId,
                escapedError);
        }
    }
}
