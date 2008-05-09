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

import static com.google.calendar.interoperability.connectorplugin.base.GwIo.FOLDER.HEADERS_IN;

import java.util.Collections;
import java.util.HashSet;
import java.util.Set;
import java.util.logging.Level;
import java.util.logging.Logger;

/**
 * Scans an inbound folder for incoming API messages and enqueues them
 * into a sink. This object does not contain any threading logic -- its
 * scan()-method needs to be invoked by an external process.
 */
public class InputScanner {
  
  private static final Logger LOGGER 
      = Logger.getLogger(InputScanner.class.getName());
  
  private final Sink<String> sink;
  private final GwIo io;
  
  // Set of filenames at the last scan
  private Set<String> knownNames;
  
  /**
   * Constructor. Invoking this will not automatically start
   * any threading on this object -- it is passive and needs to
   * be triggered by calling the "scan"-method.
   */
  public InputScanner(GwIo io, Sink<String> sink) {
    this.io = io;
    this.sink = sink;
    this.knownNames = Collections.emptySet();
  }
  
  /**
   * Scans the HEADERS_IN-folder for new files and puts them into the
   * sink.
   */
  public synchronized void scan() {
    Set<String> newNames = 
      new HashSet<String>(Math.max(5, 2 * knownNames.size()));
    for (String file : io.listFiles(HEADERS_IN)) {
      newNames.add(file);
      if (knownNames.contains(file)) {
        continue;
      }
      sink.accept(file);
    }
    knownNames = newNames;
  }
  
  /**
   * This method will call the scan-method in an endless loop until the
   * thread calling is interrupted.
   */
  public void scanForever() {
    LOGGER.log(Level.FINE, "Scanning process begun");
    while (!Thread.interrupted()) {
      try {
        scan();
      } catch (Throwable t) {
        LOGGER.log(Level.WARNING, "Scanning input directory failed", t);
      }
      try {
        Thread.sleep(500);
      } catch (InterruptedException e) {
        LOGGER.log(Level.FINE, "Scanning process interruped", e);
        return;
      }
    }
    LOGGER.log(Level.FINE, "Scanning process interruped");
  }
}
 
