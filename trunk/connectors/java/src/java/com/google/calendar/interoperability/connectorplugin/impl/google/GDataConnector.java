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

package com.google.calendar.interoperability.connectorplugin.impl.google;

import com.google.calendar.interoperability.connectorplugin.base.CommandHandler;
import com.google.calendar.interoperability.connectorplugin.base.SelfTestable;
import com.google.calendar.interoperability.connectorplugin.base.messages.AdminCommand;
import com.google.calendar.interoperability.connectorplugin.base.messages.SearchCommand;

/**
 * This class sets up a connection to google apps for your domain
 * feeds
 */
public class GDataConnector implements SelfTestable {
  
  private GDataAccessObject dao;
  
  public GDataConnector(CommandHandler handler) {
    dao = new GDataAccessObject();
    handler.registerSubhandler(AdminCommand.class, new AdminHandler(dao));
    handler.registerSubhandler(
        SearchCommand.class, new SearchHandler(dao));
  }
  
  /**
   * Performs a self-check to make sure the connector is configured
   * correctly. Throw an exception or error if the evaluation fails
   */
  public void selfTest() {
    System.out.println("Performing a test user query for domain " + 
        dao.getDomain());
    if (dao.retrieveAllUsers() == null) {
      throw new RuntimeException("Could not retrieve user feed");
    }
  }
}
 
