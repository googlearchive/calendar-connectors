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
    public class IntervalTreeTest
    {
        private DateTime baseDate;
        private IntervalTree<DateTimeRange> tree;
 
        private void AddToTree(IntervalTree<DateTimeRange> tree, List<DateTimeRange> list)
        {
            foreach (DateTimeRange s in list)
            {
                tree.Insert(s, s);
            }
        }

        [SetUp]
        public void Init()
        {
            baseDate = DateUtil.ParseDateToUtc("2007-06-30T05:16:11.000Z");
            tree = new IntervalTree<DateTimeRange>();
        }

        [Test]
        public void TestIntervalTree()
        {
            DateTimeRange r0 = new DateTimeRange(baseDate, baseDate.AddHours(2));
            DateTimeRange r1 = new DateTimeRange(baseDate, baseDate.AddHours(4));
            DateTimeRange r2 = new DateTimeRange(baseDate.AddHours(5), baseDate.AddHours(7));
            DateTimeRange r3 = new DateTimeRange(baseDate, baseDate.AddHours(7));
            DateTimeRange r4 = new DateTimeRange(baseDate.AddHours(3), baseDate.AddHours(3));
            DateTimeRange r5 = new DateTimeRange(baseDate.AddHours(4), baseDate.AddHours(12));

            tree.Insert(r0, r0);
            tree.Insert(r1, r1);
            tree.Insert(r2, r2);
            tree.Insert(r3, r3);
            tree.Insert(r4, r4);
            tree.Insert(r5, r5);

            Assert.AreEqual(6, tree.NumNodes);
            Assert.AreEqual(5, tree.MaxDepth);

            DateTimeRange s0 = new DateTimeRange(baseDate.AddHours(1), baseDate.AddHours(1));
            List<DateTimeRange> result = tree.FindAll(s0);

            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(r0, result[0]);
            Assert.AreEqual(r1, result[1]);
            Assert.AreEqual(r3, result[2]);
        }

        [Test]
        public void TestSortedIntervalTree()
        {
            DateTimeRange r0 = new DateTimeRange(baseDate, baseDate.AddHours(2));
            DateTimeRange r1 = new DateTimeRange(baseDate, baseDate.AddHours(4));
            DateTimeRange r2 = new DateTimeRange(baseDate, baseDate.AddHours(7));
            DateTimeRange r3 = new DateTimeRange(baseDate.AddHours(3), baseDate.AddHours(3));
            DateTimeRange r4 = new DateTimeRange(baseDate.AddHours(4), baseDate.AddHours(12));
            DateTimeRange r5 = new DateTimeRange(baseDate.AddHours(5), baseDate.AddHours(7));

            tree.Insert(r0, r0);
            tree.Insert(r1, r1);
            tree.Insert(r2, r2);
            tree.Insert(r3, r3);
            tree.Insert(r4, r4);
            tree.Insert(r5, r5);

            Assert.AreEqual(6, tree.NumNodes);
            Assert.AreEqual(5, tree.MaxDepth); // TODO

            DateTimeRange s0 = new DateTimeRange(baseDate.AddHours(1), baseDate.AddHours(1));
            List<DateTimeRange> result = tree.FindAll(s0);

            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(r0, result[0]);
            Assert.AreEqual(r1, result[1]);
            Assert.AreEqual(r2, result[2]);
        }

        [Test]
        public void TestIntervalTreeTraversal()
        {
            List<DateTimeRange> source = new List<DateTimeRange>();
            source.Add(new DateTimeRange(baseDate, baseDate.AddHours(2)));
            source.Add(new DateTimeRange(baseDate, baseDate.AddHours(4)));
            source.Add(new DateTimeRange(baseDate, baseDate.AddHours(7)));
            source.Add(new DateTimeRange(baseDate.AddHours(3), baseDate.AddHours(3)));
            source.Add(new DateTimeRange(baseDate.AddHours(4), baseDate.AddHours(12)));
            source.Add(new DateTimeRange(baseDate.AddHours(5), baseDate.AddHours(7)));

            foreach (DateTimeRange s in source)
            {
                tree.Insert(s, s);
            }

            Assert.AreEqual(6, tree.NumNodes);
            Assert.AreEqual(5, tree.MaxDepth); // TODO

            List<DateTimeRange> result = tree.GetNodeList();

            Assert.AreEqual(source.Count, result.Count);
            for (int i = 0; i < result.Count; i++)
            {
                Assert.AreEqual(source[i], result[i]);
            }
        }

        [Test]
        public void TestIntervalTreeFindExact()
        {
            List<DateTimeRange> source = new List<DateTimeRange>();
            source.Add(new DateTimeRange(baseDate, baseDate.AddHours(2)));
            source.Add(new DateTimeRange(baseDate, baseDate.AddHours(4)));
            source.Add(new DateTimeRange(baseDate, baseDate.AddHours(7)));
            source.Add(new DateTimeRange(baseDate.AddHours(3), baseDate.AddHours(3)));
            source.Add(new DateTimeRange(baseDate.AddHours(4), baseDate.AddHours(12)));
            source.Add(new DateTimeRange(baseDate.AddHours(5), baseDate.AddHours(7)));
            AddToTree(tree, source);

            Assert.AreEqual(6, tree.NumNodes);

            foreach (DateTimeRange s in source)
            {
                Assert.AreEqual(s, tree.FindExact(s));
            }
        }

        [Test]
        public void TestIntervalTreeFindAllExact()
        {
            List<DateTimeRange> source = new List<DateTimeRange>();
            source.Add(new DateTimeRange(baseDate, baseDate.AddHours(2)));
            source.Add(new DateTimeRange(baseDate, baseDate.AddHours(4)));
            source.Add(new DateTimeRange(baseDate, baseDate.AddHours(7)));
            source.Add(new DateTimeRange(baseDate.AddHours(3), baseDate.AddHours(3)));
            source.Add(new DateTimeRange(baseDate.AddHours(4), baseDate.AddHours(12)));
            source.Add(new DateTimeRange(baseDate.AddHours(5), baseDate.AddHours(7)));

            List<DateTimeRange> dupes = new List<DateTimeRange>();
            source.Add(new DateTimeRange(baseDate, baseDate.AddHours(2)));
            source.Add(new DateTimeRange(baseDate, baseDate.AddHours(7)));
            source.Add(new DateTimeRange(baseDate.AddHours(4), baseDate.AddHours(12)));

            AddToTree(tree, source);
            AddToTree(tree, dupes);
            Assert.AreEqual(9, tree.NumNodes);

            foreach (DateTimeRange r in dupes)
            {
                List<DateTimeRange> result =
                    tree.FindAll(r, IntervalTreeMatch.Exact);
                Assert.AreEqual(2, result.Count);
                Assert.AreEqual(r, result[0]);
                Assert.AreEqual(r, result[1]);
            }
        }

        [Test]
        public void TestIntervalTreeFindAllContained()
        {
            List<DateTimeRange> source = new List<DateTimeRange>();
            source.Add(new DateTimeRange(baseDate, baseDate.AddHours(2)));
            source.Add(new DateTimeRange(baseDate, baseDate.AddHours(4)));
            source.Add(new DateTimeRange(baseDate, baseDate.AddHours(7)));
            source.Add(new DateTimeRange(baseDate.AddHours(3), baseDate.AddHours(3)));
            source.Add(new DateTimeRange(baseDate.AddHours(4), baseDate.AddHours(12)));
            source.Add(new DateTimeRange(baseDate.AddHours(5), baseDate.AddHours(7)));

            AddToTree(tree, source);
            Assert.AreEqual(source.Count, tree.NumNodes);

            List<DateTimeRange> result = new List<DateTimeRange>();

            result = tree.FindAll(source[0], IntervalTreeMatch.Contained);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(source[0], result[0]);
        }

        private DateTimeRange MakeRange(string from, string to, DateTimeKind kind)
        {
            return new DateTimeRange(
                DateTime.SpecifyKind(DateTime.Parse(from), kind),
                DateTime.SpecifyKind(DateTime.Parse(to), kind));
        }

        [Test]
        public void TestIntervalTreeFindAllReverseOrder()
        {
            List<DateTimeRange> source = new List<DateTimeRange>();
            source.Add(MakeRange(
                "2008-04-21T08:00:00.000Z",
                "2008-04-21T08:15:00.000Z", 
                DateTimeKind.Local));
            source.Add(MakeRange(
                "2008-04-21T09:15:00.000Z",
                "2008-04-21T09:30:00.000Z", 
                DateTimeKind.Local));
            source.Add(MakeRange(
                "2008-04-21T10:30:00.000Z",
                "2008-04-21T10:45:00.000Z", 
                DateTimeKind.Local));
            source.Add(MakeRange(
                "2008-04-21T11:45:00.000Z",
                "2008-04-21T12:00:00.000Z", 
                DateTimeKind.Local));
            source.Add(MakeRange(
                "2008-04-21T13:00:00.000Z",
                "2008-04-21T18:00:00.000Z", 
                DateTimeKind.Local));
            source.Add(MakeRange(
                "2008-04-21T14:00:00.000Z",
                "2008-04-21T15:00:00.000Z", 
                DateTimeKind.Local));
            source.Add(MakeRange(
                "2008-04-21T14:00:00.000Z",
                "2008-04-21T14:30:00.000Z", 
                DateTimeKind.Local));
            source.Add(MakeRange(
                "2008-04-21T14:30:00.000Z",
                "2008-04-21T15:00:00.000Z", 
                DateTimeKind.Local));
            source.Add(MakeRange(
                "2008-04-21T15:00:00.000Z",
                "2008-04-21T17:15:00.000Z", 
                DateTimeKind.Local));
            source.Add(MakeRange(
                "2008-04-21T15:00:00.000Z",
                "2008-04-21T16:00:00.000Z", 
                DateTimeKind.Local));
            source.Add(MakeRange(
                "2008-04-21T15:30:00.000Z",
                "2008-04-21T17:45:00.000Z", 
                DateTimeKind.Local));
            source.Add(MakeRange(
                "2008-04-21T16:00:00.000Z", 
                "2008-04-2T17:45:00.000Z", 
                DateTimeKind.Local));
            source.Reverse();

            List<DateTimeRange> keys = new List<DateTimeRange>();
            keys.Add(MakeRange(
                "2008-04-21T08:00:00.000Z",
                "2008-04-21T08:15:00.000Z",
                DateTimeKind.Unspecified));
            keys.Add(MakeRange(
                "2008-04-21T09:15:00.000Z",
                "2008-04-21T09:30:00.000Z",
                DateTimeKind.Unspecified));
            keys.Add(MakeRange(
                "2008-04-21T10:30:00.000Z",
                "2008-04-21T10:45:00.000Z",
                DateTimeKind.Unspecified));
            keys.Add(MakeRange(
                "2008-04-21T11:45:00.000Z",
                "2008-04-21T12:00:00.000Z",
                DateTimeKind.Unspecified));
            keys.Add(MakeRange(
                "2008-04-21T13:00:00.000Z",
                "2008-04-21T18:00:00.000Z",
                DateTimeKind.Unspecified));
            keys.Add(MakeRange(
                "2008-04-21T14:00:00.000Z",
                "2008-04-21T15:00:00.000Z",
                DateTimeKind.Unspecified));
            keys.Add(MakeRange(
                "2008-04-21T14:00:00.000Z",
                "2008-04-21T14:30:00.000Z",
                DateTimeKind.Unspecified));
            keys.Add(MakeRange(
                "2008-04-21T14:30:00.000Z",
                "2008-04-21T15:00:00.000Z",
                DateTimeKind.Unspecified));
            keys.Add(MakeRange(
                "2008-04-21T15:00:00.000Z",
                "2008-04-21T17:15:00.000Z",
                DateTimeKind.Unspecified));
            keys.Add(MakeRange(
                "2008-04-21T15:00:00.000Z",
                "2008-04-21T16:00:00.000Z",
                DateTimeKind.Unspecified));
            keys.Add(MakeRange(
                "2008-04-21T15:30:00.000Z",
                "2008-04-21T17:45:00.000Z",
                DateTimeKind.Unspecified));
            keys.Add(MakeRange(
                "2008-04-21T16:00:00.000Z",
                "2008-04-2T17:45:00.000Z",
                DateTimeKind.Unspecified));

            AddToTree(tree, source);
            Assert.AreEqual(source.Count, tree.NumNodes);

            foreach (DateTimeRange r in keys)
            {
                List<DateTimeRange> result = 
                    tree.FindAll(r, IntervalTreeMatch.Exact);
                Assert.AreEqual(1, result.Count);
                Assert.AreEqual(r, result[0]);
            }
        }

        [Test]
        public void TestIntervalTreeFindExactReverseOrder()
        {
            List<DateTimeRange> source = new List<DateTimeRange>();
            source.Add(MakeRange(
                "2008-04-21T08:00:00.000Z",
                "2008-04-21T08:15:00.000Z",
                DateTimeKind.Local));
            source.Add(MakeRange(
                "2008-04-21T09:15:00.000Z",
                "2008-04-21T09:30:00.000Z",
                DateTimeKind.Local));
            source.Add(MakeRange(
                "2008-04-21T10:30:00.000Z",
                "2008-04-21T10:45:00.000Z",
                DateTimeKind.Local));
            source.Add(MakeRange(
                "2008-04-21T11:45:00.000Z",
                "2008-04-21T12:00:00.000Z",
                DateTimeKind.Local));
            source.Add(MakeRange(
                "2008-04-21T13:00:00.000Z",
                "2008-04-21T18:00:00.000Z",
                DateTimeKind.Local));
            source.Add(MakeRange(
                "2008-04-21T14:00:00.000Z",
                "2008-04-21T15:00:00.000Z",
                DateTimeKind.Local));
            source.Add(MakeRange(
                "2008-04-21T14:00:00.000Z",
                "2008-04-21T14:30:00.000Z",
                DateTimeKind.Local));
            source.Add(MakeRange(
                "2008-04-21T14:30:00.000Z",
                "2008-04-21T15:00:00.000Z",
                DateTimeKind.Local));
            source.Add(MakeRange(
                "2008-04-21T15:00:00.000Z",
                "2008-04-21T17:15:00.000Z",
                DateTimeKind.Local));
            source.Add(MakeRange(
                "2008-04-21T15:00:00.000Z",
                "2008-04-21T16:00:00.000Z",
                DateTimeKind.Local));
            source.Add(MakeRange(
                "2008-04-21T15:30:00.000Z",
                "2008-04-21T17:45:00.000Z",
                DateTimeKind.Local));
            source.Add(MakeRange(
                "2008-04-21T16:00:00.000Z",
                "2008-04-21T17:45:00.000Z",
                DateTimeKind.Local));
            source.Reverse();

            List<DateTimeRange> keys = new List<DateTimeRange>();
            keys.Add(MakeRange(
                "2008-04-21T08:00:00.000Z",
                "2008-04-21T08:15:00.000Z",
                DateTimeKind.Unspecified));
            keys.Add(MakeRange(
                "2008-04-21T09:15:00.000Z",
                "2008-04-21T09:30:00.000Z",
                DateTimeKind.Unspecified));
            keys.Add(MakeRange(
                "2008-04-21T10:30:00.000Z",
                "2008-04-21T10:45:00.000Z",
                DateTimeKind.Unspecified));
            keys.Add(MakeRange(
                "2008-04-21T11:45:00.000Z",
                "2008-04-21T12:00:00.000Z",
                DateTimeKind.Unspecified));
            keys.Add(MakeRange(
                "2008-04-21T13:00:00.000Z",
                "2008-04-21T18:00:00.000Z",
                DateTimeKind.Unspecified));
            keys.Add(MakeRange(
                "2008-04-21T14:00:00.000Z",
                "2008-04-21T15:00:00.000Z",
                DateTimeKind.Unspecified));
            keys.Add(MakeRange(
                "2008-04-21T14:00:00.000Z",
                "2008-04-21T14:30:00.000Z",
                DateTimeKind.Unspecified));
            keys.Add(MakeRange(
                "2008-04-21T14:30:00.000Z",
                "2008-04-21T15:00:00.000Z",
                DateTimeKind.Unspecified));
            keys.Add(MakeRange(
                "2008-04-21T15:00:00.000Z",
                "2008-04-21T17:15:00.000Z",
                DateTimeKind.Unspecified));
            keys.Add(MakeRange(
                "2008-04-21T15:00:00.000Z",
                "2008-04-21T16:00:00.000Z",
                DateTimeKind.Unspecified));
            keys.Add(MakeRange(
                "2008-04-21T15:30:00.000Z",
                "2008-04-21T17:45:00.000Z",
                DateTimeKind.Unspecified));
            keys.Add(MakeRange(
                "2008-04-21T16:00:00.000Z",
                "2008-04-21T17:45:00.000Z",
                DateTimeKind.Unspecified));

            AddToTree(tree, source);
            Assert.AreEqual(source.Count, tree.NumNodes);

            foreach (DateTimeRange r in keys)
            {
                DateTimeRange result =
                    tree.FindExact(r);
                Assert.AreEqual(r, result);
            }
        }

    }
}
