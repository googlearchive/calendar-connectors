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

import static com.google.calendar.interoperability.connectorplugin.base.Configurable.Type.integer;
import static com.google.calendar.interoperability.connectorplugin.base.Configurable.Type.string;

import com.google.calendar.interoperability.connectorplugin.base.CommandHandler;
import com.google.calendar.interoperability.connectorplugin.base.Configurable;
import com.google.calendar.interoperability.connectorplugin.base.messages.AdminCommand;
import com.google.calendar.interoperability.connectorplugin.base.messages.SearchCommand;

/**
 * This class sets up a mock infrastructure and augments a command
 * handler with logic to handle Exchange commands on it
 */
public class MockInfrastructure extends Configurable{
  
  private MockServer server;
  
  public MockInfrastructure() {
    super("mock");
    registerParameter("domain", string);
    registerParameter("firstAppointmentFromNowInMillis", integer);
    registerParameter("latestAppointmentFromNowInMillis", integer);
    registerParameter("eventLengthInMillis", integer);
    registerParameter("eventDistanceInMillis", integer);
    
    server = new MockServer();
    server.prefillServer("mockUser", 50, "event", 
        System.currentTimeMillis() + 
          getInteger("firstAppointmentFromNowInMillis"),
        System.currentTimeMillis() + 
          getInteger("latestAppointmentFromNowInMillis"),
        getInteger("eventLengthInMillis").intValue(),
        getInteger("eventDistanceInMillis").intValue(),
        getString("domain"));
  }
  
  public MockInfrastructure(CommandHandler handler) {
    this();
    handler.registerSubhandler(
        AdminCommand.class, new AdminHandler(server));
    handler.registerSubhandler(
        SearchCommand.class, new SearchHandler(server));
  }
}
 
