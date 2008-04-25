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
    public class ExchangeGatewayTest : ExchangeGatewayMock
	{
		private readonly string calendarUrl;
		private static readonly string exchangeServer = "http://example.org";

		private ExchangeUser _user;
		private List<DateTimeRange> _freeBusy;
		private List<Appointment> _appointments;
		private readonly DateTimeRange _window;

		private ExchangeUser createFauxUser(string name, string email)
		{
			ExchangeUser result = new ExchangeUser();
			result.Email = email;
			result.MailNickname = name;
			result.LegacyExchangeDN = "/o=Microsoft/ou=APPS-ABC/cn=RECIPIENTS/cn=ASAMPLE";
			return result;
		}

		public ExchangeGatewayTest() : base()
		{
			calendarUrl = ExchangeUtil.GetDefaultCalendarUrl(exchangeServer, "test");
			_freeBusy = new List<DateTimeRange>();
			_appointments = new List<Appointment>();

			DateTime start = DateTime.Parse("2007-06-30T05:16:11.000Z");
			_window = new DateTimeRange(start, start.AddDays(60));

			// One hour block as free busy AND appointment
			_freeBusy.Add(new DateTimeRange(start.AddHours(2), start.AddHours(3)));
			_appointments.Add(createAppointment(start.AddHours(2), start.AddHours(3)));

			// Two adjacent appointments - one free busy event
			start = start.AddDays(5);

			_freeBusy.Add(new DateTimeRange(start, start.AddMinutes(60)));
			_appointments.Add(createAppointment(start, start.AddMinutes(30)));
			_appointments.Add(createAppointment(start.AddMinutes(30), start.AddMinutes(60)));

			// Add event only to Free Busy
			start = start.AddDays(4);

			_freeBusy.Add(new DateTimeRange(start.AddMinutes(30), start.AddMinutes(50)));

			// Add event only to Appointments
			start = start.AddDays(20);

			_appointments.Add(createAppointment(start.AddMinutes(45), start.AddMinutes(50)));

			// Add all day appointment
			start = DateTime.Parse("2007-07-30T00:00:00.000Z");

			_appointments.Add(createAppointment(start, start.AddHours(24), true));
			_freeBusy.Add(new DateTimeRange(start, start.AddHours(24)));

			// Add two appointments that start at the same time,
			// one overlapping and one not
			start = start.AddDays(2);

			_appointments.Add(createAppointment(start, start.AddHours(2)));
			_appointments.Add(createAppointment(start, start.AddHours(6)));
			_freeBusy.Add(new DateTimeRange(start, start.AddHours(2)));

		}

		private Appointment createAppointment(DateTime start, DateTime end)
		{
			return createAppointment(start, end, false);
		}

		private Appointment createAppointment(DateTime start, DateTime end, bool isAllDay)
		{
			Appointment result = new Appointment();
			result.StartDate = start;
			result.Created = DateTime.Now;
			result.BusyStatus = BusyStatus.Busy;
			result.EndDate = end;
			result.AllDayEvent = isAllDay;
			return result;
		}

		[SetUp]
		public void Init()
		{
			_user = createFauxUser("test", "test@example.org");
		}

		private FreeBusy createFreeBusy(ExchangeUser user)
		{
			FreeBusy result = new FreeBusy();
			result.User = user;

			result.All = _freeBusy.GetRange(0, _freeBusy.Count);
			result.Busy = _freeBusy.GetRange(0, _freeBusy.Count);
			return result;
		}

		private List<Appointment> createAppointments()
		{
            // Need to make a copy
			return _appointments.GetRange(0, _appointments.Count);
		}

		[Test]
		public void TestFreeBusyAppointmentMerge()
		{
			FreeBusy fb = createFreeBusy(_user);
			this.MergeFreeBusyWithAppointments(_user, fb, _appointments, _window.Start, _window.End);

			Assert.AreEqual(5, _user.BusyTimes.Count);
			Assert.AreEqual(7, _appointments.Count);
			Assert.AreEqual(5, fb.All.Count);

			DateTime start = fb.All[0].Start;
			DateTime end = fb.All[0].End;

			Assert.AreEqual(start, _user.BusyTimes[start].StartDate);
			Assert.AreEqual(end, _user.BusyTimes[start].EndDate);
			Assert.AreEqual(1, _user.BusyTimes[start].Appointments.Count);
			Assert.AreEqual(start, _user.BusyTimes[start].Appointments[0].StartDate);
			Assert.AreEqual(end, _user.BusyTimes[start].Appointments[0].EndDate);
			Assert.IsFalse(_user.BusyTimes[start].Appointments[0].AllDayEvent);

			start = fb.All[1].Start;
			end = fb.All[1].End;

			Assert.AreEqual(start, _user.BusyTimes[start].StartDate);
			Assert.AreEqual(end, _user.BusyTimes[start].EndDate);
			Assert.AreEqual(2, _user.BusyTimes[start].Appointments.Count);
			Assert.AreEqual(start, _user.BusyTimes[start].Appointments[0].StartDate);
			Assert.AreEqual(end, _user.BusyTimes[start].Appointments[1].EndDate);
			Assert.IsFalse(_user.BusyTimes[start].Appointments[0].AllDayEvent);
			Assert.IsFalse(_user.BusyTimes[start].Appointments[1].AllDayEvent);

			start = fb.All[2].Start;
			end = fb.All[2].End;

			Assert.AreEqual(start, _user.BusyTimes[start].StartDate);
			Assert.AreEqual(end, _user.BusyTimes[start].EndDate);
			Assert.AreEqual(0, _user.BusyTimes[start].Appointments.Count);

			start = fb.All[3].Start;
			end = fb.All[3].End;

			Assert.AreEqual(start, _user.BusyTimes[start].StartDate);
			Assert.AreEqual(end, _user.BusyTimes[start].EndDate);
			Assert.AreEqual(1, _user.BusyTimes[start].Appointments.Count);
			Assert.AreEqual(start, _user.BusyTimes[start].Appointments[0].StartDate);
			Assert.AreEqual(end, _user.BusyTimes[start].Appointments[0].EndDate);
			Assert.IsTrue(_user.BusyTimes[start].Appointments[0].AllDayEvent);

			start = fb.All[4].Start;
			end = fb.All[4].End;

			Assert.AreEqual(start, _user.BusyTimes[start].StartDate);
			Assert.AreEqual(end, _user.BusyTimes[start].EndDate);
			Assert.AreEqual(1, _user.BusyTimes[start].Appointments.Count);
			Assert.AreEqual(start, _user.BusyTimes[start].Appointments[0].StartDate);
			Assert.AreEqual(start.AddHours(2), _user.BusyTimes[start].Appointments[0].EndDate);

			//dump(_user.BusyTimes);
		}

        [Test]
        public void TestAppointmentLookup()
        {
            this.WebDAVMock.Appointments = createAppointments();
            DateTimeRange range = DateTimeRange.Full;

            List<Appointment> result = this.Appointments.Lookup(_user, range);

            Assert.IsTrue(_user.HaveAppointmentDetail);
            Assert.AreEqual(_appointments.Count, result.Count);
        }

        [Test]
        public void TestAppointmentLookupFailure()
        {
            this.WebDAVMock.Appointments = createAppointments();
            this.WebDAVMock.WithFailure = true;

            DateTimeRange range = DateTimeRange.Full;

            List<Appointment> result = this.Appointments.Lookup(_user, range);

            Assert.IsFalse(_user.HaveAppointmentDetail);
            Assert.AreEqual(0, result.Count);
        }

        void dump(FreeBusyCollection fbc)
		{
			Console.WriteLine("Begin FreeBusyCollection");

			foreach (DateTime dt in fbc.Keys)
			{
				FreeBusyTimeBlock b = fbc[dt];
				Console.WriteLine("{0}: {1} -> {2} [{3}]", 
					dt, b.StartDate, b.EndDate, b.Appointments.Count );
			}
			Console.WriteLine("End FreeBusyCollection");
		}
	}
}
