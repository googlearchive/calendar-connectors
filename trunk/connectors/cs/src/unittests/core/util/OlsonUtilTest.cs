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
	public class OlsonUtilTest
	{
		static DateTime dateUnspec = new DateTime(2008, 03, 09, 2, 00, 00, DateTimeKind.Unspecified);
        static DateTime dateUTC = new DateTime(2008, 03, 09, 02, 00, 00, DateTimeKind.Utc);
        static DateTime dateLocal = new DateTime(2008, 03, 09, 02, 00, 00, DateTimeKind.Local);

		[Test]
		public void TestDateCompare()
		{
            Assert.That(dateUnspec.CompareTo(dateLocal) == 0);
            Assert.That(dateUnspec.Equals(dateLocal));
            Assert.AreEqual(dateUnspec.GetHashCode(), dateLocal.GetHashCode());
            Assert.That(dateLocal.CompareTo(dateUnspec) == 0);
            Assert.That(dateLocal.Equals(dateUnspec));
            Assert.AreEqual(dateLocal.GetHashCode(), dateUnspec.GetHashCode());

            DateTimeRange rangeUnspec = new DateTimeRange(dateUnspec, dateUnspec);
            DateTimeRange rangeLocal = new DateTimeRange(dateLocal, dateLocal);

            Assert.That(rangeUnspec.CompareTo(rangeLocal) == 0);
            Assert.That(rangeUnspec.Equals(rangeLocal));
            Assert.AreEqual(rangeUnspec.GetHashCode(), rangeLocal.GetHashCode());
        }
	}
}

