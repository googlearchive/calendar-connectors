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
import com.google.calendar.interoperability.connectorplugin.base.messages.ProbeResponse;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.Address;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.AddressList;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.NovellDate;

import junit.framework.TestCase;

/**
 * Smoke test for the ProbeResponse
 */
public class ProbeResponseTest extends TestCase {
  
  public void testSmoketest() {
    AdminCommand cmd = new AdminCommand("", "");
    ProbeResponse response = new ProbeResponse(cmd);
    Address address  = 
      new Address(
          "Fish", "food", "FB-PROBE" , 
          "Fish.food.FB-PROBE", "badFormat");
    AddressList to = new AddressList();
    to.add(address);
    cmd.setTo(to);
    cmd.setFrom(address);
    cmd.setBeginTime(new NovellDate());
    cmd.setEndTime(new NovellDate());
    assertNotNull(response.renderResponse());
  }

}
 
