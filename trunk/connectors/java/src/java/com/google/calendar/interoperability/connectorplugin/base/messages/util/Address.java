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
 * A Novell groupwise address
 */
public final class Address {

  private String wPD = null; 
  private String wPPO = null; 
  private String wPU = null; 
  private String cDBA = null;
  
  private String badFormattedName = null;
  
  public Address(
      String wPD, 
      String wPPO, 
      String wPU, 
      String cDBA,
      String badFormattedName) {
    this.wPD = wPD;
    this.wPPO = wPPO;
    this.wPU = wPU;
    this.cDBA = cDBA;
    
    this.badFormattedName = badFormattedName;
  }

  public Address() {
    // Empty Constructor
  }

  public void addPair(String key, String value) {
    if ("WPD".equalsIgnoreCase(key)) {
      wPD = value;
    } else if ("WPPO".equalsIgnoreCase(key)) {
      wPPO = value;
    } else if ("WPU".equalsIgnoreCase(key)) {
      wPU = value;
    } else if ("CDBA".equalsIgnoreCase(key)) {
      cDBA = value;
    }    
  }
  
  @Override
  public String toString() {
    if (badFormattedName != null) {
      return badFormattedName;
    }
    String str = "";
    if (wPD != null) {
      str = str + "    WPD = " + wPD + ";\n";
    }
    if (wPPO != null) {
      str = str + "    WPPO = " + wPPO + ";\n";
    }
    if (wPU != null) {
      str = str + "    WPU = " + wPU + ";\n";
    }
    if (cDBA != null) {
      str = str + "    CDBA = " + cDBA + ";";
    }
    return str;
  }
  
  public String toString(String key) {
    return key + "\n" + toString() + " ;";
  }
  
  public String getWPD() {
    return wPD;
  }

  public String getWPPO() {
    return wPPO;
  }

  public String getWPU() {
    return wPU;
  }

  public String getCDBA() {
    return cDBA;
  }
  
  
}
