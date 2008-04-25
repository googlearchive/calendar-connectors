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

import com.google.calendar.interoperability.connectorplugin.base.messages.GwCommand;
import com.google.calendar.interoperability.connectorplugin.base.messages.GwResponse;
import com.google.calendar.interoperability.connectorplugin.base.messages.UnknownCommand;
import com.google.calendar.interoperability.connectorplugin.base.messages.VoidResponse;
import com.google.common.base.Function;

import java.util.HashMap;
import java.util.Map;

/**
 * Extensible base command for handling GwCommands
 */
public class CommandHandler implements Function<GwCommand, GwResponse> {
  
  private Map<
      Class<? extends GwCommand>, 
      Function<GwCommand, GwResponse>> handlers;  
  
  /**
   * Constructor. Registers a default handler for unknown commands
   */
  public CommandHandler() {
    handlers =
      new HashMap<Class<? extends GwCommand>, 
      Function<GwCommand, GwResponse>>();    
  }

  /**
   * Registers a handler for a particular command class
   * @param handledClass the exact class that the handler is for. Sub- and
   *   superclass relationships have no effect
   */
  @SuppressWarnings("unchecked")
  public<T extends GwCommand> void registerSubhandler(
      Class<T> handledClass, Function<T, GwResponse> handler) {
    handlers.put(handledClass, (Function<GwCommand, GwResponse>) handler);
  }
  
  public GwResponse apply(GwCommand from) {
    
    // Do we have a direct match?
    if (handlers.containsKey(from.getClass())) {
      return handlers.get(from.getClass()).apply(from);
    }

    // Is it an unknown command?
    if (from instanceof UnknownCommand) {
      return new VoidResponse(from, "Unable to parse command", "unknown");
    }
    
    // Is it an unhandled command?
    return new VoidResponse(
        from, "No handler registered for " + from.getClass(), "unsupported");
  }

}
 
