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
            this.request = request;
        }

        /// <summary>
        /// Generates a response from a GCalRequest
        /// </summary>
        /// <returns></returns>
        public string GenerateResponse()
        {
            /* Create a string builder to hold the text for the response */
            StringBuilder sb = new StringBuilder();

            /* Create an exchange provider */
            ExchangeService gateway = new ExchangeService(
                ConfigCache.ExchangeServerUrl, 
                ConfigCache.ExchangeUserLogin, 
                ConfigCache.ExchangeUserPassword);

            /* Return the exchangers from the GCal Request that was passed in */
            DateTimeRange range = new DateTimeRange(request.UTCStartDate, request.UTCEndDate);
            ExchangeUserDict exchangeUsers = gateway.SearchByEmail(range, request.ExchangeUsers);

            /* Create the header of the request */
            sb.AppendFormat("['{0}','{1}',", request.VersionNumber, request.MessageId);

            sb.AppendFormat("['_ME_AddData','{0}/{1}','{2}'",
                DateUtil.FormatDateForGoogle(request.StartDate),
                DateUtil.FormatDateForGoogle(request.EndDate),
                DateUtil.FormatDateTimeForGoogle(request.Since));

            /* Flag for inserting commas */            
            bool firstUser = true;

            sb.Append(",[");

            foreach (ExchangeUser user in exchangeUsers.Values)
            {
                /* Flag for inserting commas */
                bool firstAppointment = true;

                /* Don't add a comma if this is the first user */
                if (!firstUser)
                {
                    sb.Append(",");
                }

                /* Add the user's credentials */
                string email = ConfigCache.MapToExternalDomain(user.Email);
                sb.AppendFormat("'{0}','{1}','{2}',[", user.DisplayName, email, (int)user.AccessLevel);

                /* If a user has time blocks associate with him / her */
                if (user.BusyTimes != null && user.BusyTimes.Count > 0)
                {
                    /* Iterate over each FreeBusyTimeBlock */
                    foreach (FreeBusyTimeBlock timeBlock in user.BusyTimes.Values)
                    {
                        if ( timeBlock.Appointments != null && timeBlock.Appointments.Count > 0)
                        {
                            foreach ( Appointment appt in timeBlock.Appointments )
                            {
                                if (!firstAppointment)
                                {
                                    sb.Append(",");
                                }

                                DateTime startLocal = OlsonUtil.ConvertFromUTC(appt.StartDate, Request.TimeZone);
                                DateTime endLocal = OlsonUtil.ConvertFromUTC(appt.EndDate, Request.TimeZone);

                                if (!appt.IsPrivate)
                                {
                                    sb.AppendFormat( "['{0}','{1}','{2}','{3}','{4}','{5}']",
                                        ConversionsUtil.EscapeNonAlphaNumeric( appt.Subject ),
                                        DateUtil.FormatDateTimeForGoogle(startLocal),
                                        DateUtil.FormatDateTimeForGoogle(endLocal),
                                        ConversionsUtil.EscapeNonAlphaNumeric( appt.Location ),
                                        ConversionsUtil.EscapeNonAlphaNumeric( appt.Organizer ),
                                        ConversionsUtil.ConvertExchangeResponseToGoogleResponse( appt.ResponseStatus ) );
                                }
                                else
                                {
                                    AppendPrivateFreeBusyEntry(startLocal, endLocal, user, sb);
                                }

                                firstAppointment = false;
                            }
                        }
                        else
                        {
                            if (!firstAppointment)
                            {
                                sb.Append(",");
                            }

                            AppendPrivateFreeBusyEntry(timeBlock.StartDate,timeBlock.EndDate, user, sb);

                            firstAppointment = false;
                        }
                    }
                }

                sb.Append("]");
                firstUser = false;
            }
            sb.Append("]");
            sb.Append("]");
            sb.Append("]");

            log.Info( "GCal Free/Busy response successfully generated." );

            return sb.ToString();
        }

        private void AppendPrivateFreeBusyEntry( DateTime startDate, DateTime endDate, ExchangeUser user, StringBuilder sb )
        {
            sb.AppendFormat( "['{0}','{1}','{2}','{3}','{4}','{5}']",
               "",
               DateUtil.FormatDateTimeForGoogle(startDate),
               DateUtil.FormatDateTimeForGoogle(endDate),
               "",
               ConversionsUtil.EscapeNonAlphaNumeric( user.CommonName ),
               "1" );
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
        /// Generate an error response ased on the exception
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
            string errorString = 
                ConversionsUtil.EscapeNonAlphaNumeric( errorMessage );

            // GCal expects errors in the following format:
            // [VERSION NUMBER, MESSAGE ID, ERROR ID, ERROR STRING]
            return string.Format(
                "['{0}','{1}','{2}','{3}']",
                versionId,
                messageId,
                errorId,
                errorString );
        }
    }
}
