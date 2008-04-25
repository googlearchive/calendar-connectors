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
import com.google.calendar.interoperability.connectorplugin.impl.mock.AdminHandler;
import com.google.calendar.interoperability.connectorplugin.impl.mock.MockServer;

import junit.framework.TestCase;

/**
 * Test for the admin-handler
 */
public class AdminHandlerTest extends TestCase {
  
  private MockServer server;
  private AdminHandler handler;
  private AdminCommand cmd;
  
  @Override
  public void setUp() {
    server = new MockServer();
    server.prefillServer("X", 1, "E", 1, 2, 1, 1, "g");
    handler = new AdminHandler(server);
    cmd = new AdminCommand("A", "B");
    cmd.setGetDirectory(true);
  }
  
  public void testBasicFunctionality() {
    GwResponse response = handler.apply(cmd);
    assertTrue(response instanceof GetDirectoryResponse);
    assertEquals(1, ((GetDirectoryResponse) response).countUsers());    
  }
  

}
 
