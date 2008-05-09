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
using System.Collections;
using System.Collections.Generic;
using System.DirectoryServices;

using Google.GCalExchangeSync.Library.Util;
using TZ4Net;

namespace Google.GCalExchangeSync.Library
{
    /// <summary>
    /// Wrapper to map a dictionary from email address to ExchangeUser
    /// </summary>
    public class ExchangeUserDict : Dictionary<string, ExchangeUser>
    {
        /// <summary>
        /// Add a user to the dictionary
        /// </summary>
        /// <param name="email">Email address of the user</param>
        /// <param name="user">ExchangeUser object for the user</param>
        public void AddExchangeUser(string email, ExchangeUser user)
        {
            this.Add(email.ToLower(), user);
        }
        
        /// <summary>
        /// Determine if an exchange user is available for the email address 
        /// </summary>
        /// <param name="email">The email address to find</param>
        /// <returns>True if there is an Exchange user for the email address</returns>
        public bool Contains(string email)
        {
            return this.ContainsKey(email.ToLower());
        }
        
        /// <summary>
        /// Find an exchange user by email address
        /// </summary>
        /// <param name="email">The email address to find</param>
        /// <returns>The ExchangeUser for the email address</returns>
        public ExchangeUser FindUser(string email)
        {
            return this[email.ToLower()];
        }
    }
}
