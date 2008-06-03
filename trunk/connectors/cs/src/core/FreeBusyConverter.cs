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
using System.IO;
using System.Text;

using Google.GData.Calendar;
using Google.GCalExchangeSync.Library.WebDav;
using Google.GCalExchangeSync.Library.Util;

namespace Google.GCalExchangeSync.Library
{
    /// <summary>
    /// Handle conversion to/from the Exchnage Public Folder free busy format
    /// </summary>
    public class FreeBusyConverter
    {
        static private readonly DateTime utc1601_1_1 =
            new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Get the free busy format value for the month
        /// </summary>
        /// <param name="dt">DateTime to get the month from</param>
        /// <returns>The month value in the free busy format</returns>
        public static int GetFreeBusyMonthValue(DateTime dt)
        {
            return dt.Year * 16 + dt.Month;
        }

        /// <summary>
        /// Convert an Exchange free busy format month value to a datetime
        /// </summary>
        /// <param name="exchangeMonth">The Exchange free busy format month value</param>
        /// <returns>Datetime with the month set</returns>
        public static DateTime ParseFreeBusyMonthValue(int exchangeMonth)
        {
            return new DateTime(exchangeMonth >> 4, exchangeMonth & 15, 1);
        }

        /// <summary>
        /// Get the integer used in the free busy format for the DateTime
        /// </summary>
        /// <param name="dt">Datetime to convert</param>
        /// <returns>Integer to represent the date</returns>
        public static int GetFreeBusyTimeValue(DateTime dt)
        {
            return (60 * ((24 * (dt.Day - 1)) + dt.Hour)) + dt.Minute;
        }

        private static void AddFreeBusyHexTimeValue(List<Byte> hexValues, DateTime dt)
        {
            int freeBusyTime = GetFreeBusyTimeValue(dt);

            byte b1 = (byte)(freeBusyTime & 0xff);
            byte b2 = (byte)((freeBusyTime >> 8) & 0xff);

            hexValues.Add(b1);
            hexValues.Add(b2);
        }

        private static void EnsureMonthIsPresent(Dictionary<int, List<Byte>> result, int month)
        {
            List<Byte> data = null;

            if (!result.TryGetValue(month, out data))
            {
                data = new List<byte>();
                result[month] = data;
            }
        }

        private static void AddDateRange(Dictionary<int, List<Byte>> result, DateTimeRange r)
        {
            if (r.Start.Month != r.End.Month || r.Start.Year != r.End.Year)
            {
                throw new Exception("Free Busy Range Months must match");
            }

            int start = GetFreeBusyMonthValue(r.Start);

            List<Byte> data = null;

            if (!result.TryGetValue(start, out data))
            {
                data = new List<byte>();
                result[start] = data;
            }

            AddFreeBusyHexTimeValue(data, r.Start);
            AddFreeBusyHexTimeValue(data, r.End);
        }

        /// <summary>
        /// Convery DateTime ranges to the Exchange free busy format
        /// </summary>
        /// <param name="startDate">The start date of the free busy period</param>
        /// <param name="endDate">The end date of the free busy period</param>
        /// <param name="freeBusyRanges">List of datetime ranges to convert</param>
        /// <param name="monthData">Output month data</param>
        /// <param name="base64FreeBusyData">Output datetime data</param>
        public static void ConvertDateTimeBlocksToBase64String(
            DateTime startDate,
            DateTime endDate,
            List<DateTimeRange> freeBusyRanges,
            List<string> monthData,
            List<string> base64FreeBusyData)
        {
            Dictionary<int, List<Byte>> result = new Dictionary<int, List<byte>>();
            int startMonth = GetFreeBusyMonthValue(startDate);
            int endMonth = GetFreeBusyMonthValue(endDate);

            for (int month = startMonth; month <= endMonth; month++)
            {
                int extractedMonth = month & 15;
                if (extractedMonth >= 1 && extractedMonth <= 12)
                {
                    EnsureMonthIsPresent(result, month);
                }
            }

            foreach (DateTimeRange r in freeBusyRanges)
            {
                int start = GetFreeBusyMonthValue(r.Start);
                int end = GetFreeBusyMonthValue(r.End);

                if (start == end)
                {
                    // Common case - entire period is within the month
                    AddDateRange(result, r);
                }
                else
                {
                    // Date spans multiple months
                    DateTime monthEnd = DateUtil.StartOfNextMonth(r.Start);
                    DateTimeRange s = new DateTimeRange(r.Start, monthEnd.AddSeconds(-1));

                    AddDateRange(result, s);

                    int current = GetFreeBusyMonthValue(monthEnd);
                    while (current < end)
                    {
                        DateTime monthStart = monthEnd;
                        monthEnd = DateUtil.StartOfNextMonth(monthEnd);
                        s = new DateTimeRange(monthStart, monthEnd.AddSeconds(-1));
                        AddDateRange(result, s);
                        current = GetFreeBusyMonthValue(monthEnd);
                    }

                    s = new DateTimeRange(monthEnd, r.End);
                    AddDateRange(result, s);
                }
            }

            monthData.Clear();
            base64FreeBusyData.Clear();

            foreach (KeyValuePair<int, List<Byte>> pair in result)
            {
                monthData.Add(pair.Key.ToString());
                base64FreeBusyData.Add(Convert.ToBase64String(pair.Value.ToArray()));
            }
        }

        /// <summary>
        /// Convert Exchange Free Busy data to a list of DateTime ranges
        /// </summary>
        /// <param name="exchangeMonthValue">Exchange free busy month data</param>
        /// <param name="base64FreeBusyData">Exchange free busy date time data</param>
        /// <returns>A list of date time ranges</returns>
        public static List<DateTimeRange> ConvertBase64StringsToDateTimeBlocks(
            int exchangeMonthValue,
            string base64FreeBusyData)
        {
            List<DateTimeRange> dateTimeRanges = new List<DateTimeRange>();

            byte[] hexValuesArray = Convert.FromBase64String(base64FreeBusyData);

            if (hexValuesArray.Length % 4 != 0)
            {
                throw new Exception( "Byte array is not valid length" );
            }

            DateTime monthStart = ParseFreeBusyMonthValue(exchangeMonthValue);
            DateTime start = DateTime.MinValue;
            DateTime end = DateTime.MinValue;
            uint lowByte = 0;
            uint state = 0;

            foreach (uint hexValue in hexValuesArray)
            {
                // This loop essentially does:
                // start = monthStart +
                //     TimeSpan.FromMinutes(hexValuesArray[i + 0] + hexValuesArray[i + 1] << 8);
                // end   = monthStart +
                //     TimeSpan.FromMinutes(hexValuesArray[i + 2] + hexValuesArray[i + 3] << 8);
                // The dance is necessary in order to convince the JIT not to do size checks,
                // which happens 4 times if indexing was used.
                // So the state variable is used to keep the offset (0, 1, 2 or 3) and do
                // the right action.
                switch (state)
                {
                    case 0:
                    case 2:
                        lowByte = hexValue;
                        state++;

                        break;

                    case 1:
                        start = monthStart + TimeSpan.FromMinutes(lowByte + (uint)(hexValue << 8));
                        state = 2;

                        break;

                    case 3:
                        end = monthStart + TimeSpan.FromMinutes(lowByte + (uint)(hexValue << 8));
                        dateTimeRanges.Add(new DateTimeRange(start, end));
                        state = 0;

                        break;
                }
            }

            return dateTimeRanges;
        }

        /// <summary>
        /// Convert a date time to system time - minutes since 1601/1/1 UTC
        /// </summary>
        /// <param name="dt">DateTime to convert</param>
        /// <returns>System time corresponding to the value</returns>
        public static double ConvertToSysTime(DateTime dt)
        {
            TimeSpan ts = dt.Subtract(utc1601_1_1);
            return ts.TotalMinutes;
        }
    }
}
