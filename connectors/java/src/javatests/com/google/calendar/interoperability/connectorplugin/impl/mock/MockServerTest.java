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

import com.google.calendar.interoperability.connectorplugin.impl.mock.MockAppointment;
import com.google.calendar.interoperability.connectorplugin.impl.mock.MockDomain;
import com.google.calendar.interoperability.connectorplugin.impl.mock.MockServer;
import com.google.calendar.interoperability.connectorplugin.impl.mock.MockUser;

import junit.framework.TestCase;

/**
 * Tests for the mock server
 */
public class MockServerTest extends TestCase {
  
  private MockServer server;
  
  @Override
  public void setUp() {
    server = new MockServer();
  }
  
  public void testCreateDomain() {    
    assertNotNull(server.createDomain("1"));
    assertNull(server.createDomain("1"));
    assertNotSame(server.createDomain("2"), server.createDomain("3"));
    int count = 0;
    for (MockDomain domain : server) {
      count++;
    }
    assertEquals(3, count);
  }
  
  public void testGetDomain() {
    assertNull(server.getDomain("1"));
    assertNotNull(server.createDomain("1"));
    assertNotNull(server.getDomain("1"));
  }
  
  public void testPrefillServer() {
    server.prefillServer("user", 5, "event", 0, 9, 1, 2, 
        "google.com", "gmail.com");
    assertNotNull(server.getDomain("google.com"));
    assertNotNull(server.getDomain("gmail.com"));
    assertEquals(5, server.getDomain("google.com").countUsers());
    MockUser user = server.getDomain("google.com").iterator().next();
    int count = 0;
    for (MockAppointment appointment : 
        user.getCalendar().scanForAppointments(0, 10)) {
      assertEquals(2L * count, appointment.getStartTimeUtc());
      count++;
    }
    assertEquals(5, count);
  }
}
 
