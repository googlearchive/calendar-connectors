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

import com.google.calendar.interoperability.connectorplugin.base.messages.util.Address;

import junit.framework.TestCase;

/**
 * Test class for Novell groupwise address
 */
public class AddressTest extends TestCase {

  public void testGets() {
    Address address = new Address("a", "b", "c", "d", null);
    
    assertEquals("a", address.getWPD());
    assertEquals("b", address.getWPPO());
    assertEquals("c", address.getWPU());
    assertEquals("d", address.getCDBA());
    assertEquals(
        "    WPD = a;\n" +
        "    WPPO = b;\n" +
        "    WPU = c;\n" +
        "    CDBA = d;", address.toString());
  }
}
