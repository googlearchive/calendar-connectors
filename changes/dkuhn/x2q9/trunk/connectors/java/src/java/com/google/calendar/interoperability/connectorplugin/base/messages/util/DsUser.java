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
 * A Novell groupwise DS-User data structure. It is unknown. 
 */
public class DsUser {
  
  private String networkId;
  private String domain;  
  private String postOffice;  
  private String objectName;  
  private String lastName;  
  private String firstName;  
  
  public DsUser(String networkId, String domain, String postOffice,
      String objectName, String lastName, String firstName) {
    super();
    this.networkId = networkId;
    this.domain = domain;
    this.postOffice = postOffice;
    this.objectName = objectName;
    this.lastName = lastName;
    this.firstName = firstName;
  }

  public DsUser() {
    // Empty Constructor
  }
  
  @Override 
  public String toString() {
    return "DS-User= \n    ;";
  }

  public String getDomain() {
    return domain;
  }

  public void setDomain(String domain) {
    this.domain = domain;
  }

  public String getPostOffice() {
    return postOffice;
  }

  public void setPostOffice(String postOffice) {
    this.postOffice = postOffice;
  }

  public String getObjectName() {
    return objectName;
  }

  public void setObjectName(String objectName) {
    this.objectName = objectName;
  }

  public String getLastName() {
    return lastName;
  }

  public void setLastName(String lastName) {
    this.lastName = lastName;
  }

  public String getFirstName() {
    return firstName;
  }

  public void setFirstName(String firstName) {
    this.firstName = firstName;
  }

  public String getNetworkId() {
    return networkId;
  }

  public void setNetworkId(String networkId) {
    this.networkId = networkId;
  }
}
