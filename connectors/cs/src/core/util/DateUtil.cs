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
using System.Globalization;

namespace Google.GCalExchangeSync.Library.Util
{
    /// <summary>
    /// Date utilities
    /// </summary>
    public class DateUtil
    {
        /// <summary>
        /// Logger for DateUtil
        /// </summary>
        protected static readonly log4net.ILog _log =
            log4net.LogManager.GetLogger( typeof( DateUtil ) );

        /// <summary>
        /// Is the DateTime within the date range
        /// </summary>
        /// <param name="dateToCompare">DateTime to check</param>
        /// <param name="startDate">Start of interval</param>
        /// <param name="endDate">End of interval</param>
        /// <returns>True if dateToCompare is within the range</returns>
        public static bool IsWithinRange( DateTime dateToCompare, DateTime startDate, DateTime endDate )
        {
            bool val = false;

            if ( dateToCompare >= startDate && dateToCompare <= endDate )
            {
                val = true;
            }

            return val;
        }

        /// <summary>
        /// Format a date as an ISO8601 Date String
        /// </summary>
        /// <param name="dt">Datetime to convert</param>
        /// <returns>An ISO8601 date time string</returns>
		public static string FormatDateForISO8601(DateTime dt)
		{
			return dt.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'");
		}

        /// <summary>
        /// Formate a datetme as an exchange style date string
        /// </summary>
        /// <param name="dt">Datetime to convert</param>
        /// <returns>An exchane style date string</returns>
		public static string FormatDateForExchange(DateTime dt)
        {
            return dt.ToUniversalTime().ToString( "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'" );
        }

        /// <summary>
        /// Format a datetime as an DASL style date string
        /// </summary>
        /// <param name="dt">Datetime to convert</param>
        /// <returns>A DASL style date string</returns>
		public static string FormatDateForDASL(DateTime dt)
		{
			return dt.ToUniversalTime().ToString("yyyy/MM/dd HH:mm:ss");
		}

		/// <summary>
        /// Formats a DateTime into "yyyyMMdd" format
        /// </summary>
        /// <param name="date">Datetime to convert</param>
        /// <returns>A Google Calendar style date string</returns>
        public static string FormatDateForGoogle(DateTime date)
        {
            return date.ToString( "yyyyMMdd" );
        }

        /// <summary>
        /// Formats a DateTime into "yyyyMMddTHHmmss" format, where 
        /// "T" is the separator between date values and time values
        /// </summary>
        /// <param name="date">Datetime to convert</param>
        /// <returns>A Google Calendar style date time string</returns>
        public static string FormatDateTimeForGoogle(DateTime date)
        {
            return string.Format( "{0}T{1}", date.ToString( "yyyyMMdd" ), date.ToString( "HHmmss" ) );
        }

        /// <summary>
        /// Parses date supplied from GCal requests. Parses dates in form "yyyyMMddTHHmmss" 
        /// where T is the delimiter between date and time information
        /// </summary>
        /// <param name="dateString">GCal date to parse</param>
        /// <returns>A DateTime parsed from the string</returns>

        public static DateTime ParseGoogleDate( string dateString )
        {
            DateTime dt = DateTime.MinValue;

            try
            {
                dt = DateTime.ParseExact(
                    dateString, new string[] { "yyyyMMdd'T'HHmmss", "yyyyMMdd" }, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None );
            }
            catch ( Exception ex )
            {
                throw new GCalExchangeException( GCalExchangeErrorCode.MalformedRequest,
                    string.Format( "Date to parse is not in proper GCal supplied format. [{0}]", dateString ), ex );
            }

            return dt;
        }

		/// <summary>
		/// Return a date time at the start of the month after the one specified by val
		/// </summary>
		/// <param name="val">Date to return the EOM for</param>
        /// <returns>DateTime at the start of the month after val</returns>
		public static DateTime StartOfNextMonth(DateTime val)
		{
			return StartOfMonth(val).AddMonths(1);
		}

		/// <summary>
		/// Return a date time at the start of the month specified by val
		/// </summary>
		/// <param name="val">Date to return the SOM for</param>
        /// <returns>DateTime at the start of the current month</returns>
		public static DateTime StartOfMonth(DateTime val)
		{
			return new DateTime(val.Year, val.Month , 1, 0, 0, 0, DateTimeKind.Utc);
		}

    }
}
