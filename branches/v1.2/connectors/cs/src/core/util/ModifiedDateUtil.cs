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
using System.IO;

namespace Google.GCalExchangeSync.Library.Util
{
    /// <summary>
    /// Storage of per user last modified times for Google calendar sync
    /// </summary>
    public class ModifiedDateUtil
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger( typeof( ModifiedDateUtil ) );

        private string modifiedXMLFileName = string.Empty;
        private Dictionary<string, DateTime> userModifiedTimes;

        /// <summary>
        /// The filename the modified times will be stored in
        /// </summary>
        public string ModifiedXMLFileName
        {
            get { return modifiedXMLFileName; }
        }

        /// <summary>
        /// Ctor for persisting user feed modification times
        /// </summary>
        /// <param name="modifiedXMLFileName">Filename to persist valus to / from</param>
        public ModifiedDateUtil(string modifiedXMLFileName)
        {
            this.modifiedXMLFileName = modifiedXMLFileName;
            userModifiedTimes = new Dictionary<string,DateTime>();

            if (modifiedXMLFileName == null)
                LoadModifiedTimes();
        }

        /// <summary>
        /// Updates the last modified time stamp for login provided to the time provided.
        /// </summary>
        /// <param name="login">Login timestamp to change</param>
        /// <param name="timeStamp">Value to set for last modified timestamp </param>
        public void UpdateUserModifiedTime( string login, DateTime timeStamp )
        {
            if (userModifiedTimes.ContainsKey(login))
            {
                userModifiedTimes[login] = timeStamp;
            }
            else
            {
                userModifiedTimes.Add( login, timeStamp );
            }
        }

        /// <summary>
        /// Gets the last modified timestamp for the user, returns DateTime.MinValue if none available
        /// </summary>
        /// <param name="login">Login to lookup last modified timestamp for</param>
        /// <returns>The last modified time for the user</returns>
        public DateTime GetModifiedDateForUser( string login )
        {
            if (userModifiedTimes.ContainsKey(login))
            {
                return userModifiedTimes[login];
            }

            return DateTime.MinValue;
        }

        /// <summary>
        /// Loads the last modified timestamps for the current domain users from the persisted XML store
        /// </summary>
        public void LoadModifiedTimes()
        {
            try
            {
                if ( File.Exists( modifiedXMLFileName ) )
                {
                    using ( Stream str = File.Open( modifiedXMLFileName, FileMode.Open ) )
                    using ( XmlReader reader = XmlReader.Create( str ) )
                    {

                        reader.Read();
                        reader.ReadStartElement( "lastModified" );

                        while ( reader.NodeType != XmlNodeType.EndElement )
                        {
                            if (reader.IsStartElement("entry"))
                            {
                                reader.ReadStartElement("entry");

                                string key = reader.ReadElementString("user");
                                DateTime value = DateTime.Parse(reader.ReadElementString("modTime"));
                                reader.ReadEndElement();
                                reader.MoveToContent();
                                userModifiedTimes.Add(key, value);
                                log.DebugFormat("User Modified - {0} / {1}", key, value);
                            }
                            else break;
                        }

                        reader.ReadEndElement();
                        str.Close();
                    }
                }
            }
            catch ( Exception ex )
            {
                // If the file can't be read, just move on
                log.Error(ex);
            }
        }

        /// <summary>
        /// Persist the last modified timestamps out to disk
        /// </summary>
        public void PersistModifiedTimes()
        {
            if ( modifiedXMLFileName != null )
            {
                try
                {
                    using (Stream str = File.Open(modifiedXMLFileName, FileMode.Create))
                    using (XmlWriter writer = XmlWriter.Create(str))
                    {
                        writer.WriteStartElement("lastModified");

                        foreach(KeyValuePair<string, DateTime> pair in userModifiedTimes)
                        {
                            string login = pair.Key;
                            DateTime value = pair.Value;

                            writer.WriteStartElement("entry");
                            writer.WriteElementString("user", login);
                            writer.WriteElementString("modTime", value.ToString());
                            writer.WriteEndElement();
                        }

                        writer.WriteEndElement();
                    }
                }
                catch ( Exception ex )
                {
                    throw new GCalExchangeException(
                        GCalExchangeErrorCode.GenericError,
                        "Error persisting modified times", ex );
                }
            }
        }
    }
}
