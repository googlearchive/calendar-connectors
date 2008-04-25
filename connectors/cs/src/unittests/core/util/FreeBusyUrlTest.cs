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

using Google.GCalExchangeSync.Library;

namespace Google.GCalExchangeSync.Library.Util
{
	[TestFixture]
	public class FreeBusyUrlTest
	{
		private static readonly string ExchangeServer = "http://example.org";
		private static readonly string Organization = "Dev Project 1 Organization/";
		private static readonly string OrgUnit = "Administrative Group";
		private static readonly string LegacyDN = "/o=Microsoft/ou=APPS-ABC/cn=FOLDER/cn=ASAMPLE";
		private static readonly string AdminGroup = "Group";

		private static readonly string FreeBusyTestUrl =
			"http://example.org/public/NON_IPM_SUBTREE/SCHEDULE%2B%20FREE%20BUSY/EX:Dev%20Project%201%20Organization_xF8FF_/USER-_xF8FF_cn=RECIPIENTS_xF8FF_cn=Administrative%20Group.EML";

		private static readonly string FreeBusyFromDNUrl =
			"http://example.org/public/NON_IPM_SUBTREE/SCHEDULE%2B%20FREE%20BUSY/EX:_xF8FF_o=Microsoft_xF8FF_ou=APPS-ABC/USER-_xF8FF_cn=FOLDER_xF8FF_cn=ASAMPLE.EML";

		private static readonly string AdminGroupUrl =
			"http://example.org/public/NON_IPM_SUBTREE/SCHEDULE%2B%20FREE%20BUSY/EX:Group/";

		private static readonly string SingleUserFreeBusyUrl =
			"http://example.org/public/?cmd=freebusy&start=2007-06-30T05:16:11Z&end=2007-06-30T08:16:11Z&interval=15&u=user1@example.org";
		private static readonly string MultiUserFreeBusyUrl =
			"http://example.org/public/?cmd=freebusy&start=2007-06-30T05:16:11Z&end=2007-06-30T08:16:11Z&interval=15&u=user1@example.org&u=user2@example.org";

		private static readonly string User1Email = "user1@example.org";
		private static readonly string User2Email = "user2@example.org";

		private static readonly DateTime start = DateUtil.ParseDateToUtc("2007-06-30T05:16:11.000Z");
        private static readonly DateTime end = DateUtil.ParseDateToUtc("2007-06-30T08:16:11.000Z");

		[SetUp]
		public void Init()
		{
		}

		private ExchangeUser createFauxUser(string name, string email)
		{
			ExchangeUser result = new ExchangeUser();
			result.Email = email;
			result.MailNickname = name;
			result.LegacyExchangeDN = "/o=Microsoft/ou=APPS-ABC/cn=RECIPIENTS/cn=ASAMPLE";
			return result;
		}

		[Test]
		public void FreeBusyUrls()
		{
			Assert.AreEqual(FreeBusyTestUrl, FreeBusyUrl.GenerateUrl(ExchangeServer, Organization, OrgUnit));
			Assert.AreEqual(FreeBusyFromDNUrl, FreeBusyUrl.GenerateUrlFromDN(ExchangeServer, LegacyDN));
			Assert.AreEqual(AdminGroupUrl, FreeBusyUrl.GenerateAdminGroupUrl(ExchangeServer, AdminGroup));

			ExchangeUserDict users = new ExchangeUserDict();
			users.Add(User1Email, createFauxUser("User1", User1Email));

			DateTimeRange range = new DateTimeRange(start, end);
			Assert.AreEqual(SingleUserFreeBusyUrl, FreeBusyUrl.GenerateFreeBusyLookupUrl(ExchangeServer, users, range, 15));

			users.Add(User2Email, createFauxUser("User2", User2Email));
			Assert.AreEqual(MultiUserFreeBusyUrl, FreeBusyUrl.GenerateFreeBusyLookupUrl(ExchangeServer, users, range, 15));
		}
	}
}
