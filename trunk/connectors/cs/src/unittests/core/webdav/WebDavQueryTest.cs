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

using Google.GCalExchangeSync.Library;
using Google.GCalExchangeSync.Library.Util;

using NUnit.Framework;

namespace Google.GCalExchangeSync.Library.WebDav
{
    [TestFixture]
    public class WebDavQueryTest
    {
        private static readonly string organizer =
            "\"Phoney McRingring\" <phoney@barnabyjames.com>";

        WebDavQuery _webdav;
        XmlRequestMock _requestor;
        ExchangeUser _user;

        private readonly string calendarUrl;
        private readonly string exchangeServer = "http://example.org";

        private string  getResponseXML(string resourceName)
        {
            string resource =
                string.Format("UnitTests.core.webdav.responses.{0}", resourceName);
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream( resource );

            using ( StreamReader reader = new StreamReader( stream ) )
            {
                return reader.ReadToEnd();
            }
        }

        private ExchangeUser createFauxUser(string name, string email)
        {
            ExchangeUser result = new ExchangeUser();
            result.Email = email;
            result.MailNickname = name;
            result.LegacyExchangeDN = "/o=Microsoft/ou=APPS-ABC/cn=RECIPIENTS/cn=ASAMPLE";
            return result;
        }

        public WebDavQueryTest()
        {
            calendarUrl = ExchangeUtil.GetDefaultCalendarUrl(exchangeServer, "test");
        }

        [SetUp]
        public void Init()
        {
            _requestor = new XmlRequestMock();
            _webdav = new WebDavQuery(CredentialCache.DefaultCredentials, _requestor);
            _user = createFauxUser("test", "test@example.org");
        }

        [Test]
        public void TestAppointmentLookup()
        {
            _requestor.ValidMethod = Method.SEARCH;
            _requestor.ResponseBody = getResponseXML("AppointmentResponse.xml");

            DateTime start = DateTime.Now.AddDays(-7);
            DateTime end = DateTime.Now.AddDays(+7);

            List<Appointment> result = _webdav.LoadAppointments(calendarUrl, start, end);
            Assert.AreEqual(result.Count, 6);

            Assert.AreEqual(result[0].Created, DateTime.Parse("2007-12-30T05:16:11.844Z"));
            Assert.AreEqual(result[0].StartDate, DateTime.Parse("2008-01-02T21:00:00.000Z"));
            Assert.AreEqual(result[0].EndDate, DateTime.Parse("2008-01-03T00:00:00.000Z"));
            
            Assert.IsEmpty(result[0].Body);
            Assert.AreEqual(result[0].Subject, "fefefewfew");
            Assert.IsEmpty(result[0].Location);
            Assert.IsEmpty(result[0].Comment);
            Assert.AreEqual(result[0].Organizer, organizer);
            Assert.AreEqual(result[0].BusyStatus, BusyStatus.Busy);
            Assert.AreEqual(result[0].MeetingStatus, MeetingStatus.Tentative);
            Assert.IsFalse(result[0].AllDayEvent);
            Assert.IsFalse(result[0].IsPrivate);
        }

        [Test]
        public void TestFastFreeBusyLookup()
        {
            _requestor.ValidMethod = Method.GET;
            _requestor.ResponseBody = getResponseXML("FreeBusyResponse.xml");

            // These dates correspond to when the response XML was captured
            DateTime start = DateTime.Parse("12/25/2007 01:42:50");
            DateTime end = DateTime.Parse("01/08/2008 01:42:50");
            DateTimeRange range = new DateTimeRange(start, end);

            _webdav.FastFreeBusyLookup = true;

            ExchangeUserDict users = new ExchangeUserDict();
            users.Add(_user.Email, _user);

            Dictionary<ExchangeUser, FreeBusy> result = _webdav.LoadFreeBusy(exchangeServer, users, range);

            Assert.AreEqual(1, result.Count);

            FreeBusy fb = result[_user];

            Assert.AreEqual(6, fb.All.Count);
            Assert.AreEqual(6, fb.Busy.Count);
            Assert.AreEqual(0, fb.OutOfOffice.Count);
            Assert.AreEqual(0, fb.Tentative.Count);

            //dumpFreeBusy(fb.Busy);

            Assert.AreEqual(DateTime.Parse("12/26/2007 18:00:00"), fb.Busy[0].Start);
            Assert.AreEqual(DateTime.Parse("12/26/2007 18:30:00"), fb.Busy[0].End);
            Assert.AreEqual(DateTime.Parse("12/26/2007 20:30:00"), fb.Busy[1].Start);
            Assert.AreEqual(DateTime.Parse("12/26/2007 21:00:00"), fb.Busy[1].End);
            Assert.AreEqual(DateTime.Parse("12/31/2007 17:30:00"), fb.Busy[2].Start);
            Assert.AreEqual(DateTime.Parse("12/31/2007 18:00:00"), fb.Busy[2].End);
            Assert.AreEqual(DateTime.Parse("12/31/2007 21:00:00"), fb.Busy[3].Start);
            Assert.AreEqual(DateTime.Parse("12/31/2007 21:30:00"), fb.Busy[3].End);
        }

        [Test]
        public void TestSlowFreeBusyLookup()
        {
            _requestor.ValidMethod = Method.SEARCH;
            _requestor.ResponseBody = getResponseXML("FreeBusyPublicFolderResponse.xml");

            // These dates correspond to when the response XML was captured
            DateTime start = DateTime.Parse("12/25/2007 01:42:50");
            DateTime end = DateTime.Parse("01/08/2008 01:42:50");
            DateTimeRange range = new DateTimeRange(start, end);

            _webdav.FastFreeBusyLookup = false;

            ExchangeUserDict users = new ExchangeUserDict();
            users.Add(_user.Email, _user);

            Dictionary<ExchangeUser, FreeBusy> result = _webdav.LoadFreeBusy(exchangeServer, users, range);

            Assert.AreEqual(1, result.Count);

            FreeBusy fb = result[_user];

            Assert.AreEqual(13, fb.All.Count);
            Assert.AreEqual(13, fb.Busy.Count);
            Assert.AreEqual(0, fb.OutOfOffice.Count);
            Assert.AreEqual(0, fb.Tentative.Count);

            //dumpFreeBusy(fb.Busy);

            Assert.AreEqual(DateTime.Parse("12/11/2007 12:30:00"), fb.Busy[0].Start);
            Assert.AreEqual(DateTime.Parse("12/11/2007 13:00:00"), fb.Busy[0].End);

            Assert.AreEqual(DateTime.Parse("12/26/2007 18:00:00Z"), fb.Busy[7].Start);
            Assert.AreEqual(DateTime.Parse("12/26/2007 18:30:00Z"), fb.Busy[7].End);
            Assert.AreEqual(DateTime.Parse("12/26/2007 20:30:00Z"), fb.Busy[8].Start);
            Assert.AreEqual(DateTime.Parse("12/26/2007 21:00:00Z"), fb.Busy[8].End);
            Assert.AreEqual(DateTime.Parse("12/31/2007 17:30:00Z"), fb.Busy[9].Start);
            Assert.AreEqual(DateTime.Parse("12/31/2007 18:00:00Z"), fb.Busy[9].End);
            Assert.AreEqual(DateTime.Parse("12/31/2007 21:00:00Z"), fb.Busy[10].Start);
            Assert.AreEqual(DateTime.Parse("12/31/2007 21:30:00Z"), fb.Busy[10].End);

        }

        private void dumpFreeBusy(List<DateTimeRange> dtl)
        {
            Console.WriteLine("Begin FB - {0}", dtl.Count);
            foreach (DateTimeRange dt in dtl)
            {
                Console.WriteLine("Busy: {0} -> {1} [{2}]", dt.Start, dt.End, dt.Start.Kind);
            }

            Console.WriteLine("End FB");
        }
    }
}