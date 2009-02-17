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
 * Top-level mock domain with a set of users attached to it. Since google
 * does not have the concept of a post office, there is no organizational
 * layer in-between (unlike in GroupWise)
 */
class MockDomain implements Iterable<MockUser>{
  
  private String name;
  private Map<String, MockUser> users;
  
  private MockUser getOrCreateUser(String objectName, boolean create) {
    Preconditions.checkNotNull(objectName);
    if (users.containsKey(objectName)) {
      return create ? null : users.get(objectName);
    } else if (!create) {
      return null;
    }
    MockUser result = new MockUser(this, objectName);
    users.put(objectName, result);
    return result;
  }
  
  public MockDomain(String name) {
    Preconditions.checkNotNull(name);
    this.name = name;
    this.users =
      Collections.synchronizedMap(new HashMap<String, MockUser>());
  }
  
  /**
   * Creates a new user with a given primary key
   * @return the new object with the primary key or null if the key is
   *   already taken
   */
  public MockUser createUser(String objectName) {
    return getOrCreateUser(objectName, true);
  }
  
  /**
   * Finds an existing user with a given primary key
   * @return the object with the primary key or null if the key is
   *   not mapped to an existing user
   */
  public MockUser getUser(String objectName) {
    return getOrCreateUser(objectName, false);
  }
  
  /**
   * @return the count of users in this domain
   */
  public int countUsers() {
    return users.size();
  }

  /**
   * @return the name of the domain, such as google.com
   */
  public String getName() {
    return name;
  }

  /**
   * Returns an iterator over all users of this domain
   */
  public Iterator<MockUser> iterator() {
    return new ArrayList<MockUser>(users.values()).iterator(); 
  }
  
  /**
   * Prefills the domain with a bunch of generic users
   * @param prefix the prefix for the id and name
   * @param amount the amount of users to be created
   */
  public void prefillDomain (String prefix, int amount) {
    Preconditions.checkNotNull(prefix);
    for (int i = 0; i < amount; i++) {
      MockUser user = createUser(prefix + i);
    }    
  }
  
  
}