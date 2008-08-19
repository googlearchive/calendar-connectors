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

        public void TestConvertEventsToFreeBusy()
        {
            ExchangeUser user = new ExchangeUser();
            EventEntry googleAppsEvent = new EventEntry("title", "description", "location");
            DateTimeRange coveredRange = new DateTimeRange(DateTime.MaxValue, DateTime.MinValue);
            List<DateTimeRange> busyTimes = new List<DateTimeRange>();
            List<DateTimeRange> tentativeTimes = new List<DateTimeRange>();
            DateTime startDate = new DateTime(2007, 07, 1, 10, 0, 0, DateTimeKind.Utc);
            DateTime endDate = new DateTime(2007, 07, 1, 11, 0, 0, DateTimeKind.Utc);
            When when = new When(startDate, endDate);
            Uri uri = new Uri("https://www.google.com/calendar/feeds/john@doe.com/private/full");
            EventFeed googleAppsFeed = new EventFeed(uri, null);
            AtomEntryCollection entries = new AtomEntryCollection(googleAppsFeed);
        }

        [Test]
        public void TestConvertEventToFreeBusy()
        {
            ExchangeUser user = new ExchangeUser();
            EventEntry googleAppsEvent = new EventEntry("title", "description", "location");
            DateTimeRange coveredRange = new DateTimeRange(DateTime.MaxValue, DateTime.MinValue);
            List<DateTimeRange> busyTimes = new List<DateTimeRange>();
            List<DateTimeRange> tentativeTimes = new List<DateTimeRange>();
            DateTime startDate = new DateTime(2007, 07, 1, 10, 0, 0, DateTimeKind.Utc);
            DateTime endDate = new DateTime(2007, 07, 1, 11, 0, 0, DateTimeKind.Utc);
            When when = new When(startDate, endDate);

            user.Email = "john@doe.com";

            // Event w/o valid times set should be ignored.
            CallConvertEventToFreeBusy(user,
                                       googleAppsEvent,
                                       coveredRange,
                                       busyTimes,
                                       tentativeTimes);
            Assert.AreEqual(coveredRange.Start, DateTime.MaxValue);
            Assert.AreEqual(coveredRange.End, DateTime.MinValue);
            Assert.AreEqual(busyTimes.Count, 0);
            Assert.AreEqual(tentativeTimes.Count, 0);

            googleAppsEvent.Times.Add(when);

            // Event w/o explicit status should be treated as busy, since this is how the data
            // comes from the free busy projection
            CallConvertEventToFreeBusy(user,
                                       googleAppsEvent,
                                       coveredRange,
                                       busyTimes,
                                       tentativeTimes);
            Assert.AreEqual(coveredRange.Start, startDate);
            Assert.AreEqual(coveredRange.End, endDate);
            Assert.AreEqual(tentativeTimes.Count, 0);
            Assert.AreEqual(busyTimes.Count, 1);
            Assert.AreEqual(busyTimes[0].Start, startDate);
            Assert.AreEqual(busyTimes[0].End, endDate);
            busyTimes.Clear();

            // Confirmed event w/o attendees should be treated as busy.
            googleAppsEvent.Status = EventEntry.EventStatus.CONFIRMED;
            CallConvertEventToFreeBusy(user,
                                       googleAppsEvent,
                                       coveredRange,
                                       busyTimes,
                                       tentativeTimes);
            Assert.AreEqual(coveredRange.Start, startDate);
            Assert.AreEqual(coveredRange.End, endDate);
            Assert.AreEqual(tentativeTimes.Count, 0);
            Assert.AreEqual(busyTimes.Count, 1);
            Assert.AreEqual(busyTimes[0].Start, startDate);
            Assert.AreEqual(busyTimes[0].End, endDate);
            busyTimes.Clear();

            // Cancelled event should be treated as free.
            googleAppsEvent.Status = EventEntry.EventStatus.CANCELED;
            CallConvertEventToFreeBusy(user,
                                       googleAppsEvent,
                                       coveredRange,
                                       busyTimes,
                                       tentativeTimes);
            Assert.AreEqual(coveredRange.Start, startDate);
            Assert.AreEqual(coveredRange.End, endDate);
            Assert.AreEqual(tentativeTimes.Count, 0);
            Assert.AreEqual(busyTimes.Count, 0);

            // Tentative event w/o attendees should be treated as tentative.
            googleAppsEvent.Status = EventEntry.EventStatus.TENTATIVE;
            CallConvertEventToFreeBusy(user,
                                       googleAppsEvent,
                                       coveredRange,
                                       busyTimes,
                                       tentativeTimes);
            Assert.AreEqual(coveredRange.Start, startDate);
            Assert.AreEqual(coveredRange.End, endDate);
            Assert.AreEqual(busyTimes.Count, 0);
            Assert.AreEqual(tentativeTimes.Count, 1);
            Assert.AreEqual(tentativeTimes[0].Start, startDate);
            Assert.AreEqual(tentativeTimes[0].End, endDate);
            tentativeTimes.Clear();

            Who john = new Who();
            googleAppsEvent.Participants.Add(john);

            john.Attendee_Status = new Who.AttendeeStatus();
            john.Email = user.Email;
            googleAppsEvent.Status = EventEntry.EventStatus.CONFIRMED;

            // Busy event with attendee tentative should be treated as tentative.
            john.Attendee_Status.Value = Who.AttendeeStatus.EVENT_TENTATIVE;
            CallConvertEventToFreeBusy(user,
                                       googleAppsEvent,
                                       coveredRange,
                                       busyTimes,
                                       tentativeTimes);
            Assert.AreEqual(coveredRange.Start, startDate);
            Assert.AreEqual(coveredRange.End, endDate);
            Assert.AreEqual(busyTimes.Count, 0);
            Assert.AreEqual(tentativeTimes.Count, 1);
            Assert.AreEqual(tentativeTimes[0].Start, startDate);
            Assert.AreEqual(tentativeTimes[0].End, endDate);
            tentativeTimes.Clear();


            // Busy event with attendee invited should be treated as tentative.
            john.Attendee_Status.Value = Who.AttendeeStatus.EVENT_INVITED;
            CallConvertEventToFreeBusy(user,
                                       googleAppsEvent,
                                       coveredRange,
                                       busyTimes,
                                       tentativeTimes);
            Assert.AreEqual(coveredRange.Start, startDate);
            Assert.AreEqual(coveredRange.End, endDate);
            Assert.AreEqual(busyTimes.Count, 0);
            Assert.AreEqual(tentativeTimes.Count, 1);
            Assert.AreEqual(tentativeTimes[0].Start, startDate);
            Assert.AreEqual(tentativeTimes[0].End, endDate);
            tentativeTimes.Clear();

            // Busy event with attendee accepted should be treated as busy.
            john.Attendee_Status.Value = Who.AttendeeStatus.EVENT_ACCEPTED;
            CallConvertEventToFreeBusy(user,
                                       googleAppsEvent,
                                       coveredRange,
                                       busyTimes,
                                       tentativeTimes);
            Assert.AreEqual(coveredRange.Start, startDate);
            Assert.AreEqual(coveredRange.End, endDate);
            Assert.AreEqual(tentativeTimes.Count, 0);
            Assert.AreEqual(busyTimes.Count, 1);
            Assert.AreEqual(busyTimes[0].Start, startDate);
            Assert.AreEqual(busyTimes[0].End, endDate);
            busyTimes.Clear();

            // Busy event with attendee declined should be treated as free.
            john.Attendee_Status.Value = Who.AttendeeStatus.EVENT_DECLINED;
            CallConvertEventToFreeBusy(user,
                                       googleAppsEvent,
                                       coveredRange,
                                       busyTimes,
                                       tentativeTimes);
            Assert.AreEqual(coveredRange.Start, startDate);
            Assert.AreEqual(coveredRange.End, endDate);
            Assert.AreEqual(tentativeTimes.Count, 0);
            Assert.AreEqual(busyTimes.Count, 0);

            googleAppsEvent.Status = EventEntry.EventStatus.TENTATIVE;

            // Tentative event with attendee tentative should be treated as tentative.
            john.Attendee_Status.Value = Who.AttendeeStatus.EVENT_TENTATIVE;
            CallConvertEventToFreeBusy(user,
                                       googleAppsEvent,
                                       coveredRange,
                                       busyTimes,
                                       tentativeTimes);
            Assert.AreEqual(coveredRange.Start, startDate);
            Assert.AreEqual(coveredRange.End, endDate);
            Assert.AreEqual(busyTimes.Count, 0);
            Assert.AreEqual(tentativeTimes.Count, 1);
            Assert.AreEqual(tentativeTimes[0].Start, startDate);
            Assert.AreEqual(tentativeTimes[0].End, endDate);
            tentativeTimes.Clear();


            // Tentative event with attendee invited should be treated as tentative.
            john.Attendee_Status.Value = Who.AttendeeStatus.EVENT_INVITED;
            CallConvertEventToFreeBusy(user,
                                       googleAppsEvent,
                                       coveredRange,
                                       busyTimes,
                                       tentativeTimes);
            Assert.AreEqual(coveredRange.Start, startDate);
            Assert.AreEqual(coveredRange.End, endDate);
            Assert.AreEqual(busyTimes.Count, 0);
            Assert.AreEqual(tentativeTimes.Count, 1);
            Assert.AreEqual(tentativeTimes[0].Start, startDate);
            Assert.AreEqual(tentativeTimes[0].End, endDate);
            tentativeTimes.Clear();

            // Tentative event with attendee accepted should be treated as tentative.
            john.Attendee_Status.Value = Who.AttendeeStatus.EVENT_ACCEPTED;
            CallConvertEventToFreeBusy(user,
                                       googleAppsEvent,
                                       coveredRange,
                                       busyTimes,
                                       tentativeTimes);
            Assert.AreEqual(coveredRange.Start, startDate);
            Assert.AreEqual(coveredRange.End, endDate);
            Assert.AreEqual(busyTimes.Count, 0);
            Assert.AreEqual(tentativeTimes.Count, 1);
            Assert.AreEqual(tentativeTimes[0].Start, startDate);
            Assert.AreEqual(tentativeTimes[0].End, endDate);
            tentativeTimes.Clear();

            // Tentative event with attendee declined should be treated as free.
            john.Attendee_Status.Value = Who.AttendeeStatus.EVENT_DECLINED;
            CallConvertEventToFreeBusy(user,
                                       googleAppsEvent,
                                       coveredRange,
                                       busyTimes,
                                       tentativeTimes);
            Assert.AreEqual(coveredRange.Start, startDate);
            Assert.AreEqual(coveredRange.End, endDate);
            Assert.AreEqual(tentativeTimes.Count, 0);
            Assert.AreEqual(busyTimes.Count, 0);

            googleAppsEvent.Status = EventEntry.EventStatus.CANCELED;

            // Cancelled event with attendee tentative should be treated as free.
            john.Attendee_Status.Value = Who.AttendeeStatus.EVENT_TENTATIVE;
            CallConvertEventToFreeBusy(user,
                                       googleAppsEvent,
                                       coveredRange,
                                       busyTimes,
                                       tentativeTimes);
            Assert.AreEqual(coveredRange.Start, startDate);
            Assert.AreEqual(coveredRange.End, endDate);
            Assert.AreEqual(tentativeTimes.Count, 0);
            Assert.AreEqual(busyTimes.Count, 0);


            // Cancelled event with attendee invited should be treated as free.
            john.Attendee_Status.Value = Who.AttendeeStatus.EVENT_INVITED;
            CallConvertEventToFreeBusy(user,
                                       googleAppsEvent,
                                       coveredRange,
                                       busyTimes,
                                       tentativeTimes);
            Assert.AreEqual(coveredRange.Start, startDate);
            Assert.AreEqual(coveredRange.End, endDate);
            Assert.AreEqual(tentativeTimes.Count, 0);
            Assert.AreEqual(busyTimes.Count, 0);

            // Cancelled event with attendee accepted should be treated as free.
            john.Attendee_Status.Value = Who.AttendeeStatus.EVENT_ACCEPTED;
            CallConvertEventToFreeBusy(user,
                                       googleAppsEvent,
                                       coveredRange,
                                       busyTimes,
                                       tentativeTimes);
            Assert.AreEqual(coveredRange.Start, startDate);
            Assert.AreEqual(coveredRange.End, endDate);
            Assert.AreEqual(tentativeTimes.Count, 0);
            Assert.AreEqual(busyTimes.Count, 0);

            // Cancelled event with attendee declined should be treated as free.
            john.Attendee_Status.Value = Who.AttendeeStatus.EVENT_DECLINED;
            CallConvertEventToFreeBusy(user,
                                       googleAppsEvent,
                                       coveredRange,
                                       busyTimes,
                                       tentativeTimes);
            Assert.AreEqual(coveredRange.Start, startDate);
            Assert.AreEqual(coveredRange.End, endDate);
            Assert.AreEqual(tentativeTimes.Count, 0);
            Assert.AreEqual(busyTimes.Count, 0);
        }

        private static void CallConvertEventToFreeBusy(
            ExchangeUser user,
            EventEntry googleAppsEvent,
            DateTimeRange coveredRange,
            List<DateTimeRange> busyTimes,
            List<DateTimeRange> tentativeTimes)
        {
            object[] parameters = new object[5] { user, googleAppsEvent, coveredRange, busyTimes, tentativeTimes };
            BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;

            Type type = typeof(SchedulePlusFreeBusyWriter);
            MethodInfo methodInfo = type.GetMethod("ConvertEventToFreeBusy", flags);

            methodInfo.Invoke(null, parameters);
        }

    }
}
