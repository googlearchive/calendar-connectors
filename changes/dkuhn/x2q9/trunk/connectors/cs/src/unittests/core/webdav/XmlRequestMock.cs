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
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.XPath;

using Google.GCalExchangeSync.Library;
using Google.GCalExchangeSync.Library.Util;

namespace Google.GCalExchangeSync.Library.WebDav
{
    public class XmlRequestMock : IXmlRequest
    {
        private Exception _exceptionToThrow = null;
        private Stream _response;
        private string _validUrl;
        private Method _method;

        public Exception ExceptionToThrow
        {
            get { return _exceptionToThrow; }
            set { _exceptionToThrow = value; }
        }

        public Stream ResponseBody
        {
            get { return _response; }
            set { _response = value; }
        }

        public string ValidUrl
        {
            get { return _validUrl; }
            set { _validUrl = value; }
        }

        public Method ValidMethod
        {
            get { return _method; }
            set { _method = value; }
        }

        public Stream IssueRequest(string url, Method method, string body, HttpHeader[] headers)
        {
            if ( _exceptionToThrow != null )
                throw _exceptionToThrow;

            if (!string.IsNullOrEmpty(_validUrl) && !_validUrl.Equals(url))
                throw new Exception("Invalid URL used");

            if (_method != method)
            {
                string msg = string.Format("Invalid Method: {0}", method.Name);
                throw new Exception(msg);
            }

            return _response;
        }
    }
}
