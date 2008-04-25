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

package com.google.calendar.interoperability.connectorplugin.base.messages;

import com.google.calendar.interoperability.connectorplugin.base.messages.util.DsUser;
import com.google.common.base.Preconditions;

import java.util.ArrayList;
import java.util.List;

/**
 * A response that can be used for processing a
 * GetDirectory request
 */
public final class GetDirectoryResponse extends TemplateResponse {
  
  private static final String USER_TEMPLATE = 
    "DS-USER=  \r\n"
    + "    Operation= List;  \r\n"  
    + "    Domain= ${getDomain};  \r\n"
    + "    Post-Office= ${getPostOffice};  \r\n"
    + "    Object= ${getObjectName};  \r\n"
    + "    Visibility= System;  \r\n"
    + "    Last-Name= ${getLastName};  \r\n"
    + "    First-Name= ${getFirstName};  \r\n"
    + "    Network-ID= ${getNetworkId};  \r\n"
    + ";  \r\n";
  
  private List<DsUser> users = new ArrayList<DsUser>();

  public GetDirectoryResponse(AdminCommand originalCommand) {
    super(originalCommand, 
        "WPC-API= 1.2;  \r\n" 
        + "Header-Char= T50;  \r\n"
        + "MSG-TYPE= ADMIN;  \r\n"
        + "${writeUsers}"
        + "-END-\r\n");
  }
  
  public String writeUsers() {
    StringBuilder result = new StringBuilder();
    for (DsUser user : users) {
      result.append(replace(USER_TEMPLATE, user));
    }
    return result.toString();
  }
  
  public void addUser(DsUser user) {
    Preconditions.checkNotNull(user);
    users.add(user);
  }
  
  public int countUsers() {
    return users.size();
  }
}
 
