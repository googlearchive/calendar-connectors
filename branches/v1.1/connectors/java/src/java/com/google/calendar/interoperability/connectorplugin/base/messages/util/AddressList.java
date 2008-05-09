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


import java.util.Iterator;
import java.util.LinkedHashSet;
import java.util.Set;

/**
 * Set of recipients
 */
public class AddressList {

  private Set<Address> recipients;

  public AddressList() {
    recipients = new LinkedHashSet<Address>();
  }
  
  public AddressList(Set<Address> recipients) {
    this.recipients = recipients;
  }

  public void add(Address address) {
    recipients.add(address);
  }
  
  /**
   * Returns the set of addresses. We use a LinkedHashSet to maintain
   * order of addresses.
   * 
   * @return A Set of addresses
   */
  public Set<Address> getAddresses() {
    return recipients;
  }
  
  @Override
  public String toString() {
    
    String addressList = "";
    
    int numAddress = recipients.size();
    if (numAddress == 0) {
      return addressList;
    }
    
    Iterator<Address> iterator = recipients.iterator();
    if (!iterator.hasNext()) {
      return addressList;
    }
    addressList = iterator.next().toString();
    
    while (iterator.hasNext()) {
      addressList = addressList + "  ,\n" + iterator.next().toString(); 
    }
    return addressList;
  }
}
