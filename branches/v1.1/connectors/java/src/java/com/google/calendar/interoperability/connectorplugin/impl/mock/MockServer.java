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

package com.google.calendar.interoperability.connectorplugin.impl.mock;

import com.google.common.base.Preconditions;

import java.util.ArrayList;
import java.util.Collections;
import java.util.HashMap;
import java.util.Iterator;
import java.util.Map;

/**
 * Top-Level mock class: this is essentially a singleton within
 * a mocking context
 */
class MockServer implements Iterable<MockDomain> {
  
  private Map<String, MockDomain> domains;
  
  public MockServer() {
    domains = Collections.synchronizedMap(new HashMap<String, MockDomain>());
  }
  
  private MockDomain getOrCreateDomain(String objectName, boolean create) {
    Preconditions.checkNotNull(objectName);
    if (domains.containsKey(objectName)) {
      return create ? null : domains.get(objectName);
    } else if (!create) {
      return null;
    }
    MockDomain result = new MockDomain(objectName);
    domains.put(objectName, result);
    return result;
  }
  
  /**
   * Creates a new domain with a given primary key
   * @return the new object with the primary key or null if the key is
   *   already taken
   */
  public MockDomain createDomain(String objectName) {
    return getOrCreateDomain(objectName, true);
  }

  /**
   * Gets a new domain with a given primary key
   * @return the new object with the primary key or null if the key is
   *   not mapped
   */
  public MockDomain getDomain(String objectName) {
    return getOrCreateDomain(objectName, false);
  }

  /**
   * Creates an iterator over all existing domains
   */
  public Iterator<MockDomain> iterator() {
    return new ArrayList<MockDomain>(domains.values()).iterator(); 
  }
  
  /**
   * Helper: initializes the server with a set of given domains, users
   * and calendars
   */
  public void prefillServer(
      String userPrefix, int numberOfUsers,
      String eventPrefix, long fromUtc, long untilUtc, 
      int eventLength, int distance, String... domainName) {
    for (String s : domainName) {
      MockDomain domain = createDomain(s);
      domain.prefillDomain(userPrefix, numberOfUsers);
      for (MockUser user : domain) {
        user.getCalendar().prefillCalendar(
            eventPrefix, fromUtc, untilUtc, eventLength, distance);
      }
    }
  }
}
 
