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

package com.google.calendar.interoperability.connectorplugin.base.messages;

import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Date;
import java.util.Iterator;
import java.util.List;

/**
 * Response for free-busy requests.
 */
public class FreeBusyResponse extends TemplateResponse {
  
  private static final String MESSAGE_TEMPLATE =
    "WPC-API= 1.2; \r\n" +
    "Header-Char= T50; \r\n" +
    "Msg-Type= SEARCH; \r\n" +
    "Orig-Msg-ID= $(getMsgId); \r\n" +
    "To= \r\n" +
    "${renderTo}" +
    "    ;\r\n" +
    "Busy-For= \r\n" +
    "${renderFor}" +
    "Busy-Report=  \r\n" +
    "${renderTimes}" + 
    "    ;\r\n" +
    "Send-Options= None; \r\n" +
    "-END-\r\n";
  
  private static final String ADDRESS_TEMPLATE_TO = 
    "    CDBA= ${getCDBA}; \r\n";
  
  private static final String ADDRESS_BUSY_FOR = 
    "        CDBA= ${getCDBA}; \r\n";
  
  private static final String SLOT = 
    "    Start-Time= %s; \r\n" +
    "    End-Time= %s; %s\r\n";
  
  private static class Timeslot {
    private static final String DATE_FORMAT = "dd/MM/yy HH:mm";
    
    private long start;
    private long end;
    
    public Timeslot(long start, long end) {
      super();
      this.start = start;
      this.end = end;
    }
    
    private String render(final long time) {
      SimpleDateFormat format = new SimpleDateFormat(DATE_FORMAT);
      return format.format(new Date(time));
    }
    
    public String getStart() {
      return render(start);
    }
    
    public String getEnd() {
      return render(end);
    }
  }

  private List<Timeslot> timeslots;
  
  public FreeBusyResponse(GwCommand originalCommand) {
    super(originalCommand, MESSAGE_TEMPLATE);
    timeslots = new ArrayList<Timeslot>();
  }
  
  public void addTimeslot(long start, long end) {
    timeslots.add(new Timeslot(start, end));
  }
  
  public String renderTimes() {
    StringBuffer sb = new StringBuffer();
    for (Iterator<Timeslot> it = timeslots.iterator(); it.hasNext(); ) {
      Timeslot slot = it.next();
      sb.append(String.format(
          SLOT, 
          slot.getStart(), 
          slot.getEnd(),
          it.hasNext() ? ", " : ""));
    }
    return sb.toString();
  }
  
  public String renderTo() {
    return replace(ADDRESS_TEMPLATE_TO, getOriginalCommand().getFrom());
  }
  
  public String renderFor() {
    return replace(ADDRESS_BUSY_FOR, 
        getOriginalCommand().getTo().getAddresses().iterator().next());
  }
}
 
