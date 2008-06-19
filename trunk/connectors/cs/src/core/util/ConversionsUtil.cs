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
using System.Text;
using System.Web;

using Google.GData.Client;
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

        private static readonly int kMaxBits = 32;
        private static readonly char[] kDigitToHexChar = new char[] {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
        private static readonly int[] kLog2Table = new int[17] {
         // 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, a, b, c, d, e, f, 10
            0, 0, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 4 };

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
        /// Convert an Exchange Event Busy Status to a Google Calendar response
        /// </summary>
        /// <param name="busyStatus">The Exchange event status</param>
        /// <returns>The Google Calendar response</returns>
        public static int ConvertBusyStatusToGoogleResponse(BusyStatus busyStatus)
        {
            GCalResponseStatus googleStatus = GCalResponseStatus.Uninvited;

            switch (busyStatus)
            {
                case BusyStatus.OutOfOffice:
                case BusyStatus.Busy:
                    googleStatus = GCalResponseStatus.Accepted;
                    break;

                case BusyStatus.Free:
                    googleStatus = GCalResponseStatus.Declined;
                    break;

                case BusyStatus.Tentative:
                    googleStatus = GCalResponseStatus.Tentative;
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
        /// <param name="result">Receives the escaped string</param>
        public static void EscapeNonAlphaNumeric(string input, StringBuilder result)
        {
            result.Length = 0;

            if (input == null)
            {
                return;
            }

            input = HttpUtility.HtmlDecode(input);

            if (input == null)
            {
                return;
            }

            result.EnsureCapacity(input.Length);

            StringBuilder oct = new StringBuilder(kMaxBits);

            /* For each character, check to see if its a letter number or space
             * If not, replace with a backslash followed by the numeric equivalent for the character */
            foreach (Char c in input)
            {
                if (char.IsLetter(c) || char.IsNumber(c) || c == ' ')
                {
                    result.Append(c);
                }
                else
                {
                    DecimalToBase((int)c, 8, 3, '0', oct);

                    result.Append(@"\");
                    result.Append(oct);
                }
            }
        }

        private static void DecimalToBase(
            int dec,
            int numBase,
            int totalWidth,
            char leftPadding,
            StringBuilder result)
        {
            if (result != null)
            {
                result.Length = 0;
            }
            else
            {
                throw new ArgumentException("Null result passed");
            }

            if ((dec < 0) || (numBase < 2) || (numBase > 16) || (totalWidth <0))
            {
                throw new ArgumentException("Cannot convert due to invalid arguments");
            }

            if (dec == 0)
            {
                result.Append('0');
            }

            if ((numBase & (numBase - 1)) != 0)
            {
                for (; dec != 0; dec /= numBase)
                {
                    int rem = dec % numBase;
                    result.Append(kDigitToHexChar[rem]);
                }
            }
            else
            {
                int log2Numbase = kLog2Table[numBase];
                int reminderMask = numBase - 1;

                for (; dec != 0; dec >>= log2Numbase)
                {
                    int rem = dec & reminderMask;

                    result.Append(kDigitToHexChar[rem]);
                }
            }

            for (int i = result.Length; i < totalWidth; i++)
            {
                result.Append(leftPadding);
            }

            int resultLength = result.Length;
            for (int i = 0; i < resultLength / 2; i++)
            {
                char temp = result[i];
                result[i] = result[resultLength - 1 - i];
                result[resultLength - 1 - i] = temp;
            }
        }

        /// <summary>
        /// Get the content of possibly null AtomContent
        /// </summary>
        /// <param name="content">The Google AtomContent</param>
        /// <returns>The content or empty string if the content or it's Content was null</returns>
        public static string SafeGetContent(AtomContent content)
        {
            if (content == null)
            {
                return string.Empty;
            }

            return content.Content ?? string.Empty;
        }

        /// <summary>
        /// Get the Value of possibly null EnumConstruct
        /// </summary>
        /// <param name="enumConstruct">The Google EnumConstruct</param>
        /// <returns>The value or empty string if the enumConstruct or it's Value was null</returns>
        public static string SafeGetValue(EnumConstruct enumConstruct)
        {
            if (enumConstruct == null)
            {
                return string.Empty;
            }

            return enumConstruct.Value ?? string.Empty;
        }

        /// <summary>
        /// Get the Text of possibly null AtomTextConstruct
        /// </summary>
        /// <param name="textConstruct">The Google AtomTextConstruct</param>
        /// <returns>The value or empty string if the textConstruct or it's Text was null</returns>
        public static string SafeGetText(AtomTextConstruct textConstruct)
        {
            if (textConstruct == null)
            {
                return string.Empty;
            }

            return textConstruct.Text ?? string.Empty;
        }

        /// <summary>
        /// Convert a Google Calender Event status to an Exchange meeting status
        /// </summary>
        /// <param name="status">The Google Calendar event status</param>
        /// <returns>The Exchange meeting status</returns>
        public static MeetingStatus ConvertGoogleEventStatus(EventEntry.EventStatus status)
        {
            MeetingStatus exchangeStatus = MeetingStatus.Confirmed;
            // Default is busy, because in order to make free-buys projections work correctly,
            // since in that case the status is not set at all.

            switch (SafeGetValue(status))
            {
                case EventEntry.EventStatus.CONFIRMED_VALUE:
                    exchangeStatus = MeetingStatus.Confirmed;
                    break;

                case EventEntry.EventStatus.CANCELED_VALUE:
                    exchangeStatus = MeetingStatus.Cancelled;
                    break;

                case EventEntry.EventStatus.TENTATIVE_VALUE:
                    exchangeStatus = MeetingStatus.Tentative;
                    break;
            }

            return exchangeStatus;
        }

        /// <summary>
        /// Convert a Google Calender attendee status to an Exchange status
        /// </summary>
        /// <param name="googleAppsEvent">The Google Calendar event</param>
        /// <param name="user">The user to get the status for</param>
        /// <returns>The Exchange status for the given user</returns>
        public static BusyStatus ConvertParticipantStatus(
            ExchangeUser user,
            EventEntry googleAppsEvent)
        {
            BusyStatus result = BusyStatus.Busy;
            // Default is busy, because in order to make free-buys projections work correctly,
            // since in that case the participants are not set at all.

            string externalEmail = ConfigCache.MapToExternalDomain(user.Email);

            foreach (Who participant in googleAppsEvent.Participants)
            {
                if (!string.IsNullOrEmpty(participant.Email) &&
                    (participant.Email.Equals(user.Email) ||
                     participant.Email.Equals(externalEmail)))
                {
                    switch (SafeGetValue(participant.Attendee_Status))
                    {
                        case Who.AttendeeStatus.EVENT_ACCEPTED:
                            result = BusyStatus.Busy;
                            break;

                        case Who.AttendeeStatus.EVENT_DECLINED:
                            result = BusyStatus.Free;
                            break;

                        case Who.AttendeeStatus.EVENT_INVITED:
                            result = BusyStatus.Tentative;
                            break;

                        case Who.AttendeeStatus.EVENT_TENTATIVE:
                            result = BusyStatus.Tentative;
                            break;
                    }

                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Return the user status, with support for tentative, if the event has the needed details.
        /// </summary>
        /// <param name="googleAppsEvent">The Google Calendar event</param>
        /// <param name="user">The user to get the status for</param>
        /// <returns>The Exchange status for the given user</returns>
        public static BusyStatus GetUserStatusForEvent(
            ExchangeUser user,
            EventEntry googleAppsEvent)
        {
            MeetingStatus meetingStatus = MeetingStatus.Confirmed;
            BusyStatus userStatus = BusyStatus.Free;

            if (_log.IsDebugEnabled)
            {
                _log.DebugFormat("Looking the status of {0} in {1}",
                                 user.Email,
                                 googleAppsEvent.Title.Text);
            }

            // Treat events w/o proper times as free. There isn't much we can do.
            if (googleAppsEvent.Times == null || googleAppsEvent.Times.Count == 0)
            {
                return BusyStatus.Free;
            }

            meetingStatus = ConvertGoogleEventStatus(googleAppsEvent.Status);

            if (_log.IsDebugEnabled)
            {
                _log.DebugFormat("The event status is {0}", meetingStatus.ToString());
            }

            // If the meeting is cancelled, treat it as free time.
            if (meetingStatus == MeetingStatus.Cancelled)
            {
                return BusyStatus.Free;
            }

            userStatus = ConvertParticipantStatus(user, googleAppsEvent);
            if (_log.IsDebugEnabled)
            {
                _log.DebugFormat("The user status is {0}", userStatus.ToString());
            }

            if (userStatus == BusyStatus.Free)
            {
                return BusyStatus.Free;
            }

            // There is no mapping from GCal to OOF right now. If/when it is added, it should
            // be handled in a manner similar to busy and tentative.
            Debug.Assert(userStatus != BusyStatus.OutOfOffice);

            // If the meeting is set to show as tentative, set the time as tentative, not busy
            // if the user accepted the meeting.
            if (meetingStatus == MeetingStatus.Tentative && userStatus == BusyStatus.Busy)
            {
                userStatus = BusyStatus.Tentative;
            }

            return userStatus;
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
