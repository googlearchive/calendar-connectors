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

import com.google.calendar.interoperability.connectorplugin.base.messages.AdminCommand;
import com.google.calendar.interoperability.connectorplugin.base.messages.FreeBusyResponse;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.Address;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.AddressList;

import junit.framework.TestCase;

import java.text.SimpleDateFormat;
import java.util.Date;

/**
 * Unit tests for free/busy response
 */
public class FreeBusyResponseTest extends TestCase {

  private AdminCommand cmd;
  private FreeBusyResponse response;
  private Address address;
  private final SimpleDateFormat format = 
    new SimpleDateFormat("dd/MM/yy HH:mm");
  
  
  @Override
  public void setUp() {
    cmd = new AdminCommand("", "");
    response = new FreeBusyResponse(cmd);
    address  = new Address("wPD", "wPPO", "wPU" , "cDBA", "badFormat");
  }
  
  public void testRenderFor() {
    AddressList to = new AddressList();
    to.add(address);
    cmd.setTo(to);
    assertEquals("        CDBA= cDBA; \r\n", response.renderFor());
  }
  
  public void testRenderTo() {
    cmd.setFrom(address);
    assertEquals("    CDBA= cDBA; \r\n", response.renderTo());
  }
  
  public void testRenderTimes() {
    final Date d1 = new Date(0);
    final Date d2 = new Date(1);
    final Date d3 = new Date(500 * 36000000L);
    final Date d4 = new Date(500 * 72000000L);
    response.addTimeslot(d1.getTime(), d2.getTime());    
    response.addTimeslot(d3.getTime(), d4.getTime());
    assertEquals(
        String.format(
          "    Start-Time= %s; \r\n" +
            "    End-Time= %s; , \r\n" +
            "    Start-Time= %s; \r\n" +
            "    End-Time= %s; \r\n",
          format.format(d1),
          format.format(d2),
          format.format(d3),
          format.format(d4)
        )
        , response.renderTimes());
  }
  
  public void testSmoketest() {
    testRenderFor();
    testRenderTo();
    testRenderTimes();
    assertNotNull(response.renderResponse());
  }
}
 
