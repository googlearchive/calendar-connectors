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

import com.google.calendar.interoperability.connectorplugin.base.BasicSearchHandler;
import com.google.calendar.interoperability.connectorplugin.base.messages.FreeBusyResponse;
import com.google.calendar.interoperability.connectorplugin.base.messages.GwResponse;
import com.google.calendar.interoperability.connectorplugin.base.messages.SearchCommand;
import com.google.calendar.interoperability.connectorplugin.base.messages.VoidResponse;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.Address;

import java.util.Date;
import java.util.Iterator;
import java.util.logging.Level;

/**
 * Handles a Search request in the mock infrastructure 
 */
class SearchHandler extends BasicSearchHandler {
  
  private MockServer server;
  
  public SearchHandler(MockServer server) {
    this.server = server;
  }
  
  @Override
  protected GwResponse handleSearch(
      SearchCommand searchCommand, 
      Address fromAddress,
      Address toAddress) {
    
    // Look up the user
    String domainName = toAddress.getWPD();
    String userName = toAddress.getWPU();
    if (userName.indexOf('.') >= 0) {
      final String wpu = toAddress.getWPU();
      
      final int split = wpu.lastIndexOf('.');
      if (split < 0) {
        logger.log(Level.INFO, "wpu could not be split. It was " + wpu);
        return VoidResponse.invalid(searchCommand);
      }
      domainName = wpu.substring(0, split);
      userName = wpu.substring(split + 1);      
    }
    logger.log(Level.INFO, "F/B for " + userName + "@" + domainName + 
        " from " + new Date(searchCommand.getBeginTime().getTimeInUtc()) +
        " until " + new Date(searchCommand.getEndTime().getTimeInUtc()));
    if (server.getDomain(domainName) == null) {
      logger.log(Level.INFO, "servers domain name was null");
      return VoidResponse.invalid(searchCommand);
    }
    final MockUser user = server.getDomain(domainName).getUser(userName);
    if (user == null) {
      logger.log(Level.INFO, "user is null");
      return VoidResponse.invalid(searchCommand);
    }
    
    // Create the basic response object
    FreeBusyResponse response = new FreeBusyResponse(searchCommand);
    for (Iterator<MockAppointment> it = 
      user.getCalendar().scanForAppointments(
        searchCommand.getBeginTime().getTimeInUtc(), 
        searchCommand.getEndTime().getTimeInUtc()).iterator(); 
       it.hasNext();) {
      MockAppointment apt = it.next();
      response.addTimeslot(apt.getStartTimeUtc(), apt.getEndTimeUtc());
    }
    return response;
  }
}
 
