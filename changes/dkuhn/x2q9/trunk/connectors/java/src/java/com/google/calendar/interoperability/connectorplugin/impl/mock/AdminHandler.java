/* Copyright (c) 2007 Google Inc.
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

package com.google.calendar.interoperability.connectorplugin.impl.mock;

import com.google.calendar.interoperability.connectorplugin.base.messages.AdminCommand;
import com.google.calendar.interoperability.connectorplugin.base.messages.GetDirectoryResponse;
import com.google.calendar.interoperability.connectorplugin.base.messages.GwResponse;
import com.google.calendar.interoperability.connectorplugin.base.messages.VoidResponse;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.DsUser;
import com.google.common.base.Function;

/**
 * Handles an incoming admin commands
 */
class AdminHandler implements Function<AdminCommand, GwResponse> {
  
  private MockServer server;
  
  public AdminHandler(MockServer server) {
    this.server = server;
  }

  public GwResponse apply(AdminCommand from) {
    
    // Make sure that basic data fields are set
    if (!from.getGetDirectory()) {
      return 
        new VoidResponse(from, "unsupported admin command", "unsupported");
    }
    
    // Case: getDirectory
    final GetDirectoryResponse result = new GetDirectoryResponse(from);
    for(MockDomain domain : server) {
      for (MockUser user: domain) {
        result.addUser(new DsUser(
          domain.getName(), domain.getName(), domain.getName(),
          user.getObjectName(), user.getObjectName(), "Joe"
        ));
      }
    }
    return result;
  }
}
 
