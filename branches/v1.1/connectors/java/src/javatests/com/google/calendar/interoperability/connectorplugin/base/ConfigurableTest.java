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

import static com.google.calendar.interoperability.connectorplugin.base.Configurable.Type.bool;
import static com.google.calendar.interoperability.connectorplugin.base.Configurable.Type.integer;
import static com.google.calendar.interoperability.connectorplugin.base.Configurable.Type.string;

import com.google.calendar.interoperability.connectorplugin.base.Configurable;

import junit.framework.TestCase;

import java.util.Properties;

/**
 * Unit tests for Configurable-class
 */
public class ConfigurableTest extends TestCase {
  
  private static final String BASE = "base";
  
  private Properties properties;
  private Configurable configurable;
  private boolean prependBase;
  
  @Override
  public void setUp() throws Exception {
    super.setUp();
    properties = new Properties();
    configurable = new Configurable(BASE){};
    configurable.setLocalConfig(properties);
  }
  
  private void assertProperty(
      String property, String presetValue, Object expected) {
    if (presetValue != null) {
      properties.setProperty(BASE + "." + property, presetValue);
    }
    if (prependBase) {
      property = BASE + "." + property;
    }
    assertEquals(expected, configurable.getValue(property));
  }
  
  private void assertNoProperty(
      String property, String presetValue, 
      Class<? extends Exception> expected) {
    if (presetValue != null) {
      properties.setProperty(BASE + "." + property, presetValue);
    }
    if (prependBase) {
      property = BASE + "." + property;
    }
    try {
      Object value = configurable.getValue(property);
      fail("Expected exception, but found value: " + value);  // COV_NF_LINE
    } catch (RuntimeException e) {
      assertEquals(expected, e.getClass());
    }
  }
  
  private void basicFunctionality() {
    assertNoProperty("1", null, IllegalArgumentException.class);
    assertNoProperty("2", null, IllegalArgumentException.class);
    assertNoProperty("3", null, IllegalArgumentException.class);
    assertNoProperty("4", null, IllegalArgumentException.class);
    assertNoProperty("5", null, IllegalArgumentException.class);
    assertNoProperty("6", null, IllegalArgumentException.class);
    configurable.registerParameter("1", string);
    configurable.registerParameter("2", bool);
    configurable.registerParameter("3", integer);
    assertNoProperty("1", null, NullPointerException.class);
    assertNoProperty("2", null, NullPointerException.class);
    assertNoProperty("3", null, NullPointerException.class);
    assertProperty("1", "A", "A");
    assertProperty("2", "True", true);
    assertProperty("3", "1", 1L);
    assertNoProperty("3", "X", NumberFormatException.class);
    configurable.registerParameter("4", string, "default");
    configurable.registerParameter("5", bool, "false");
    configurable.registerParameter("6", integer, "13");
    assertProperty("4", null, "default");
    assertProperty("5", null, false);
    assertProperty("6", null, 13L);    
  }
  
  public void testWithoutBase() {
    prependBase = false;
    basicFunctionality();
  }

  public void testWithBase() {
    prependBase = true;
    basicFunctionality();
  }
}
 
