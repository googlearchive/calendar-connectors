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
 * A Novell groupwise DS-External-Post-Office data structure containing a list 
 * of post-offices under foreign domains
 */
public class DsExternalPostOffice {
  private final String description;
  private final String domain;
  private final String postOffice;
  private final String operation;
  private final String timeZone;
  
  public DsExternalPostOffice(
      String description,
      String domain,
      String postOffice,
      String operation,
      String timeZone) {
    this.description = description;
    this.domain = domain;
    this.postOffice = postOffice;
    this.operation = operation;
    this.timeZone = timeZone;
  }
  
  @Override
  public String toString() {
    String str = "DS-External-Post-Office= \n";
    if (operation != null) {
      str = str + "    Operation = " + operation + ";\n";
    }
    if (domain != null) {
      str = str + "    Domain = " + domain + ";\n";
    }
    if (postOffice != null) {
      str = str + "    Post-Office = " + postOffice + ";\n";
    }
    if (description != null) {
      str = str + "    Description = " + description + ";\n";
    }
    if (timeZone != null) {
      str = str + "    Time-Zone = " + timeZone + ";\n";
    }
    str = str + "    ;";
    return str;
  }
}
