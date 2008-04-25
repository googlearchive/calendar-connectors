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
    public class AppointmentGatewayMock : AppointmentService
    {
        public AppointmentGatewayMock(string exchangeServer, WebDavQuery webdav) :
            base(exchangeServer, webdav)
        {
        }

        private List<Appointment> _writtenAppt = new List<Appointment>();
        private List<Appointment> _updatedAppt = new List<Appointment>();
        private List<Appointment> _deletedAppt = new List<Appointment>();

        public List<Appointment> Written
        {
            get { return _writtenAppt; }
        }

        public List<Appointment> Updated
        {
            get { return _updatedAppt; }
        }

        public List<Appointment> Deleted
        {
            get { return _deletedAppt; }
        }

        public void Reset()
        {
            _writtenAppt = new List<Appointment>();
            _updatedAppt = new List<Appointment>();
            _deletedAppt = new List<Appointment>();
        }

        public override void WriteAppointments(ExchangeUser user, List<Appointment> appointments)
        {
            foreach (Appointment a in appointments)
            {
                _writtenAppt.Add(a);
            }
        }

        public override void UpdateAppointments(ExchangeUser user, List<Appointment> appointments)
        {
            foreach (Appointment a in appointments)
            {
                _updatedAppt.Add(a);
            }
        }

        public override void DeleteAppointments(ExchangeUser user, List<Appointment> appointments)
        {
            foreach (Appointment a in appointments)
            {
                _deletedAppt.Add(a);
            }
        }
    }

    public class WebDavQueryMock : WebDavQuery
    {
        public WebDavQueryMock(ICredentials credentials, IXmlRequest requestor)
            : base(credentials, requestor)
        {
        }

        public bool WithFailure
        {
            get { return withFailure; }
            set { withFailure = value; }
        }

        public override List<Appointment> LoadAppointments(string folderUrl, DateTime start, DateTime end)
        {
            if (withFailure)
            {
                throw new WebException();
            }

            return Appointments;
        }

        public List<Appointment> Appointments
        {
            get { return appointments; }
            set { appointments = value; }
        }

        private List<Appointment> appointments = new List<Appointment>();
        private bool withFailure = false;
    }

    public class ExchangeGatewayMock : ExchangeService
    {
        private AppointmentGatewayMock appointmentsMock;
        private static readonly string server = "http://localhost";
        private static readonly string username = "user";
        private static readonly string password = "pass";
        private WebDavQueryMock webdav;

        public ExchangeGatewayMock()
            : base(server, username, password)
        {
            ICredentials credentials = new NetworkCredential(username, password);
            IXmlRequest requestor = new XmlRequestMock();
            webdav = new WebDavQueryMock(credentials, requestor);
            this.appointmentsMock =
                new AppointmentGatewayMock(server, webdav);
        }

        public override AppointmentService Appointments
        {
            get { return appointmentsMock; }
        }

        public AppointmentGatewayMock AppointmentsMock
        {
            get { return appointmentsMock; }
        }

        public WebDavQueryMock WebDAVMock
        {
            get { return webdav; }
        }

        public override void GetCalendarInfoForUser(ExchangeUser user, DateTimeRange window)
        {
            return;
        }
    }
}
