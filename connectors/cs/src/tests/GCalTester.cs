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

using Google.GData.Client;
using Google.GData.Calendar;
using Google.GCalExchangeSync.Library.Util;

using Google.GCalExchangeSync.Library;

namespace Google.GCalExchangeSync.Tests.Diagnostics
{
    public class GCalTester
    {
        public static EventFeed QueryGCalFreeBusy( string gcalUserEmail )
        {
            GCalGateway gw =
                new GCalGateway(
                    ConfigCache.GoogleAppsLogin,
                    ConfigCache.GoogleAppsPassword,
                    ConfigCache.GoogleAppsDomain );

            DateTime start = DateTime.Now.AddDays(-7);
            DateTime end = DateTime.Now.AddDays(+7);
            DateTimeRange range = new DateTimeRange(start, end);

            EventFeed feed = gw.QueryGCal(gcalUserEmail,
                                          GCalVisibility.Private,
                                          GCalProjection.FreeBusy,
                                          false,
                                          DateTime.MinValue,
                                          range);
            return feed;
        }
    }
}
