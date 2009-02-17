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
using System.IO;
using System.Net;
using System.Text;
using System.Reflection;
using System.Globalization;


using NUnit.Framework;

namespace Google.GCalExchangeSync.Library.Util
{
    [TestFixture]
    public class DateUtilTest
    {
        [SetUp]
        public void Init()
        {
        }

        [Test]
        public void TestRoundRangeToInterval()
        {
            DateTime startDate = new DateTime(2008, 05, 1, 10, 2, 3, 4, DateTimeKind.Utc);
            DateTime startRounded1 = new DateTime(2008, 05, 1, 10, 2, 0, 0, DateTimeKind.Utc);
            DateTime startRounded15 = new DateTime(2008, 05, 1, 10, 0, 0, 0, DateTimeKind.Utc);
            DateTime endDate = new DateTime(2008, 05, 1, 11, 2, 3, 4, DateTimeKind.Utc);
            DateTime endRounded1 = new DateTime(2008, 05, 1, 11, 3, 0, 0, DateTimeKind.Utc);
            DateTime endRounded15 = new DateTime(2008, 05, 1, 11, 15, 0, 0, DateTimeKind.Utc);

            DateTimeRange range = new DateTimeRange(startDate, endDate);
            DateTimeRange rounded = DateUtil.RoundRangeToInterval(range, 1);
            Assert.AreEqual(rounded.Start, startRounded1);
            Assert.AreEqual(rounded.End, endRounded1);

            DateTimeRange twiceRounded = DateUtil.RoundRangeToInterval(rounded, 1);
            Assert.AreEqual(rounded.Start, twiceRounded.Start);
            Assert.AreEqual(rounded.End, twiceRounded.End);

            rounded = DateUtil.RoundRangeToInterval(range, 15);
            Assert.AreEqual(rounded.Start, startRounded15);
            Assert.AreEqual(rounded.End, endRounded15);

            twiceRounded = DateUtil.RoundRangeToInterval(rounded, 15);
            Assert.AreEqual(rounded.Start, twiceRounded.Start);
            Assert.AreEqual(rounded.End, twiceRounded.End);
        }

    }

}
