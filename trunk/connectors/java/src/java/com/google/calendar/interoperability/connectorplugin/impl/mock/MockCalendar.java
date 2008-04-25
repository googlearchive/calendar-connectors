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

import com.google.common.base.Preconditions;

import java.util.HashMap;
import java.util.LinkedList;
import java.util.List;
import java.util.Map;
import java.util.SortedSet;
import java.util.TreeSet;

/**
 * A mock calendar containing mock appointments
 */
class MockCalendar {
  
  private MockUser user;
  private Map<String, MockAppointment> allAppointments;
  private SortedSet<MockAppointment> sortedAppointments;
  
  // Visible for unit testing
  MockCalendar() {
    allAppointments = new HashMap<String, MockAppointment>();
    sortedAppointments = new TreeSet<MockAppointment>();
  }
  
  public MockCalendar(MockUser user) {
    this();
    Preconditions.checkNotNull(user);
    this.user = user;
  }

  public MockUser getUser() {
    return user;
  }
  
  /**
   * Creates an appointment that either adds to the calendar or replaces
   * an existing appointment. This method accepts all parameters required
   * to uniquely identify and sort the event, but nothing more. All other
   * parameters need to be set through mutators in the MockAppointment object.
   */
  public synchronized MockAppointment createAppointment(
      String eventId, long startTimeUtc, long endTimeUtc) {
    MockAppointment result = 
        new MockAppointment(this, eventId, startTimeUtc, endTimeUtc);
    cancelAppointment(eventId);
    sortedAppointments.add(result);
    allAppointments.put(eventId, result);
    return result;
  }
  
  /**
   * returns an appointment with a certain event-id, null if the appointment
   * does not exist
   */
  public synchronized MockAppointment getAppointment(String eventId) {
    return allAppointments.get(eventId);
  }
  
  /** 
   * Returns an iterable of appointments, sorted by start time, that overlap
   * with a given time range
   */
  public synchronized Iterable<MockAppointment> 
      scanForAppointments(long fromUtc, long endUtc) {
    List<MockAppointment> result = new LinkedList<MockAppointment>();
    for (MockAppointment appointment : sortedAppointments) {
      if (appointment.overlaps(fromUtc, endUtc)) {
        result.add(appointment);
      }
    }
    return result;
  }
  
  /**
   * Removes an appointment from this calendar
   */
  public synchronized void cancelAppointment(String eventId) {
    if (allAppointments.containsKey(eventId)) {
      sortedAppointments.remove(allAppointments.remove(eventId));
    }    
  }

  /**
   * Prefills the calendar with a bunch of generic events
   * @param prefix the prefix for the id and subject
   * @param fromUtc start time in Utc
   * @param untilUtc time to stop filling elements in for
   * @param eventLength the duration of each event
   * @param distance the increment of the start times
   */
  public void prefillCalendar (
      String prefix, long fromUtc, long untilUtc, 
      int eventLength, int distance) {
    int i = 0;
    for (
        long currentTime = fromUtc; 
        currentTime <= untilUtc; 
        currentTime += distance) {
      MockAppointment currentAppointment = createAppointment(
          prefix + i, currentTime, currentTime + eventLength);
      currentAppointment.setSubject(
          "Subject: " + currentAppointment.getEventId());
      i++;
    }
  }
}
 
