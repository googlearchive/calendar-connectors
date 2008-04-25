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
using log4net;

namespace Google.GCalExchangeSync.Library.WebDav
{
    /// <summary>
    /// Implementaton of an XML request
    /// </summary>
    public class XmlRequestImpl : IXmlRequest
    {
        private static readonly int maxRedirects = 5;

        /// <summary>
        /// Logger for XmlRequestImpl
        /// </summary>
        protected static readonly log4net.ILog log =
               log4net.LogManager.GetLogger(typeof(XmlRequestImpl));

        private ICredentials credentials;
        private readonly string connectionGroup;

        /// <summary>
        /// Create an XML request
        /// </summary>
        /// <param name="credentials">Rhe credentials to use</param>
        /// <param name="group">The connection group to use</param>
        public XmlRequestImpl( ICredentials credentials, string group )
        {
            this.credentials = credentials;
            this.connectionGroup = group;
        }

        /// <summary>
        /// Issue a request to the URL and return the response
        /// </summary>
        /// <param name="url">The URL to make a request to</param>
        /// <param name="method">The HTTP verb to use</param>
        /// <param name="body">The request body to use</param>
        /// <returns>The response body</returns>
        public string IssueRequest( string url, Method method, string body )
        {
            int remainingAttempts = maxRedirects;

            while (remainingAttempts > 0)
            {
                log.DebugFormat(
                    "Issuing WebDAV Request: {0} to {1} - Group {2}", 
                    method.Name, url, connectionGroup);

                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);

                // Handle redirects manually
                request.AllowAutoRedirect = false;
                request.KeepAlive = true;

                // This is necesssary to make Windows Authentication
                // use keep alive. Due to the large number of
                // connections we may make to Exchange, if we don't do this,
                // the process may exhaust the supply of available ports. To keep
                // this "safe", requests are isolated by connection pool. See:
                // http://msdn2.microsoft.com/en-us/library/system.net.httpwebrequest.unsafeauthenticatedconnectionsharing.aspx
                request.UnsafeAuthenticatedConnectionSharing = true;
                request.ConnectionGroupName = connectionGroup;


                // BUG: Exchange sometimes serves different content based on the user agent so
                // we have to fake being IE?!?!?!
                request.UserAgent = 
                    "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; SV1; .NET CLR 2.0.50727)";

                request.Credentials = credentials;
                request.Method = method.Name;
                request.Accept = "*/*";

                // Set request body if t here is one
                if (!string.IsNullOrEmpty(body))
                {
                    byte[] bodyBytes = Encoding.UTF8.GetBytes(body);

                    request.ContentType = "text/xml";

                    using (Stream writer = request.GetRequestStream())
                    {
                        writer.Write(bodyBytes, 0, bodyBytes.Length);
                        writer.Close();
                    }
                }

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.Found:
                        case HttpStatusCode.Moved:
                        case HttpStatusCode.RedirectKeepVerb:
                        case HttpStatusCode.RedirectMethod:
                            url = response.GetResponseHeader("Location");

                            if (response.GetResponseStream() != null)
                                response.GetResponseStream().Close();
                            response.Close();

                            if (!string.IsNullOrEmpty(ConfigCache.ExchangeDefaultDomain))
                            {
                                // If a default domain is provided, don't redirect outside it
                                Uri uri = new Uri(url);
                                if (!uri.Host.EndsWith(ConfigCache.ExchangeDefaultDomain))
                                {
                                    throw new WebException("Cannot redirect outside the default domain");
                                }
                            }

                            log.DebugFormat(
                                "Redirect: {0} to {1} status {2}", 
                                method, url, response.StatusCode);

                            // Preserve the credentials and verb though all types
                            // of redirects for WebDAV
                            remainingAttempts--;
                            break;

                        default:
                            {
                                StreamReader reader = new StreamReader(response.GetResponseStream());
                                string result = reader.ReadToEnd();

                                reader.Close();
                                response.GetResponseStream().Close();
                                response.Close();

                                return result;
                            }
                    }
                }
            }

            return string.Empty;
        }
   }
}
