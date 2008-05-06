/* Copyright (c) 2008 Google Inc.
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
import com.google.gdata.data.appsforyourdomain.provisioning.UserFeed;

import junit.framework.TestCase;

import java.util.Iterator;

/**
 * Unit tests for the AdminHandler class
 */
public class AdminHandlerTest extends TestCase {
  
  private GDataAccessObject dao;
  private LdapUserFilter filter;
  private AdminHandler handler;
  private boolean filterResult;
  private UserFeed feed;
  
  @Override
  public void setUp() {
    dao = new GDataAccessObject() {
      @Override
      public UserFeed retrieveAllUsers() {
        return feed;
      }
      @Override
      public String getDomain() {
        return "somewhere.org";
      }
    };
    filter = new LdapUserFilter() {
      @Override
      public boolean doFilter(Iterator<String> emails) {
        return filterResult;
      }
    };
    handler = new AdminHandler(dao, filter);
  }
  
  /**
   * Tests for gdata failure: this should throw a runtime exception
   */
  public void testGdataFailure() {
    AdminCommand command = new AdminCommand("", "");
    command.setGetDirectory(true);
    assertTrue(command.getGetDirectory());
    try {
      handler.apply(command);
    } catch(RuntimeException e) {
      
      // Expected
      return;
    }
    fail("Expected a runtime-exception");
  }
  
  /**
   * Tests for ldap failure: this should throw a runtime exception
   */
  public void testLdapFailure() {
    AdminCommand command = new AdminCommand("", "");
    command.setGetDirectory(true);
    assertTrue(command.getGetDirectory());
    feed = new UserFeed();
    try {
      handler.apply(command);
    } catch(RuntimeException e) {
      
      // Expected
      return;
    }
    fail("Expected a runtime-exception");
  }
  
  /** 
   * Tests for the case that everything goes well
   */
  public void testRegularBehavior() {
    AdminCommand command = new AdminCommand("", "");
    command.setGetDirectory(true);
    assertTrue(command.getGetDirectory());
    feed = new UserFeed();
    filterResult = true;
    assertEquals(GetDirectoryResponse.class, handler.apply(command).getClass());
  }

}
 
