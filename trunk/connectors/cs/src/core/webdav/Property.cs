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
    /// A WebDAV property
    /// </summary>
    public class Property
    {
        private string name = string.Empty;
        private string nameSpace = string.Empty;

        /// <summary>The name of the property</summary>
        public string Name
        {
            get { return this.name; }
        }

        /// <summary>The namespace of the property</summary>
        public string NameSpace
        {
            get { return this.nameSpace; }
        }

        /// <summary>
        /// Create a new WebDAV property
        /// </summary>
        /// <param name="name">The property name</param>
        /// <param name="nameSpace">The property namespace</param>
        public Property( string name, string nameSpace )
        {
            this.name = name;
            this.nameSpace = nameSpace;
        }
    }
}
