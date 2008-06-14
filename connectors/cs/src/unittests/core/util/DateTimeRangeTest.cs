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

using NUnit.Framework;

namespace Google.GCalExchangeSync.Library.Util
{
    [TestFixture]
    public class DateTimeRangeTest
    {
        private DateTime startDate;
        private DateTime endDate;

        [SetUp]
        public void Init()
        {
            startDate = DateUtil.ParseDateToUtc("2007-06-30T05:16:11.000Z");
            endDate = startDate.AddHours(2);
        }

        [Test]
        public void TestDateTimeRange()
        {
            DateTimeRange range = new DateTimeRange(startDate, endDate);
            DateTimeRange compareRange = new DateTimeRange(startDate, endDate);

            Assert.AreEqual(startDate, range.Start);
            Assert.AreEqual(endDate, range.End);
            Assert.AreEqual(range, compareRange);
            Assert.AreEqual(range.GetHashCode(), compareRange.GetHashCode());
        }

        [Test]
        public void TestOverlapsAndContains()
        {
            DateTime date7AM = new DateTime(2007, 1, 1, 7, 0, 0);
            DateTime date8AM = new DateTime(2007, 1, 1, 8, 0, 0);
            DateTime date9AM = new DateTime(2007, 1, 1, 9, 0, 0);
            DateTime date10AM = new DateTime(2007, 1, 1, 10, 0, 0);

            DateTimeRange range7AM8AM = new DateTimeRange(date7AM, date8AM);
            DateTimeRange range9AM10AM = new DateTimeRange(date9AM, date10AM);

            // The first test case is
            // 7-8  [.....]
            // 9-10           [.....]
            // Don't intersect and don't contain
            Assert.IsFalse(range7AM8AM.Overlaps(range9AM10AM));
            Assert.IsFalse(range9AM10AM.Overlaps(range7AM8AM));
            Assert.IsFalse(range7AM8AM.Contains(range9AM10AM));
            Assert.IsFalse(range9AM10AM.Contains(range7AM8AM));

            DateTimeRange range7AM10AM = new DateTimeRange(date7AM, date10AM);
            DateTimeRange range8AM9AM = new DateTimeRange(date8AM, date9AM);

            // The second test case is
            // 7-10 [...............]
            // 8-9       [.....]
            // Intersect, the bigger contains the smaller
            Assert.IsTrue(range7AM10AM.Overlaps(range8AM9AM));
            Assert.IsTrue(range8AM9AM.Overlaps(range7AM10AM));
            Assert.IsTrue(range7AM10AM.Contains(range8AM9AM));
            Assert.IsFalse(range8AM9AM.Contains(range7AM10AM));

            // The third test case is
            // 8-9       [.....]
            // 9-10           [.....]
            // Intersect (touch), don't contain each other
            Assert.IsTrue(range8AM9AM.Overlaps(range9AM10AM));
            Assert.IsTrue(range9AM10AM.Overlaps(range8AM9AM));
            Assert.IsFalse(range8AM9AM.Contains(range9AM10AM));
            Assert.IsFalse(range9AM10AM.Contains(range8AM9AM));

            DateTimeRange range7AM9AM = new DateTimeRange(date7AM, date9AM);
            DateTimeRange range8AM10AM = new DateTimeRange(date8AM, date10AM);

            // The third test case is
            // 7-9  [..........]
            // 9-10      [..........]
            // Intersect (touch), don't contain each other
            Assert.IsTrue(range7AM9AM.Overlaps(range8AM10AM));
            Assert.IsTrue(range8AM10AM.Overlaps(range7AM9AM));
            Assert.IsFalse(range7AM9AM.Contains(range8AM10AM));
            Assert.IsFalse(range8AM10AM.Contains(range7AM9AM));
        }

        [Test]
        public void TestDateTimeCompare()
        {
            // (From MSDN) For objects A, B, and C, the following must be true:
            //
            // 0. By definition, any object compares greater than a null reference.
            //
            // 1. A.CompareTo(A) is required to return zero.
            //
            // 2. If A.CompareTo(B) returns zero, then B.CompareTo(A) is required to return zero.
            //
            // 3. If A.CompareTo(B) returns zero and B.CompareTo(C) returns zero,
            //    then A.CompareTo(C) is required to return zero.
            //
            // 4. If A.CompareTo(B) returns a value other than zero,
            //    then B.CompareTo(A) is required to return a value of the opposite sign.
            //
            // 5. If A.CompareTo(B) returns a value x that is not equal to zero,
            //    and B.CompareTo(C) returns a value y of the same sign as x,
            //    then A.CompareTo(C) is required to return a value of the same sign as x and y.
            //

            DateTimeRange r0 = new DateTimeRange(
                DateTime.Parse("2007-06-30T05:16:11.000Z"),
                DateTime.Parse("2007-07-01T06:16:11.000Z"));

            DateTimeRange r0Copy1 = new DateTimeRange(
                DateTime.Parse("2007-06-30T05:16:11.000Z"),
                DateTime.Parse("2007-07-01T06:16:11.000Z"));

            DateTimeRange r0Copy2 = new DateTimeRange(
                DateTime.Parse("2007-06-30T05:16:11.000Z"),
                DateTime.Parse("2007-07-01T06:16:11.000Z"));

            DateTimeRange r1 = new DateTimeRange(
                DateTime.Parse("2007-06-30T05:16:11.000Z").ToUniversalTime(),
                DateTime.Parse("2007-07-01T06:16:11.000Z").ToUniversalTime());

            Assert.AreEqual(DateTimeKind.Local, r0.Start.Kind);
            Assert.AreEqual(DateTimeKind.Local, r0.End.Kind);

            Assert.AreEqual(DateTimeKind.Utc, r1.Start.Kind);
            Assert.AreEqual(DateTimeKind.Utc, r1.End.Kind);

            Assert.Greater(r0.CompareTo(null), 0); // 0.

            Assert.AreEqual(r0.CompareTo(r0), 0); // 1.
            Assert.AreEqual(r1.CompareTo(r1), 0); // 1.

            Assert.AreNotEqual(r0, r1);
            Assert.AreNotEqual(r0.CompareTo(r1), 0);
            Assert.AreNotEqual(r1.CompareTo(r0), 0);

            Assert.AreEqual(Sign(r0.CompareTo(r1)), -Sign(r1.CompareTo(r0))); // 4.

            Assert.AreEqual(r0.CompareTo(r0Copy1), 0);
            Assert.AreEqual(r0Copy1.CompareTo(r0), 0); // 2.
            Assert.AreEqual(r0.CompareTo(r0Copy2), 0);
            Assert.AreEqual(r0Copy2.CompareTo(r0), 0); // 2.
            Assert.AreEqual(r0Copy2.CompareTo(r0Copy1), 0); // 3.
            Assert.AreEqual(r0Copy1.CompareTo(r0Copy2), 0); // 3.

            // Get a basic range.
            DateTimeRange rAB = new DateTimeRange(
                DateTime.Parse("2007-06-30T05:16:11.000Z"),
                DateTime.Parse("2007-07-01T06:16:11.000Z"));

            // Get two ranges that are bigger than the basic, because of the start or end.
            DateTimeRange rAPlusOneB = new DateTimeRange(
                DateTime.Parse("2007-06-30T05:16:12.000Z"),
                DateTime.Parse("2007-07-01T06:16:11.000Z"));

            DateTimeRange rABPlusOne = new DateTimeRange(
                DateTime.Parse("2007-06-30T05:16:11.000Z"),
                DateTime.Parse("2007-07-01T06:16:12.000Z"));

            // Do the same for the two derived ranges
            // This produces 3 new ranges, because the A+1B+1 is shared
            DateTimeRange rAPlusTwoB = new DateTimeRange(
                DateTime.Parse("2007-06-30T05:16:13.000Z"),
                DateTime.Parse("2007-07-01T06:16:11.000Z"));

            DateTimeRange rAPlusOneBPlusOne = new DateTimeRange(
                DateTime.Parse("2007-06-30T05:16:12.000Z"),
                DateTime.Parse("2007-07-01T06:16:12.000Z"));

            DateTimeRange rABPlusTwo = new DateTimeRange(
                DateTime.Parse("2007-06-30T05:16:11.000Z"),
                DateTime.Parse("2007-07-01T06:16:13.000Z"));

            Assert.Less(rAB.CompareTo(rAPlusOneB), 0);
            Assert.Less(rAB.CompareTo(rABPlusOne), 0);

            Assert.Greater(rAPlusOneB.CompareTo(rAB), 0); // 3.
            Assert.Greater(rABPlusOne.CompareTo(rAB), 0); // 3.

            Assert.Less(rAPlusOneB.CompareTo(rAPlusOneBPlusOne), 0);
            Assert.Less(rAPlusOneB.CompareTo(rAPlusTwoB), 0);

            Assert.Greater(rAPlusOneBPlusOne.CompareTo(rAPlusOneB), 0); // 3.
            Assert.Greater(rAPlusTwoB.CompareTo(rAPlusOneB), 0);        // 3.

            Assert.Less(rABPlusOne.CompareTo(rAPlusOneBPlusOne), 0);
            Assert.Less(rABPlusOne.CompareTo(rABPlusTwo), 0);

            Assert.Greater(rAPlusOneBPlusOne.CompareTo(rABPlusOne), 0); // 3.
            Assert.Greater(rABPlusTwo.CompareTo(rABPlusOne), 0);        // 3.

            Assert.Less(rAB.CompareTo(rAPlusTwoB), 0);        // 5.
            Assert.Less(rAB.CompareTo(rAPlusOneBPlusOne), 0); // 5.
            Assert.Less(rAB.CompareTo(rABPlusTwo), 0);        // 5.

            Assert.Greater(rAPlusTwoB.CompareTo(rAB), 0);        // 3.
            Assert.Greater(rAPlusOneBPlusOne.CompareTo(rAB), 0); // 3.
            Assert.Greater(rABPlusTwo.CompareTo(rAB), 0);        // 3.
        }

        private static int Sign(int operand)
        {
            if (operand == 0)
            {
                return 0;
            }

            if (operand < 0)
            {
                return -1;
            }

            return 1;
        }
    }
}
