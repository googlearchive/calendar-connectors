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

import junit.framework.TestCase;

/**
 * Unit tests for the MockAppointment class
 */
public class MockAppointmentTest extends TestCase {
  
  private MockAppointment appointment;
  
  public void testBasicSetup() {
    appointment = new MockAppointment("id", 0, 1);
    appointment.setSubject("s");
    assertEquals("id", appointment.getEventId());
    assertEquals(0, appointment.getStartTimeUtc());
    assertEquals(1, appointment.getEndTimeUtc());
    assertEquals("s", appointment.getSubject());
  }
  
  private void assertCompares(
      int expected, 
      String id1, long start1, long end1,
      String id2, long start2, long end2
  ) {
    MockAppointment appointment1 = new MockAppointment(id1, start1, end1);
    MockAppointment appointment2 = new MockAppointment(id2, start2, end2);
    assertEquals(expected, appointment1.compareTo(appointment2));
  }
  
  public void testCompareTo() {
    assertCompares(0, "a", 1, 2, "a", 1, 2);
    assertCompares(-1, "a", 1, 2, "b", 1, 2);
    assertCompares(1, "b", 1, 2, "a", 1, 2);
    assertCompares(1, "a", 2, 2, "a", 1, 2);
    assertCompares(-1, "a", 1, 2, "a", 2, 2);
    assertCompares(-1, "a", 1, 1, "a", 1, 2);
    assertCompares(1, "a", 1, 2, "a", 1, 1);
    assertCompares(-1, "a", 1, 2, "a", 3, 4);
    assertCompares(1, "a", 3, 4, "a", 1, 2);
  }
  
  public void testContainsMethods() {
    appointment = new MockAppointment("id", 0, 10);
    assertFalse(appointment.contains(-1));
    assertTrue(appointment.contains(0));
    assertTrue(appointment.contains(5));
    assertTrue(appointment.contains(10));
    assertFalse(appointment.contains(11));
  }
  
  public void testOverlapsMethod() {
    appointment = new MockAppointment("id", 0, 10);
    assertFalse(appointment.overlaps(-2, -1));
    assertFalse(appointment.overlaps(11, 12));
    assertTrue(appointment.overlaps(-2, 0));
    assertTrue(appointment.overlaps(1, 2));
    assertTrue(appointment.overlaps(10, 11));
  }

}
 
