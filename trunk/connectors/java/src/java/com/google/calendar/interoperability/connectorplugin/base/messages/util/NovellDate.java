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

package com.google.calendar.interoperability.connectorplugin.base.messages.util;


import java.text.SimpleDateFormat;
import java.util.Calendar;
import java.util.GregorianCalendar;

/**
 * Date object as defined by novell
 * 
 * Docs from novel:
 *   25/5/94 11:05 +11:00
 *   A four-digit year is optional, such as 1994. If you use a two-digit year,
 *   the range 70-99 assumes 1970-1999, while years in the 00-69 range assume 
 *   2000-2069. 
 *
 *   DD/MM/YY[YY] HH:MM[:SS] [+/-HH[:MM]] where items in square brackets, 
 *   such as [:SS], indicate optional elements. If you use GMT offset, notice 
 *   that a space must be inserted in front of the +/- sign. Single digits are 
 *   accepted in all fields, as in 25/5/94.
 */
public class NovellDate {
  
  private static final String FORMAT = "dd/MM/yy HH:mm"; 
  
  private Calendar calendar; 
  private float offsetHours;
  private float offsetMins;
  
  public NovellDate() {
    calendar = new GregorianCalendar();
    offsetHours = 0;
    offsetMins = 0;
  }
  
  public boolean set(final String dateToSet) {
    String novellDate = dateToSet.trim();
    calendar.set(0, 0, 0, 0, 0, 0);
    calendar.set(Calendar.MILLISECOND, 0);
    
    String []parts = novellDate.split(" ");
    if (parts.length < 2) {
      return false;
    }
    
    String []days = parts[0].split("/");
    if (days.length < 3) {
      return false;
    }
    int date = Integer.parseInt(days[0]);
    int month = Integer.parseInt(days[1]);
    int year = Integer.parseInt(days[2]);
    
    if (year < 70) {
      year = 2000 + year;
    } else if (year < 100) {
      year = 1900 + year;
    }
    
    String[] time = parts[1].split(":");
    if (time.length != 2) {
      return false;
    }
    int hourOfDay = Integer.parseInt(time[0]);
    int minute = Integer.parseInt(time[1]);
    
    if (parts.length == 3) {
      String []offset = parts[2].split(":");
      if (offset.length != 2) {
        return false;
      }
      // if we have a plus, we need to remove it
      if (offset[0].startsWith("+")) {
        offset[0] = offset[0].substring(1, offset[0].length());
      }
      offsetHours = Integer.parseInt(offset[0]);
      offsetMins = Integer.parseInt(offset[1]);
    }
    
    calendar.set(year, month - 1, date, hourOfDay, minute);
    return true;
  }
  
  /**
   * returns the date contained in this object as formatted for
   * Novell
   */
  @Override
  public String toString() {
    return new SimpleDateFormat(FORMAT).format(calendar.getTime());
  }

  // Should not be used at the moment
  float getOffsetHours() {
    return offsetHours;
  }

  // Should not be used at the moment
  float getOffsetMins() {
    return offsetMins;
  }
 
  public long getTimeInUtc() {
    return calendar.getTimeInMillis();
  }
 
  public void setTimeInUtc(long time) {
    calendar.setTimeInMillis(time);
  }
}
