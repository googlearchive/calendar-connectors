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
using System.Xml.Schema;
using System.Xml.XPath;

using Google.GCalExchangeSync.Library;
using Google.GCalExchangeSync.Library.Util;
using log4net;

namespace Google.GCalExchangeSync.Library.WebDav
{
    /// <summary>
    /// Handle building WebDAV queries to Exchangeaffecting multile properties.
    /// </summary>
    public class WebDavQueryBuilder
    {
        /// <summary>
        /// Logger for WebDavQuery
        /// </summary>
        protected static readonly log4net.ILog log =
           log4net.LogManager.GetLogger(typeof(WebDavQueryBuilder));

        private Dictionary<string, char> namespacesMap;
        private char davPrefix;
        private char dtPrefix;
        private char xmlPrefix;
        private char nextNamespacePrefix;
        private StringBuilder updateXml;
        private StringBuilder removeXml;
        private StringBuilder queryBody;

        /// <summary>
        /// Create a WebDAV query builder
        /// </summary>
        public WebDavQueryBuilder()
        {
            updateXml = new StringBuilder(1024);
            removeXml = new StringBuilder(1024);
            queryBody = new StringBuilder(1024);

            nextNamespacePrefix = 'a';
            davPrefix = ' ';
            dtPrefix = ' ';
            xmlPrefix = ' ';
            namespacesMap = new Dictionary<string, char>();

            Reset();
        }

        /// <summary>
        /// Reset the query builder to initial state, so it can be used to build a new query.
        /// </summary>
        public void Reset()
        {
            updateXml.Length = 0;
            removeXml.Length = 0;
            queryBody.Length = 0;

            nextNamespacePrefix = 'a';
            namespacesMap.Clear();

            davPrefix = FindOrAddNamespace("DAV:");
            Debug.Assert(davPrefix == 'a');

            dtPrefix = FindOrAddNamespace("urn:uuid:c2f41010-65b3-11d1-a29f-00aa00c14882/");
            Debug.Assert(dtPrefix == 'b');

            xmlPrefix = FindOrAddNamespace("xml:");
            Debug.Assert(xmlPrefix == 'c');
        }

        /// <summary>
        /// Add a property to the query to be updated to the given value.
        /// </summary>
        /// <param name="property">Property to add</param>
        /// <param name="propertyValue">The value for the property</param>
        public void AddUpdateProperty(
            Property property,
            string propertyValue)
        {
            AddUpdateProperty(property.Name,
                              property.NameSpace,
                              propertyValue);
        }

        /// <summary>
        /// Add a property to the query to be updated to the given value.
        /// </summary>
        /// <param name="propertyName">The name of the property to add</param>
        /// <param name="propertyNamespace">The Xml namespace of the property</param>
        /// <param name="propertyValue">The value for the property</param>
        public void AddUpdateProperty(
            string propertyName,
            string propertyNamespace,
            string propertyValue)
        {
            char namespacePrefix = ' ';

            if (log.IsDebugEnabled)
            {
                log.DebugFormat("Adding {0}:{1} = \"{2}\"",
                                propertyNamespace,
                                propertyName,
                                propertyValue);
            }

            namespacePrefix = FindOrAddNamespace(propertyNamespace);

            //  <property's namespace prefix : property's name>
            //      property's value
            //  </property's namespace prefix : property's name>
            updateXml.AppendFormat(
                "<{0}:{1}>{2}</{0}:{1}>",
                namespacePrefix,
                propertyName,
                propertyValue);
        }

        /// <summary>
        /// Add a property to the query to be deleted.
        /// </summary>
        /// <param name="property">The property to delete</param>
        public void AddRemoveProperty(
            Property property)
        {
            AddRemoveProperty(property.Name,
                              property.NameSpace);
        }

        /// <summary>
        /// Add a property to the query to be deleted.
        /// </summary>
        /// <param name="propertyName">The name of the property to delete</param>
        /// <param name="propertyNamespace">The Xml namespace of the property</param>
        public void AddRemoveProperty(
            string propertyName,
            string propertyNamespace)
        {
            char namespacePrefix = ' ';

            if (log.IsDebugEnabled)
            {
                log.DebugFormat("Adding {0}:{1} to be deleted",
                                propertyNamespace,
                                propertyName);
            }

            namespacePrefix = FindOrAddNamespace(propertyNamespace);

            //  <property's namespace prefix : property's name/>
            removeXml.AppendFormat(
                "<{0}:{1}/>",
                namespacePrefix,
                propertyName);
        }

        /// <summary>
        /// Add a free busy property to the query to be updated to the given value.
        /// </summary>
        /// <param name="property">Property to add</param>
        /// <param name="propertyValue">The value for the property</param>
        public void AddUpdateProperty(
            FreeBusyProperty property,
            string propertyValue)
        {
            AddUpdateProperty(property.Name,
                              property.NameSpace,
                              property.Type,
                              propertyValue);
        }

        /// <summary>
        /// Add a free busy property to the query to be updated to the given value.
        /// </summary>
        /// <param name="propertyName">The name of the property to add</param>
        /// <param name="propertyNamespace">The Xml namespace of the property</param>
        /// <param name="propertyType">The data type of the property</param>
        /// <param name="propertyValue">The value for the property</param>
        public void AddUpdateProperty(
            string propertyName,
            string propertyNamespace,
            string propertyType,
            string propertyValue)
        {
            char namespacePrefix = ' ';

            if (log.IsDebugEnabled)
            {
                log.DebugFormat("Adding {0}:{1} = ({2})\"{3}\"",
                                propertyNamespace,
                                propertyName,
                                propertyType,
                                propertyValue);
            }

            namespacePrefix = FindOrAddNamespace(propertyNamespace);

            //  <property's namespace prefix : property's name "dt"'s prefix : dt = property's type>
            //      property's value
            //  </property's namespace prefix : property's name>
            updateXml.AppendFormat(
                "<{0}:{2} {1}:dt=\"{3}\">{4}</{0}:{2}>",
                namespacePrefix,
                dtPrefix,
                propertyName,
                propertyType,
                propertyValue);
        }

        /// <summary>
        /// Add a multivalued free busy property to the query to be updated to the given values.
        /// </summary>
        /// <param name="property">Property to add</param>
        /// <param name="propertyValues">The list of value for the property</param>
        public void AddUpdateProperty(
            FreeBusyProperty property,
            List<string> propertyValues)
        {
            AddUpdateProperty(property.Name,
                              property.NameSpace,
                              property.Type,
                              propertyValues);
        }

        /// <summary>
        /// Add a multivalued free busy property to the query to be updated to the given values.
        /// </summary>
        /// <param name="propertyName">The name of the property to add</param>
        /// <param name="propertyNamespace">The Xml namespace of the property</param>
        /// <param name="propertyType">The data type of the property</param>
        /// <param name="propertyValues">The list of value for the property</param>
        public void AddUpdateProperty(
            string propertyName,
            string propertyNamespace,
            string propertyType,
            List<string> propertyValues)
        {
            char namespacePrefix = ' ';

            if (log.IsDebugEnabled)
            {
                // TODO: that Join+ToArray is really bad. We need after way.
                log.DebugFormat("Adding {0}:{1} = ({2})\"{3}\"",
                                propertyNamespace,
                                propertyName,
                                propertyType,
                                string.Join(",", propertyValues.ToArray()));
            }

            if (propertyValues.Count == 0)
            {
                throw new ArgumentException("Multi valued properties must have at least one value");
            }

            namespacePrefix = FindOrAddNamespace(propertyNamespace);

            //  <property's namespace prefix : property's name "dt"'s prefix : dt = property's type>
            //      <"xml:"'s prefix : v>
            //          property's value1
            //      </"xml:"'s prefix : v>
            //      ...
            //      <"xml:"'s prefix : v>
            //          property's valueN
            //      </"xml:"'s prefix : v>
            //  </property's namespace prefix : property's name>
            updateXml.AppendFormat(
                "<{0}:{2} {1}:dt=\"{3}\">",
                namespacePrefix,
                dtPrefix,
                propertyName,
                propertyType);

            foreach (string propertyValue in propertyValues)
            {
                updateXml.AppendFormat(
                    "<{0}:v>{1}</{0}:v>",
                    xmlPrefix,
                    propertyValue);
            }

            updateXml.AppendFormat(
                "</{0}:{1}>",
                namespacePrefix,
                propertyName);
        }

        /// <summary>
        /// Builds the query body to update/delete the set properties.
        /// </summary>
        public string BuildQueryBody()
        {
            string result = null;

            if (updateXml.Length == 0 && removeXml.Length == 0)
            {
                throw new Exception("At least one of remove or update should be set");
            }

            queryBody.Length = 0;
            queryBody.EnsureCapacity(updateXml.Length + removeXml.Length + 256);

            queryBody.AppendFormat(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><{0}:propertyupdate ",
                davPrefix);

            foreach (KeyValuePair<string, char> pair in namespacesMap)
            {
                queryBody.AppendFormat("xmlns:{0}=\"{1}\" ", pair.Value, pair.Key);
            }

            queryBody.Append(">");

            // The order of the update vs remove elements in the body of the query
            // might be significant if the same property is both updated and delete.
            // Since this is an indication that something wrong is going on in the caller,
            // we are not going to provide any checks or guarantees.

            if (updateXml.Length != 0)
            {
                queryBody.AppendFormat(
                    "<{0}:set><{0}:prop>{1}</{0}:prop></{0}:set>",
                    davPrefix,
                    updateXml);
            }

            if (removeXml.Length != 0)
            {
                queryBody.AppendFormat(
                    "<{0}:remove><{0}:prop>{1}</{0}:prop></{0}:remove>",
                    davPrefix,
                    removeXml);
            }

            queryBody.AppendFormat("</{0}:propertyupdate>", davPrefix);

            result = queryBody.ToString();

            if (log.IsDebugEnabled)
            {
                log.DebugFormat("The query built is {0}", result);
            }

            return result;
        }

        private char FindOrAddNamespace(
            string namespaceName)
        {
            char namespacePrefix = ' ';

            if (namespacesMap.TryGetValue(namespaceName, out namespacePrefix))
            {
                return namespacePrefix;
            }

            if (nextNamespacePrefix > 'z')
            {
                throw new ArgumentException("Ran out of namespace prefixes");
            }

            namespacePrefix = nextNamespacePrefix;
            namespacesMap.Add(namespaceName, namespacePrefix);
            nextNamespacePrefix++;

            return namespacePrefix;
        }
    }

    #region WebDAV Queries

    /// <summary>
    /// Handle making WebDAV queries to Exchange
    /// </summary>
    public class WebDavQuery
    {
        /// <summary>
        /// Logger for WebDavQuery
        /// </summary>
        protected static readonly log4net.ILog _log =
           log4net.LogManager.GetLogger(typeof(WebDavQuery));

        private static readonly int kFreeBusyInterval = 15;

        private ICredentials _credentials;
        private bool _isLogToFileEnabled;
        private IXmlRequest _requestor;

        /// <summary>
        /// Create a WebDAV manager
        /// </summary>
        /// <param name="credentials">Credentials to use</param>
        /// <param name="group">Connection group to use</param>
        public WebDavQuery( ICredentials credentials, string group ) :
            this(credentials, new XmlRequestImpl(credentials, group))
        {
        }

        /// <summary>
        /// Create a WebDAV manager
        /// </summary>
        /// <param name="credentials">Credentials to use</param>
        /// <param name="requestor">Underlying networking implementation</param>
        public WebDavQuery(ICredentials credentials, IXmlRequest requestor)
        {
            _credentials = credentials;
            _isLogToFileEnabled = false;
            _requestor = requestor;
        }

        /// <summary>
        /// Enable logging request / resonse to end
        /// </summary>
        public bool IsLogToFileEnabled
        {
            get { return _isLogToFileEnabled; }
            set { _isLogToFileEnabled = value; }
        }

        /// <summary>
        /// Issue a requet to the networking implementation
        /// </summary>
        /// <param name="url">The URL to request</param>
        /// <param name="method">The HTTP method to use</param>
        /// <param name="body">The request body to send</param>
        /// <returns>The response body</returns>
        public Stream IssueRequest(string url, Method method, string body)
        {
            return _requestor.IssueRequest(url, method, body, null);
        }

        /// <summary>
        /// Issue a requet to the networking implementation
        /// </summary>
        /// <param name="url">The URL to request</param>
        /// <param name="method">The HTTP method to use</param>
        /// <param name="body">The request body to send</param>
        /// <param name="headers">Optional headers to add to the request</param>
        /// <returns>The response body</returns>
        public Stream IssueRequest(string url, Method method, string body, HttpHeader[] headers)
        {
            return _requestor.IssueRequest(url, method, body, headers);
        }

        /// <summary>
        /// Issue a requet to the networking implementation
        /// </summary>
        /// <param name="url">The URL to request</param>
        /// <param name="method">The HTTP method to use</param>
        /// <param name="body">The request body to send</param>
        /// <returns>void</returns>
        public void IssueRequestIgnoreResponse(
            string url,
            Method method,
            string body)
        {
            _requestor.IssueRequest(url, method, body, null).Close();
        }

        /// <summary>
        /// Issue a requet to the networking implementation
        /// </summary>
        /// <param name="url">The URL to request</param>
        /// <param name="method">The HTTP method to use</param>
        /// <param name="body">The request body to send</param>
        /// <param name="headers">Optional headers to add to the request</param>
        /// <returns>void</returns>
        public void IssueRequestIgnoreResponse(
            string url,
            Method method,
            string body,
            HttpHeader[] headers)
        {
            _requestor.IssueRequest(url, method, body, headers).Close();
        }

        /// <summary>
        /// Issue a requet to the networking implementation
        /// </summary>
        /// <param name="url">The URL to request</param>
        /// <param name="method">The HTTP method to use</param>
        /// <param name="body">The request body to send</param>
        /// <returns>The response read into string body</returns>
        public string IssueRequestAndFetchReponse(
            string url,
            Method method,
            string body)
        {
            Stream stream = _requestor.IssueRequest(url, method, body, null);

            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();

                reader.Close();
                stream.Close();

                return result;
            }
        }

        /// <summary>
        /// Do a WebDAV COPY on the server of one URL to another.
        /// </summary>
        /// <param name="source">The URL to copy from</param>
        /// <param name="destination">The URL to copy to</param>
        /// <returns>The response body</returns>
        public string Copy( string source, string destination )
        {
            string result = string.Empty;

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create( source );

            request.ContentType = "text/xml";
            request.Credentials = _credentials;
            request.Method = Method.COPY.Name;

            request.Headers.Add( "Destination", destination );
            request.Headers.Add( "Overwrite", "T" );
            request.Headers.Add( "Allow-rename", "f" );
            request.Headers.Add( "Depth", "infinity" );

            using ( HttpWebResponse response = (HttpWebResponse)request.GetResponse() )
            {
                StreamReader reader = new StreamReader( response.GetResponseStream() );

                result = reader.ReadToEnd();
            }

            return result;
        }

        /// <summary>
        /// Remove a property from a WebDAV resource
        /// </summary>
        /// <param name="url">The URL of the resource</param>
        /// <param name="propName">Property name to remove</param>
        /// <param name="nameSpace">Property namespace to remove</param>
        /// <returns>void</returns>
        public void RemoveProperty( string url, string propName, string nameSpace )
        {
            string body = string.Format(
                 "<?xml version=\"1.0\"?>" +
                 "<g:propertyupdate xmlns:g=\"DAV:\" xmlns:o=\"{0}\">" +
                 "<g:remove>" +
                 "<g:prop>" +
                 "<o:{1}/>" +
                 "</g:prop>" +
                 "</g:remove>" +
                 "</g:propertyupdate>",
                 nameSpace,
                 propName );

            IssueRequestIgnoreResponse(url, Method.PROPPATCH, body);
        }

        /// <summary>
        /// Modify a property from a WebDAV resource
        /// </summary>
        /// <param name="destination">The URL of the resource</param>
        /// <param name="property">Property to modify</param>
        /// <param name="propertyValue">The new Property value</param>
        /// <returns>void</returns>
        public void UpdateProperty(string destination, Property property, string propertyValue)
        {
            string body = BuildPropertyQuery( property.Name, property.NameSpace, propertyValue );

            IssueRequestIgnoreResponse(destination, Method.PROPPATCH, body);
        }

        /// <summary>
        /// Modify a free busy property from a WebDAV resource
        /// </summary>
        /// <param name="destination">The URL of the resource</param>
        /// <param name="property">Property to modify</param>
        /// <param name="propertyValue">The new Property value</param>
        /// <returns>void</returns>
        public void UpdateFreeBusyProperty(
            string destination,
            FreeBusyProperty property,
            string propertyValue)
        {
            string body = BuildFreeBusyPropertyQuery( property.Name, property.NameSpace, property.Type, propertyValue );

            IssueRequestIgnoreResponse(destination, Method.PROPPATCH, body);
        }

        /// <summary>
        /// Modify a multi-valued free busy property from a WebDAV resource
        /// </summary>
        /// <param name="destination">The URL of the resource</param>
        /// <param name="property">Property to modify</param>
        /// <param name="propertyValues">The new Property values</param>
        /// <returns>void</returns>
        public void UpdateFreeBusyProperty(
            string destination,
            FreeBusyProperty property,
            List<string> propertyValues )
        {
            string body = BuildFreeBusyMultiValuedPropertyQuery( property.Name, property.NameSpace, property.Type, propertyValues );

            IssueRequestIgnoreResponse(destination, Method.PROPPATCH, body);
        }

        #region Property Query Builders
        private string BuildPropertyQuery( string propertyName, string propertyNamespace, string propertyValue )
        {
            string body = String.Format(
                  "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                + "<a:propertyupdate xmlns:d=\"{1}\" xmlns:a=\"DAV:\">"
                + " <a:set>"
                + "     <a:prop>"
                + "         <d:{0}>{2}</d:{0}>"
                + "     </a:prop>"
                + " </a:set>"
                + "</a:propertyupdate>",
                propertyName,
                propertyNamespace,
                propertyValue );

            return body;
        }

        private string BuildFreeBusyPropertyQuery(string propertyName, string propertyNamespace, string propertyType, string propertyValue)
        {
            string body = string.Format(
                  "<?xml version=\"1.0\"?>"
                + "<a:propertyupdate xmlns:a=\"DAV:\" xmlns:d=\"{3}\" "
                + "xmlns:b=\"urn:uuid:c2f41010-65b3-11d1-a29f-00aa00c14882/\" xmlns:c=\"xml:\">"
                + "<a:set>"
                + "  <a:prop>"
                + "    <d:{0} b:dt=\"{1}\">{2}</d:{0}>"
                + "  </a:prop>"
                + "</a:set>"
                + "</a:propertyupdate>",
                propertyName,
                propertyType,
                propertyValue,
                propertyNamespace );

            return body;
        }

        private string BuildFreeBusyMultiValuedPropertyQuery(
            string propertyName, string propertyNamespace, string propertyType, List<string> propertyValues )
        {
            StringBuilder body = new StringBuilder();

            body.Append(
                  "<?xml version=\"1.0\"?>"
                + "<a:propertyupdate xmlns:a=\"DAV:\" xmlns:d=\"http://schemas.microsoft.com/mapi/proptag/\" "
                + "xmlns:b=\"urn:uuid:c2f41010-65b3-11d1-a29f-00aa00c14882/\" xmlns:c=\"xml:\">"
                + "  <a:set>"
                + "    <a:prop>" );

            body.AppendFormat(
                  "      <d:{0} b:dt=\"mv.{1}\">", propertyName, propertyType );

            foreach ( string propertyValue in propertyValues )
            {
                body.AppendFormat( "        <c:v>{0}</c:v>", propertyValue );
            }

            body.AppendFormat(
                  "      </d:{0}>", propertyName );

            body.Append(
                  "    </a:prop>"
                + "  </a:set>"
                + "</a:propertyupdate>" );

            return body.ToString();
        }
        #endregion

        #region Appointments

        /// <summary>
        /// Create a mailbox appointment
        /// </summary>
        /// <param name="mailboxUrl">The mailbox URL to create the appointment at</param>
        /// <param name="appointment">The appointment to create</param>
        /// <returns>The URL of the created appointment</returns>
        public string CreateAppointment( string mailboxUrl, Appointment appointment )
        {
            string body = WebDavXmlResources.GetText(
                WebDavXmlResources.CreateAppointment,
                appointment.Subject,
                appointment.Body,
                appointment.Comment,
                appointment.IsPrivate ? "1" : "0",
                appointment.AllDayEvent ? "1" : "0",
                DateUtil.FormatDateForExchange( appointment.EndDate ),
                DateUtil.FormatDateForExchange( appointment.StartDate ),
                Convert.ToInt32( appointment.InstanceType ),
                appointment.Location,
                appointment.MeetingStatus.ToString(),
                appointment.ResponseStatus.ToString(),
                appointment.Organizer,
                appointment.BusyStatus.ToString() );

            if ( !mailboxUrl.EndsWith( "/" ) )
                mailboxUrl += "/";

            string appointmentUrl = string.Format(
                "{0}{{{1}}}.eml",
                mailboxUrl,
                Guid.NewGuid().ToString() );

            IssueRequestIgnoreResponse(appointmentUrl, Method.PROPPATCH, body);

            appointment.HRef = appointmentUrl;

            return appointmentUrl;
        }

        /// <summary>
        /// Update an existing appointment
        /// </summary>
        /// <param name="mailboxUrl">The URL of the appointment in the mailbox</param>
        /// <param name="appointment">The Update Appointment value</param>
        /// <returns>The appointment URL</returns>
        public string UpdateAppointment( string mailboxUrl, Appointment appointment )
        {
            string body = WebDavXmlResources.GetText(
                WebDavXmlResources.CreateAppointment,
                appointment.Subject,
                appointment.Body,
                appointment.Comment,
                appointment.IsPrivate ? "1" : "0",
                appointment.AllDayEvent ? "1" : "0",
                DateUtil.FormatDateForExchange( appointment.EndDate ),
                DateUtil.FormatDateForExchange( appointment.StartDate ),
                Convert.ToInt32( appointment.InstanceType ),
                appointment.Location,
                appointment.MeetingStatus.ToString(),
                appointment.ResponseStatus.ToString(),
                appointment.Organizer,
                appointment.BusyStatus.ToString() );

            IssueRequestIgnoreResponse(appointment.HRef, Method.PROPPATCH, body);

            return appointment.HRef;
        }

        /// <summary>
        /// Load a set of appointments from the server
        /// </summary>
        /// <param name="folderUrl">The mailbox URL</param>
        /// <param name="start">Start time of date range to fetch</param>
        /// <param name="end">End time of date range to fetch</param>
        /// <returns>A list of appointments from the mailbox</returns>
        public virtual List<Appointment> LoadAppointments(
            string folderUrl, DateTime start, DateTime end)
        {
            using (BlockTimer bt = new BlockTimer("LoadAppointments"))
            {
                string request = WebDavXmlResources.GetText(
                    WebDavXmlResources.FindAppointments,
                    folderUrl,
                    DateUtil.FormatDateForDASL(start),
                    DateUtil.FormatDateForDASL(end));

                string response = IssueRequestAndFetchReponse(folderUrl, Method.SEARCH, request);
                XmlDocument responseXML = ParseExchangeXml(response);
                return ParseAppointmentResultSetXml(responseXML);
            }
        }

        private List<Appointment> ParseAppointmentResultSetXml(XmlDocument doc)
        {
            List<Appointment> result = new List<Appointment>();
            XPathNavigator nav = doc.CreateNavigator();
            bool active = nav.MoveToFirstChild();

            XmlNamespaceManager ns = new XmlNamespaceManager( doc.NameTable );
            ns.AddNamespace("dav", "DAV:");
            ns.AddNamespace("cal", "urn:schemas:calendar:");
            ns.AddNamespace("mail", "urn:schemas:mailheader:");
            ns.AddNamespace("f", "http://schemas.microsoft.com/mapi/proptag/");
            ns.AddNamespace("g", "http://schemas.microsoft.com/mapi/id/{00062008-0000-0000-C000-000000000046}/");
            ns.AddNamespace("h", "http://schemas.microsoft.com/mapi/id/{00062002-0000-0000-C000-000000000046}/");

            XmlNodeList nodes = doc.SelectNodes( "//dav:response", ns );
            foreach ( XmlNode node in nodes )
            {
                Appointment appt = new Appointment();

                appt.HRef = node.SelectSingleNode("dav:href", ns).InnerXml;
                XmlNode prop = node.SelectSingleNode("dav:propstat[1]/dav:prop", ns);

                appt.AllDayEvent =
                    GetPropertyAsBool(prop, "cal:alldayevent", ns);

                appt.Body =
                    GetPropertyAsXML(prop, "f:x1000001e", ns);

                appt.BusyStatus = ConversionsUtil.ParseBusyStatus(
                    GetPropertyAsString(prop, "cal:busystatus", appt.BusyStatus.ToString(), ns));

                appt.Comment =
                    GetPropertyAsString(prop, "dav:comment", ns);

                appt.Created =
                    GetPropertyAsDateTime(prop, "dav:creationdate", ns);

                appt.StartDate =
                    GetPropertyAsDateTime(prop, "cal:dtstart", ns);

                appt.EndDate =
                    GetPropertyAsDateTime(prop, "cal:dtend", ns);

                appt.InstanceType = (InstanceType)Enum.Parse(
                    typeof(InstanceType),
                    GetPropertyAsString(prop, "cal:instancetype", appt.InstanceType.ToString(), ns),
                    true);

                appt.IsPrivate =
                    GetPropertyAsBool(prop, "g:x8506", ns);

                appt.Location =
                    GetPropertyAsString(prop, "cal:location", ns);

                appt.MeetingStatus = (MeetingStatus)Enum.Parse(
                    typeof(MeetingStatus),
                    GetPropertyAsString(prop, "cal:meetingstatus", appt.MeetingStatus.ToString(), ns),
                    true);

                appt.Organizer =
                    GetPropertyAsString(prop, "cal:organizer", ns);

                appt.ResponseStatus = (ResponseStatus)GetPropertyAsInt(prop, "h:x8218", (int)appt.ResponseStatus, ns);

                appt.Subject =
                    GetPropertyAsString(prop, "mail:subject", ns);

                result.Add(appt);
            }

            return result;
        }

        private string GetPropertyAsString(XmlNode props, string id, XmlNamespaceManager ns)
        {
            return GetPropertyAsString(props, id, string.Empty, ns);
        }

        private string GetPropertyAsString(XmlNode props, string id, string defaultValue, XmlNamespaceManager ns)
        {
            XmlNode node = props.SelectSingleNode(id, ns);
            return (node != null) ? node.InnerText : defaultValue;
        }

        private int GetPropertyAsInt(XmlNode props, string id, int defaultValue, XmlNamespaceManager ns)
        {
            XmlNode node = props.SelectSingleNode(id, ns);
            if (node == null)
            {
                return defaultValue;
            }

            string innerText = node.InnerText;
            if (string.IsNullOrEmpty(innerText))
            {
                return defaultValue;
            }

            try
            {
                return int.Parse(innerText);
            }
            catch
            {
                return defaultValue;
            }
        }

        private string GetPropertyAsXML(XmlNode props, string id, XmlNamespaceManager ns)
        {
            XmlNode node = props.SelectSingleNode(id, ns);
            return (node != null) ? node.InnerXml : string.Empty;
        }

        private DateTime GetPropertyAsDateTime(XmlNode props, string id, XmlNamespaceManager ns)
        {
            XmlNode node = props.SelectSingleNode(id, ns);
            return (node != null) ? DateUtil.ParseDateToUtc(node.InnerXml) : DateTime.MinValue;
        }

        private bool GetPropertyAsBool(XmlNode props, string id, XmlNamespaceManager ns)
        {
            XmlNode node = props.SelectSingleNode(id, ns);
            return (node != null) ? node.InnerXml.Equals("1") : false;
        }
        #endregion

        #region General Queries

        /// <summary>
        /// Delete an item from a WebDAV store
        /// </summary>
        /// <param name="itemUrl">The item to delete</param>
        /// <returns>void</returns>
        public void Delete( string itemUrl )
        {
            IssueRequestIgnoreResponse(itemUrl, Method.DELETE, string.Empty);
        }

        #endregion

        #region Free Busy Queries

        /// <summary>
        /// Load free busy information from the public folder
        /// </summary>
        /// <param name="exchangeServerUrl">The exchange server to use</param>
        /// <param name="users">The set of users to get free busy info for</param>
        /// <param name="window">The DateTime range to get free busy info for</param>
        /// <returns>A set of FreeBusy information for each user</returns>
        public Dictionary<ExchangeUser, FreeBusy> LoadFreeBusy(
            string exchangeServerUrl,
            ExchangeUserDict users,
            DateTimeRange window)
        {
            DateTimeRange roundedWindow = DateUtil.RoundRangeToInterval(window, kFreeBusyInterval);

            string url = FreeBusyUrl.GenerateFreeBusyLookupUrl(exchangeServerUrl,
                                                               users,
                                                               roundedWindow,
                                                               kFreeBusyInterval);
            Stream response = IssueRequest(url, Method.GET, string.Empty);
            Dictionary<ExchangeUser, FreeBusy> result = null;

            try
            {
                result = ParseRasterFreeBusyResponse(users,
                                                     roundedWindow.Start,
                                                     response);
            }
            finally
            {
                response.Close();
            }

            return result;
        }

        private static Dictionary<ExchangeUser, FreeBusy> ParseRasterFreeBusyResponse(
            ExchangeUserDict users,
            DateTime baseTime,
            Stream response)
        {
            Dictionary<ExchangeUser, FreeBusy> result = new Dictionary<ExchangeUser, FreeBusy>();
            string emailAddress = null;
            string freeBusyRaster = null;
            string elementName = null;
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.XmlResolver = null;
            settings.IgnoreComments = true;
            settings.IgnoreWhitespace = true;
            settings.ValidationType = ValidationType.None;
            settings.ValidationFlags = XmlSchemaValidationFlags.None;
            XmlReader reader = XmlReader.Create(response, settings);

            reader.MoveToContent();
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        elementName = reader.LocalName;
                        if (elementName == "item")
                        {
                            emailAddress = null;
                            freeBusyRaster = null;
                        }

                        break;

                    case XmlNodeType.Text:
                        if (elementName == "email")
                        {
                            emailAddress = reader.Value.ToLower();
                        }
                        else
                        {
                            if (elementName == "fbdata")
                            {
                                freeBusyRaster = reader.Value;
                            }
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (reader.LocalName == "item")
                        {
                            ParseFreeBusyRaster(users,
                                                baseTime,
                                                emailAddress,
                                                freeBusyRaster,
                                                result);

                            emailAddress = null;
                            freeBusyRaster = null;
                        }
                        elementName = string.Empty;

                        break;
                }
            }

            return result;
        }

        private static void ParseFreeBusyRaster(
            ExchangeUserDict users,
            DateTime baseTime,
            string emailAddress,
            string freeBusyRaster,
            Dictionary<ExchangeUser, FreeBusy> result)
        {
            ExchangeUser user = null;

            if (emailAddress == null || !users.TryGetValue(emailAddress, out user))
            {
                return;
            }

            FreeBusy freeBusy = new FreeBusy();
            freeBusy.User = user;

            FreeBusyConverter.ParseRasterFreeBusy(baseTime,
                                                  kFreeBusyInterval,
                                                  freeBusyRaster,
                                                  freeBusy);

            FreeBusyConverter.CondenseFreeBusyTimes(freeBusy.All);

            result[user] = freeBusy;
        }

        #endregion

        #region Utility Functions

        private XmlDocument ParseExchangeXml( string xml )
        {
            // Exchange returns invalid XML - element names that start with "0x"
            // strip them out.
            xml = xml.Replace( ":0x", ":x" );

            XmlDocument doc = new XmlDocument();
            doc.LoadXml( xml );

            return doc;
        }

        #endregion
    }

    #endregion
}
