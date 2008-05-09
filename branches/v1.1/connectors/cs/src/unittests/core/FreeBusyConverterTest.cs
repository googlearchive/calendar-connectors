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
    public class FreeBusyConverterTest
    {
        [Test]
        public void TestDateAdvance()
        {

            DateTime r = DateUtil.ParseDateToUtc("2007-12-30T00:00:00.000Z");
            r = DateUtil.StartOfNextMonth(r);

            Assert.AreEqual(new DateTime(2008, 01, 01, 00, 00, 00, DateTimeKind.Unspecified), r);
        }

        [Test]
        public void TestConvertDateTimeBlocks()
        {
            List<DateTimeRange> src = new List<DateTimeRange>();
            List<DateTimeRange> dst = new List<DateTimeRange>();

            DateTime s, e;
            s = new DateTime(2007, 07, 23, 13, 45, 00, DateTimeKind.Unspecified);
            src.Add(new DateTimeRange(s, s.AddHours(3)));
            dst.Add(new DateTimeRange(s, s.AddHours(3)));

            s = new DateTime(2007, 07, 30, 13, 45, 00, DateTimeKind.Unspecified);
            src.Add(new DateTimeRange(s, s));
            dst.Add(new DateTimeRange(s, s));

            s = new DateTime(2007, 08, 15, 13, 45, 00, DateTimeKind.Unspecified);
            e = new DateTime(2007, 10, 11, 02, 45, 00, DateTimeKind.Unspecified);
            src.Add(new DateTimeRange(s, e));
            dst.Add(new DateTimeRange(s, 
                new DateTime(2007, 08, 31, 23, 59, 00, DateTimeKind.Unspecified)));
            dst.Add(new DateTimeRange(
                new DateTime(2007, 09, 01, 00, 00, 00, DateTimeKind.Unspecified),
                new DateTime(2007, 09, 30, 23, 59, 00, DateTimeKind.Unspecified)));
            dst.Add(new DateTimeRange(
                new DateTime(2007, 10, 01, 00, 00, 00, DateTimeKind.Unspecified), e));

            src.Add(new DateTimeRange(
                new DateTime(2008, 01, 01, 00, 00, 00, DateTimeKind.Unspecified),
                new DateTime(2008, 03, 31, 23, 59, 00, DateTimeKind.Unspecified)));
            dst.Add(new DateTimeRange(
                new DateTime(2008, 01, 01, 00, 00, 00, DateTimeKind.Unspecified),
                new DateTime(2008, 01, 31, 23, 59, 00, DateTimeKind.Unspecified)));
            dst.Add(new DateTimeRange(
                new DateTime(2008, 02, 01, 00, 00, 00, DateTimeKind.Unspecified),
                new DateTime(2008, 02, 29, 23, 59, 00, DateTimeKind.Unspecified)));
            dst.Add(new DateTimeRange(
                new DateTime(2008, 03, 01, 00, 00, 00, DateTimeKind.Unspecified),
                new DateTime(2008, 03, 31, 23, 59, 00, DateTimeKind.Unspecified)));

            src.Add(new DateTimeRange(
                new DateTime(2008, 04, 01, 00, 00, 00, DateTimeKind.Unspecified),
                new DateTime(2008, 05, 30, 23, 59, 00, DateTimeKind.Unspecified)));
            dst.Add(new DateTimeRange(
                new DateTime(2008, 04, 01, 00, 00, 00, DateTimeKind.Unspecified),
                new DateTime(2008, 04, 30, 23, 59, 00, DateTimeKind.Unspecified)));
            dst.Add(new DateTimeRange(
                new DateTime(2008, 05, 01, 00, 00, 00, DateTimeKind.Unspecified),
                new DateTime(2008, 05, 30, 23, 59, 00, DateTimeKind.Unspecified)));

            List<string> monthData = new List<string>();
            List<string> base64Data = new List<string>();

            FreeBusyConverter.ConvertDateTimeBlocksToBase64String(src, monthData, base64Data);

            List<DateTimeRange> result = new List<DateTimeRange>();

            for (int i = 0; i < monthData.Count; i++)
            {
                int month = int.Parse(monthData[i]);
                string data = base64Data[i];

                List<DateTimeRange> r =
                    FreeBusyConverter.ConvertBase64StringsToDateTimeBlocks(month, data);
                result.AddRange(r);
            }

            Assert.AreEqual(dst.Count, result.Count);
            for (int i = 0; i < dst.Count; i++)
            {
                Assert.AreEqual(dst[i], result[i]);
            }
        }

        void dump(List<DateTimeRange> range)
        {
            foreach (DateTimeRange r in range)
            {
                Console.WriteLine("DST: {0}", r);
            }
        }
    }
}
