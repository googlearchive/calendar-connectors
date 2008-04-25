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
 * A Novell groupwise Status-Report data structure. It is unknown. 
 *
 */
public class StatusReport {

  private String action = null;
  private NovellDate time = null;
  
  public StatusReport(String action, NovellDate time) {
    this.action = action;
    this.time = time;
  }
  
  public StatusReport() {
    // Empty Constructor
  }
  
  public void set(String key, String value) {
    if ("ACTION".equalsIgnoreCase(key)) {
      action = value;
    } else if ("TIME".equalsIgnoreCase(key)) {
      NovellDate date = new NovellDate();
      date.set(value);
      time = date;
    }
  }
  
  @Override
  public String toString() {
    String str = "STATUS-REPORT =";
    if (action != null) {
      str = str + "    " + action + ";\n";
    }
    if (time != null) {
      str = str + "    " + time.toString() + ";\n";
    }
    str = str + "    ;";
    
    return str; 
  }
}
