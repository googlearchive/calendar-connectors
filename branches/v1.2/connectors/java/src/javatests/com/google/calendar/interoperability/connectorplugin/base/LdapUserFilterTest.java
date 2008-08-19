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

import com.google.calendar.interoperability.connectorplugin.base.LdapUserFilter;

import junit.framework.TestCase;

import org.jmock.Expectations;
import org.jmock.Mockery;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collections;
import java.util.HashSet;
import java.util.List;
import java.util.Properties;
import java.util.Set;

/**
 * Unit tests for the LdapUserFilter
 */
public class LdapUserFilterTest extends TestCase {
  
  private Mockery context;
  private LdapUserFilter.Connector connector;
  private LdapUserFilter.Connector connector2;
  private Properties config;
  private LdapUserFilter filter;
  private Set<String> blacklistSet, whitelistSet;
  private final List<String> a = Collections.singletonList("A");
  private final List<String> b = Collections.singletonList("B");

  @Override
  public void setUp() {
    context = new Mockery();
    connector = context.mock(LdapUserFilter.Connector.class, "connector1");
    connector2 = context.mock(LdapUserFilter.Connector.class, "connector2");
    config = new Properties();
    config.setProperty("ldap.url", "ldap://testUrl");
    config.setProperty("ldap.authMethod", "simple");
    config.setProperty("ldap.user", "johnDoe");
    config.setProperty("ldap.password", "testPassword");
    config.setProperty("ldap.base", "base");
    filter = new LdapUserFilter(connector, config);
    blacklistSet = new HashSet<String>();
    whitelistSet = new HashSet<String>();
  }

  public void testWillNotLoginWithoutQuery() {
    context.checking(new Expectations(){});
    assertTrue(filter.executeQuery(null, null, null, null));
    context.assertIsSatisfied();
  }  
  
  public void testLogin() {
    context.checking(new Expectations(){{
      exactly(1).of(connector).clone();
      will(returnValue(connector2));
      exactly(1).of(connector2).login(
          "ldap://testUrl", "simple", "johnDoe", "testPassword");
      will(returnValue(false));
    }});
    assertFalse(filter.executeQuery("B", null, "W", null));
    context.assertIsSatisfied();
    config.remove("ldap.authMethod");
    context.checking(new Expectations(){{
      exactly(1).of(connector).clone();
      will(returnValue(connector2));
      exactly(1).of(connector2).login(
          "ldap://testUrl", "DIGEST-MD5", "johnDoe", "testPassword");
      will(returnValue(false));
    }});
    assertFalse(filter.executeQuery("B", null, "W", null));
    context.assertIsSatisfied();
  }

  public void testQueries() {
    context.checking(new Expectations(){{
      exactly(1).of(connector).clone();
      will(returnValue(connector2));
      exactly(1).of(connector2).login(
          "ldap://testUrl", "simple", "johnDoe", "testPassword");
      will(returnValue(true));
      exactly(1).of(connector2).searchEmails(
          "base", "B");
      will(returnValue(a));
      exactly(1).of(connector2).searchEmails(
          "base", "W");
      will(returnValue(b));
      exactly(1).of(connector2).close();
    }});
    assertTrue(filter.executeQuery("B", blacklistSet, "W", whitelistSet));
    context.assertIsSatisfied();    
  }
  
  private List<String> filter(
      List<String> blacklist, List<String> whitelist, String... input) {
    List<String> result = new ArrayList<String>(Arrays.asList(input));
    Set<String> bl = 
      (blacklist == null) ? null : new HashSet<String>(blacklist);
    Set<String> wl = 
      (whitelist == null) ? null : new HashSet<String>(whitelist);
    LdapUserFilter.doFilter(bl, wl, result.iterator());
    return result;
  }
  
  public void testFilterLogic() {
    List<String> list = filter(a, b, "A", "B", "C");
    assertEquals(1, list.size());
    assertTrue(list.contains("B"));
    list = filter(null, b, "A", "B", "C");
    assertEquals(1, list.size());
    assertTrue(list.contains("B"));
    list = filter(a, null, "A", "B", "C");
    assertEquals(2, list.size());
    assertTrue(list.contains("B"));
    assertTrue(list.contains("C"));
    list = filter(null, null, "A", "B", "C");
    assertEquals(3, list.size());
  }
  
  public void testDoFilterRunsThrough() {
    context.checking(new Expectations(){{
      exactly(1).of(connector).clone();
      will(returnValue(connector2));
      exactly(1).of(connector2).login(
          "ldap://testUrl", "simple", "johnDoe", "testPassword");
      will(returnValue(true));
      exactly(1).of(connector2).searchEmails(
          "base", "B");
      will(returnValue(b));
      exactly(1).of(connector2).close();
    }});
    config.setProperty("ldap.blacklist", "B");
    Set<String> set = new HashSet<String>();
    set.add("A");
    set.add("B");
    assertTrue(filter.doFilter(set.iterator()));
    context.assertIsSatisfied();
    assertEquals(1, set.size());
    assertTrue("\"A\" expected in content of " + set.toString(), 
        set.contains("A"));
  }
  
  public void testTransform() {
    Set<String> set = new HashSet<String>();
    assertTrue(filter.transform(Collections.singleton("joe@foobar.com"), set));
    assertEquals(set.iterator().next(), "JOE@FOOBAR.COM");
    config.setProperty("ldap.domainMap", 
        "foo.com,foo.bar.com; bar.com , bar.foo.com");
    assertTrue(filter.transform(Collections.singleton("joe@foo.bar.com"), set));
    assertEquals(set.iterator().next(), "JOE@FOO.COM");
    assertTrue(filter.transform(Collections.singleton("joe@bar.foo.com"), set));
    assertEquals(set.iterator().next(), "JOE@BAR.COM");
    assertTrue(filter.transform(Collections.singleton("joe@foobar.com"), set));
    assertEquals(set.iterator().next(), "JOE@FOOBAR.COM");
    config.setProperty("ldap.domainMap", "thisWontWork");
    assertFalse(filter.transform(Collections.singleton("joe@foobar.com"), set));
  }
}
 
