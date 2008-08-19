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


/**
 * A novell groupwise file descriptor used in message headers
 */
public class FileDescriptor {

  boolean conversionAllowed;
  String currentFile;
  String originalFile;
  int size;
  NovellDate date;
  
  public FileDescriptor() {
    // Empty Constructor
  }
  
  public void addPair(String key, String value) {
    if ("-CONVERSION-ALLOWED-".equalsIgnoreCase(key)) {
      conversionAllowed = Boolean.parseBoolean(value);
    } else if ("Current-File".equalsIgnoreCase(key)) {
      currentFile = value;
    } else if ("Original-File".equalsIgnoreCase(key)) {
      originalFile = value;
    } else if ("Size".equalsIgnoreCase(key)) {
      size = Integer.parseInt(value);
    } else if ("Date".equalsIgnoreCase(key)) {
      date = new NovellDate();
      date.set(value);
    }
  }

  public boolean isConversionAllowed() {
    return conversionAllowed;
  }

  public void setConversionAllowed(boolean conversionAllowed) {
    this.conversionAllowed = conversionAllowed;
  }

  public String getCurrentFile() {
    return currentFile;
  }

  public void setCurrentFile(String currentFile) {
    this.currentFile = currentFile;
  }

  public String getOriginalFile() {
    return originalFile;
  }

  public void setOriginalFile(String originalFile) {
    this.originalFile = originalFile;
  }

  public int getSize() {
    return size;
  }

  public void setSize(int size) {
    this.size = size;
  }

  public NovellDate getDate() {
    return date;
  }

  public void setDate(NovellDate date) {
    this.date = date;
  }
}
