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

import com.google.calendar.interoperability.connectorplugin.base.BasicSearchHandler;
import com.google.calendar.interoperability.connectorplugin.base.messages.FreeBusyResponse;
import com.google.calendar.interoperability.connectorplugin.base.messages.GwResponse;
import com.google.calendar.interoperability.connectorplugin.base.messages.SearchCommand;
import com.google.calendar.interoperability.connectorplugin.base.messages.VoidResponse;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.Address;
import com.google.gdata.data.DateTime;
import com.google.gdata.data.calendar.CalendarEventEntry;
import com.google.gdata.data.calendar.CalendarEventFeed;
import com.google.gdata.data.extensions.When;

import java.text.ParseException;
import java.text.SimpleDateFormat;
import java.util.Calendar;
import java.util.GregorianCalendar;
import java.util.List;
import java.util.logging.Level;

/**
 * Handles a free/busy search
 */
class SearchHandler extends BasicSearchHandler {
  
  private GDataAccessObject dao;
  
  public SearchHandler(GDataAccessObject dataAccess) {
    this.dao = dataAccess;
  }
  
  private static long toMidnight(DateTime time, boolean addOne) {
    GregorianCalendar calendar = new GregorianCalendar();
    try {
      calendar.setTimeInMillis(
          new SimpleDateFormat("yyyy-MM-dd").parse(time.toUiString())
              .getTime());
    } catch (ParseException e) {
      
      // Fallback: use value directly (not timezone-intolerant)
      calendar.setTimeInMillis(time.getValue());
    }
    if (addOne) {
      calendar.add(Calendar.DATE, 1);
    }
    calendar.set(Calendar.MILLISECOND, 0);
    calendar.set(Calendar.SECOND, 0);
    calendar.set(Calendar.MINUTE, 0);
    calendar.set(Calendar.HOUR_OF_DAY, 0);
    return calendar.getTimeInMillis();
  }
  
  static long renderFrom(When when) {
    DateTime time = when.getStartTime();
    if (time.isDateOnly()) {
      return toMidnight(time, false);
    }
    return time.getValue();
  }
  
  static long renderUntil(When when) {
    DateTime time = when.getEndTime();
    if (time != null) {
      if (time.isDateOnly()) {
        return toMidnight(time, false) - 1;
      }
      return time.getValue();
    }
    return toMidnight(when.getStartTime(), true) - 1;
  }

  @Override
  protected GwResponse handleSearch(SearchCommand command, Address requestor,
      Address searchFor) {
    
    // Extract the username and times
    String userName = searchFor.getCDBA();
    final int delim = userName.indexOf(".."); 
    if (delim < 0 || delim + 2 == userName.length()) { 
      logger.log(Level.WARNING, "Could not decode toAddress: " + userName);
      return VoidResponse.invalid(command);      
    }
    userName = userName.substring(delim + 2);
    final long from = command.getBeginTime().getTimeInUtc();
    final long until = command.getEndTime().getTimeInUtc();
    
    // Do the gdata query
    final Iterable<CalendarEventFeed> feeds = 
        dao.retrieveFreeBusy(userName, from, until);
    if (feeds == null) {
      logger.log(Level.WARNING, "Could not load f/b feed: " + userName);
      return VoidResponse.invalid(command);     
    }

    // Create the response object
    final FreeBusyResponse response = new FreeBusyResponse(command);
    for (CalendarEventFeed feed : feeds) {
      for (CalendarEventEntry event : feed.getEntries()) {
        List<When> times = event.getTimes();
        for (When when : times) {
          response.addTimeslot(renderFrom(when), renderUntil(when));
        }
      }
    }
    return response;
  }

}
 
