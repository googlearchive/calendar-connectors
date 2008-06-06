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
using System.Xml.Schema;
using System.Xml.XPath;
using System.Reflection;

using Google.GCalExchangeSync.Library;
using Google.GCalExchangeSync.Library.Util;

using NUnit.Framework;

namespace Google.GCalExchangeSync.Library.WebDav
{
    [TestFixture]
    public class WebDavQueryBuilderTest
    {
        [SetUp]
        public void Init()
        {
        }

        [Test]
        public void TestPropertiesDefinition()
        {
            Assert.IsTrue(CheckSingleValueProperty(FreeBusyProperty.DisableFullFidelity));
            Assert.IsTrue(CheckSingleValueProperty(FreeBusyProperty.MessageLocaleId));
            Assert.IsTrue(CheckSingleValueProperty(FreeBusyProperty.LocaleId));
            Assert.IsTrue(CheckSingleValueProperty(FreeBusyProperty.ScheduleInfoResourceType));
            Assert.IsTrue(CheckSingleValueProperty(FreeBusyProperty.StartOfPublishedRange));
            Assert.IsTrue(CheckSingleValueProperty(FreeBusyProperty.EndOfPublishedRange));
            Assert.IsTrue(CheckSingleValueProperty(FreeBusyProperty.FreeBusyRangeTimestamp));

            Assert.IsTrue(CheckMultiValueProperty(FreeBusyProperty.MergedMonths));
            Assert.IsTrue(CheckMultiValueProperty(FreeBusyProperty.MergedEvents));
            Assert.IsTrue(CheckMultiValueProperty(FreeBusyProperty.TentativeMonths));
            Assert.IsTrue(CheckMultiValueProperty(FreeBusyProperty.TentativeEvents));
            Assert.IsTrue(CheckMultiValueProperty(FreeBusyProperty.BusyMonths));
            Assert.IsTrue(CheckMultiValueProperty(FreeBusyProperty.BusyEvents));
            Assert.IsTrue(CheckMultiValueProperty(FreeBusyProperty.OutOfOfficeMonths));
            Assert.IsTrue(CheckMultiValueProperty(FreeBusyProperty.OutOfOfficeEvents));
        }

        [Test]
        public void TestUpdate()
        {
            string body = null;
            WebDavQueryBuilder queryBuilder = new WebDavQueryBuilder();
            Property up2 = new Property("up2", "NS1");
            FreeBusyProperty up3 = new FreeBusyProperty("up3", "FB", "type1");
            FreeBusyProperty up4 = new FreeBusyProperty("up4", "MV", "mv.type2");
            FreeBusyProperty up5 = new FreeBusyProperty("up5", "MV", "mv.type5");
            List<string> emptyList = new List<string>();
            List<string> up4Values = new List<string>();
            List<string> up5Values = new List<string>();
            string expectedBody =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<a:propertyupdate " +
                "xmlns:a=\"DAV:\" " +
                "xmlns:b=\"urn:uuid:c2f41010-65b3-11d1-a29f-00aa00c14882/\" " +
                "xmlns:c=\"xml:\" " +
                "xmlns:d=\"NS1\" " +
                "xmlns:e=\"FB\" " +
                "xmlns:f=\"MV\">" +
                "<a:set>" +
                "<a:prop>" +
                "<a:up1>" +
                "1" +
                "</a:up1>" +
                "<d:up2>" +
                "2" +
                "</d:up2>" +
                "<e:up3 b:dt=\"type1\">" +
                "3" +
                "</e:up3>" +
                "<f:up4 b:dt=\"mv.type2\">" +
                "<c:v>" +
                "one" +
                "</c:v>" +
                "</f:up4>" +
                "<f:up5 b:dt=\"mv.type5\">" +
                "<c:v>" +
                "0x1234" +
                "</c:v>" +
                "<c:v>" +
                "0xabcd" +
                "</c:v>" +
                "</f:up5>" +
                "</a:prop>" +
                "</a:set>" +
                "</a:propertyupdate>";

            up4Values.Add("one");

            up5Values.Add("0x1234");
            up5Values.Add("0xabcd");

            VerifyWellFormedXml(expectedBody);

            for (int i = 0; i < 2; i++)
            {
                bool exceptionCaught = false;
                try
                {
                    body = queryBuilder.BuildQueryBody();
                }
                catch (Exception)
                {
                    exceptionCaught = true;
                }
                Assert.IsTrue(exceptionCaught);

                queryBuilder.AddUpdateProperty("up1", "DAV:", "1");
                queryBuilder.AddUpdateProperty(up2, "2");
                queryBuilder.AddUpdateProperty(up3, "3");

                try
                {
                    queryBuilder.AddUpdateProperty(up4, emptyList);
                }
                catch (Exception)
                {
                    exceptionCaught = true;
                }
                Assert.IsTrue(exceptionCaught);

                queryBuilder.AddUpdateProperty(up4, up4Values);
                queryBuilder.AddUpdateProperty(up5, up5Values);

                body = queryBuilder.BuildQueryBody();
                VerifyWellFormedXml(body);
                Assert.IsTrue(CompareXml(body, expectedBody));
                queryBuilder.Reset();
            }
        }

        [Test]
        public void TestRemove()
        {
            string body = null;
            WebDavQueryBuilder queryBuilder = new WebDavQueryBuilder();
            Property del2 = new Property("del2", "NS2");
            FreeBusyProperty del3 = new FreeBusyProperty("del3", "FB", "type2");
            string expectedBody =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<a:propertyupdate " +
                "xmlns:a=\"DAV:\" " +
                "xmlns:b=\"urn:uuid:c2f41010-65b3-11d1-a29f-00aa00c14882/\" " +
                "xmlns:c=\"xml:\" " +
                "xmlns:d=\"NS1\" " +
                "xmlns:e=\"NS2\" " +
                "xmlns:f=\"FB\">" +
                "<a:remove>" +
                "<a:prop>" +
                "<d:del1/>" +
                "<e:del2/>" +
                "<f:del3/>" +
                "</a:prop>" +
                "</a:remove>" +
                "</a:propertyupdate>";

            VerifyWellFormedXml(expectedBody);

            for (int i = 0; i < 2; i++)
            {
                bool exceptionCaught = false;
                try
                {
                    body = queryBuilder.BuildQueryBody();
                }
                catch (Exception)
                {
                    exceptionCaught = true;
                }
                Assert.IsTrue(exceptionCaught);

                queryBuilder.AddRemoveProperty("del1", "NS1");
                queryBuilder.AddRemoveProperty(del2);
                queryBuilder.AddRemoveProperty(del3);

                body = queryBuilder.BuildQueryBody();
                VerifyWellFormedXml(body);
                Assert.IsTrue(CompareXml(body, expectedBody));
                queryBuilder.Reset();
            }
        }

        [Test]
        public void TestUpdateAndRemove()
        {
            string body = null;
            WebDavQueryBuilder queryBuilder = new WebDavQueryBuilder();
            Property up2 = new Property("up2", "NS1");
            FreeBusyProperty up3 = new FreeBusyProperty("up3", "FB", "type1");
            Property del2 = new Property("del2", "NS2");
            FreeBusyProperty del3 = new FreeBusyProperty("del3", "FB", "type2");
            string expectedBody =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<a:propertyupdate " +
                "xmlns:a=\"DAV:\" " +
                "xmlns:b=\"urn:uuid:c2f41010-65b3-11d1-a29f-00aa00c14882/\" " +
                "xmlns:c=\"xml:\" " +
                "xmlns:d=\"NS1\" " +
                "xmlns:e=\"FB\" " +
                "xmlns:f=\"NS2\">" +
                "<a:set>" +
                "<a:prop>" +
                "<a:up1>" +
                "1" +
                "</a:up1>" +
                "<d:up2>" +
                "2" +
                "</d:up2>" +
                "<e:up3 b:dt=\"type1\">" +
                "3" +
                "</e:up3>" +
                "</a:prop>" +
                "</a:set>" +
                "<a:remove>" +
                "<a:prop>" +
                "<d:del1/>" +
                "<f:del2/>" +
                "<e:del3/>" +
                "</a:prop>" +
                "</a:remove>" +
                "</a:propertyupdate>";

            VerifyWellFormedXml(expectedBody);

            for (int i = 0; i < 2; i++)
            {
                bool exceptionCaught = false;
                try
                {
                    body = queryBuilder.BuildQueryBody();
                }
                catch (Exception)
                {
                    exceptionCaught = true;
                }
                Assert.IsTrue(exceptionCaught);

                queryBuilder.AddUpdateProperty("up1", "DAV:", "1");
                queryBuilder.AddUpdateProperty(up2, "2");
                queryBuilder.AddUpdateProperty(up3, "3");

                queryBuilder.AddRemoveProperty("del1", "NS1");
                queryBuilder.AddRemoveProperty(del2);
                queryBuilder.AddRemoveProperty(del3);

                body = queryBuilder.BuildQueryBody();
                VerifyWellFormedXml(body);
                Assert.IsTrue(CompareXml(body, expectedBody));
                queryBuilder.Reset();
            }
        }

        [Test]
        public void TestNamespaceManagement()
        {
            char ns1Prefix = ' ';
            char ns2Prefix = ' ';
            char davPrefix = ' ';
            char xmlPrefix = ' ';
            char dtPrefix = ' ';
            char tempPrefix = ' ';
            WebDavQueryBuilder queryBuilder = new WebDavQueryBuilder();

            ns1Prefix = CallFindOrAddNamespace(queryBuilder, "NS1");
            Assert.IsTrue(ns1Prefix > 'a' && ns1Prefix < ('a' + 'z') / 2);
            tempPrefix = CallFindOrAddNamespace(queryBuilder, "NS1");
            Assert.AreEqual(ns1Prefix, tempPrefix);

            davPrefix = CallFindOrAddNamespace(queryBuilder, "DAV:");
            Assert.IsTrue(davPrefix < ns1Prefix && davPrefix >= 'a');
            tempPrefix = CallFindOrAddNamespace(queryBuilder, "DAV:");
            Assert.AreEqual(davPrefix, tempPrefix);

            xmlPrefix = CallFindOrAddNamespace(queryBuilder, "xml:");
            Assert.IsTrue(xmlPrefix < ns1Prefix && xmlPrefix >= 'a');
            tempPrefix = CallFindOrAddNamespace(queryBuilder, "xml:");
            Assert.AreEqual(xmlPrefix, tempPrefix);

            dtPrefix = CallFindOrAddNamespace(queryBuilder,
                                              "urn:uuid:c2f41010-65b3-11d1-a29f-00aa00c14882/");
            Assert.IsTrue(dtPrefix < ns1Prefix && dtPrefix >= 'a');
            tempPrefix = CallFindOrAddNamespace(queryBuilder,
                                                "urn:uuid:c2f41010-65b3-11d1-a29f-00aa00c14882/");
            Assert.AreEqual(dtPrefix, tempPrefix);

            ns2Prefix = CallFindOrAddNamespace(queryBuilder, "NS2");
            Assert.IsTrue(ns2Prefix > ns1Prefix && xmlPrefix <= 'z');
            tempPrefix = CallFindOrAddNamespace(queryBuilder, "NS2");
            Assert.AreEqual(ns2Prefix, tempPrefix);

            char[] prefixes = new char[5] { ns1Prefix, ns2Prefix, davPrefix, dtPrefix, xmlPrefix };

            for (int i = 0; i < prefixes.Length; i++)
            {
                for (int j = i + 1; j < prefixes.Length; j++)
                {
                    Assert.AreNotEqual(prefixes[i], prefixes[j]);
                }
            }

            char c = ns2Prefix;
            for (c++; c <= 'z'; c++)
            {
                tempPrefix = CallFindOrAddNamespace(queryBuilder, c.ToString());
                Assert.AreEqual(tempPrefix, c);
            }

            bool exceptionCaught = false;
            try
            {
                tempPrefix = CallFindOrAddNamespace(queryBuilder, "Boom:");
            }
            catch (Exception)
            {
                exceptionCaught = true;
            }
            Assert.IsTrue(exceptionCaught);

            queryBuilder.Reset();
            tempPrefix = CallFindOrAddNamespace(queryBuilder, "NEW:");
            Assert.AreEqual(ns1Prefix, tempPrefix);
        }

        private static char CallFindOrAddNamespace(
            WebDavQueryBuilder queryBuilder,
            string namespaceName)
        {
            char namespacePrefix = ' ';
            object[] parameters = new object[1] { namespaceName };
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;

            Type type = typeof(WebDavQueryBuilder);
            MethodInfo methodInfo = type.GetMethod("FindOrAddNamespace", flags);

            namespacePrefix = (char)methodInfo.Invoke(queryBuilder, parameters);

            return namespacePrefix;
        }

        private static void VerifyWellFormedXml(
            string xml)
        {
            XmlDocument xmlDoc = new XmlDocument();

            xmlDoc.LoadXml(xml);
        }

        private static bool CompareAttributes(
            XmlReader reader1,
            XmlReader reader2)
        {
            bool more = true;

            more = reader1.MoveToFirstAttribute();
            if (more ^ reader2.MoveToFirstAttribute())
            {
                return false;
            }

            while (more)
            {
                if (reader1.Prefix == "xmlns")
                {
                    if (reader2.Prefix != "xmlns" ||
                        reader1.IsEmptyElement != reader2.IsEmptyElement ||
                        reader1.Value != reader2.Value)
                    {
                        return false;
                    }
                }
                else
                {
                    if (reader1.LocalName != reader2.LocalName ||
                        reader1.NamespaceURI != reader2.NamespaceURI ||
                        reader1.IsEmptyElement != reader2.IsEmptyElement ||
                        reader1.Value != reader2.Value)
                    {
                        return false;
                    }
                }

                more = reader1.MoveToNextAttribute();
                if (more ^ reader2.MoveToNextAttribute())
                {
                    return false;
                }
            }

            return true;
        }


        private static bool CompareXml(
            string xml1,
            string xml2)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.XmlResolver = null;
            settings.IgnoreComments = true;
            settings.IgnoreWhitespace = true;
            settings.ValidationType = ValidationType.None;
            settings.ValidationFlags = XmlSchemaValidationFlags.None;
            StringReader stringReader1 = new StringReader(xml1);
            StringReader stringReader2 = new StringReader(xml2);
            XmlReader reader1 = XmlReader.Create(stringReader1, settings);
            XmlReader reader2 = XmlReader.Create(stringReader2, settings);

            while (reader1.Read() && reader2.Read())
            {
                if (reader1.NodeType != reader2.NodeType)
                {
                    return false;
                }

                switch (reader1.NodeType)
                {
                    case XmlNodeType.Element:
                        {
                            if (reader1.LocalName != reader2.LocalName ||
                                reader1.NamespaceURI != reader2.NamespaceURI ||
                                reader1.IsEmptyElement != reader2.IsEmptyElement)
                            {
                                return false;
                            }

                            if (!CompareAttributes(reader1, reader2))
                            {
                                return false;
                            }

                            break;
                        }

                    case XmlNodeType.Text:
                        {
                            if (reader1.Value != reader2.Value)
                            {
                                return false;
                            }
                            break;
                        }

                    case XmlNodeType.CDATA:
                        {
                            if (reader1.Value != reader2.Value)
                            {
                                return false;
                            }
                            break;
                        }

                    case XmlNodeType.EntityReference:
                        {
                            return false;
                        }

                    case XmlNodeType.Entity:
                        {
                            return false;
                        }

                    case XmlNodeType.ProcessingInstruction:
                        {
                            return false;
                        }

                    case XmlNodeType.Comment:
                        {
                            break;
                        }

                    case XmlNodeType.Document:
                        {
                            break;
                        }

                    case XmlNodeType.DocumentType:
                        {
                            return false;
                        }

                    case XmlNodeType.DocumentFragment:
                        {
                            return false;
                        }

                    case XmlNodeType.Notation:
                        {
                            return false;
                        }

                    case XmlNodeType.Whitespace:
                        {
                            break;
                        }

                    case XmlNodeType.SignificantWhitespace:
                        {
                            return false;
                        }

                    case XmlNodeType.EndElement:
                        {
                            if (reader1.LocalName != reader2.LocalName ||
                                reader1.NamespaceURI != reader2.NamespaceURI)
                            {
                                return false;
                            }

                            break;
                        }

                    case XmlNodeType.EndEntity:
                        {
                            return false;
                        }

                    case XmlNodeType.XmlDeclaration:
                        {
                            break;
                        }

                    default:
                        {
                            Debug.Assert(false);
                            return false;
                        }
                }
            }

            return reader1.Read() == reader2.Read();
        }

        private static bool CheckMultiValueProperty(
            FreeBusyProperty property)
        {
            string propertyName = property.Name;
            string propertyNamespace = property.NameSpace;
            string propertyType = property.Type;

            if (propertyType.Substring(0, 3) != "mv.")
            {
                // The property is supposed to be multi-valued, thus the type must start with "mv."
                return false;
            }

            if (propertyNamespace != "http://schemas.microsoft.com/mapi/proptag/")
            {
                return true;
            }

            if (propertyName[0] != 'x' && propertyName[0] != 'X')
            {
                return true;
            }

            int propTag = 0;

            try
            {
                propTag = Convert.ToInt32("0" + propertyName, 16);
            }
            catch (FormatException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }

            if ((propTag & 0x1000) == 0)
            {
                return false;
            }

            switch (propTag & 0xfff)
            {
                case 3:
                    return propertyType == "mv.int";

                case 0x102:
                    return propertyType == "mv.bin.base64";

                default:
                    return false;
            }
        }

        private static bool CheckSingleValueProperty(
            FreeBusyProperty property)
        {
            string propertyName = property.Name;
            string propertyNamespace = property.NameSpace;
            string propertyType = property.Type;

            if (propertyType.Substring(0, 3) == "mv.")
            {
                // The property is supposed to be sinlge-valued, thus the type must not start with "mv."
                return false;
            }

            if (propertyNamespace != "http://schemas.microsoft.com/mapi/proptag/")
            {
                return true;
            }

            if (propertyName[0] != 'x' && propertyName[0] != 'X')
            {
                return true;
            }

            int propTag = 0;

            try
            {
                propTag = Convert.ToInt32("0" + propertyName, 16);
            }
            catch (FormatException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }

            if ((propTag & 0x1000) != 0)
            {
                return false;
            }

            switch (propTag & 0xfff)
            {
                case 3:
                    return propertyType == "int";

                case 0xb:
                    return propertyType == "boolean";

                case 0x40:
                    return propertyType == "dateTime.tz";

                case 0x102:
                    return propertyType == "bin.base64";

                default:
                    return false;
            }
        }

    }

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

        private Stream getResponseXML(string resourceName)
        {
            string resource =
                string.Format("UnitTests.core.webdav.responses.{0}", resourceName);
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource);

            return stream;
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

            DateTime start = DateUtil.NowUtc.AddDays(-7);
            DateTime end = DateUtil.NowUtc.AddDays(+7);

            List<Appointment> result = _webdav.LoadAppointments(calendarUrl, start, end);
            Assert.AreEqual(result.Count, 6);

            Assert.AreEqual(result[0].Created, DateUtil.ParseDateToUtc("2007-12-30T05:16:11.844Z"));
            Assert.AreEqual(result[0].StartDate, DateUtil.ParseDateToUtc("2008-01-02T21:00:00.000Z"));
            Assert.AreEqual(result[0].EndDate, DateUtil.ParseDateToUtc("2008-01-03T00:00:00.000Z"));

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
            DateTime start = DateUtil.ParseDateToUtc("2007-12-25T01:42:50Z");
            DateTime end = DateUtil.ParseDateToUtc("2008-01-08T01:42:50Z");
            DateTimeRange range = new DateTimeRange(start, end);

            ExchangeUserDict users = new ExchangeUserDict();
            users.Add(_user.Email, _user);

            Dictionary<ExchangeUser, FreeBusy> result = _webdav.LoadFreeBusy(exchangeServer,
                                                                             users,
                                                                             range);
            Assert.AreEqual(1, result.Count);

            FreeBusy fb = result[_user];

            Assert.AreEqual(6, fb.All.Count);
            Assert.AreEqual(6, fb.Busy.Count);
            Assert.AreEqual(0, fb.OutOfOffice.Count);
            Assert.AreEqual(0, fb.Tentative.Count);

            //dumpFreeBusy(fb.Busy);

            Assert.AreEqual(DateUtil.ParseDateToUtc("2007-12-26T18:00:00Z"), fb.Busy[0].Start);
            Assert.AreEqual(DateUtil.ParseDateToUtc("2007-12-26T18:30:00Z"), fb.Busy[0].End);
            Assert.AreEqual(DateUtil.ParseDateToUtc("2007-12-26T20:30:00Z"), fb.Busy[1].Start);
            Assert.AreEqual(DateUtil.ParseDateToUtc("2007-12-26T21:00:00Z"), fb.Busy[1].End);
            Assert.AreEqual(DateUtil.ParseDateToUtc("2007-12-31T17:30:00Z"), fb.Busy[2].Start);
            Assert.AreEqual(DateUtil.ParseDateToUtc("2007-12-31T18:00:00Z"), fb.Busy[2].End);
            Assert.AreEqual(DateUtil.ParseDateToUtc("2007-12-31T21:00:00Z"), fb.Busy[3].Start);
            Assert.AreEqual(DateUtil.ParseDateToUtc("2007-12-31T21:30:00Z"), fb.Busy[3].End);

            Assert.AreEqual(DateUtil.ParseDateToUtc("2007-12-26T18:00:00Z"), fb.All[0].Start);
            Assert.AreEqual(DateUtil.ParseDateToUtc("2007-12-26T18:30:00Z"), fb.All[0].End);
            Assert.AreEqual(DateUtil.ParseDateToUtc("2007-12-26T20:30:00Z"), fb.All[1].Start);
            Assert.AreEqual(DateUtil.ParseDateToUtc("2007-12-26T21:00:00Z"), fb.All[1].End);
            Assert.AreEqual(DateUtil.ParseDateToUtc("2007-12-31T17:30:00Z"), fb.All[2].Start);
            Assert.AreEqual(DateUtil.ParseDateToUtc("2007-12-31T18:00:00Z"), fb.All[2].End);
            Assert.AreEqual(DateUtil.ParseDateToUtc("2007-12-31T21:00:00Z"), fb.All[3].Start);
            Assert.AreEqual(DateUtil.ParseDateToUtc("2007-12-31T21:30:00Z"), fb.All[3].End);
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
