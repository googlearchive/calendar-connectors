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

import com.google.common.base.Preconditions;

import java.io.File;
import java.io.FileInputStream;
import java.io.IOException;
import java.util.HashMap;
import java.util.Map;
import java.util.Properties;

/**
 * Helper-class. Instances register themselves as elements that
 * can listen to key/value pairs from a property file.
 * 
 * Each instance of this class has to be given a base-key.
 * Properties in the configuration file are in the format
 * <base>.<property>=<value>
 */
public class Configurable {
  
  /**
   * Supported types that can be parsed
   */
  public static enum Type {
    integer, string, bool;
    
    /**
     * Parses an object, throws an exception if invalid
     */
    public Object parse(String value) throws
        NullPointerException, 
        NumberFormatException, 
        IllegalArgumentException {
      Preconditions.checkNotNull(value);
      switch(this) {
        case string:
          return value;
        case integer:
          return Long.parseLong(value);
        case bool:
          return Boolean.parseBoolean(value);
      }
      throw new IllegalArgumentException("unsupported type: " + this);
    }
  }
  
  private static Properties PROPERTIES;

  /**
   * The base for keys to be used in this object. Every property key has
   * to start with this base. If for instance the base is "ldap", a key
   * "user" will be transformed to "ldap.user"
   */
  private String base;
  
  /**
   * A map of a registered key to what type it should be parsed to. This
   * map is used to validate that only registered keys are used for
   * parameter lookup (helps to avoid typos)
   */
  private Map<String, Type> configDef;
  
  /**
   * A map for optional default values if a property is not set.
   */
  private Map<String, String> defaultValues;
  
  /**
   * An optional set of local properties that can be used instead of
   * the global config file, e.g. for unit tests
   */
  private Properties properties;
  
  /**
   * Constructor
   * @param base the base-key
   * @exception NullPointerException if base is null
   */
  public Configurable(String base) {
    Preconditions.checkNotNull(base);
    this.base = base;
    configDef = new HashMap<String, Type>();
    defaultValues = new HashMap<String, String>();
  }
  
  /**
   * Overrides the global properties for this class
   */
  public synchronized void setLocalConfig(Properties localProperties) {
    Preconditions.checkNotNull(localProperties);
    this.properties = localProperties;
  }
  
  /**
   * @return the global properties or its local replacement, if set
   * @exception NullPointerException if neither the local config nor the
   *   global properties are set
   */
  private synchronized Properties getProperties() {
    if (properties != null) {
      return properties;
    }
    synchronized (Configurable.class) {
      if (PROPERTIES == null) {
        throw new NullPointerException("configuration not set yet");
      }
      return PROPERTIES;
    }    
  }
  
  /** 
   * Makes sure that a key starts with the base by prepending if necessary
   */
  private String wrap(String s) {
    Preconditions.checkNotNull(s);
    if (s.startsWith(base + ".")) {
      return s;
    }
    return base + "." + s;
  }
  
  /**
   * Defines a valid required parameter for this object
   * @param name name of the parameter (without base-key)
   * @param type type to be parsed 
   */
  protected synchronized void registerParameter(
      String name, Type type) {
    Preconditions.checkNotNull(name);
    Preconditions.checkNotNull(type);
    configDef.put(wrap(name), type);
  }
  
  /**
   * Defines a valid optional parameter for this object
   */
  protected synchronized void registerParameter(
      String name, Type type, String defaultValue) {
    Preconditions.checkNotNull(name);
    Preconditions.checkNotNull(type);
    configDef.put(wrap(name), type);
    defaultValues.put(wrap(name), defaultValue);
  }
  
  /**
   * Sets the global parameter set for all Configurable instances.
   * Can be overridden locally using the setLocalConfig method
   */
  public static synchronized void setGlobalConfig(Properties config) {
    Preconditions.checkNotNull(config);
    PROPERTIES = config;
  }
  
  /**
   * Sets the global parameter set for all Configurable instances.
   * Can be overridden locally using the setLocalConfig method
   */
  public static synchronized void setGlobalConfig(File file) 
      throws IOException {
    Preconditions.checkNotNull(file);
    if (!file.exists()) {
      throw new IllegalArgumentException("Cannot find configuration file");
    }
    Properties props = new Properties();
    FileInputStream stream = new FileInputStream(file);
    props.load(stream);
    stream.close();
    setGlobalConfig(props);
  }
  
  /**
   * Parses a property entry (package visibility for testing purposes)
   */
  Object getValue(final String key) {
    Preconditions.checkNotNull(key);
    final String fullKey = wrap(key);
    if (!configDef.containsKey(fullKey)) {
      throw new IllegalArgumentException("Undefined key: " + key);
    }
    final Properties props = getProperties();
    String value = null;
    if (props.containsKey(fullKey)) {
      value = props.getProperty(fullKey);
    } else {
      if (!defaultValues.containsKey(fullKey)) {
        throw new NullPointerException(fullKey + " missing in configuration");        
      }
      value = defaultValues.get(fullKey);
    }
    return (value == null) ? null : configDef.get(fullKey).parse(value);
  }
  
  /**
   * Gets a registered parameter. Throws any exception defined in the
   * enum plus a potential ClassCastException 
   */
  public String getString(String key) {
    return (String) getValue(key);
  }
  
  /**
   * Gets a registered parameter. Throws any exception defined in the
   * enum plus a potential ClassCastException 
   */
  public Long getInteger(String key) {
    return (Long) getValue(key);
  }
  
  /**
   * Gets a registered parameter. Throws any exception defined in the
   * enum plus a potential ClassCastException 
   */
  public Boolean getBoolean(String key) {
    return (Boolean) getValue(key);
  }
  
  
}
 
