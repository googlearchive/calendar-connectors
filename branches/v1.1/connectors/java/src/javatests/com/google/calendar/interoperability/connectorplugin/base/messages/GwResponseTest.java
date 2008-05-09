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
import com.google.calendar.interoperability.connectorplugin.base.messages.GwResponse;
import com.google.calendar.interoperability.connectorplugin.base.messages.UnknownCommand;

import junit.framework.TestCase;

/**
 * Unit tests for the GwResponse class
 */
public class GwResponseTest extends TestCase {
  
  private GwCommand command;
  private GwResponse response;
  private String rendered;
  
  private class TestResponse extends GwResponse {
    public TestResponse() {
      super(command);
    }    
    @Override
    public String renderResponse() {
      if (rendered != null) {
        return rendered;
      }
      return super.renderResponse();
    }
  }
  
  @Override
  public void setUp() throws Exception {
    super.setUp();
    command = new UnknownCommand("A", "B");
    response = new TestResponse();
  }
  
  public void testConstructorWithNullCommand() {
    try {
      command = null;
      new TestResponse();
      fail("Expected a NullPointerException");  // COV_NF_LINE
    } catch (NullPointerException expected) {
      // Expected
    }    
  }
  
  public void testTrivialFunctions() {
    assertSame(command, response.getOriginalCommand());
    assertNull(response.renderResponse());
    assertEquals("A", response.suggestFilename());
    assertEquals("processed_A", response.suggestLogFilename());
  }
  
  public void testRenderLogNoResponse() {
    assertTrue(response.renderLog().indexOf("NO RESPONSE") >= 0);
  }
  
  public void testRenderLogWithResponse() {
    final String expected = String.format(
        "INCOMING COMMAND:%n" +
        "=================%n" +
        "B%n%n" +
        "RESPONSE:%n" +
        "=========%n" +
        "C%n");
    rendered = "C";
    assertEquals(expected, response.renderLog());
  }

  public void testEscaping() {
    for (char c : new char[] {';', '\\', ','}) {
      String escaped = response.escape(c + "A" + c + "B" + c);
      assertEquals("\\" + c + "A\\" + c + "B\\" + c, escaped);
    }
  }
  
}
 
