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

import com.google.calendar.interoperability.connectorplugin.base.messages.GwCommand;
import com.google.calendar.interoperability.connectorplugin.base.messages.UnknownCommand;
import com.google.calendar.interoperability.connectorplugin.base.messages.VoidResponse;

import junit.framework.TestCase;

/**
 * Unit test for the VoidResponse class
 */
public class VoidResponseTest extends TestCase {
  
  private GwCommand command;
  private VoidResponse response;
  
  @Override
  public void setUp() {
    command = new UnknownCommand("A", "B");
    response = new VoidResponse(command, "reason", "P");
  }
  
  public void testSuggestedLogFilename() {
    assertEquals("P_A", response.suggestLogFilename());
    assertEquals("nonrespond_A", new VoidResponse(command, "reason")
        .suggestLogFilename());
  }
  
  public void testRenderLog() {
    assertTrue(response.renderLog().endsWith(String.format("reason%n")));
  }

}
 
