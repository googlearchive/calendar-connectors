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
using System.Text;
using System.Xml;

using Google.GCalExchangeSync.Library.Util;

namespace Google.GCalExchangeSync.Library.WebDav
{
    /// <summary>
    /// A set of resources for WebDAV XML request bodies. 
    /// </summary>
    public class WebDavXmlResources
    {
        /// <summary>
        /// Request template for creating an appointment
        /// </summary>
        public const string CreateAppointment = 
            "Google.GCalExchangeSync.Library.webdav.xml.CreateAppointment.xml";

        /// <summary>
        /// Request template for Finding item URLs
        /// </summary>
        public const string FindItemUrls =
            "Google.GCalExchangeSync.Library.webdav.xml.FindItemUrls.xml";

        /// <summary>
        /// Request template for loading free busy info from a public folder
        /// </summary>
        public const string LoadFreeBusy =
            "Google.GCalExchangeSync.Library.webdav.xml.LoadFreeBusy.xml";

        /// <summary>
        /// Request template for finding a set of appointments 
        /// </summary>
		public const string FindAppointments =
		   "Google.GCalExchangeSync.Library.webdav.xml.FindAppointments.xml";

        /// <summary>
        /// Return the contents of one of the WebDAV XML resources
        /// </summary>
        /// <param name="resourceAddress">Name of the resource</param>
        /// <param name="formatArgs">A set of parameters to substitute into the resource</param>
        /// <returns>The resource with substitutions made</returns>
		public static string GetText(string resourceAddress, params object[] formatArgs)
        {
            string text;
                        
            text = EmbeddedResourceUtility.GetTextDocument( resourceAddress );
            
            // TODO: BUG: each formatArg value needs to be xml-encoded
            if ( formatArgs.Length > 0 )
                text = string.Format( text, formatArgs );

            return text;
        }
    }
}
