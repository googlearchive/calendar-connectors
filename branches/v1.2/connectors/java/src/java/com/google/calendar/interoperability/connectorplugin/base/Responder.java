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
import com.google.common.base.Function;

import static com.google.calendar.interoperability.connectorplugin.base.GwIo.FOLDER.HEADERS_OUT;
import static com.google.calendar.interoperability.connectorplugin.base.GwIo.FOLDER.LOG;

/**
 * This class takes a response object and writes it back into the
 * outgoing folder for Exchange
 */
public class Responder implements Function<GwResponse, GwCommand>{
  
  private GwIo io;
  private boolean log;
  
  public Responder(GwIo io, boolean log) {
    this.io = io;
    this.log = log;
  }

  public GwCommand apply(GwResponse from) {
    
    // Need to respond?
    final String respondToClient = from.renderResponse();
    if (respondToClient != null) {
      if (!io.store(HEADERS_OUT, from.suggestFilename(), 
          respondToClient.getBytes())) {
        throw new RuntimeException("Could not write response, I/O problem?");
      }
    }
    
    // Need to log ?
    if (log) {
      io.store(LOG, from.suggestLogFilename(),
          from.renderLog().getBytes());
    }
    
    // Done :-)
    return from.getOriginalCommand();
  }
}
 
