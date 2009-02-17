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
    public class GCalRequestTest
    {
        private string createEmailList(List<string> emails)
        {
            string result = "";

            foreach (string email in emails)
            {
                if (result.Length > 0)
                {
                    result += "," + email;
                }
                else
                {
                    result += email;
                }
            }

            return result;
        }

        private string createDate(DateTime d)
        {
            return DateUtil.FormatDateForGoogle(d);
        }

        private string createDateTime(DateTime d)
        {
            return DateUtil.FormatDateTimeForGoogle(d);
        }

        [Test]
        public void TestParse()
        {
            string id = "test-id";
            List<string> emails = new List<string>();
            emails.Add("user1@example.org");
            emails.Add("user2@example.org");
            emails.Add("user3@example.org");
            emails.Add("user4@example.org");

            DateTime start = DateUtil.ParseDateToUtc("2008-01-01T00:00:00.000Z");
            DateTime end = DateUtil.ParseDateToUtc("2008-01-31T00:00:00.000Z");
            DateTime since = DateUtil.ParseDateToUtc("2008-01-01T00:00:00.000Z");
            string tz = "America/Los_Angeles";

            string query = string.Format("[ 1, {0}, [{1}], {2}/{3}, {4}, {5}]",
                id,
                createEmailList(emails),
                createDate(start),
                createDate(end),
                createDateTime(since),
                tz);

            GCalFreeBusyRequest request =
                new GCalFreeBusyRequest(query);

            Assert.AreEqual(start, request.StartDate);
            Assert.AreEqual(end, request.EndDate);
            Assert.AreEqual(since, request.Since);
            Assert.AreEqual(id, request.MessageId);
            Assert.AreEqual("1", request.VersionNumber);
            Assert.AreEqual(emails.Count, request.ExchangeUsers.Length);

            for (int i = 0; i < request.ExchangeUsers.Length; i++)
            {
                Assert.AreEqual(emails[i], request.ExchangeUsers[i]);
            }
        }

        [Test]
        public void TestParseWithDomainMap()
        {
            string id = "test-id";
            List<string> srcEmails = new List<string>();
            srcEmails.Add("user1@example.org");
            srcEmails.Add("user2@example.org");
            srcEmails.Add("user3@example.org");
            srcEmails.Add("user4@example.org");

            List<string> dstEmails = new List<string>();
            dstEmails.Add("user1@woot.org");
            dstEmails.Add("user2@woot.org");
            dstEmails.Add("user3@woot.org");
            dstEmails.Add("user4@woot.org");

            DateTime start = DateUtil.ParseDateToUtc("2008-01-01T00:00:00.000Z");
            DateTime end = DateUtil.ParseDateToUtc("2008-01-31T00:00:00.000Z");
            DateTime since = DateUtil.ParseDateToUtc("2008-01-01T00:00:00.000Z");
            string tz = "America/Los_Angeles";

            string query = string.Format("[ 1, {0}, [{1}], {2}/{3}, {4}, {5}]",
                id,
                createEmailList(srcEmails),
                createDate(start),
                createDate(end),
                createDateTime(since),
                tz);

            ConfigCache.AddDomainMap("example.org", "woot.org");

            GCalFreeBusyRequest request =
                new GCalFreeBusyRequest(query);

            Assert.AreEqual(start, request.StartDate);
            Assert.AreEqual(end, request.EndDate);
            Assert.AreEqual(since, request.Since);
            Assert.AreEqual(id, request.MessageId);
            Assert.AreEqual("1", request.VersionNumber);
            Assert.AreEqual(dstEmails.Count, request.ExchangeUsers.Length);

            for (int i = 0; i < request.ExchangeUsers.Length; i++)
            {
                Assert.AreEqual(dstEmails[i], request.ExchangeUsers[i]);
            }
        }

    }
}
