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

import com.google.calendar.interoperability.connectorplugin.base.LdapUserFilter;
import com.google.calendar.interoperability.connectorplugin.base.messages.AdminCommand;
import com.google.calendar.interoperability.connectorplugin.base.messages.GetDirectoryResponse;
import com.google.calendar.interoperability.connectorplugin.base.messages.GwResponse;
import com.google.calendar.interoperability.connectorplugin.base.messages.VoidResponse;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.DsUser;
import com.google.common.base.Function;
import com.google.gdata.data.appsforyourdomain.provisioning.UserEntry;
import com.google.gdata.data.appsforyourdomain.provisioning.UserFeed;

import java.util.HashMap;
import java.util.Map;
import java.util.logging.Level;
import java.util.logging.Logger;

/**
 * Handles incoming admin commands
 */
class AdminHandler implements Function<AdminCommand, GwResponse> {
  
  private static final Logger LOGGER = 
    Logger.getLogger(AdminHandler.class.getName());
  
  private GDataAccessObject dao;
  private LdapUserFilter filter = new LdapUserFilter();
  
  public AdminHandler(GDataAccessObject dataAccess) {
    this.dao = dataAccess;
    this.filter = new LdapUserFilter();
  }

  public GwResponse apply(AdminCommand from) {
    // Make sure that basic data fields are set
    if (!from.getGetDirectory()) {
      return 
        new VoidResponse(from, "unsupported admin command", "unsupported");
    }
    
    // Fetch the user list from GData
    LOGGER.log(Level.INFO, "Executing Directory sync");
    LOGGER.log(Level.FINE, "Performing GDATA query");
    final UserFeed feed = dao.retrieveAllUsers();
    final String domain = dao.getDomain();
    Map<String, DsUser> users = new HashMap<String, DsUser>();
    for (UserEntry user : feed.getEntries()) {
      
      final String username = user.getLogin().getUserName();
      final String email = username + "@" + domain;
      users.put(email, new DsUser(
          email, domain, "", email, 
          user.getName().getFamilyName(), user.getName().getGivenName()));      
    }
    
    // Now, filter out any "bad" users
    LOGGER.log(Level.FINE, "Filtering user list");
    if (!filter.doFilter(users.keySet().iterator())) {
      LOGGER.log
        (Level.WARNING, "Could not perform filter operation, aborting sync");
      return new VoidResponse(from, "LDAP filtering failed", "ERROR");
    }
    
    // Return the result
    final GetDirectoryResponse result = new GetDirectoryResponse(from);
    for (DsUser user : users.values()) {
      result.addUser(user);
    }    
    return result;
  }

}
 
