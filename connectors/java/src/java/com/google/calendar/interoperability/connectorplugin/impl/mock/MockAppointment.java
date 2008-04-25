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

/**
 * A fake appointment on a particular calendar
 */
class MockAppointment implements Comparable<MockAppointment> {
  
  private MockCalendar calendar;
  private String eventId;
  private long startTimeUtc;
  private long endTimeUtc;
  private String subject;
  
  // Visible for unit testing
  MockAppointment(String eventId, long startTimeUtc, long endTimeUtc) {
    Preconditions.checkNotNull(eventId);
    if (endTimeUtc < startTimeUtc) {
      throw new IllegalArgumentException("startTime > endTime");
    }
    this.eventId = eventId;
    this.startTimeUtc = startTimeUtc;
    this.endTimeUtc = endTimeUtc;
  }
  
  public MockAppointment(
      MockCalendar calendar, String eventId, 
      long startTimeUtc, long endTimeUtc) {
    this(eventId, startTimeUtc, endTimeUtc);
    Preconditions.checkNotNull(calendar);
    this.calendar = calendar;
  }

  public int compareTo(MockAppointment o) {
    if (o.startTimeUtc != startTimeUtc) {
      return (int) Math.signum(startTimeUtc - o.startTimeUtc);
    }
    if (o.endTimeUtc != endTimeUtc) {
      return (int) Math.signum(endTimeUtc - o.endTimeUtc);
    }
    return eventId.compareTo(o.eventId);
  }
  
  public boolean contains(long timeUtc) {
    return startTimeUtc <= timeUtc && endTimeUtc >= timeUtc;
  }
  
  public boolean overlaps(long fromUtc, long untilUtc) {
    if (untilUtc < fromUtc) {
      throw new IllegalArgumentException("fromUtc > untilUtc");
    }
    return !(untilUtc < startTimeUtc || fromUtc > endTimeUtc);
  }
  
  public String getSubject() {
    return subject;
  }

  public void setSubject(String subject) {
    this.subject = subject;
  }

  public MockCalendar getCalendar() {
    return calendar;
  }

  public String getEventId() {
    return eventId;
  }

  public long getStartTimeUtc() {
    return startTimeUtc;
  }

  public long getEndTimeUtc() {
    return endTimeUtc;
  }

  @Override
  public String toString() {
    return getEventId();
  }
}
 
