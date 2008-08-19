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
    /// WebDAV properties for Exchange
    /// </summary>
    public class MessageProperty
    {
        /// <summary>Exchange WebDAV Subject property</summary>
        public static readonly Property Subject =
            new Property("subject", "http://schemas.microsoft.com/mapi/");

        /// <summary>Exchange WebDAV subject prefix (RE:/FW:) property</summary>
        public static readonly Property SubjectPrefix =
            new Property("x003D001F", "http://schemas.microsoft.com/mapi/proptag/");

        /// <summary>Exchange WebDAV Conversation Topic</summary>
        public static readonly Property ConversationTopic =
            new Property("x0070001F", "http://schemas.microsoft.com/mapi/proptag/");

        /// <summary>Exchange WebDAV Normalized Subject property (the subject w/o any prefix)</summary>
        public static readonly Property NormalizedSubject =
            new Property("x0E1D001F", "http://schemas.microsoft.com/mapi/proptag/");

    }
}
