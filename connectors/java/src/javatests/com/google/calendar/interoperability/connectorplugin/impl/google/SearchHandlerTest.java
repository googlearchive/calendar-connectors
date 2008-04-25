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

import com.google.gdata.data.DateTime;
import static com.google.gdata.data.DateTime.parseDateTimeChoice;
import com.google.gdata.data.extensions.When;

import static com.google.calendar.interoperability.connectorplugin.impl.google.SearchHandler.renderFrom;
import static com.google.calendar.interoperability.connectorplugin.impl.google.SearchHandler.renderUntil;

import junit.framework.TestCase;

import java.util.Calendar;
import java.util.Date;
import java.util.GregorianCalendar;

/**
 * Unit tests for certain aspects of the SearchHandler
 */
public class SearchHandlerTest extends TestCase {
  
  private void assertTimesMatch(long original, long toCheck) {
    if (original == toCheck) {
      return;
    }
    fail(String.format(
        "times do not match, differ by %s seconds. " +
        "checked time was %s",
        (toCheck - original) / 1000, new Date(toCheck)));
  }
  
  private static long getMidnight(int year, int month, int day) {
    GregorianCalendar cal = new GregorianCalendar(year, month - 1, day, 0, 0, 0);
    cal.set(Calendar.MILLISECOND, 0);
    return cal.getTimeInMillis();
  }
  
  public void testFromRegular() {
    When when = new When();
    DateTime from = parseDateTimeChoice("2005-06-06T17:00:00-08:00");
    assertFalse(from.isDateOnly());
    when.setStartTime(from);
    assertTimesMatch(when.getStartTime().getValue(), renderFrom(when));
  }

  public void testFromAllDay() {
    When when = new When();
    DateTime from = parseDateTimeChoice("2005-06-06");
    assertTrue(from.isDateOnly());
    when.setStartTime(from);
    assertTimesMatch(getMidnight(2005, 6, 6), renderFrom(when));
  }
  
  public void testUntilRegular() {
    When when = new When();
    DateTime until = parseDateTimeChoice("2005-06-06T17:00:00-08:00");
    assertFalse(until.isDateOnly());
    when.setEndTime(until);
    assertTimesMatch(when.getEndTime().getValue(), renderUntil(when));
  }
  
  public void testUntilOnedayMeetingWithoutSecondTime() {
    When when = new When();
    DateTime from = parseDateTimeChoice("2005-06-06");
    assertTrue(from.isDateOnly());
    when.setStartTime(from);
    assertTimesMatch(getMidnight(2005, 6, 7) - 1, renderUntil(when));
  }
  
  public void testUntilOnedayMeetingWithSecondTime() {
    When when = new When();
    DateTime from = parseDateTimeChoice("2005-06-06");
    DateTime until = parseDateTimeChoice("2005-06-07");
    assertTrue(from.isDateOnly());
    assertTrue(until.isDateOnly());
    when.setStartTime(from);
    when.setEndTime(until);
    assertTimesMatch(getMidnight(2005, 6, 7) - 1, renderUntil(when));
  }
  
  public void testUntilTwodayMeeting() {
    When when = new When();
    DateTime from = parseDateTimeChoice("2005-06-06");
    DateTime until = parseDateTimeChoice("2005-06-08");
    assertTrue(from.isDateOnly());
    assertTrue(until.isDateOnly());
    when.setStartTime(from);
    when.setEndTime(until);
    assertTimesMatch(getMidnight(2005, 6, 8) - 1, renderUntil(when));    
  }
  
}
 
