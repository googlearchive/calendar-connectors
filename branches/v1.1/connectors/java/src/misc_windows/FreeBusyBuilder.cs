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
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.DirectoryServices;
using System.Net;
using System.Web;
using System.Xml;
using System.Xml.Schema;

namespace Google.CalendarConnector.Plugin
{
    class GWiseContact
    {
        /// <summary>
        /// The UID is the GUID used to identify the imported account.
        /// It is generateed by the GroupWise connector by hashing
        /// some properties of the contact.
        /// The format of the string is not the traditional GUID format
        /// {5F64F157-0CCA-43CC-A0A9-3A7DAB963F06}, but 4 DWORDs printed
        /// in hex with no leading zeros and separated with dashes
        /// D3143CCF-E9938337-91AD646-AB45321E, so the lentgh is somewhere
        /// between 7 and 19 characters.
        /// </summary>
        private string uid;

        /// <summary>
        /// The GroupWise email address including the GWISE: prefix
        /// extracted from the proxy addresses.
        /// </summary>
        private string Address;

        /// <summary>
        /// The common group used for the account. Normally it is "RECIPIENTS",
        /// but some companies use different names, for example "WORKERS".
        /// </summary>
        private string group;

        /// <summary>
        /// Generic Boolean weather the contact is marked or not.
        /// Right now is used only to say whether the free busy message
        /// was found in the public folder, but I wanted to use it for other
        /// things like whether the account changed between to queries to AD.
        /// </summary>
        private bool mark;

        /// <summary>
        /// Constructs a new GroupWise contact.
        /// </summary>
        /// <param name="gwiseUid">The contact's UID</param>
        /// <param name="gwiseAddress">The contact's GWISE address</param>
        /// <param name="commonGroup">The contact's common group</param>
        public GWiseContact(
            string gwiseUid,
            string gwiseAddress,
            string commonGroup)
        {
            uid = gwiseUid;
            Address = gwiseAddress;
            group = commonGroup;
            mark = false;
        }

        /// <summary>
        /// The contact UID.
        /// </summary>
        public string GwiseUid
        {
            get
            {
                return uid;
            }
        }

        /// <summary>
        /// The contact GWISE proxy address inlcuding the GWISE: prefix.
        /// </summary>
        public string GwiseAddress
        {
            get
            {
                return Address;
            }
        }

        /// <summary>
        /// The contact common group from the legacy DN. Usually RECIPIENTS.
        /// </summary>
        public string CommonGroup
        {
            get
            {
                return group;
            }
        }

        /// <summary>
        /// Generic Boolean to mark the contact in certain state.
        /// </summary>
        public bool Marked
        {
            get
            {
                return mark;
            }

            set
            {
                mark = value;
            }
        }
    };

    class FreeBusyBuilder
    {
        private static readonly int LDAP_PAGE_SIZE = 500;
        private static readonly int ONE_MB = 1024 * 1204;

        private static string SLASH_LOWER_CASE_CN = "/cn=";
        private static string SLASH_UPPER_CASE_CN = "/CN=";
        private static string LEGACY_EXCHANGE_DN = "legacyExchangeDN";
        private static string PROXY_ADDRESSES = "proxyAddresses";
        private static string GWISE_COLON = "GWISE:";

        /// <summary>
        /// List of properties we ask to get back from the AD query.
        /// Right now only these two are necessary, but others can be added.
        /// Still one has to be cautious so we don't ask for too much data.
        /// </summary>
        private static readonly string[] PROPERTIES_TO_LOAD = 
            { LEGACY_EXCHANGE_DN, PROXY_ADDRESSES };

        /// <summary>
        /// The free busy messages are keep in a public folder
        /// in the non-ipm. Normally this is above the ipm, but in DAV
        /// the way to address them makes it look like a peer to the
        /// other public folders. The rest of the path, organization and
        /// organizational unit (something like 
        /// o=GooLab/ou=First Administrative Group) is extracted from the
        /// data we get from AD for the contacts.
        /// </summary>
        private static string FREE_BUSY_URL_TEMPLATE =
            "http://localhost/public/NON_IPM_SUBTREE/" +
            "SCHEDULE%2B%20FREE%20BUSY/EX:{0}/";

        /// <summary>
        /// The free busy emails have a subject in the format:
        /// USER-/CN=<common_group_name>/CN=<user_id>
        /// </summary>
        private static readonly string USER_DASH = "USER-";

        // The slashes need to be encoded with _xF8FF_ for Exchange,
        // since they are not meant to be new folder,
        // but rather part of the URL component.
        private static readonly string FREE_BUSY_EML_TEMPLATE =
            "{0}USER-_xF8FF_CN={1}_xF8FF_CN={2}.EML";

        /// <summary>
        /// This is the LDAP query for the contacts imported by the GroupWise
        /// connector. It is trying to get object that are contacts
        /// (objectClass=contact)(objectCategory=person) same as the Exchange
        /// recepient policy builder does, then those that are imported by any
        /// connector, then those that have their legacyExchangeDN set
        /// (I have seem ocasions there was delay), then have their 
        /// targetAddress as SMTP so we don't get Notes connector contacts,
        /// and at the end those that have secondary (hence lower case) gwise
        /// address with their UID.
        /// </summary>
        private static readonly string GWISE_CONTACTS_QUERY =
            "(&(objectClass=contact)(objectCategory=person)(importedFrom=*)" +
            "(legacyExchangeDN=*)(targetAddress=SMTP:*)" +
            "(proxyAddresses=gwise:UID=*))";

        private static readonly string PROPPATCH = "PROPPATCH";
        private static readonly string SEARCH = "SEARCH";
        private static readonly string USER_AGENT =
            "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; SV1; " +
            ".NET CLR 2.0.50727)";
        private static readonly string SLASH_START_SLASH = "*/*";
        private static readonly string TEXT_SLASH_XML = "text/xml";

        private static readonly string COLON_SUBJECT = ":subject";

        /// <summary>
        /// This is the search WebDAV request to find all free busy emails
        /// for GroupWise contacts.
        /// The SQL query does the following:
        /// - it asks for the subject, so I can extract the UID, from something
        ///   easier to parse than the href. Also it must ask for at leats one
        ///   property, or the query fails.
        /// - it looks just in the folder specified by the url, not subfolders.
        /// - it looks for messages, not folders.
        /// - it looks for non-hidden messages.
        /// - it looks for messages having the connector property set
        ///   (see http://support.microsoft.com/kb/928874)
        ///   and set to GWISE address, so we don't get Notes contacts.
        /// Note that the spaces at the end of the strings are significant,
        /// so if you edit the query make sure the spacing is right.
        /// Also note that despite the MSDN page about like GWISE:% must be
        /// in single, not double quotes. This is (indirectly) explained on
        /// another MSDN page about tokens.
        /// </summary>
        private static readonly string SEARCH_REQUEST =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<D:searchrequest xmlns:D = \"DAV:\">" +
            "<D:sql>" +
            "SELECT \"urn:schemas:httpmail:subject\" " +
            "FROM Scope('SHALLOW TRAVERSAL OF \"\"') " +
            "WHERE " +
            "(\"DAV:ishidden\" is Null OR \"DAV:ishidden\" = false) " +
            "AND \"DAV:isfolder\" = false " +
            "AND NOT " +
            "(\"http://schemas.microsoft.com/mapi/proptag/x7AE0001E\" " +
            "is Null) " +
            "AND \"http://schemas.microsoft.com/mapi/proptag/x7AE0001E\" " +
            "LIKE 'GWISE:%' " +
            "</D:sql>" +
            "</D:searchrequest>";

        /// <summary>
        /// This is the proppatch request to create or fix up the free busy
        /// email for a single contact. Since the email has no body,
        /// a proppath request is enough.
        /// It sets the following properties:
        /// - PR_SUBJECT (0x0037001E) to the expected format.
        /// - The connector property (0x7AE0001E) per the KB article above.
        /// - PR_MESSAGE_LOCALE_ID and PR_LOCALE_ID to the hard coded 1033,
        ///   because by default they are 0, which may cause problems.
        ///   Since there is no body, the locale should not matter if valid.
        /// </summary>
        private static readonly string PROPPATCH_REQUEST =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<D:propertyupdate " +
            "xmlns:M=\"http://schemas.microsoft.com/mapi/proptag/\" " +
            "xmlns:D=\"DAV:\">" +
            "<D:set>" +
            "<D:prop>" +
            "<M:x0037001E>USER-/CN={0}/CN={1}</M:x0037001E>" +
            "<M:x7AE0001E>{2}</M:x7AE0001E>" +
            "<M:x3FF10003>1033</M:x3FF10003>" +
            "<M:x66A10003>1033</M:x66A10003>" +
            "</D:prop>" +
            "</D:set>" +
            "</D:propertyupdate>";

        /// <summary>
        /// Returns the string encoded for Exchange, replacing slashes with
        /// _xF8FF_ in addition to the normal URL encoding.
        /// </summary>
        /// <param name="element">The string to encode</param>
        /// <returns></returns>
        private static string ExchangeEncode(string element)
        {
            return HttpUtility.UrlPathEncode(element.Replace(@"/", "_xF8FF_"));
        }

        /// <summary>
        /// Searches AD for the objects matching the given filter.
        /// The root path, user name and password are optional.
        /// If they are given they will be used, otherwise RootDSE
        /// and the current user will be used instead.
        /// </summary>
        /// <param name="searchRootPath">The LDAP root to search,
        /// null will search the default domain</param>
        /// <param name="userName">User to use to make the query,
        /// null will use the default credentials</param>
        /// <param name="password">Password for the user</param>
        /// <param name="filter">The LDAP filter for the query</param>
        /// <param name="propertiesToLoad">The names of the properties
        /// to load from AD.</param>
        /// <returns>The results got from AD</returns>
        private static SearchResultCollection FindGWiseContacts(
            string searchRootPath,
            string userName,
            string password,
            string filter,
            string[] propertiesToLoad)
        {
            DirectoryEntry searchRoot = new DirectoryEntry();
            DirectorySearcher searcher = new DirectorySearcher();

            if (searchRootPath == null)
            {
                searchRoot.Path = searcher.SearchRoot.Path;
            }
            else
            {
                searchRoot.Path = searchRootPath;
            }

            if ((userName != null) && (password != null))
            {
                searchRoot.Username = userName;
                searchRoot.Password = password;
            }

            searcher.SearchRoot = searchRoot;
            searcher.Filter = filter;
            searcher.SearchScope = SearchScope.Subtree;
            searcher.PropertiesToLoad.AddRange(propertiesToLoad);
            searcher.PageSize = LDAP_PAGE_SIZE;

            return searcher.FindAll();
        }

        /// <summary>
        /// Parses the GroupWise UID from legacyExchangeDN AD property.
        /// If the property is different (not legacyExchangeDN) or mallformed
        /// the function will return null.
        /// The string will be returned in upper case.
        /// The UID is assumed to be the last CN component.
        /// For example for 
        /// /o=G..b/ou=F..p/cn=Recipients/cn=d0692608-dea9c581-466bd07a-f0d12967
        /// it will return D0692608-DEA9C581-466BD07A-F0D12967.
        /// </summary>
        /// <param name="propName">The name of the property</param>
        /// <param name="propValue">The value of the property</param>
        /// <returns>The UID or null</returns>
        private static string GetGWiseUidFromLegacyExchangeDN(
            string propName,
            string propValue)
        {
            string gwiseUid = null;

            if (string.Compare(propName, LEGACY_EXCHANGE_DN, true) == 0)
            {
                int lastCN = propValue.LastIndexOf(SLASH_LOWER_CASE_CN);

                if (lastCN != -1)
                {
                    gwiseUid =
                        propValue.Substring(
                            lastCN + SLASH_LOWER_CASE_CN.Length).ToUpper();
                }
            }

            return gwiseUid;
        }

        /// <summary>
        /// Parses the common group from legacyExchangeDN AD property.
        /// If the property is different (not legacyExchangeDN) or mallformed
        /// the function will return null.
        /// The string will be returned in upper case.
        /// The group is assumed to be prelast CN component.
        /// For example for 
        /// /o=G..b/ou=F..p/cn=Recipients/cn=d0692608-dea9c581-466bd07a-f0d12967
        /// it will return RECIPIENTS.
        /// </summary>
        /// <param name="propName">The name of the property</param>
        /// <param name="propValue">The value of the property</param>
        /// <returns>The common group or null</returns>
        private static string GetCommonGroupFromLegacyExchangeDN(
            string propName,
            string propValue)
        {
            string commonGroup = null;

            if (string.Compare(propName, LEGACY_EXCHANGE_DN, true) == 0)
            {
                int lastCN = propValue.LastIndexOf(SLASH_LOWER_CASE_CN);
                int prelastCN = -1;

                if (lastCN != -1)
                {
                    prelastCN =
                        propValue.LastIndexOf(SLASH_LOWER_CASE_CN, lastCN);
                }

                if (prelastCN != -1)
                {
                    commonGroup =
                        propValue.Substring(
                            prelastCN + SLASH_LOWER_CASE_CN.Length,
                            lastCN - prelastCN - SLASH_LOWER_CASE_CN.Length
                        ).ToUpper();
                }
            }

            return commonGroup;
        }

        /// <summary>
        /// Parses the folder name of for the free busy messages from
        /// the legacyExchangeDN AD property and constructs an URL for the
        /// free busy folder.
        /// The returned string will be also Exchange URL encoded.
        /// If the property is different (not legacyExchangeDN) or mallformed
        /// the function will return null.
        /// The group is assumed to be all components prior to the common group.
        /// For example for 
        /// /o=GooLab/ou=First Administrative Group/cn=Recipients/cn=d...7
        /// it will return
        /// http://localhost/public/NON_IPM_SUBTREE/SCHEDULE%2B%20FREE%20BUSY/
        ///  EX:_xF8FF_o=GooLab_xF8FF_ou=First%20Administrative%20Group/
        /// </summary>
        /// <param name="propName">The name of the property</param>
        /// <param name="propValue">The value of the property</param>
        /// <returns>The free busy folder url or null</returns>
        private static string GenerateParentFreeBusyFolderUrl(
            string propName,
            string propValue)
        {
            string freeBusyUrl = null;

            if (string.Compare(propName, LEGACY_EXCHANGE_DN, true) == 0)
            {
                int lastCN = propValue.LastIndexOf(SLASH_LOWER_CASE_CN);
                int prelastCN = -1;

                if (lastCN != -1)
                {
                    prelastCN = propValue.LastIndexOf(
                        SLASH_LOWER_CASE_CN, lastCN);
                }

                if (prelastCN != -1)
                {
                    string encoded =
                        ExchangeEncode(propValue.Substring(0, prelastCN));

                    freeBusyUrl = string.Format(
                                        FREE_BUSY_URL_TEMPLATE, encoded);
                }
            }

            return freeBusyUrl;
        }

        /// <summary>
        /// Parses the GroupWise email address from proxy address.
        /// If the property is different (not legacyExchangeDN) or 
        /// the proxy address is in other formar, or mallformed
        /// the function will return null.
        /// The address return will have the GWISE: prefix.
        /// For example for 
        /// GWISE:user1.postoffice1.domain1
        /// it will return GWISE:user1.postoffice1.domain1.
        /// </summary>
        /// <param name="propName">The name of the property</param>
        /// <param name="propValue">The value of the property</param>
        /// <returns>The GWISE address (including the GWISE:) or null</returns>
        private static string GetGWiseAddressFromProxyAddresses(
            string propName,
            string propValue)
        {
            string gwiseAddress = null;

            if ((string.Compare(propName, PROXY_ADDRESSES, true) == 0) &&
                (string.Compare(propValue, 0, GWISE_COLON, 0, 6)) == 0)
            {
                gwiseAddress = propValue;
            }

            return gwiseAddress;
        }

        /// <summary>
        /// Parses the properties for a contact that came from AD
        /// and return GWiseContact object created from those properties.
        /// If some of the properties are misisng or mallformed
        /// the function will return null.
        /// In addition to parsing the contact the function returns the
        /// URL for the folder that contains the free busy message for
        /// the account, if it wasn't computed yet.
        /// </summary>
        /// <param name="contactProps">The properties of the contact</param>
        /// <param name="freeBusyUrl">If not already computed,
        /// it will be set to the free busy folder URL</param>
        /// <returns>A contact object or null</returns>
        private static GWiseContact ParseGWiseContactsFromADProperties(
            ResultPropertyCollection contactProps,
            ref string freeBusyUrl)
        {
            string gwiseUid = null;
            string gwiseAddress = null;
            string commonGroup = null;
            string freeBusyUrlTemp = null;
            GWiseContact gwiseContact = null;

            foreach (string propName in contactProps.PropertyNames)
            {
                foreach (Object propObject in contactProps[propName])
                {
                    string propValue = propObject.ToString();

                    if ((freeBusyUrl == null) &&
                        (freeBusyUrlTemp == null))
                    {
                        freeBusyUrlTemp =
                            GenerateParentFreeBusyFolderUrl(
                                propName, propValue);
                    }

                    if (gwiseUid == null)
                    {
                        gwiseUid =
                            GetGWiseUidFromLegacyExchangeDN(
                                propName, propValue);
                    }

                    if (commonGroup == null)
                    {
                        commonGroup =
                            GetCommonGroupFromLegacyExchangeDN(
                                propName, propValue);
                    }

                    if (gwiseAddress == null)
                    {
                        gwiseAddress =
                            GetGWiseAddressFromProxyAddresses(
                                propName, propValue);
                    }
                }
            }

            if ((gwiseAddress != null) &&
                (gwiseUid != null) &&
                (commonGroup != null))
            {
                gwiseContact =
                    new GWiseContact(gwiseUid, gwiseAddress, commonGroup);
            }

            if ((freeBusyUrl == null) &&
                (gwiseContact != null) &&
                (freeBusyUrlTemp != null))
            {
                // Return the free busy URL if not set already,
                // but do that only for well formed accounts.
                freeBusyUrl = freeBusyUrlTemp;
            }

            return gwiseContact;
        }

        /// <summary>
        /// Queries AD for all Groupwise connector contacts and returns 
        /// a dictionary of GWiseContacts. The key is the UID of the contact.
        /// If no contacts are found an empty dictionary will be returned, 
        /// no null.
        /// The root path, user name and password are optional.
        /// If they are given they will be used, otherwise RootDSE
        /// and the current user will be used instead.
        /// In addition to all contacts function returns the
        /// URL for the folder that contains the free busy message for
        /// the accounts.
        /// </summary>
        /// <param name="searchRootPath">The LDAP root to search,
        /// null will search the default domain</param>
        /// <param name="userName">User to use to make the query,
        /// null will use the default credentials</param>
        /// <param name="password">Password for the user</param>
        /// <param name="filter">The LDAP filter for the query</param>
        /// <param name="freeBusyUrl">
        /// Will be set to the free busy folder URL</param>
        /// <returns>Dictionary of all contacts keyed by the UID</returns>
        private static Dictionary<string, GWiseContact> GetGWiseContactsFromAD(
            string searchRootPath,
            string userName,
            string password,
            out string freeBusyUrl)
        {
            freeBusyUrl = null;
            Dictionary<string, GWiseContact> gwise_contacts =
                new Dictionary<string, GWiseContact>();

            SearchResultCollection adContacts = FindGWiseContacts(
                searchRootPath,
                userName,
                password,
                GWISE_CONTACTS_QUERY, PROPERTIES_TO_LOAD);

            foreach (SearchResult adContact in adContacts)
            {
                ResultPropertyCollection contactProps = adContact.Properties;
                GWiseContact gwiseContact =
                    ParseGWiseContactsFromADProperties(
                        contactProps, ref freeBusyUrl);

                if (gwiseContact != null)
                {
                    gwise_contacts.Add(
                        gwiseContact.GwiseUid, gwiseContact);
                }
            }

            return gwise_contacts;
        }

        /// <summary>
        /// A small help function to build credentials object from 
        /// given user name and password, if they are given,
        /// or return null if either of them is null.
        /// Note if the password is empty it should be empty string, not null.
        /// </summary>
        /// <param name="userName">User to use to for the credentials,
        /// or null to use the default credentials</param>
        /// <param name="password">Password for the user</param>
        /// <returns>Credentials object or null</returns>
        private static ICredentials BuildCredentialsHelper(
            string userName,
            string password)
        {
            ICredentials credentials = null;

            if ((userName != null) && (password != null))
            {
                credentials = new NetworkCredential(userName, password);
            }

            return credentials;
        }

        /// <summary>
        /// The function creates a free busy email for the given GWise contact
        /// under the public folder specified in free_busy_url.
        /// The credentials are optional.
        /// If they are given they will be used, otherwise
        /// the current user will be used to make the WebDAV call.
        /// </summary>
        /// <param name="userName">User to authenticate as or
        /// null will use the default credentials</param>
        /// <param name="password">Password for the user</param>
        /// <param name="freeBusyUrl">The Url of the free busy folder,
        /// where the free busy email should be created</param>
        /// <param name="gwiseContact">The contact for 
        /// which to create the free busy email</param>
        private static void CreateGWiseFreeBusyEmail(
            ICredentials credentials,
            string freeBusyUrl,
            GWiseContact gwiseContact)
        {
            string gwiseUid = gwiseContact.GwiseUid;
            string gwiseAddress = gwiseContact.GwiseAddress;
            string commonGroup = gwiseContact.CommonGroup;

            string freeBusyMessageUrl =
                string.Format(FREE_BUSY_EML_TEMPLATE,
                    freeBusyUrl, commonGroup, gwiseUid);

            HttpWebRequest request =
                (HttpWebRequest)HttpWebRequest.Create(freeBusyMessageUrl);

            if (credentials != null)
            {
                request.Credentials = credentials;
                request.ConnectionGroupName = "CustomCredentials";
            }
            else
            {
                request.UseDefaultCredentials = true;
                request.ConnectionGroupName = "DefaultNetworkCredentials";
            }
            request.Method = PROPPATCH;
            // This is necesssary to make Windows Auth use keep alive.
            // Due to the large number of connections we may make to Exchange,
            // if we don't do this, the process may exhaust the supply of 
            // available ports.
            // To keep this "safe", requests are isolated by connection pool.
            // See UnsafeAuthenticatedConnectionSharing on MSDN.
            request.UnsafeAuthenticatedConnectionSharing = true;
            request.PreAuthenticate = true;
            request.AllowAutoRedirect = false;
            request.KeepAlive = true;
            request.UserAgent = USER_AGENT;
            request.Accept = SLASH_START_SLASH;
            request.ContentType = TEXT_SLASH_XML;
            request.Headers.Add("Translate", "F");
            request.Headers.Add("Brief", "t");

            string propPatchRequest =
                string.Format(PROPPATCH_REQUEST,
                    commonGroup, gwiseUid, gwiseAddress);

            byte[] encodedBody = Encoding.UTF8.GetBytes(propPatchRequest);
            request.ContentLength = encodedBody.Length;
            Stream requestStream = request.GetRequestStream();
            requestStream.Write(encodedBody, 0, encodedBody.Length);
            requestStream.Close();

            WebResponse response = (HttpWebResponse)request.GetResponse();

            Stream responseStream = response.GetResponseStream();

            responseStream.Close();
            response.Close();
        }

        /// <summary>
        /// The function creates a free busy email for the GWise
        /// contacts that are not marked as having working one.
        /// The emails are created under the public folder 
        /// specified in free_busy_url.
        /// The credentials are optional.
        /// If they are given they will be used, otherwise
        /// the current user will be used to make the WebDAV call.
        /// </summary>
        /// <param name="gwiseContacts">The contacts</param>
        /// <param name="credentials">Optional credentials to use</param>
        /// <param name="freeBusyUrl">The Url of the free busy folder,
        /// where the free busy email should be created</param>
        /// <param name="contactsFixed">Will be set to the number of contacts
        /// with created free busy emails</param>
        /// <param name="contactsSkipped">
        /// Will be set to the number of contacts, which free busy
        /// emails failed do create</param>
        private static void CreateGWiseFreeBusyEmails(
            Dictionary<string, GWiseContact> gwiseContacts,
            ICredentials credentials,
            string freeBusyUrl,
            out int contactsFixed,
            out int contactsSkipped)
        {
            int contactsFixedTemp = 0;
            int contactsSkippedTemp = 0;

            contactsFixed = 0;
            contactsSkipped = 0;

            foreach (KeyValuePair<string, GWiseContact> contactPair in
                gwiseContacts)
            {
                if (!contactPair.Value.Marked)
                {
                    try
                    {
                        CreateGWiseFreeBusyEmail(
                            credentials,
                            freeBusyUrl,
                            contactPair.Value);

                        contactsFixedTemp++;
                    }
                    catch (Exception)
                    {
                        // If one contact fails, don't bail totally.
                        // Instead try to create the others.
                        contactsSkippedTemp++;
                    }
                }
            }

            contactsFixed = contactsFixedTemp;
            contactsSkipped = contactsSkippedTemp;
        }

        /// <summary>
        /// The function queries Exchange for all the free busy emails
        /// of the GroupWise connector contacts.
        /// The returned stream is the response from the server,
        /// a multistatus xml response, containing the URL/href and the subject
        /// of the free busy messages.
        /// The credentials are optional.
        /// If they are given they will be used, otherwise
        /// the current user will be used to make the WebDAV call.
        /// </summary>
        /// <param name="credentials">Optional credentials to use</param>
        /// <param name="freeBusyUrl">The Url of the free busy folder,
        /// where to look for free busy </param>
        /// <returns></returns>
        private static Stream FindGWiseFreeBusyEmails(
            ICredentials credentials,
            string freeBusyUrl)
        {
            HttpWebRequest request =
                (HttpWebRequest)HttpWebRequest.Create(freeBusyUrl);

            if (credentials != null)
            {
                request.Credentials = credentials;
            }
            else
            {
                request.UseDefaultCredentials = true;
            }
            request.Method = SEARCH;
            request.AllowAutoRedirect = false;
            request.KeepAlive = true;
            request.UserAgent = USER_AGENT;
            request.Accept = SLASH_START_SLASH;
            request.ContentType = TEXT_SLASH_XML;
            request.Headers.Add("Translate", "F");

            byte[] encodedBody = Encoding.UTF8.GetBytes(SEARCH_REQUEST);
            request.ContentLength = encodedBody.Length;
            Stream requestStream = request.GetRequestStream();
            requestStream.Write(encodedBody, 0, encodedBody.Length);
            requestStream.Close();

            WebResponse response = (HttpWebResponse)request.GetResponse();
            Stream responseStream = response.GetResponseStream();

            return responseStream;
        }

        /// <summary>
        /// Parses the GroupWise UID from the subject of free busy email.
        /// If the subject is mallformed the function will return null.
        /// The UID is supposed to be in upper case and the function will
        /// return it as is.
        /// The UID is assumed to be the last CN component in the subject.
        /// For example for 
        /// USER-/CN=RECIPIENTS/CN=D3143CCF-E9938337-91AD646-AB45321E
        /// it will return D3143CCF-E9938337-91AD646-AB45321E.
        /// </summary>
        /// <param name="subject">The subject of the free busy message</param>
        /// <returns>The UID or null</returns>
        private static string GetGWiseUidFromFreeBusySubject(
            string subject)
        {
            string gwiseUid = null;
            int compareResult =
                string.Compare(USER_DASH, 0, subject, 0, USER_DASH.Length);

            if (compareResult == 0)
            {
                int lastCN = subject.LastIndexOf(SLASH_UPPER_CASE_CN);

                if (lastCN != -1)
                {
                    gwiseUid =
                        subject.Substring(lastCN + SLASH_UPPER_CASE_CN.Length);

                }
            }

            return gwiseUid;
        }

        /// <summary>
        /// The function gets a stream, which is the result of WebDAV query
        /// for free busy emails. It parses the contacts UIDs from the XML
        /// response and marks the contacts, which have free buys emails.
        /// If the subject is mallformed the function will return null.
        /// I am not perfectly happy about mixing the response and 
        /// the dictionary in a single function. It will be cleaner to parse
        /// the response and have this function work on a list.
        /// the problem with that is the number of additional allocations that
        /// are going to be made, just to be thrown away. So the current design
        /// is a compromise for better performance.
        /// </summary>
        /// <param name="gwiseContacts">Dictionary of contacts</param>
        /// <param name="stream">Http response from Exchange</param>
        private static void MarkWorkingContacts(
            Dictionary<string, GWiseContact> gwiseContacts,
            Stream stream)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.XmlResolver = null;
            settings.IgnoreComments = true;
            settings.IgnoreWhitespace = true;
            settings.ValidationType = ValidationType.None;
            settings.ValidationFlags = XmlSchemaValidationFlags.None;

            XmlReader reader = XmlReader.Create(stream, settings);
            reader.MoveToContent();
            while (reader.Read())
            {
                if ((reader.NodeType == XmlNodeType.Element) &&
                    (reader.Name.IndexOf(COLON_SUBJECT) != -1))
                {
                    string element = reader.ReadElementString();
                    string uid = GetGWiseUidFromFreeBusySubject(element);

                    if (uid != null)
                    {
                        GWiseContact gwiseContact = null;

                        if (gwiseContacts.TryGetValue(uid, out gwiseContact))
                        {
                            gwiseContact.Marked = true;
                        }
                    }

                    reader.ReadEndElement();
                }
            }
        }

        static int Main(string[] args)
        {
            bool verbose = false;
            string freeBusyUrl = null;
            string searchRootPath = null;
            string userName = null;
            string password = null;
            ICredentials credentials =
                BuildCredentialsHelper(userName, password);
            Process currentProcess = Process.GetCurrentProcess();

            currentProcess.PriorityClass = ProcessPriorityClass.BelowNormal;

            if ((args.Length > 0) && ((args[0] == "-v") || (args[0] == "/v")))
            {
                verbose = true;
            }

            Dictionary<string, GWiseContact> gwiseContacts =
                GetGWiseContactsFromAD(
                    searchRootPath,
                    userName,
                    password,
                    out freeBusyUrl);

            int contactsFound = gwiseContacts.Count;
            int contactsFixed = 0;
            int contactsSkipped = 0;

            if (contactsFound > 0)
            {
                Stream stream =
                    FindGWiseFreeBusyEmails(
                        credentials,
                        freeBusyUrl);

                MarkWorkingContacts(gwiseContacts, stream);

                CreateGWiseFreeBusyEmails(
                    gwiseContacts,
                    credentials,
                    freeBusyUrl,
                    out contactsFixed,
                    out contactsSkipped);
            }

            Console.WriteLine(
                "Found {0} GroupWise contacts, " +
                "recreated the free busy email for {1} of them " +
                "and skipped {2} of them due to errors.",
                contactsFound, contactsFixed, contactsSkipped);

            if (verbose)
            {
                Console.WriteLine(
                    "PeakPagedMemorySize = {0}MB\n" +
                    "PeakVirtualMemorySize = {1}MB\n" +
                    "PeakWorkingSet = {2}MB\n" +
                    "Handles = {3}\n" +
                    "UserProcessorTime = {4}\n" +
                    "TotalProcessorTime = {5}",
                    currentProcess.PeakPagedMemorySize64 / ONE_MB,
                    currentProcess.PeakVirtualMemorySize64 / ONE_MB,
                    currentProcess.PeakWorkingSet64 / ONE_MB,
                    currentProcess.HandleCount,
                    currentProcess.UserProcessorTime.ToString(),
                    currentProcess.TotalProcessorTime.ToString());
            }

            Console.WriteLine("Finished successfully.");

            return 0;
        }
    }
}