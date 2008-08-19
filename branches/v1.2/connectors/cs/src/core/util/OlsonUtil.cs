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
using TZ4Net;

namespace Google.GCalExchangeSync.Library.Util
{
    /// <summary>
    /// Utilities with working with Olson Timezone data
    /// </summary>
    public class OlsonUtil
    {
        /// <summary>
        /// logging for OlsonUtil
        /// </summary>
        protected static readonly log4net.ILog _log =
            log4net.LogManager.GetLogger(typeof(OlsonUtil));

        /// <summary>
        /// Convert a time in a timezone to UTC
        /// </summary>
        /// <param name="srcTime">Time to convert</param>
        /// <param name="srcZone">Timezone of the DateTime</param>
        /// <returns>DateTime in UTC</returns>
        public static DateTime ConvertToUTC(DateTime srcTime, OlsonTimeZone srcZone)
        {
            TimeCheckResult srcCheckRes = srcZone.Check(srcTime);

            switch (srcCheckRes)
            {
                case TimeCheckResult.Valid:
                    {
                        return srcZone.ToUniversalTime(srcTime);
                    }
                case TimeCheckResult.InSpringForwardGap:
                case TimeCheckResult.InFallBackRange:
                    _log.ErrorFormat("Source time in transition period - retry.  [date={0}, tz={1}]", srcTime, srcZone.Name );
                    return ConvertFromUTC(srcTime.AddHours(1), srcZone);

                default:
                    {
                        string errorMessage = string.Format(
                            "Source time out of range.  [date={0}, tz={1}]", srcTime, srcZone.Name );

                        throw new GCalExchangeException(
                            GCalExchangeErrorCode.OlsonTZError, errorMessage );
                    }
            }
        }

        /// <summary>
        /// Convert a time in a timezone to UTC
        /// </summary>
        /// <param name="srcTime">Time to convert</param>
        /// <param name="srcName">Timezone of the DateTime</param>
        /// <returns>DateTime in UTC</returns>
        public static DateTime ConvertToUTC(DateTime srcTime, string srcName)
        {
            OlsonTimeZone srcZone = GetTimeZone(srcName);

            return ConvertToUTC(srcTime, srcZone);
        }

        /// <summary>
        /// Convert a time from UTC to a timezone
        /// </summary>
        /// <param name="srcTime">Time to convert</param>
        /// <param name="dstZone">Timezone to convert the datetime to</param>
        /// <returns>DateTime in the timezone</returns>
        public static DateTime ConvertFromUTC(DateTime srcTime, OlsonTimeZone dstZone)
        {
            TimeCheckResult srcCheckRes = dstZone.Check(srcTime);

            switch (srcCheckRes)
            {
                case TimeCheckResult.Valid:
                    {
                        DateTime dstTime = dstZone.ToLocalTime(srcTime);
                        return dstTime;
                    }
                case TimeCheckResult.InSpringForwardGap:
                case TimeCheckResult.InFallBackRange:
                    _log.ErrorFormat("Source time in transition period - retry.  [date={0}, tz={1}]", srcTime, dstZone.Name);
                    return ConvertFromUTC(srcTime.AddHours(1), dstZone);

                default:
                    {
                        string errorMessage = string.Format(
                            "Source time out of range.  [date={0}, tz={1}]", srcTime, dstZone.Name );

                        throw new GCalExchangeException(
                            GCalExchangeErrorCode.OlsonTZError, errorMessage );
                    }
            }
        }

        /// <summary>
        /// Convert a time from UTC to a timezone
        /// </summary>
        /// <param name="srcTime">Time to convert</param>
        /// <param name="dstName">Timezone to convert the datetime to</param>
        /// <returns>DateTime in the timezone</returns>
        public static DateTime ConvertFromUTC(DateTime srcTime, string dstName)
        {
            OlsonTimeZone dstZone = GetTimeZone(dstName);

            return ConvertFromUTC(srcTime, dstZone);
        }

        /// <summary>
        /// Convert a date time from one timezone to another.
        /// </summary>
        /// <param name="src">The datetime to convert</param>
        /// <param name="srcName">The timezone to convert from</param>
        /// <param name="dstTname">The timezone to convert to</param>
        /// <returns>The Datetime in the new timezone</returns>
        public static DateTime ConvertToTimeZone(DateTime src, string srcName, string dstTname)
        {
            DateTime ret = ConvertToUTC(src, srcName);

            ret = ConvertFromUTC(ret, dstTname);

            return ret;
        }

        /// <summary>
        /// Convert a date time from one timezone to another.
        /// </summary>
        /// <param name="src">The datetime to convert</param>
        /// <param name="srcZone">The timezone to convert from</param>
        /// <param name="dstZone">The timezone to convert to</param>
        /// <returns>The Datetime in the new timezone</returns>
        public static DateTime ConvertToTimeZone(DateTime src, OlsonTimeZone srcZone, OlsonTimeZone dstZone)
        {
            DateTime ret = ConvertToUTC(src, srcZone);

            ret = ConvertFromUTC(ret, dstZone);

            return ret;
        }

        /// <summary>
        /// Convert a timezone name into a time zone object
        /// </summary>
        /// <param name="name">The timezone name</param>
        /// <returns>The OlsonTimeZone for the time zone</returns>
        public static OlsonTimeZone GetTimeZone(string name)
        {
            if (OlsonTimeZone.LookupName(name) == null)
            {
                throw new GCalExchangeException(GCalExchangeErrorCode.OlsonTZError,
                           "Unknown destintation timezone name.");
            }

            OlsonTimeZone tz = OlsonTimeZone.GetInstance(name);

            return tz;
        }

        /// <summary>
        /// Default Time Zone for the system
        /// </summary>
        public static OlsonTimeZone DefaultTimeZone =
            OlsonUtil.GetTimeZone(System.TimeZone.CurrentTimeZone.StandardName);
    }
}
