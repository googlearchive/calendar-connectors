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
using System.Text;
using System.Web;

using Google.GData.Calendar;
using Google.GData.Extensions;

using Google.GCalExchangeSync.Library.WebDav;

namespace Google.GCalExchangeSync.Library.Util
{
    /// <summary>
    /// Conversion utilities
    /// </summary>
    public class ConversionsUtil
    {
        /// <summary>
        /// Logger for ConversionsUtil
        /// </summary>
        protected static readonly log4net.ILog _log =
            log4net.LogManager.GetLogger(typeof(ConversionsUtil));

        private static readonly int base10 = 10;
        private static readonly char[] cHexa = new char[] { 'A', 'B', 'C', 'D', 'E', 'F' };
        private static readonly int[] iHexaNumeric = new int[] { 10, 11, 12, 13, 14, 15 };
        private static readonly int[] iHexaIndices = new int[] { 0, 1, 2, 3, 4, 5 };

        /// <summary>
        /// Convert an Exchange Event Response to a Google Calendar response
        /// </summary>
        /// <param name="resp">The Exchange event response</param>
        /// <returns>The Google Calendar response</returns>
        public static int ConvertExchangeResponseToGoogleResponse(ResponseStatus resp)
        {
            GCalResponseStatus googleStatus = GCalResponseStatus.Uninvited;

            switch (resp)
            {
                case ResponseStatus.NotResponded:
                    googleStatus = GCalResponseStatus.NeedsAction;
                    break;

                case ResponseStatus.Accepted:
                    googleStatus = GCalResponseStatus.Accepted;
                    break;

                case ResponseStatus.Declined:
                    googleStatus = GCalResponseStatus.Declined;
                    break;

                case ResponseStatus.Tentative:
                    googleStatus = GCalResponseStatus.Tentative;
                    break;

                /* For testing purposes None will be mapped to uninvited (4)
                 * This may change in the future */

                case ResponseStatus.None:
                    googleStatus = GCalResponseStatus.Uninvited;
                    break;

                case ResponseStatus.Organized:
                    googleStatus = GCalResponseStatus.Organizer;
                    break;

                default:
                    googleStatus = GCalResponseStatus.Uninvited;
                    break;
            }

            return (int)googleStatus;
        }

        /// <summary>
        /// Perform escaping on the string for use in Exchange WebDAV messages
        /// </summary>
        /// <param name="input">The string to escape</param>
        /// <returns>The escaped string</returns>
        public static string EscapeNonAlphaNumeric(string input)
        {
            input = HttpUtility.HtmlDecode(input);

            StringBuilder sb = new StringBuilder();

            if (input != null)
            {
                /* For each character, check to see if its a letter number or space
                 * If not, replace with a backslash followed by the numeric equivalent for the character */

                foreach (Char c in input)
                {
                    if (char.IsLetter(c) || char.IsNumber(c) || c == ' ')
                    {
                        sb.Append(c);
                    }
                    else
                    {
                        string oct = DecimalToBase((int)c, 8);

                        sb.AppendFormat(@"\{0}", oct.PadLeft(3, '0'));
                    }
                }
            }

            return sb.ToString();
        }

        private static string DecimalToBase(int iDec, int numbase)
        {
            string strBin = "";
            int[] result = new int[32];
            int MaxBit = 32;

            for (; iDec > 0; iDec /= numbase)
            {
                int rem = iDec % numbase;
                result[--MaxBit] = rem;
            }

            for (int i = 0; i < result.Length; i++)
            {
                if ((int)result.GetValue(i) >= base10)
                {
                    strBin += cHexa[(int)result.GetValue(i) % base10];
                }
                else
                {
                    strBin += result.GetValue(i);
                }
            }

            strBin = strBin.TrimStart(new char[] { '0' });

            return strBin;
        }

        /// <summary>
        /// Convert a Google Calender Event status to an Exchange meeting status
        /// </summary>
        /// <param name="status">The Google Calendar event status</param>
        /// <returns>The Exchange meeting status</returns>
        public static MeetingStatus ConvertGoogleEventStatus(EventEntry.EventStatus status)
        {
            MeetingStatus exchangeStatus = MeetingStatus.Tentative;

            switch (status.Value)
            {
                case EventEntry.EventStatus.CONFIRMED_VALUE:
                    exchangeStatus = MeetingStatus.Confirmed;
                    break;
                case EventEntry.EventStatus.CANCELED_VALUE:
                    exchangeStatus = MeetingStatus.Cancelled;
                    break;
                case EventEntry.EventStatus.TENTATIVE_VALUE:
                default:
                    exchangeStatus = MeetingStatus.Tentative;
                    break;
            }

            return exchangeStatus;
        }

        /// <summary>
        /// Convert a Google Calendar busy status into an Exchnage Busy Status
        /// </summary>
        /// <param name="busyStatus">GoogleCalendar busy status</param>
        /// <returns>An exchange busy status</returns>
        public static BusyStatus ParseBusyStatus( string busyStatus )
        {
            switch ( busyStatus.ToUpper() )
            {
                case "BUSY" :
                    return BusyStatus.Busy;

                case "FREE" :
                    return BusyStatus.Free;

                case "OUTOFOFFICE" :
                case "OOF" :
                    return BusyStatus.OutOfOffice;

                case "TENTATIVE" :
                    return BusyStatus.Tentative;

                default:
                    throw new ApplicationException( "Unknown BusyStatus returned from Exchange: " + busyStatus );
            }
        }
    }

    /// <summary>
    /// Google Calendar Response Status
    /// </summary>
    public enum GCalResponseStatus
    {
        /// <summary>
        /// Event needs an action to be taken
        /// </summary>
        NeedsAction = 0, 

        /// <summary>
        /// Event has been accepted
        /// </summary>
        Accepted = 1, 

        /// <summary>
        /// Event has been declined
        /// </summary>
        Declined = 2, 

        /// <summary>
        /// Event has been accepted tenatively
        /// </summary>
        Tentative = 3, 

        /// <summary>
        /// User has been univited fri the event
        /// </summary>
        Uninvited = 4, 

        /// <summary>
        /// User is the organizer of the event
        /// </summary>
        Organizer = 5
    }
}
