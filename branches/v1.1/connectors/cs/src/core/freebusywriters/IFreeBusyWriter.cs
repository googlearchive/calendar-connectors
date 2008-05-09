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

using log4net;
using Google.GData.Calendar;
using Google.GCalExchangeSync.Library.Util;

namespace Google.GCalExchangeSync.Library
{
    /// <summary>
    /// Interface for a free busy writer to synchronizer events with Exchange
    /// </summary>
    public interface IFreeBusyWriter
    {
        /// <summary>
        /// Sync a users events with Exchange
        /// </summary>
        /// <param name="user">The user to synchronize</param>
        /// <param name="googleAppsFeed">The most recent events from Google Apps</param>
        /// <param name="exchangeGateway">An Exchange Gateway to make changes</param>
        /// <param name="window">DateTime window to synchronize events for</param>
        void SyncUser(
            ExchangeUser user, 
            EventFeed googleAppsFeed, 
            ExchangeService exchangeGateway, 
            DateTimeRange window);

        /// <summary>
        /// Initialize the free busy gateway
        /// </summary>
        /// <param name="exchangeGateway">The free busy gateway to use</param>
        void Initialize(ExchangeService exchangeGateway);
    }
}
