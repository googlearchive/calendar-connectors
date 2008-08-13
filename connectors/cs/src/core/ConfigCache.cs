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
using System.Collections.Specialized;
using System.Configuration;
using System.Web.Configuration;
using System.Net;

using TZ4Net;

using Google.GCalExchangeSync.Library.Util;

namespace Google.GCalExchangeSync.Library
{
    /// <summary>
    ///  Configuration options from the .config file
    /// </summary>
    public class ConfigCache
    {
        /// <summary>
        /// Logging for the ConfigCache
        /// </summary>
        protected static readonly log4net.ILog log =
          log4net.LogManager.GetLogger(typeof(ConfigCache));

        private static readonly string DEFAULT_CALENDAR_POST_URL =
            "https://www.google.com/calendar/hosted/{0}/mailslot";

        private static readonly string DEFAULT_CALENDAR_SERVICE_URL =
            "https://www.google.com/calendar/";

        /// <summary>
        /// Encrypt the config file when it is read with the current user credential
        /// </summary>
        public static readonly bool EncryptOnNextRun =
            GetBooleanSetting("Configuration.EncryptOnNextRun", "false");

        /// <summary>
        /// Active Directory Domain Controller
        /// </summary>
        public static readonly string DomainController =
            (ConfigurationManager.AppSettings["ActiveDirectory.DomainController"] ?? "None").ToUpper();

        /// <summary>
        /// Active Directory Domain Username
        /// </summary>
        public static readonly string DomainUserLogin =
            ConfigurationManager.AppSettings["ActiveDirectory.DomainUser.Login"];

        /// <summary>
        /// Active Directory Domain User password
        /// </summary>
        public static readonly string DomainUserPassword =
            ConfigurationManager.AppSettings["ActiveDirectory.DomainUser.Password"];

        /// <summary>
        /// Exchange server to use
        /// </summary>
        public static readonly string ExchangeServerUrl = GetExchangeServerUrl();

        /// <summary>
        /// Exchange server to use for free busy public folders if not the same as
        /// ExchangeServerUrl
        /// </summary>
        public static readonly string ExchangeFreeBusyServerUrl = GetExchangeFreeBusyServerUrl();

        /// <summary>
        /// Username to use to login to Exchange
        /// </summary>
        public static readonly string ExchangeUserLogin =
            ConfigurationManager.AppSettings["Exchange.GCalQueryUser.Login"];

        /// <summary>
        /// Password to use for login to Exchange
        /// </summary>
        public static readonly string ExchangeUserPassword =
            ConfigurationManager.AppSettings["Exchange.GCalQueryUser.Password"];

        /// <summary>
        /// Admin username to use for login to Exchange
        /// </summary>
        public static readonly string ExchangeAdminLogin =
            ConfigurationManager.AppSettings["Exchange.GCalQueryAdmin.Login"];

        /// <summary>
        /// Admin password to use for login to Exchange
        /// </summary>
        public static readonly string ExchangeAdminPassword =
            ConfigurationManager.AppSettings["Exchange.GCalQueryAdmin.Password"];

        /// <summary>
        /// Domain to use to restrict HTTP redirects from Exchange server
        /// </summary>
        public static readonly string ExchangeDefaultDomain =
            ConfigurationManager.AppSettings["Exchange.DefaultDomain"];

        /// <summary>
        /// Allow enable / disable of Exchnage appointment lookup
        /// </summary>
        public static readonly bool EnableAppointmentLookup =
            GetBooleanSetting("Exchange.EnableAppointmentLookup", "true");

        /// <summary>
        /// Maximum # of simultaneious open connections back to exchange
        /// </summary>
        public static readonly int ExchangeMaxConnections =
            GetIntegerSetting("Exchange.MaxConnections", "50");

        /// <summary>
        /// Google Apps domain to use
        /// </summary>
        public static readonly string GoogleAppsDomain = GetCalendarDomain();

        /// <summary>
        /// Admin Login to Google Apps domain
        /// </summary>
        public static readonly string GoogleAppsLogin =
            ConfigurationManager.AppSettings["GoogleApps.AdminUser.Login"];

        /// <summary>
        /// Admin password to Google Apps Domain
        /// </summary>
        public static readonly string GoogleAppsPassword =
            ConfigurationManager.AppSettings["GoogleApps.AdminUser.Password"];

        /// <summary>
        /// Number of days in future + past to synchronize for
        /// </summary>
        public static readonly int GCalSyncWindow =
            GetIntegerSetting("GoogleApps.GCal.SyncWindow", "30", 1, 30);

        /// <summary>
        /// URL of Google Calendar mailslot that Free / Busy lookup results are sent to
        /// </summary>
        public static readonly string GCalPostUrl = GetCalendarPostUrl();

        /// <summary>
        /// URL of Google Calendar to sync with
        /// </summary>
        public static readonly string GCalAddress = GetCalendarServiceUrl();

        /// <summary>
        /// Enable HTTP Compression when syncing with Google Calendar
        /// </summary>
        public static readonly bool EnableHttpCompression =
            GetBooleanSetting("GoogleApps.GCal.EnableHttpCompression", "false");

        /// <summary>
        /// Default SSL setting for Google Calendar
        /// </summary>
        public static readonly bool DefaultGCalSSL =
            GetBooleanSetting("WebService.DefaultGoogleCalendarSSL", "true");

        /// <summary>
        /// Only allow access to the Diagnostic page from localhost
        /// </summary>
        public static readonly bool RequireDiagnosticsLocalAccess =
            GetBooleanSetting("WebService.RequireLocalAccessforDiagnostics", "true");

        /// <summary>
        /// Directory to write Google Calendar tracing to
        /// </summary>
        public static readonly string GCalLogDirectory = GetGCalLogDirectory();

        /// <summary>
        /// Number of minutes between sync of Google Calendar
        /// </summary>
        public static readonly int ServiceRefreshMinutes =
            GetIntegerSetting("SyncService.RefreshTimeInMinutes", "30");

        /// <summary>
        /// Number of times to retry syncing an individual users feed
        /// </summary>
        public static readonly int ServiceErrorCountThreshold =
            GetIntegerSetting("SyncService.ErrorCountThreshold", "15");

        /// <summary>
        /// Directory to store the XML file with User last modified times
        /// </summary>
        public static readonly string ServiceModifiedXmlStorageDirectory =
            ConfigurationManager.AppSettings["SyncService.XmlStorageDirectory"];

        /// <summary>
        /// File name to use for the User last modified times
        /// </summary>
        public static readonly string ServiceModifiedXmlFileName =
            GetServiceModifedXmlFileName();

        /// <summary>
        /// Number of threads to use when syncing with Google Calendar
        /// </summary>
        public static readonly int ServiceThreadCount =
            GetIntegerSetting("SyncService.ThreadCount", "1");

        /// <summary>
        /// LDAP Query to use for the set of users to sync with Google Calendar
        /// </summary>
        public static readonly string ServiceLDAPUserFilter =
            ConfigurationManager.AppSettings["SyncService.LDAPUserFilter"];

        /// <summary>
        /// Type of free / busy writing to use (Appointment or SchedulePlus)
        /// </summary>
        public static readonly string FreeBusyWriter =
            ConfigurationManager.AppSettings["SyncService.FreeBusy.Writer"];

        /// <summary>
        /// Detail Level for Free / Busy information (Full or Basic)
        /// </summary>
        public static readonly GCalProjection FreeBusyDetailLevel =
            string.Compare(
                ConfigurationManager.AppSettings["SyncService.FreeBusy.DetailLevel"] ?? "Full",
                "Basic",
                true) == 0 ?
            GCalProjection.FreeBusy :
            GCalProjection.Full;

        /// <summary>
        /// Allow enable / disable of meeting details synchronization in the appointment writer
        /// </summary>
        public static readonly bool SyncAppointmentDetails =
            GetBooleanSetting("SyncService.SyncAppointmentDetails", "false");

        /// <summary>
        /// Allow enable / disable of Exchange appointment lookup
        /// </summary>
        public static readonly string PlaceHolderMessage =
            ConfigurationManager.AppSettings["SyncService.PlaceHolderMessage"] ?? "GCal Free/Busy Placeholder";

        /// <summary>
        /// Group to use for Exchange public folder subject
        /// Only necessary with SchedulePlus FreeBusyWriter
        /// </summary>
        public static readonly string AdminServerGroup =
            ConfigurationManager.AppSettings["SyncService.FreeBusy.AdminGroup"];

        /// <summary>
        /// Allow keep alive connections when syncing with Google Calendar
        /// </summary>
        public static readonly bool EnableKeepAlive =
            GetBooleanSetting("SyncService.KeepAlive", "true");

        private static Dictionary<string, string> mapToLocalDomain = null;
        private static Dictionary<string, string> mapToExternalDomain = null;

        private static bool GetBooleanSetting(string setting, string defaultValue)
        {
            return (ConfigurationManager.AppSettings[setting] ?? defaultValue).ToUpperInvariant() == "TRUE";
        }

        private static int GetIntegerSetting(string setting, string defaultValue)
        {
            return Convert.ToInt32(ConfigurationManager.AppSettings[setting] ?? defaultValue);
        }

        private static int GetIntegerSetting(string setting, string defaultValue, int min, int max)
        {
            return System.Math.Max(min,
                                   System.Math.Min(max,
                                                   GetIntegerSetting(setting, defaultValue)));
        }

        private static string GetServiceModifedXmlFileName()
        {
            string file = "UserModifiedTimes.xml";
            string root = ServiceModifiedXmlStorageDirectory;

            if ( root == null )
                return null;

            if ( !root.EndsWith( @"\" ) )
            {
                root += @"\";
            }

            return string.Format( "{0}{1}", root, file );
        }

        private static string GetGCalLogDirectory()
        {
            string dir =
                ConfigurationManager.AppSettings["GoogleApps.GCal.LogDirectory"];

            if (!string.IsNullOrEmpty(dir) && !dir.EndsWith(@"\"))
            {
                dir += @"\";
            }

            return dir;
        }

        private static string GetHttpServerUrl(string setting)
        {
            string server = ConfigurationManager.AppSettings[setting];
            if(!string.IsNullOrEmpty(server) &&
                !server.StartsWith("http://") &&
                !server.StartsWith("https://"))
            {
                server = string.Format("http://{0}", server);
            }
            return server;
        }

        private static string GetExchangeServerUrl()
        {
            return GetHttpServerUrl("Exchange.ServerName");
        }

        private static string GetExchangeFreeBusyServerUrl()
        {
            string server = GetHttpServerUrl("Exchange.FreeBusyServerName");
            if(string.IsNullOrEmpty(server))
            {
                server = GetExchangeServerUrl();
            }
            return server;
        }

        private static string GetCalendarPostUrl()
        {
            string setting =
                    ConfigurationManager.AppSettings["GoogleApps.GCal.PostUrl"];
            return string.IsNullOrEmpty(setting) ?
                string.Format(DEFAULT_CALENDAR_POST_URL, GetCalendarDomain()) :
                setting;
        }

        private static string GetCalendarServiceUrl()
        {
            string setting =
                    ConfigurationManager.AppSettings["GoogleApps.GCal.ServiceUrl"];
            return string.IsNullOrEmpty(setting) ?
                DEFAULT_CALENDAR_SERVICE_URL :
                setting;
        }

        private static string GetCalendarDomain()
        {
            return ConfigurationManager.AppSettings["GoogleApps.DomainName"];
        }

        /// <summary>
        /// Load the configuration file and update the ConfigCache
        /// </summary>
        /// <param name="config">Config settings</param>
        public static void LoadConfiguration( Configuration config )
        {
            if (EncryptOnNextRun)
            {
                EncryptAppSettings( config );
            }

            NameValueCollection settings = ConfigurationManager.AppSettings;

            foreach (string key in settings.Keys)
            {
                string value =
                    key.EndsWith("Password") ? "<hidden>" : settings[ key ];

                log.InfoFormat(
                    "Loaded config setting: {0} = {1}", key, value );
            }
        }

        /// <summary>
        /// Encrypt the Config settings for the application
        /// </summary>
        /// <param name="config">The application config settings</param>
        public static void EncryptAppSettings(Configuration config)
        {
            try
            {
                ConfigurationSection section = config.GetSection("appSettings");

                if (section != null)
                {
                    if (!section.IsReadOnly() && !section.SectionInformation.IsProtected)
                    {
                        section.SectionInformation.ProtectSection("RsaProtectedConfigurationProvider");
                        section.SectionInformation.ForceSave = true;
                        config.Save(ConfigurationSaveMode.Full);
                    }
                }
            }
            catch (ConfigurationErrorsException ex)
            {
                log.ErrorFormat("Failed writing encrypted log {0}", ex);
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Failed encrypting log {0}", ex);
            }
        }

        private static void initializeDomainMaps()
        {
            if(mapToLocalDomain != null && mapToExternalDomain != null)
                return;

            mapToLocalDomain = new Dictionary<string, string>();
            mapToExternalDomain = new Dictionary<string, string>();

            string dir = ConfigurationManager.AppSettings["GoogleApps.GCal.DomainMapping"];
            if (!string.IsNullOrEmpty(dir))
            {
                string[] mappings = dir.Split(';');
                foreach (string mapping in mappings)
                {
                    string[] comp = mapping.Split(',');
                    if (comp.Length == 2)
                    {
                        string from = comp[0].Trim();
                        string to = comp[1].Trim();

                        mapToLocalDomain.Add(from, to);
                        mapToExternalDomain.Add(to, from);

                        log.InfoFormat("Added Domain Map. {0} <-> {1}", from, to);
                    }
                    else
                    {
                        log.ErrorFormat("Failed to add domain map [{0}]", mapping);
                    }
                }
            }

            return;
        }

        private static string lookupDomain(string email, Dictionary<string, string> mapping)
        {
            string result = email;
            string[] emailParts = email.Split('@');

            if (emailParts.Length == 2 &&
                mapping.ContainsKey(emailParts[1]))
            {
                log.DebugFormat("Mapping: {0}@{1}", emailParts[0], emailParts[1]);
                result = string.Format("{0}@{1}", emailParts[0],
                                        mapping[emailParts[1]]);
            }
            return result;
        }

        /// <summary>
        /// Add a domain mapping between local domain to the external domain
        /// </summary>
        /// <param name="external">The external (Google Apps) domain</param>
        /// <param name="local">The local (Exchange) domain</param>
        public static void AddDomainMap(string external, string local)
        {
            initializeDomainMaps();
            mapToLocalDomain.Add(external, local);
            mapToExternalDomain.Add(local, external);
        }

        /// <summary>
        /// Map an email address in the external (Google Apps) domain to the local Exchange domain
        /// </summary>
        /// <param name="email">Email address to map</param>
        /// <returns>Email address in the local domain</returns>
        public static string MapToLocalDomain(string email)
        {
            initializeDomainMaps();
            return lookupDomain(email, mapToLocalDomain);
        }

        /// <summary>
        /// Map an email address in the local Exchange domain to the Google Apps Domain
        /// </summary>
        /// <param name="email">Email address to map</param>
        /// <returns>Email address in the external domain</returns>
        public static string MapToExternalDomain(string email)
        {
            initializeDomainMaps();
            return lookupDomain(email, mapToExternalDomain);
        }
    }
}
