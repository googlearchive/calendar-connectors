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

import com.google.calendar.interoperability.connectorplugin.impl.mock.MockDomain;

import junit.framework.TestCase;

/**
 * Tests for the mock domain
 */
public class MockDomainTest extends TestCase {
  
  private MockDomain domain;
  
  @Override
  public void setUp() {
    domain = new MockDomain("google.com");
    assertEquals("google.com", domain.getName());
  }
  
  public void testCreateUser() {
    assertNotNull(domain.createUser("1"));
    assertNull(domain.createUser("1"));
    assertNotSame(domain.createUser("2"), domain.createUser("3"));
    assertEquals(3, domain.countUsers());
  }
  
  public void testGetUser() {
    assertNull(domain.getUser("1"));
    assertNotNull(domain.createUser("1"));
    assertNotNull(domain.getUser("1"));
  }
  
  public void testPrefill() {
    domain.prefillDomain("user", 10);
    assertEquals(10, domain.countUsers());
    assertNotNull(domain.getUser("user0"));
  }
}
 
