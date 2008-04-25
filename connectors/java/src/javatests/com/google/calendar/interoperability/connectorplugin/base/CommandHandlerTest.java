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

import com.google.calendar.interoperability.connectorplugin.base.CommandHandler;
import com.google.calendar.interoperability.connectorplugin.base.messages.AdminCommand;
import com.google.calendar.interoperability.connectorplugin.base.messages.GwResponse;
import com.google.calendar.interoperability.connectorplugin.base.messages.UnknownCommand;
import com.google.calendar.interoperability.connectorplugin.base.messages.VoidResponse;
import com.google.common.base.Function;

import junit.framework.TestCase;

/**
 * Unit tests for the command handler
 */
public class CommandHandlerTest extends TestCase {
  
  private static final String CONTENT = "content";
  
  private CommandHandler handler;
  
  @Override
  public void setUp() {
    handler = new CommandHandler();
  }

  public void testUnknownCommand() {
    VoidResponse response = (VoidResponse) 
      handler.apply(new UnknownCommand("1", CONTENT));
    assertEquals("unknown_1", response.suggestLogFilename());
  }

  public void testUnsupportedCommand() {
    VoidResponse response = (VoidResponse)
      handler.apply(new AdminCommand("1", CONTENT));
    assertEquals("unsupported_1", response.suggestLogFilename());
  }
  
  public void testRegisteredCommand() {
    final UnknownCommand cmd = new UnknownCommand("1", CONTENT);
    final VoidResponse testResponse = new VoidResponse(cmd, "A", "B");
    final Function<UnknownCommand, GwResponse> specialHandler = 
      new Function<UnknownCommand, GwResponse>() {
        public GwResponse apply(UnknownCommand from) {
          assertSame(cmd, from);
          return testResponse;
        }      
    };
    handler.registerSubhandler(UnknownCommand.class, specialHandler);
    assertSame(testResponse, handler.apply(cmd));
  }
}
 
