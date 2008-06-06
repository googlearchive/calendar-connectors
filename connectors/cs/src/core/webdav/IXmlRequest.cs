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
using System.IO;
using System.Net;
using System.Text;
using System.Xml;

namespace Google.GCalExchangeSync.Library.WebDav
{
    /// <summary>
    /// Http header
    /// </summary>
    public class HttpHeader
    {
        /// <summary>
        /// Create a Htpp Header object with the given name and value
        /// </summary>
        /// <param name="headerName">The name of the header</param>
        /// <param name="headerValue">The value of the header</param>
        public HttpHeader(string headerName, string headerValue)
        {
            name = headerName;
            value = headerValue;
        }

        /// <summary>
        /// The name of the header
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// The name of the value
        /// </summary>
        public string Value
        {
            get { return value; }
        }

        private readonly string name;
        private readonly string value;
    }

    /// <summary>
    /// Handle making a request to
    /// </summary>
    public interface IXmlRequest
    {
        /// <summary>
        /// Make a request to receive XML data
        /// </summary>
        /// <param name="url">The URL of the endpoint to make the request to</param>
        /// <param name="method">The HTTP method to use</param>
        /// <param name="body">The optional request body</param>
        /// <param name="headers">Optional headers to add to the request</param>
        /// <returns>The Response from the call</returns>
        Stream IssueRequest(string url, Method method, string body, HttpHeader[] headers);
    }
}
