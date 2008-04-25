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

package com.google.calendar.interoperability.connectorplugin.base;

import com.google.calendar.interoperability.connectorplugin.base.BasicSearchHandler;
import com.google.calendar.interoperability.connectorplugin.base.messages.GwResponse;
import com.google.calendar.interoperability.connectorplugin.base.messages.ProbeResponse;
import com.google.calendar.interoperability.connectorplugin.base.messages.SearchCommand;
import com.google.calendar.interoperability.connectorplugin.base.messages.VoidResponse;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.AddressList;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.NovellDate;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.Address;

import junit.framework.TestCase;

/**
 * Unit tests for Configurable-class
 */
public class BasicSearchHandlerTest extends TestCase {
  
  private BasicSearchHandler handler;
  private int counter = 0;
  private SearchCommand input;
  
  @Override
  public void setUp() {
    handler = new BasicSearchHandler(){
      @Override
      protected GwResponse handleSearch(SearchCommand command,
          Address requestor, Address searchFor) {
        counter++;
        return null;
      }};
    input = new SearchCommand("A", "B");
    input.setBeginTime(new NovellDate());
    input.setEndTime(new NovellDate());
    input.setFrom(new Address());
    input.setMsgId("123ABC");
    AddressList to = new AddressList();
    to.add(new Address());
    input.setTo(to);
  }
  
  public void testBasicFunctionality() {
    input.setBeginTime(null);
    assertEquals(VoidResponse.class, handler.apply(input).getClass());
    assertEquals(0, counter);
    input.setBeginTime(new NovellDate());
    assertNull(handler.apply(input));
    assertEquals(1, counter);
    input.setHeaderContent("FB-PROBE");
    assertEquals(ProbeResponse.class, handler.apply(input).getClass());
    assertEquals(1, counter);
  }

}
 
