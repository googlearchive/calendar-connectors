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
using System.IO;
using System.Reflection;
using System.Xml;

namespace Google.GCalExchangeSync.Library.Util
{
    /// <summary>
    /// Provides a convenience method for retrieving an Xml resource embedded in the currently executing
    /// assembly.
    /// </summary>
    public class EmbeddedResourceUtility
    {
        private EmbeddedResourceUtility()
        {
        }

        /// <summary>
        /// Get the XML Document resource
        /// </summary>
        /// <param name="resourceName">The XML Document resource name</param>
        /// <returns>The XML document corresponding to the resource</returns>
        public static XmlDocument GetXmlDocument( string resourceName )
        {
            XmlDocument doc = new XmlDocument();
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream( resourceName );

            using ( StreamReader reader = new StreamReader( stream ) )
            {
                doc.Load( reader );
            }

            return doc;
        }

        /// <summary>
        /// Get the resource associated with the resource name
        /// </summary>
        /// <param name="resourceName">The resource name to return the contents for</param>
        /// <returns>The resource contents</returns>
        public static string GetTextDocument( string resourceName )
        {
            string result = string.Empty;
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream( resourceName );

            using ( StreamReader reader = new StreamReader( stream ) )
            {
                result = reader.ReadToEnd();
            }

            return result;
        }
    }
}
