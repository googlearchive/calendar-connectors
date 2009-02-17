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

namespace Google.GCalExchangeSync.Library.WebDav
{
    /// <summary>
    /// HTTP Methods
    /// </summary>
    public class Method
    {
        /// <summary>HTTP GET</summary>
        public static Method GET = new Method("GET");
        /// <summary>HTTP PUT</summary>
        public static Method PUT = new Method("PUT");
        /// <summary>HTTP POST</summary>
        public static Method POST = new Method("PUT");
        /// <summary>HTTP DELETE</summary>
        public static Method DELETE = new Method("DELETE");
        /// <summary>HTTP HEAD</summary>
        public static Method HEAD = new Method("HEAD");

        /// <summary>WebDAV BCOPY</summary>
        public static Method BCOPY = new Method("BCOPY");
        /// <summary>WebDAV BDELETE</summary>
        public static Method BDELETE = new Method("BDELETE");
        /// <summary>WebDAV RMOVE</summary>
        public static Method BMOVE = new Method("BMOVE");
        /// <summary>WebDAV BPROPFIND</summary>
        public static Method BPROPFIND = new Method("BPROPFIND");
        /// <summary>WebDAV BPROPPATCH</summary>
        public static Method BPROPPATCH = new Method("BPROPPATCH");
        /// <summary>WebDAV COPY</summary>
        public static Method COPY = new Method("COPY");
        /// <summary>WebDAV LOCK</summary>
        public static Method LOCK = new Method("LOCK");
        /// <summary>WebDAV MKCOL</summary>
        public static Method MKCOL = new Method("MKCOL");
        /// <summary>WebDAV MOVE</summary>
        public static Method MOVE = new Method("MOVE");
        /// <summary>WebDAV NOTIFY</summary>
        public static Method NOTIFY = new Method("NOTIFY");
        /// <summary>WebDAV POLL</summary>
        public static Method POLL = new Method("POLL");
        /// <summary>WebDAV PROPFIND</summary>
        public static Method PROPFIND = new Method("PROPFIND");
        /// <summary>WebDAV PROPPATCH</summary>
        public static Method PROPPATCH = new Method("PROPPATCH");
        /// <summary>WebDAV SEARCH</summary>
        public static Method SEARCH = new Method("SEARCH");
        /// <summary>WebDAV SUBSCRIBE</summary>
        public static Method SUBSCRIBE = new Method("SUBSCRIBE");
        /// <summary>WebDAV UNLOCK</summary>
        public static Method UNLOCK = new Method("UNLOCK");
        /// <summary>WebDAV UNSUBSCRIBE</summary>
        public static Method UNSUBSCRIBE = new Method("UNSUBSCRIBE");
        /// <summary>WebDAV X_MS_ENUMATTS</summary>
        public static Method X_MS_ENUMATTS = new Method("X-MS-ENUMATTS");

        private string _name = string.Empty;

        /// <summary>
        /// The name of the method
        /// </summary>
        public string Name
        {
            get { return this._name; }
        }

        /// <summary>
        /// Ctor for a WebDAV method
        /// </summary>
        /// <param name="name"></param>
        public Method(string name)
        {
            this._name = name;
        }
    }
}
