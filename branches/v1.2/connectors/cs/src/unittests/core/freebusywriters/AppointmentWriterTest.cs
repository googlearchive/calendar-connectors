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
    public class AppointmentWriterTest : AppointmentWriter
    {
        IService _service;
        DateTime _base;

        List<DateTimeRange> _gcalEvents = new List<DateTimeRange>();
        List<DateTimeRange> _exchEvents = new List<DateTimeRange>();
        List<DateTimeRange> _createEvents = new List<DateTimeRange>();
        List<DateTimeRange> _deleteEvents = new List<DateTimeRange>();

        [SetUp]
        public void Init()
        {
            _service = new CalendarService("AppointmentWriterTest");
            _base = DateUtil.ParseDateToUtc("2007-07-30T00:00:00.000Z");

            _gcalEvents.Clear();
            _exchEvents.Clear();
            _createEvents.Clear();
            _deleteEvents.Clear();
        }

        [Test]
        public void TestAppointmentOwnership()
        {
            Appointment appt = new Appointment();
            Assert.IsFalse(this.ValidateOwnership(appt));
            this.AssignOwnership(appt);
            Assert.IsTrue(this.ValidateOwnership(appt));
        }

        protected void AddEventBoth(DateTime start, DateTime end)
        {
            DateTimeRange r = new DateTimeRange(start, end);
            _gcalEvents.Add(r);
            _exchEvents.Add(r);
        }

        protected void AddEventGCal(DateTimeRange r)
        {
            _gcalEvents.Add(r);
            _createEvents.Add(r);
        }

        protected void AddEventGCal(DateTime start, DateTime end)
        {
            DateTimeRange r = new DateTimeRange(start, end);
            AddEventGCal(r);
        }

        protected void AddEventExchange(DateTime start, DateTime end)
        {
            DateTimeRange r = new DateTimeRange(start, end);
            AddEventExchange(r);
        }

        protected void AddEventExchange(DateTimeRange r)
        {
            _exchEvents.Add(r);
            _deleteEvents.Add(r);
        }

        [Test]
        public void TestMissingAppointmentDetail()
        {
            DateTime start = DateUtil.ParseDateToUtc("2007-07-30T13:00:00.000Z");
            DateTimeRange window = new DateTimeRange(start.AddDays(-7), start.AddDays(7));

            AddEventGCal(start, start.AddHours(1));

            EventFeed feed = createEventFeedFromEvents(_gcalEvents);
            FreeBusyCollection fb = createFreeBusyFromExistingEvents(_exchEvents);

            ExchangeUser user = createFauxUser("test@example.org", "text@example.org");
            user.BusyTimes = fb;
            user.HaveAppointmentDetail = false;

            ExchangeGatewayMock gateway = new ExchangeGatewayMock();
            this.SyncUser(user, feed, gateway, window);

            Assert.AreEqual(0, gateway.AppointmentsMock.Deleted.Count);
            Assert.AreEqual(0, gateway.AppointmentsMock.Written.Count);
            Assert.AreEqual(0, gateway.AppointmentsMock.Updated.Count);
        }

        [Test]
        public void TestOverlappingStartEvent()
        {
            DateTime start = DateUtil.ParseDateToUtc("2007-07-30T13:00:00.000Z");
            DateTimeRange window = new DateTimeRange(start.AddDays(-7), start.AddDays(7));

            // Test overlapping events
            // - 13:00 - 13:30 in Exchange
            // - 13:00 - 14:00 in GCal
            // Should create an exchange event [13:00-14:00]
            AddEventExchange(start, start.AddMinutes(30));
            AddEventGCal(start, start.AddHours(1));

            EventFeed feed = createEventFeedFromEvents(_gcalEvents);
            FreeBusyCollection fb = createFreeBusyFromExistingEvents(_exchEvents);

            ExchangeUser user = createFauxUser("test@example.org", "text@example.org");
            user.BusyTimes = fb;

            ExchangeGatewayMock gateway = new ExchangeGatewayMock();
            this.SyncUser(user, feed, gateway, window);

            Assert.AreEqual(0, gateway.AppointmentsMock.Deleted.Count);
            Assert.AreEqual(1, gateway.AppointmentsMock.Written.Count);
            Assert.AreEqual(0, gateway.AppointmentsMock.Updated.Count);

            Assert.AreEqual(gateway.AppointmentsMock.Written[0].StartDate, start);
            Assert.AreEqual(gateway.AppointmentsMock.Written[0].EndDate, start.AddHours(1));
        }

        [Test]
        public void TestOverlappingContainedEvent()
        {
            DateTime start = DateUtil.ParseDateToUtc("2007-07-30T13:00:00.000Z");
            DateTimeRange window = new DateTimeRange(start.AddDays(-7), start.AddDays(7));

            // Test overlapping events
            // - 13:00 - 16:00 in Exchange
            // - 14:00 - 15:00 in GCal
            // Should create an exchange event [14:00-15:00]
            AddEventExchange(start, start.AddHours(3));
            AddEventGCal(start.AddHours(1), start.AddHours(2));

            EventFeed feed = createEventFeedFromEvents(_gcalEvents);
            FreeBusyCollection fb = createFreeBusyFromExistingEvents(_exchEvents);

            ExchangeUser user = createFauxUser("test@example.org", "text@example.org");
            user.BusyTimes = fb;

            ExchangeGatewayMock gateway = new ExchangeGatewayMock();
            this.SyncUser(user, feed, gateway, window);

            Assert.AreEqual(0, gateway.AppointmentsMock.Deleted.Count);
            Assert.AreEqual(1, gateway.AppointmentsMock.Written.Count);
            Assert.AreEqual(0, gateway.AppointmentsMock.Updated.Count);

            Assert.AreEqual(gateway.AppointmentsMock.Written[0].StartDate, start.AddHours(1));
            Assert.AreEqual(gateway.AppointmentsMock.Written[0].EndDate, start.AddHours(2));
        }


        [Test]
        public void TestOverlappingExactEvent()
        {
            DateTime start = DateUtil.ParseDateToUtc("2007-07-30T13:00:00.000Z");
            DateTimeRange window = new DateTimeRange(start.AddDays(-7), start.AddDays(7));

            // Test overlapping events
            // - 13:00 - 13:30 in Exchange
            // - 13:00 - 13:30 in GCal
            // Should create an exchange event [13:00-13:30]
            AddEventExchange(start, start.AddMinutes(30));
            AddEventGCal(start, start.AddMinutes(30));

            EventFeed feed = createEventFeedFromEvents(_gcalEvents);
            FreeBusyCollection fb = createFreeBusyFromExistingEvents(_exchEvents);

            ExchangeUser user = createFauxUser("test@example.org", "text@example.org");
            user.BusyTimes = fb;

            ExchangeGatewayMock gateway = new ExchangeGatewayMock();
            this.SyncUser(user, feed, gateway, window);

            Assert.AreEqual(0, gateway.AppointmentsMock.Deleted.Count);
            Assert.AreEqual(1, gateway.AppointmentsMock.Written.Count);
            Assert.AreEqual(0, gateway.AppointmentsMock.Updated.Count);

            Assert.AreEqual(gateway.AppointmentsMock.Written[0].StartDate, start);
            Assert.AreEqual(gateway.AppointmentsMock.Written[0].EndDate, start.AddMinutes(30));
        }

        [Test]
        public void TestDuplicateGCalStartTimes()
        {
            DateTime start = DateUtil.ParseDateToUtc("2007-07-30T13:00:00.000Z");
            DateTimeRange window = new DateTimeRange(start.AddDays(-7), start.AddDays(7));

            // Test overlapping events
            // - 13:00 - 14:00 existing event from GCal in Exchange
            // - 13:00 - 13:30 in GCal
            // Should create an exchange event [13:00-13:30]
            AddEventExchange(start.AddMinutes(120), start.AddMinutes(180));
            AddEventGCal(start.AddMinutes(120), start.AddMinutes(180));
            AddEventGCal(start.AddMinutes(120), start.AddMinutes(150));

            EventFeed feed = createEventFeedFromEvents(_gcalEvents);
            FreeBusyCollection fb = createFreeBusyFromSyncEvents(_exchEvents);

            ExchangeUser user = createFauxUser("test@example.org", "text@example.org");
            user.BusyTimes = fb;

            ExchangeGatewayMock gateway = new ExchangeGatewayMock();
            this.SyncUser(user, feed, gateway, window);

            Assert.AreEqual(0, gateway.AppointmentsMock.Deleted.Count);
            Assert.AreEqual(1, gateway.AppointmentsMock.Written.Count);
            Assert.AreEqual(0, gateway.AppointmentsMock.Updated.Count);

            Assert.AreEqual(gateway.AppointmentsMock.Written[0].StartDate, start.AddMinutes(120));
            Assert.AreEqual(gateway.AppointmentsMock.Written[0].EndDate, start.AddMinutes(150));
        }

        [Test]
        public void TestNoOverlapEvent()
        {
            DateTime start = DateUtil.ParseDateToUtc("2007-07-30T13:00:00.000Z");
            DateTimeRange window = new DateTimeRange(start.AddDays(-7), start.AddDays(7));

            // Test overlapping events
            // - Nothing in Exchange
            // - 13:00 - 13:30 in GCal
            // Should create an exchange event [13:00-13:30]
            AddEventGCal(start, start.AddMinutes(30));

            EventFeed feed = createEventFeedFromEvents(_gcalEvents);
            FreeBusyCollection fb = createFreeBusyFromExistingEvents(_exchEvents);

            ExchangeUser user = createFauxUser("test@example.org", "text@example.org");
            user.BusyTimes = fb;

            ExchangeGatewayMock gateway = new ExchangeGatewayMock();
            this.SyncUser(user, feed, gateway, window);

            Assert.AreEqual(0, gateway.AppointmentsMock.Deleted.Count);
            Assert.AreEqual(1, gateway.AppointmentsMock.Written.Count);
            Assert.AreEqual(0, gateway.AppointmentsMock.Updated.Count);

            Assert.AreEqual(gateway.AppointmentsMock.Written[0].StartDate, start);
            Assert.AreEqual(gateway.AppointmentsMock.Written[0].EndDate, start.AddMinutes(30));
        }

        [Test]
        public void TestOverlappingEndEvent()
        {
            DateTime start = DateUtil.ParseDateToUtc("2007-07-30T13:00:00.000Z");
            DateTimeRange window = new DateTimeRange(start.AddDays(-7), start.AddDays(7));

            // Test overlapping events
            // - 13:30 - 14:00 in Exchange
            // - 13:00 - 14:00 in GCal
            // Should create an exchange event [13:00-14:00]
            AddEventExchange(start, start.AddMinutes(30));
            AddEventGCal(start, start.AddHours(1));

            EventFeed feed = createEventFeedFromEvents(_gcalEvents);
            FreeBusyCollection fb = createFreeBusyFromExistingEvents(_exchEvents);

            ExchangeUser user = createFauxUser("test@example.org", "text@example.org");
            user.BusyTimes = fb;

            ExchangeGatewayMock gateway = new ExchangeGatewayMock();
            this.SyncUser(user, feed, gateway, window);

            Assert.AreEqual(0, gateway.AppointmentsMock.Deleted.Count);
            Assert.AreEqual(1, gateway.AppointmentsMock.Written.Count);
            Assert.AreEqual(0, gateway.AppointmentsMock.Updated.Count);

            Assert.AreEqual(gateway.AppointmentsMock.Written[0].StartDate, start);
            Assert.AreEqual(gateway.AppointmentsMock.Written[0].EndDate, start.AddHours(1));
        }

        [Test]
        public void TestAdjacentEvent()
        {
            DateTime start = DateUtil.ParseDateToUtc("2007-07-30T13:00:00.000Z");
            DateTimeRange window = new DateTimeRange(start.AddDays(-7), start.AddDays(7));

            // Test overlapping events
            // - 13:00 - 17:00 in Exchange from GCal
            // - 17:00 - 18:00 in GCal
            // Should create an exchange event [17:00-18:00]
            AddEventExchange(start, start.AddHours(4));
            AddEventGCal(start, start.AddHours(4));
            AddEventGCal(start.AddHours(4), start.AddHours(5));

            EventFeed feed = createEventFeedFromEvents(_gcalEvents);
            FreeBusyCollection fb = createFreeBusyFromSyncEvents(_exchEvents);

            ExchangeUser user = createFauxUser("test@example.org", "text@example.org");
            user.BusyTimes = fb;

            ExchangeGatewayMock gateway = new ExchangeGatewayMock();
            this.SyncUser(user, feed, gateway, window);

            Assert.AreEqual(0, gateway.AppointmentsMock.Deleted.Count);
            Assert.AreEqual(1, gateway.AppointmentsMock.Written.Count);
            Assert.AreEqual(0, gateway.AppointmentsMock.Updated.Count);

            Assert.AreEqual(gateway.AppointmentsMock.Written[0].StartDate, start.AddHours(4));
            Assert.AreEqual(gateway.AppointmentsMock.Written[0].EndDate, start.AddHours(5));

            AddEventExchange(start.AddHours(4), start.AddHours(5));
            feed = createEventFeedFromEvents(_gcalEvents);
            user.BusyTimes = createFreeBusyFromSyncEvents(_exchEvents);

            gateway = new ExchangeGatewayMock();

            this.SyncUser(user, feed, gateway, window);
            Assert.AreEqual(0, gateway.AppointmentsMock.Deleted.Count);
            Assert.AreEqual(0, gateway.AppointmentsMock.Written.Count);
            Assert.AreEqual(0, gateway.AppointmentsMock.Updated.Count);
        }

        [Test]
        public void TestSyncUser()
        {
            DateTime start = _base;
            DateTimeRange window = new DateTimeRange(start, start.AddDays(30));

            // Add Event to both
            AddEventBoth(start, start.AddHours(2));

            // Add only to Google Calendar - i.e. Create in exchange
            start = start.AddDays(2).AddHours(3);
            AddEventGCal(start, start.AddHours(1));

            // Add only to Exchange - i.e. Delete from exchange
            start = start.AddDays(3).AddHours(3);
            AddEventExchange(start, start.AddHours(1));

            start = start.AddDays(1).AddHours(-5);
            AddEventExchange(start, start.AddMinutes(20));

            // Add to Google Calendar
            start = start.AddDays(2).AddHours(3);
            AddEventGCal(start, start.AddHours(1));

            // Add Event to both
            start = start.AddDays(2).AddHours(3);
            AddEventBoth(start, start.AddHours(2));

            // Add only to Exchange
            start = start.AddDays(2).AddHours(3);
            AddEventExchange(start, start.AddHours(1));

            // Add Event to both
            start = start.AddDays(2).AddHours(14);
            AddEventBoth(start, start.AddHours(1));

            // Add to Google Calendar
            start = start.AddDays(2).AddHours(3);
            AddEventGCal(start, start.AddHours(1));

            // Add Event to both
            start = start.AddDays(5).AddHours(12);
            AddEventBoth(start, start.AddHours(6));

            EventFeed feed = createEventFeedFromEvents(_gcalEvents);
            FreeBusyCollection fb = createFreeBusyFromSyncEvents(_exchEvents);

            ExchangeUser user = createFauxUser("test@example.org", "text@example.org");
            user.BusyTimes = fb;

            ExchangeGatewayMock gateway = new ExchangeGatewayMock();
            this.SyncUser(user, feed, gateway, window);

            Assert.AreEqual(_deleteEvents.Count, gateway.AppointmentsMock.Deleted.Count);
            Assert.AreEqual(_createEvents.Count, gateway.AppointmentsMock.Written.Count);
            Assert.AreEqual(0, gateway.AppointmentsMock.Updated.Count);

            int idx = 0;
            foreach (DateTimeRange e in _deleteEvents)
            {
                Assert.AreEqual(e.Start, gateway.AppointmentsMock.Deleted[idx].StartDate);
                Assert.AreEqual(e.End, gateway.AppointmentsMock.Deleted[idx].EndDate);
                idx++;
            }

            idx = 0;
            foreach (DateTimeRange e in _createEvents)
            {
                Assert.AreEqual(e.Start, gateway.AppointmentsMock.Written[idx].StartDate);
                Assert.AreEqual(e.End, gateway.AppointmentsMock.Written[idx].EndDate);
                idx++;
            }

        }

        private ExchangeUser createFauxUser(string name, string email)
        {
            ExchangeUser result = new ExchangeUser();
            result.Email = email;
            result.MailNickname = name;
            result.LegacyExchangeDN = "/o=Microsoft/ou=APPS-ABC/cn=RECIPIENTS/cn=ASAMPLE";
            result.HaveAppointmentDetail = true;
            return result;
        }

        private EventFeed createEventFeedFromEvents(List<DateTimeRange> events)
        {
            Uri uri = new Uri("http://localhost");
            EventFeed result = new EventFeed(uri, null);
            result.TimeZone = new Google.GData.Calendar.TimeZone("EST");

            foreach (DateTimeRange r in events)
            {
                EventEntry e = result.CreateFeedEntry() as EventEntry;

                DateTime start = DateTime.SpecifyKind(r.Start, DateTimeKind.Utc).ToLocalTime();
                DateTime end = DateTime.SpecifyKind(r.End, DateTimeKind.Utc).ToLocalTime();
                e.Times.Add(new When(start, end));
                result.Entries.Add(e);
            }

            return result;
        }

        private void AddFreeBusyBlock(
            FreeBusyCollection result,
            DateTimeRange eventRange,
            bool existingEvent)
        {
            FreeBusyTimeBlock block = new FreeBusyTimeBlock(eventRange);
            Appointment appt = new Appointment();
            appt.StartDate = eventRange.Start;
            appt.EndDate = eventRange.End;
            appt.BusyStatus = BusyStatus.Busy;

            if (existingEvent)
            {
                AssignOwnership(appt);
            }

            block.Appointments.Add(appt);
            result.Appointments.Add(appt);
            result[eventRange.Start] = block;
        }

        private FreeBusyCollection createFreeBusyFromSyncEvents(
            List<DateTimeRange> syncEvents)
        {
            return createFreeBusyFromEvents(
                syncEvents, new List<DateTimeRange>());
        }

        private FreeBusyCollection createFreeBusyFromExistingEvents(
            List<DateTimeRange> exchEvents)
        {
            return createFreeBusyFromEvents(
                new List<DateTimeRange>(), exchEvents);
        }

        private FreeBusyCollection createFreeBusyFromEvents(
            List<DateTimeRange> syncEvents,
            List<DateTimeRange> exchEvents)
        {
            FreeBusyCollection result = new FreeBusyCollection();
            foreach (DateTimeRange r in syncEvents)
            {
                AddFreeBusyBlock(result, r, true);
            }

            foreach (DateTimeRange r in exchEvents)
            {
                AddFreeBusyBlock(result, r, false);
            }
            return result;
        }
    }
}
