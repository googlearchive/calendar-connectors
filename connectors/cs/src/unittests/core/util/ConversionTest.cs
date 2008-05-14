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
using System.Globalization;
using System.Web;

using NUnit.Framework;

namespace Google.GCalExchangeSync.Library.Util
{
    [TestFixture]
    public class EscapeNonAlphaNumericTest
    {
        private static readonly int[] kNumbersToConvert = new int[] {
            1, 2, 0xf, 0x12345, 0xc001, 0x31337, 0x12345678, 0xabcdef, int.MaxValue };

        private static readonly int[] kNegativeNumbersCannotConvert = new int[] {
            -1, -2, -int.MaxValue, int.MinValue };

        private static readonly int[] kBadBases = new int[] {
            0, 1, 17, 32, int.MaxValue, -2, -int.MaxValue, int.MinValue };

        [SetUp]
        public void Init()
        {
        }

        [Test]
        public void CompareConversions()
        {
            StringBuilder converted = new StringBuilder(32);

            for (int i = 0; i < kNumbersToConvert.Length; i++)
            {
                for (int j = 2; j < 17; j++)
                {
                    CallDecimalToBase(kNumbersToConvert[i], j, 0, 'z', converted);
                    string newConversion = converted.ToString();
                    string oldConversion = OriginalDecimalToBase(kNumbersToConvert[i], j);
                    Assert.AreEqual(newConversion, oldConversion);
                }
            }
        }

        [Test]
        public void TestConversionToDecAndHex()
        {
            StringBuilder converted = new StringBuilder(32);

            for (int i = 0; i < kNumbersToConvert.Length; i++)
            {
                CallDecimalToBase(kNumbersToConvert[i], 10, 0, 'z', converted);
                string base10 = converted.ToString();
                CallDecimalToBase(kNumbersToConvert[i], 16, 0, 'z', converted);
                string base16 = converted.ToString();
                int parsed = int.Parse(base10, NumberStyles.None);
                Assert.AreEqual(kNumbersToConvert[i], parsed);
                parsed = int.Parse(base16, NumberStyles.AllowHexSpecifier);
                Assert.AreEqual(kNumbersToConvert[i], parsed);
            }
        }

        [Test]
        public void TestConversionToBinAndOct()
        {
            StringBuilder converted = new StringBuilder(32);

            for (int i = 0; i < kNumbersToConvert.Length; i++)
            {
                CallDecimalToBase(kNumbersToConvert[i], 2, 0, 'z', converted);
                string base2 = converted.ToString();
                CallDecimalToBase(kNumbersToConvert[i], 8, 0, 'z', converted);
                string base8 = converted.ToString();
                Assert.AreEqual(kNumbersToConvert[i], StringToInt(base2, 2));
                Assert.AreEqual(kNumbersToConvert[i], StringToInt(base8, 8));
            }
        }

        [Test]
        public void TestCannotConvertNegativeNumbers()
        {
            StringBuilder converted = new StringBuilder(32);

            for (int i = 0; i < kNegativeNumbersCannotConvert.Length; i++)
            {
                bool exceptionCaught = false;

                try
                {
                    CallDecimalToBase(kNegativeNumbersCannotConvert[i], 10, 0, 'z', converted);
                }
                catch (Exception)
                {
                    exceptionCaught = true;
                }

                Assert.IsTrue(exceptionCaught);
            }
        }

        [Test]
        public void TestBadBases()
        {
            StringBuilder converted = new StringBuilder(32);

            for (int i = 0; i < kBadBases.Length; i++)
            {
                bool exceptionCaught = false;

                try
                {
                    CallDecimalToBase(0xc001, kBadBases[i], 0, 'z', converted);
                }
                catch (Exception)
                {
                    exceptionCaught = true;
                }

                Assert.IsTrue(exceptionCaught);
            }
        }

        [Test]
        public void TestConvertingZero()
        {
            StringBuilder converted = new StringBuilder(32);

            CallDecimalToBase(0, 2, 0, 'z', converted);
            string base2 = converted.ToString();
            CallDecimalToBase(0, 8, 0, 'z', converted);
            string base8 = converted.ToString();
            CallDecimalToBase(0, 10, 0, 'z', converted);
            string base10 = converted.ToString();
            CallDecimalToBase(0, 16, 0, 'z', converted);
            string base16 = converted.ToString();
            Assert.AreEqual(0, StringToInt(base2, 2));
            Assert.AreEqual(0, StringToInt(base8, 8));
            int parsed = int.Parse(base10, NumberStyles.None);
            Assert.AreEqual(0, parsed);
            parsed = int.Parse(base16, NumberStyles.AllowHexSpecifier);
            Assert.AreEqual(0, parsed);
        }

        [Test]
        public void TestConversionWithPadding()
        {
            StringBuilder converted = new StringBuilder(32);

            for (int i = 0; i < 20; i++)
            {
                CallDecimalToBase(i, 8, 0, 'z', converted);
                string nonpadded = converted.ToString();
                CallDecimalToBase(i, 8, i, '0', converted);
                string padded = converted.ToString();
                Assert.AreEqual(padded, nonpadded.PadLeft(i, '0'));
            }
        }

        [Test]
        public void TestEscapeNonAlphaNumeric()
        {
            StringBuilder builder = new StringBuilder(256);
            StringBuilder escaped = new StringBuilder(256);

            for (char c = (char)1; c < (char)255; c++)
            {
                builder.Append(c);
            }

            string toBeEscaped = builder.ToString();
            ConversionsUtil.EscapeNonAlphaNumeric(toBeEscaped, escaped);
            string newEscape = escaped.ToString();
            string oldEscape = OriginalEscapeNonAlphaNumeric(toBeEscaped);
            Assert.AreEqual(oldEscape, newEscape);
        }

        private static void CallDecimalToBase(
            int dec,
            int numBase,
            int totalWidth,
            char leftPadding,
            StringBuilder result)
        {
            object[] parameters = new object[5] { dec, numBase, totalWidth, leftPadding, result };
            BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;

            Type type = typeof(ConversionsUtil);
            MethodInfo methodInfo = type.GetMethod("DecimalToBase", flags);

            methodInfo.Invoke(null, parameters);
        }

        private static int StringToInt(string str, int numBase)
        {
            int result = 0;

            foreach (char c in str)
            {
                result *= numBase;
                Assert.IsTrue(((c >= '0') && (c <= '9')) || ((c >= 'A') && (c <= 'F')));
                if ((c >= '0') && (c <= '9'))
                {
                    result += c - '0';
                }
                else
                {
                    result += c - 'A';
                }
            }

            return result;
        }

        private static readonly int base10 = 10;
        private static readonly char[] cHexa = new char[] { 'A', 'B', 'C', 'D', 'E', 'F' };
        private static readonly int[] iHexaNumeric = new int[] { 10, 11, 12, 13, 14, 15 };
        private static readonly int[] iHexaIndices = new int[] { 0, 1, 2, 3, 4, 5 };

        private static string OriginalDecimalToBase(int iDec, int numbase)
        {
            string strBin = "";
            int[] result = new int[32];
            int MaxBit = 32;

            for (; iDec > 0; iDec /= numbase)
            {
                int rem = iDec % numbase;
                result[--MaxBit] = rem;
            }

            for (int i = 0; i < result.Length; i++)
            {
                if ((int)result.GetValue(i) >= base10)
                {
                    strBin += cHexa[(int)result.GetValue(i) % base10];
                }
                else
                {
                    strBin += result.GetValue(i);
                }
            }

            strBin = strBin.TrimStart(new char[] { '0' });

            return strBin;
        }

        private static string OriginalEscapeNonAlphaNumeric(string input)
        {
            input = HttpUtility.HtmlDecode(input);
            StringBuilder sb = new StringBuilder();

            if (input != null)
            {
                foreach (Char c in input)
                {
                    if (char.IsLetter(c) || char.IsNumber(c) || c == ' ')
                    {
                        sb.Append(c);
                    }
                    else
                    {
                        string oct = OriginalDecimalToBase((int)c, 8);

                        sb.AppendFormat(@"\{0}", oct.PadLeft(3, '0'));

                    }
                }
            }

            return sb.ToString();
        }
    }
}
