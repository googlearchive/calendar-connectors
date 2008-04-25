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
import com.google.calendar.interoperability.connectorplugin.impl.mock.MockCalendar;
import com.google.calendar.interoperability.connectorplugin.impl.mock.MockUser;

import junit.framework.TestCase;

import java.util.List;

/**
 * Tests for fake calendar objects
 */
public class MockCalendarTest extends TestCase {
  
  private MockCalendar calendar;
  
  @Override
  public void setUp() {
    calendar = new MockCalendar();
  }
  
  public void testBasicSetup() {
    assertNull(calendar.getUser());
    calendar = new MockCalendar(new MockUser("joe"));
    assertEquals("joe", calendar.getUser().getObjectName());
  }
  
  public void testCreateAndGetAndCancel() {
    assertNull(calendar.getAppointment("0"));
    MockAppointment appointment = calendar.createAppointment("0", 1, 2);
    assertEquals(calendar, appointment.getCalendar());
    assertNotNull(calendar.getAppointment("0"));
    appointment.setSubject("subject");
    assertEquals("subject", calendar.getAppointment("0").getSubject());
    calendar.createAppointment("0", 2, 3);
    assertNotSame(appointment, calendar.getAppointment("0"));
    calendar.cancelAppointment("0");
    assertNull(calendar.getAppointment("0"));
  }
  
  private List<MockAppointment> scan(long from, long until, int expectedSize) {
    List<MockAppointment> result =
      (List<MockAppointment>) calendar.scanForAppointments(from, until);
    if (expectedSize != result.size()) {
      fail(String.format("Size mismatch, expected %s, was %s for %s", 
          expectedSize, result.size(), result.toString()));
    }
    return result;
  }
  
  public void testScan() {
    calendar.createAppointment("0", 1, 2);
    calendar.createAppointment("1", 1, 3);
    calendar.createAppointment("2", 6, 7);
    scan(1, 10, 3);
    scan(1, 2, 2);
    scan(1, 1, 2);
    assertEquals("0", scan(1, 1, 2).get(0).getEventId());
    scan(7, 8, 1);
    assertEquals("2", scan(6, 7, 1).get(0).getEventId());
    scan(4, 5, 0);
  }
  
  public void testPrefill() {
    calendar.prefillCalendar("e", 1, 4, 5, 2);
    scan(1, 3, 2);
    MockAppointment appointment = scan(1, 2, 1).get(0);
    assertEquals("e0", appointment.getEventId());
    assertEquals("Subject: e0", appointment.getSubject());
    assertEquals(1, appointment.getStartTimeUtc());
    assertEquals(6, appointment.getEndTimeUtc());
  }

}
 
