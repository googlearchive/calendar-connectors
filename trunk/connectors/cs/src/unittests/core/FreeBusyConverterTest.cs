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
using System.Diagnostics;
using System.DirectoryServices;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Reflection;

using Google.GCalExchangeSync.Library.WebDav;
using Google.GCalExchangeSync.Library.Util;

using NUnit.Framework;

namespace Google.GCalExchangeSync.Library
{
    [TestFixture]
    public class FreeBusyConverterTest
    {
        static private readonly DateTime kUtc1601_1_1 =
            new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        [Test]
        public void TestDateAdvance()
        {

            DateTime r = DateUtil.ParseDateToUtc("2007-12-30T00:00:00.000Z");
            r = DateUtil.StartOfNextMonth(r);

            Assert.AreEqual(new DateTime(2008, 01, 01, 00, 00, 00, DateTimeKind.Unspecified), r);
        }

        [Test]
        public void TestConvertToSysTime()
        {
            long ticksInMinute = (long)60 * 10000000;
            DateTime utc1601_1_1 = new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime rawNow = DateTime.Now;
            DateTime now = new DateTime(rawNow.Year,
                                        rawNow.Month,
                                        rawNow.Day,
                                        rawNow.Hour,
                                        rawNow.Minute,
                                        0,
                                        0,
                                        rawNow.Kind);
            int prevSysTime = FreeBusyConverter.ConvertToSysTime(now) - 1;

            for (int i = 0; i < 60 * 24 * 368; i++)
            {
                DateTime future = now.AddTicks(i * ticksInMinute);
                int sysTime = FreeBusyConverter.ConvertToSysTime(future);
                DateTime check = utc1601_1_1.AddTicks(sysTime * ticksInMinute);

                Assert.AreEqual(future, check);
                Assert.AreEqual(prevSysTime + 1, sysTime);

                prevSysTime++;
            }
        }

        [Test]
        public void TestConvertRasterToFreeBusy()
        {
            for (char c = ' '; c < 256; c++)
            {
                if (c < '0' || c > '4')
                {
                    Assert.AreEqual(BusyStatus.Free, FreeBusyConverter.ConvertRasterToFreeBusy(c));
                }
            }

            Assert.AreEqual(BusyStatus.Free, FreeBusyConverter.ConvertRasterToFreeBusy('0'));
            Assert.AreEqual(BusyStatus.Tentative, FreeBusyConverter.ConvertRasterToFreeBusy('1'));
            Assert.AreEqual(BusyStatus.Busy, FreeBusyConverter.ConvertRasterToFreeBusy('2'));
            Assert.AreEqual(BusyStatus.OutOfOffice, FreeBusyConverter.ConvertRasterToFreeBusy('3'));
            Assert.AreEqual(BusyStatus.Free, FreeBusyConverter.ConvertRasterToFreeBusy('4'));
        }

        private static void ClearFreeBusy(
            FreeBusy freeBusy)
        {
            freeBusy.All.Clear();
            freeBusy.Busy.Clear();
            freeBusy.Tentative.Clear();
            freeBusy.OutOfOffice.Clear();
        }

        [Test]
        public void TestParseRasterFreeBusy()
        {
            DateTime startDate = new DateTime(2008, 05, 1, 10, 0, 0, DateTimeKind.Utc);
            DateTime date1 = new DateTime(2008, 05, 1, 10, 15, 0, DateTimeKind.Utc);
            DateTime date2 = new DateTime(2008, 05, 1, 10, 30, 0, DateTimeKind.Utc);
            DateTime date3 = new DateTime(2008, 05, 1, 10, 45, 0, DateTimeKind.Utc);
            DateTime date4 = new DateTime(2008, 05, 1, 11, 0, 0, DateTimeKind.Utc);
            DateTime date5 = new DateTime(2008, 05, 1, 11, 15, 0, DateTimeKind.Utc);
            FreeBusy freeBusy = new FreeBusy();

            FreeBusyConverter.ParseRasterFreeBusy(startDate, 15, "1", freeBusy);
            Assert.AreEqual(freeBusy.All.Count, 0);
            Assert.AreEqual(freeBusy.Busy.Count, 0);
            Assert.AreEqual(freeBusy.OutOfOffice.Count, 0);
            Assert.AreEqual(freeBusy.Tentative.Count, 1);
            Assert.AreEqual(freeBusy.Tentative[0].Start, startDate);
            Assert.AreEqual(freeBusy.Tentative[0].End, date1);
            ClearFreeBusy(freeBusy);

            FreeBusyConverter.ParseRasterFreeBusy(startDate, 15, "2", freeBusy);
            Assert.AreEqual(freeBusy.All.Count, 1);
            Assert.AreEqual(freeBusy.Busy.Count, 1);
            Assert.AreEqual(freeBusy.OutOfOffice.Count, 0);
            Assert.AreEqual(freeBusy.Tentative.Count, 0);
            Assert.AreEqual(freeBusy.All[0].Start, startDate);
            Assert.AreEqual(freeBusy.All[0].End, date1);
            Assert.AreEqual(freeBusy.Busy[0].Start, startDate);
            Assert.AreEqual(freeBusy.Busy[0].End, date1);
            ClearFreeBusy(freeBusy);

            FreeBusyConverter.ParseRasterFreeBusy(startDate, 15, "3", freeBusy);
            Assert.AreEqual(freeBusy.All.Count, 1);
            Assert.AreEqual(freeBusy.Busy.Count, 0);
            Assert.AreEqual(freeBusy.OutOfOffice.Count, 1);
            Assert.AreEqual(freeBusy.Tentative.Count, 0);
            Assert.AreEqual(freeBusy.All[0].Start, startDate);
            Assert.AreEqual(freeBusy.All[0].End, date1);
            Assert.AreEqual(freeBusy.OutOfOffice[0].Start, startDate);
            Assert.AreEqual(freeBusy.OutOfOffice[0].End, date1);
            ClearFreeBusy(freeBusy);

            FreeBusyConverter.ParseRasterFreeBusy(startDate, 15, "4", freeBusy);
            Assert.AreEqual(freeBusy.All.Count, 0);
            Assert.AreEqual(freeBusy.Busy.Count, 0);
            Assert.AreEqual(freeBusy.OutOfOffice.Count, 0);
            Assert.AreEqual(freeBusy.Tentative.Count, 0);
            ClearFreeBusy(freeBusy);

            FreeBusyConverter.ParseRasterFreeBusy(startDate, 15, "11", freeBusy);
            Assert.AreEqual(freeBusy.All.Count, 0);
            Assert.AreEqual(freeBusy.Busy.Count, 0);
            Assert.AreEqual(freeBusy.OutOfOffice.Count, 0);
            Assert.AreEqual(freeBusy.Tentative.Count, 1);
            Assert.AreEqual(freeBusy.Tentative[0].Start, startDate);
            Assert.AreEqual(freeBusy.Tentative[0].End, date2);
            ClearFreeBusy(freeBusy);

            FreeBusyConverter.ParseRasterFreeBusy(startDate, 15, "22", freeBusy);
            Assert.AreEqual(freeBusy.All.Count, 1);
            Assert.AreEqual(freeBusy.Busy.Count, 1);
            Assert.AreEqual(freeBusy.OutOfOffice.Count, 0);
            Assert.AreEqual(freeBusy.Tentative.Count, 0);
            Assert.AreEqual(freeBusy.All[0].Start, startDate);
            Assert.AreEqual(freeBusy.All[0].End, date2);
            Assert.AreEqual(freeBusy.Busy[0].Start, startDate);
            Assert.AreEqual(freeBusy.Busy[0].End, date2);
            ClearFreeBusy(freeBusy);

            FreeBusyConverter.ParseRasterFreeBusy(startDate, 15, "33", freeBusy);
            Assert.AreEqual(freeBusy.All.Count, 1);
            Assert.AreEqual(freeBusy.Busy.Count, 0);
            Assert.AreEqual(freeBusy.OutOfOffice.Count, 1);
            Assert.AreEqual(freeBusy.Tentative.Count, 0);
            Assert.AreEqual(freeBusy.All[0].Start, startDate);
            Assert.AreEqual(freeBusy.All[0].End, date2);
            Assert.AreEqual(freeBusy.OutOfOffice[0].Start, startDate);
            Assert.AreEqual(freeBusy.OutOfOffice[0].End, date2);
            ClearFreeBusy(freeBusy);

            FreeBusyConverter.ParseRasterFreeBusy(startDate, 15, "44", freeBusy);
            Assert.AreEqual(freeBusy.All.Count, 0);
            Assert.AreEqual(freeBusy.Busy.Count, 0);
            Assert.AreEqual(freeBusy.OutOfOffice.Count, 0);
            Assert.AreEqual(freeBusy.Tentative.Count, 0);
            ClearFreeBusy(freeBusy);

            FreeBusyConverter.ParseRasterFreeBusy(startDate, 15, "0114", freeBusy);
            Assert.AreEqual(freeBusy.All.Count, 0);
            Assert.AreEqual(freeBusy.Busy.Count, 0);
            Assert.AreEqual(freeBusy.OutOfOffice.Count, 0);
            Assert.AreEqual(freeBusy.Tentative.Count, 1);
            Assert.AreEqual(freeBusy.Tentative[0].Start, date1);
            Assert.AreEqual(freeBusy.Tentative[0].End, date3);
            ClearFreeBusy(freeBusy);

            FreeBusyConverter.ParseRasterFreeBusy(startDate, 15, "0224", freeBusy);
            Assert.AreEqual(freeBusy.All.Count, 1);
            Assert.AreEqual(freeBusy.Busy.Count, 1);
            Assert.AreEqual(freeBusy.OutOfOffice.Count, 0);
            Assert.AreEqual(freeBusy.Tentative.Count, 0);
            Assert.AreEqual(freeBusy.All[0].Start, date1);
            Assert.AreEqual(freeBusy.All[0].End, date3);
            Assert.AreEqual(freeBusy.Busy[0].Start, date1);
            Assert.AreEqual(freeBusy.Busy[0].End, date3);
            ClearFreeBusy(freeBusy);

            FreeBusyConverter.ParseRasterFreeBusy(startDate, 15, "0334", freeBusy);
            Assert.AreEqual(freeBusy.All.Count, 1);
            Assert.AreEqual(freeBusy.Busy.Count, 0);
            Assert.AreEqual(freeBusy.OutOfOffice.Count, 1);
            Assert.AreEqual(freeBusy.Tentative.Count, 0);
            Assert.AreEqual(freeBusy.All[0].Start, date1);
            Assert.AreEqual(freeBusy.All[0].End, date3);
            Assert.AreEqual(freeBusy.OutOfOffice[0].Start, date1);
            Assert.AreEqual(freeBusy.OutOfOffice[0].End, date3);
            ClearFreeBusy(freeBusy);

            FreeBusyConverter.ParseRasterFreeBusy(startDate, 15, "0440", freeBusy);
            Assert.AreEqual(freeBusy.All.Count, 0);
            Assert.AreEqual(freeBusy.Busy.Count, 0);
            Assert.AreEqual(freeBusy.OutOfOffice.Count, 0);
            Assert.AreEqual(freeBusy.Tentative.Count, 0);
            ClearFreeBusy(freeBusy);

            FreeBusyConverter.ParseRasterFreeBusy(startDate, 15, "40312", freeBusy);
            Assert.AreEqual(freeBusy.All.Count, 2);
            Assert.AreEqual(freeBusy.Busy.Count, 1);
            Assert.AreEqual(freeBusy.OutOfOffice.Count, 1);
            Assert.AreEqual(freeBusy.Tentative.Count, 1);
            Assert.AreEqual(freeBusy.All[0].Start, date2);
            Assert.AreEqual(freeBusy.All[0].End, date3);
            Assert.AreEqual(freeBusy.All[1].Start, date4);
            Assert.AreEqual(freeBusy.All[1].End, date5);
            Assert.AreEqual(freeBusy.Busy[0].Start, date4);
            Assert.AreEqual(freeBusy.Busy[0].End, date5);
            Assert.AreEqual(freeBusy.Tentative[0].Start, date3);
            Assert.AreEqual(freeBusy.Tentative[0].End, date4);
            Assert.AreEqual(freeBusy.OutOfOffice[0].Start, date2);
            Assert.AreEqual(freeBusy.OutOfOffice[0].End, date3);
            ClearFreeBusy(freeBusy);
        }

        [Test]
        public void TestConvertDateTimeBlocks()
        {
            List<DateTimeRange> src = new List<DateTimeRange>();
            List<DateTimeRange> dst = new List<DateTimeRange>();

            DateTime s = DateTime.MinValue;
            DateTime e = DateTime.MinValue;
            DateTime startDate = new DateTime(2007, 07, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime endDate = new DateTime(2008, 05, 31, 0, 0, 0, DateTimeKind.Utc);

            s = new DateTime(2007, 07, 23, 13, 45, 00, DateTimeKind.Unspecified);
            src.Add(new DateTimeRange(s, s.AddHours(3)));
            dst.Add(new DateTimeRange(s, s.AddHours(3)));

            s = new DateTime(2007, 07, 30, 13, 45, 00, DateTimeKind.Unspecified);
            src.Add(new DateTimeRange(s, s));
            dst.Add(new DateTimeRange(s, s));

            s = new DateTime(2007, 08, 15, 13, 45, 00, DateTimeKind.Unspecified);
            e = new DateTime(2007, 10, 11, 02, 45, 00, DateTimeKind.Unspecified);
            src.Add(new DateTimeRange(s, e));
            dst.Add(new DateTimeRange(s,
                new DateTime(2007, 08, 31, 23, 59, 00, DateTimeKind.Unspecified)));
            dst.Add(new DateTimeRange(
                new DateTime(2007, 09, 01, 00, 00, 00, DateTimeKind.Unspecified),
                new DateTime(2007, 09, 30, 23, 59, 00, DateTimeKind.Unspecified)));
            dst.Add(new DateTimeRange(
                new DateTime(2007, 10, 01, 00, 00, 00, DateTimeKind.Unspecified), e));

            src.Add(new DateTimeRange(
                new DateTime(2008, 01, 01, 00, 00, 00, DateTimeKind.Unspecified),
                new DateTime(2008, 03, 31, 23, 59, 00, DateTimeKind.Unspecified)));
            dst.Add(new DateTimeRange(
                new DateTime(2008, 01, 01, 00, 00, 00, DateTimeKind.Unspecified),
                new DateTime(2008, 01, 31, 23, 59, 00, DateTimeKind.Unspecified)));
            dst.Add(new DateTimeRange(
                new DateTime(2008, 02, 01, 00, 00, 00, DateTimeKind.Unspecified),
                new DateTime(2008, 02, 29, 23, 59, 00, DateTimeKind.Unspecified)));
            dst.Add(new DateTimeRange(
                new DateTime(2008, 03, 01, 00, 00, 00, DateTimeKind.Unspecified),
                new DateTime(2008, 03, 31, 23, 59, 00, DateTimeKind.Unspecified)));

            src.Add(new DateTimeRange(
                new DateTime(2008, 04, 01, 00, 00, 00, DateTimeKind.Unspecified),
                new DateTime(2008, 05, 30, 23, 59, 00, DateTimeKind.Unspecified)));
            dst.Add(new DateTimeRange(
                new DateTime(2008, 04, 01, 00, 00, 00, DateTimeKind.Unspecified),
                new DateTime(2008, 04, 30, 23, 59, 00, DateTimeKind.Unspecified)));
            dst.Add(new DateTimeRange(
                new DateTime(2008, 05, 01, 00, 00, 00, DateTimeKind.Unspecified),
                new DateTime(2008, 05, 30, 23, 59, 00, DateTimeKind.Unspecified)));

            List<string> monthData = new List<string>();
            List<string> base64Data = new List<string>();

            FreeBusyConverter.ConvertDateTimeBlocksToBase64String(startDate,
                                                                  endDate,
                                                                  src,
                                                                  monthData,
                                                                  base64Data);

            List<DateTimeRange> result = new List<DateTimeRange>();

            for (int i = 0; i < monthData.Count; i++)
            {
                int month = int.Parse(monthData[i]);
                string data = base64Data[i];

                List<DateTimeRange> r =
                    FreeBusyConverter.ConvertBase64StringsToDateTimeBlocks(month, data);

                result.AddRange(r);
            }

            Assert.AreEqual(dst.Count, result.Count);
            for (int i = 0; i < dst.Count; i++)
            {
                Assert.AreEqual(dst[i], result[i]);
            }
        }

        void dump(List<DateTimeRange> range)
        {
            foreach (DateTimeRange r in range)
            {
                Console.WriteLine("DST: {0}", r);
            }
        }

        [Test]
        public void TestCondenseFreeBusyTimes()
        {
            DateTimeRange range = null;
            List<DateTimeRange> testFreeBusyTimes = new List<DateTimeRange>();
            List<DateTimeRange> expectedResult = new List<DateTimeRange>();

            // The first test case is
            // Event 1 [.....]
            // Event 2    [.....]
            // Event 3       [.....]
            // Result  [...........]
            range = new DateTimeRange(DateTime.Parse("2007-06-01T07:00:00.000Z"),
                                      DateTime.Parse("2007-06-01T08:00:00.000Z"));
            testFreeBusyTimes.Add(range);

            range = new DateTimeRange(DateTime.Parse("2007-06-01T07:30:00.000Z"),
                                      DateTime.Parse("2007-06-01T08:30:00.000Z"));
            testFreeBusyTimes.Add(range);

            range = new DateTimeRange(DateTime.Parse("2007-06-01T08:00:00.000Z"),
                                      DateTime.Parse("2007-06-01T09:00:00.000Z"));
            testFreeBusyTimes.Add(range);

            range = new DateTimeRange(DateTime.Parse("2007-06-01T07:00:00.000Z"),
                                      DateTime.Parse("2007-06-01T09:00:00.000Z"));
            expectedResult.Add(range);

            // The second test case is
            // Event 1 [.....]
            // Event 2       [.....]
            // Result  [...........]
            range = new DateTimeRange(DateTime.Parse("2007-06-02T07:00:00.000Z"),
                                      DateTime.Parse("2007-06-02T08:00:00.000Z"));
            testFreeBusyTimes.Add(range);

            range = new DateTimeRange(DateTime.Parse("2007-06-02T08:00:00.000Z"),
                                      DateTime.Parse("2007-06-02T09:00:00.000Z"));
            testFreeBusyTimes.Add(range);

            range = new DateTimeRange(DateTime.Parse("2007-06-02T07:00:00.000Z"),
                                      DateTime.Parse("2007-06-02T09:00:00.000Z"));
            expectedResult.Add(range);

            // The third test case is
            // Event 1 [.....]
            // Event 2 [.....]
            // Result  [.....]
            range = new DateTimeRange(DateTime.Parse("2007-06-03T07:00:00.000Z"),
                                      DateTime.Parse("2007-06-03T08:00:00.000Z"));
            testFreeBusyTimes.Add(range);

            range = new DateTimeRange(DateTime.Parse("2007-06-03T07:00:00.000Z"),
                                      DateTime.Parse("2007-06-03T08:00:00.000Z"));
            testFreeBusyTimes.Add(range);

            range = new DateTimeRange(DateTime.Parse("2007-06-03T07:00:00.000Z"),
                                      DateTime.Parse("2007-06-03T08:00:00.000Z"));
            expectedResult.Add(range);

            // The forth test case is
            // Event 1 [..]
            // Event 2    [.....]
            // Event 3          [..]
            // Result  [...........]
            range = new DateTimeRange(DateTime.Parse("2007-06-04T07:00:00.000Z"),
                                      DateTime.Parse("2007-06-04T07:30:00.000Z"));
            testFreeBusyTimes.Add(range);

            range = new DateTimeRange(DateTime.Parse("2007-06-04T07:30:00.000Z"),
                                      DateTime.Parse("2007-06-04T08:30:00.000Z"));
            testFreeBusyTimes.Add(range);

            range = new DateTimeRange(DateTime.Parse("2007-06-04T08:30:00.000Z"),
                                      DateTime.Parse("2007-06-04T09:00:00.000Z"));
            testFreeBusyTimes.Add(range);

            range = new DateTimeRange(DateTime.Parse("2007-06-04T07:00:00.000Z"),
                                      DateTime.Parse("2007-06-04T09:00:00.000Z"));
            expectedResult.Add(range);

            // The fifth test case is
            // Event 1 [..........]
            // Event 2   [.....]
            // Result  [..........]
            range = new DateTimeRange(DateTime.Parse("2007-06-05T07:00:00.000Z"),
                                      DateTime.Parse("2007-06-05T09:00:00.000Z"));
            testFreeBusyTimes.Add(range);

            range = new DateTimeRange(DateTime.Parse("2007-06-05T07:30:00.000Z"),
                                      DateTime.Parse("2007-06-05T08:30:00.000Z"));
            testFreeBusyTimes.Add(range);

            range = new DateTimeRange(DateTime.Parse("2007-06-05T07:00:00.000Z"),
                                      DateTime.Parse("2007-06-05T09:00:00.000Z"));
            expectedResult.Add(range);

            // The sixth test case is
            // Event 1 [.....]
            // Event 2 [...]
            // Result  [.....]
            range = new DateTimeRange(DateTime.Parse("2007-06-06T07:00:00.000Z"),
                                      DateTime.Parse("2007-06-06T08:00:00.000Z"));
            testFreeBusyTimes.Add(range);

            range = new DateTimeRange(DateTime.Parse("2007-06-06T07:00:00.000Z"),
                                      DateTime.Parse("2007-06-06T07:30:00.000Z"));
            testFreeBusyTimes.Add(range);

            range = new DateTimeRange(DateTime.Parse("2007-06-06T07:00:00.000Z"),
                                      DateTime.Parse("2007-06-06T08:00:00.000Z"));
            expectedResult.Add(range);

            // The seventh test case is
            // Event 1 [.....]
            // Event 2   [...]
            // Result  [.....]
            range = new DateTimeRange(DateTime.Parse("2007-06-07T07:00:00.000Z"),
                                      DateTime.Parse("2007-06-07T08:00:00.000Z"));
            testFreeBusyTimes.Add(range);

            range = new DateTimeRange(DateTime.Parse("2007-06-07T07:30:00.000Z"),
                                      DateTime.Parse("2007-06-07T08:00:00.000Z"));
            testFreeBusyTimes.Add(range);

            range = new DateTimeRange(DateTime.Parse("2007-06-07T07:00:00.000Z"),
                                      DateTime.Parse("2007-06-07T08:00:00.000Z"));
            expectedResult.Add(range);

            // The eight test case is
            // Event 1 [...............]
            // Event 2   [...]
            // Event 3          [...]
            // Result  [...............]
            range = new DateTimeRange(DateTime.Parse("2007-06-08T07:00:00.000Z"),
                                      DateTime.Parse("2007-06-08T10:00:00.000Z"));
            testFreeBusyTimes.Add(range);

            range = new DateTimeRange(DateTime.Parse("2007-06-08T07:30:00.000Z"),
                                      DateTime.Parse("2007-06-08T08:00:00.000Z"));
            testFreeBusyTimes.Add(range);

            range = new DateTimeRange(DateTime.Parse("2007-06-08T08:30:00.000Z"),
                                      DateTime.Parse("2007-06-08T09:00:00.000Z"));
            testFreeBusyTimes.Add(range);

            range = new DateTimeRange(DateTime.Parse("2007-06-08T07:00:00.000Z"),
                                      DateTime.Parse("2007-06-08T10:00:00.000Z"));
            expectedResult.Add(range);

            // The ninth test case is
            // Event 1 [.....]
            // Event 2            [.....]
            // Result  [.....]    [.....]
            range = new DateTimeRange(DateTime.Parse("2007-06-09T07:00:00.000Z"),
                                      DateTime.Parse("2007-06-09T08:00:00.000Z"));
            testFreeBusyTimes.Add(range);
            expectedResult.Add(range);

            range = new DateTimeRange(DateTime.Parse("2007-06-09T09:00:00.000Z"),
                                      DateTime.Parse("2007-06-09T10:00:00.000Z"));
            testFreeBusyTimes.Add(range);
            expectedResult.Add(range);

            testFreeBusyTimes.Sort(ReverseCompareRanges);

            List<DateTimeRange> copyFreeBusyTimes = new List<DateTimeRange>();
            foreach (DateTimeRange temp in testFreeBusyTimes)
            {
                copyFreeBusyTimes.Add(new DateTimeRange(temp.Start, temp.End));
            }

            List<DateTimeRange> oldResult = OldCondenseFreeBusyTimes(copyFreeBusyTimes);

            FreeBusyConverter.CondenseFreeBusyTimes(testFreeBusyTimes);

            Assert.IsTrue(CompareRangeLists(testFreeBusyTimes, expectedResult));
            Assert.IsTrue(CompareRangeLists(testFreeBusyTimes, oldResult));
        }

        private static bool CompareRangeLists(
            List<DateTimeRange> first,
            List<DateTimeRange> second)
        {
            if (first.Count != second.Count)
            {
                return false;
            }

            IEnumerable<DateTimeRange> enumerableFirst = first as IEnumerable<DateTimeRange>;
            IEnumerable<DateTimeRange> enumerableSecond = second as IEnumerable<DateTimeRange>;
            IEnumerator<DateTimeRange> enumeratorFirst = enumerableFirst.GetEnumerator();
            IEnumerator<DateTimeRange> enumeratorSecond = enumerableSecond.GetEnumerator();

            while (enumeratorFirst.MoveNext() && enumeratorSecond.MoveNext())
            {
                if (enumeratorFirst.Current.CompareTo(enumeratorSecond.Current) != 0)
                {
                    return false;
                }
            }

            return enumeratorFirst.MoveNext() == enumeratorSecond.MoveNext();
        }

        private static int ReverseCompareRanges(
            DateTimeRange x,
            DateTimeRange y)
        {
            return FreeBusyConverter.CompareRangesByStartThenEnd(y, x);
        }

        private static List<DateTimeRange> OldCondenseFreeBusyTimes(
            List<DateTimeRange> freeBusyTimes)
        {
            List<DateTimeRange> condensedFreeBusyTimes = new List<DateTimeRange>();

            freeBusyTimes.Sort();

            foreach (DateTimeRange existingTime in freeBusyTimes)
            {
                OldAddFreeBusyTime(condensedFreeBusyTimes, existingTime);
            }

            return condensedFreeBusyTimes;
        }

        private static void OldAddFreeBusyTime(
            List<DateTimeRange> freeBusyTimes,
            DateTimeRange newEntry)
        {
            int i = 0;
            DateTime minStartDate, maxEndDate;

            bool updatedExistingRange = false;

            foreach (DateTimeRange existingTime in freeBusyTimes)
            {
                if (DateUtil.IsWithinRange(newEntry.Start, existingTime.Start, existingTime.End) ||
                     DateUtil.IsWithinRange(newEntry.End, existingTime.Start, existingTime.End))
                {
                    minStartDate = (newEntry.Start < existingTime.Start) ?
                        newEntry.Start : existingTime.Start;
                    maxEndDate = (newEntry.End > existingTime.End) ?
                        newEntry.End : existingTime.End;

                    existingTime.Start = minStartDate;
                    existingTime.End = maxEndDate;

                    updatedExistingRange = true;

                    i++;
                }
            }

            if (!updatedExistingRange)
            {
                freeBusyTimes.Add(newEntry);
            }
        }
    }
}
