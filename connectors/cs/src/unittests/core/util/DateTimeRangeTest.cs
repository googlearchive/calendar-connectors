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
			startDate = DateTime.Parse("2007-06-30T05:16:11.000Z");
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
        public void TestDateTimeCompare()
        {
            DateTimeRange r0 = new DateTimeRange(
                DateTime.Parse("2007-06-30T05:16:11.000Z"),
                DateTime.Parse("2007-07-01T06:16:11.000Z"));

            Assert.AreEqual(DateTimeKind.Local, r0.Start.Kind);
            Assert.AreEqual(DateTimeKind.Local, r0.End.Kind);

            DateTimeRange r1 = new DateTimeRange(
                DateTime.Parse("2007-06-30T05:16:11.000Z").ToUniversalTime(),
                DateTime.Parse("2007-07-01T06:16:11.000Z").ToUniversalTime());

            Assert.AreEqual(DateTimeKind.Utc, r1.Start.Kind);
            Assert.AreEqual(DateTimeKind.Utc, r1.End.Kind);

            Assert.AreNotEqual(r0, r1);
        }
	}
}
