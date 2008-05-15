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

using Google.GData.Calendar;
using Google.GData.Client;
using Google.GData.Extensions;

using Google.GCalExchangeSync.Library.WebDav;
using Google.GCalExchangeSync.Library.Util;

using TZ4Net;
using NUnit.Framework;

namespace Google.GCalExchangeSync.Library
{
    [TestFixture]
    public class SchedulePlusFreeBusyWriterTest
    {

        [SetUp]
        public void Init()
        {
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

            CallCondenseFreeBusyTimes(testFreeBusyTimes);

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
            object[] parameters = new object[2] { y, x };
            BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;

            Type type = typeof(SchedulePlusFreeBusyWriter);
            MethodInfo methodInfo = type.GetMethod("CompareRangesByStartThenEnd", flags);

            return (int)methodInfo.Invoke(null, parameters);
        }

        private static void CallCondenseFreeBusyTimes(
            List<DateTimeRange> freeBusyTimes)
        {
            object[] parameters = new object[1] { freeBusyTimes };
            BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;

            Type type = typeof(SchedulePlusFreeBusyWriter);
            MethodInfo methodInfo = type.GetMethod("CondenseFreeBusyTimes", flags);

            methodInfo.Invoke(null, parameters);
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

            foreach ( DateTimeRange existingTime in freeBusyTimes )
            {
                if ( DateUtil.IsWithinRange( newEntry.Start, existingTime.Start, existingTime.End ) ||
                      DateUtil.IsWithinRange( newEntry.End, existingTime.Start, existingTime.End ) )
                {
                    minStartDate = ( newEntry.Start < existingTime.Start ) ?
                        newEntry.Start : existingTime.Start;
                    maxEndDate = ( newEntry.End > existingTime.End ) ?
                        newEntry.End : existingTime.End;

                    existingTime.Start = minStartDate;
                    existingTime.End = maxEndDate;

                    updatedExistingRange = true;

                    i++;
                }
            }

            if ( !updatedExistingRange )
            {
                freeBusyTimes.Add( newEntry );
            }
        }
    }
}
