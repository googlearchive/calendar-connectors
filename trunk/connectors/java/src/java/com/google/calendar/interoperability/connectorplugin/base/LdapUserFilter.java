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
import static com.google.calendar.interoperability.connectorplugin.base.Configurable.Type.string;

import com.google.common.base.Nullable;

import java.util.Arrays;
import java.util.HashSet;
import java.util.Hashtable;
import java.util.Iterator;
import java.util.Properties;
import java.util.Set;
import java.util.logging.Level;
import java.util.logging.Logger;

import javax.naming.Context;
import javax.naming.NamingEnumeration;
import javax.naming.NamingException;
import javax.naming.directory.Attributes;
import javax.naming.directory.DirContext;
import javax.naming.directory.SearchControls;
import javax.naming.directory.SearchResult;
import javax.naming.ldap.InitialLdapContext;

/**
 * This class contains knowledge about rules when to filter out
 * a user and not pass him along in a DirSync operation. The filter
 * will look for a blacklist and/or whitelist query and use that
 * information to decide on a per user basis. If nothing is
 * specified, it will let through all users
 */
public class LdapUserFilter extends Configurable implements SelfTestable {
  
  private static final Logger LOGGER = 
    Logger.getLogger(LdapUserFilter.class.getName()); 
  
  /**
   * the url to be used for the ldap connection, e.g. LDAPS://myserver.org
   */
  public static final String LDAP_URL = "ldap.url";
  
  /**
   * The ldap authentication method, like simple or DIGEST-MD5 (default)
   */
  public static final String LDAP_AUTH = "ldap.authMethod";
  
  /**
   * The user to login with to LDAP
   */
  public static final String LDAP_USER = "ldap.user";
  
  /**
   * The password to login with to LDAP
   */
  public static final String LDAP_PASSWORD = "ldap.password";
  
  /**
   * The base for the LDAP query
   */
  public static final String LDAP_BASE = "ldap.base";
  
  /**
   * An LDAP filter. If set, all email addresses matching the filter will
   *   be removed from the iterator.
   */  
  public static final String LDAP_BLACKLIST = "ldap.blacklist";
  
  /**
   * An LDAP filter. If set, all email addresses not matching the filter will
   *   be removed from the iterator.
   */  
  public static final String LDAP_WHITELIST = "ldap.whitelist";  
 
  /**
   * If set to true (default is false), replace the default TrustManager
   *   in https connections to accept incoming certificates for LDAP.
   */  
  public static final String LDAP_TRUST_ALL = "ldap.blindFaith";
  
  /**
   * An empty string, used as default for some of the parameters.
   */
  private static final String NONE = "";
  
  /**
   * This interface represents the connection logic for LDAP (replaced by
   * mocks in unit tests). For concurrency reasons, all LDAP operations will
   * always be executed on a clone of this object.
   */
  public static interface Connector extends Cloneable {
    
    /**
     * Log into LDAP using a given url, user and password
     * @return true if the connection was successful
     */
    public boolean login(
        String url, String authMethod,
        String user, String password);
    
    /**
     * Closes the LDAP connection
     */
    public void close();
    
    /**
     * Performs an LDAP query on the users of a particular searchbase. May
     * return null if the query failed due to some programming error or
     * connection issues/
     */
    public Iterable<String> searchEmails(String searchBase, String filter);
    
    /**
     * Creates a clone of this object -- this is necessary since the connectors
     * are not guaranteed to be threadsafe
     */
    public Connector clone();
  }
  
  /**
   * LDAP-related functionality, can be replaced for unit tests.
   */
  private class StandardConnector implements Connector {
    
    private DirContext context;

    public void close() {
     try {
      context.close();
     } catch (NamingException e) {
      LOGGER.log(Level.FINE, "Ldap-close failed with Exception", e);
     }
    }

    public boolean login(
        String url, String authMethod, 
        String user, String password) {
      Hashtable<String, String> env = new Hashtable<String, String>();    
      env.put(
          Context.INITIAL_CONTEXT_FACTORY, "com.sun.jndi.ldap.LdapCtxFactory");
      env.put(Context.PROVIDER_URL, url);     
      env.put(Context.SECURITY_AUTHENTICATION, authMethod);
      env.put(Context.SECURITY_PRINCIPAL, user);
      env.put(Context.SECURITY_CREDENTIALS, password);
      if (url.toUpperCase().startsWith("LDAPS") && 
          getBoolean(LDAP_TRUST_ALL)) {
        LOGGER.log(Level.FINE, "Disabling certificate validation for LDAP");
        env.put(
            "java.naming.ldap.factory.socket", 
            HackedSSLSocketFactory.class.getName());
      }
      try {
        context = new InitialLdapContext(env, null);
      } catch (NamingException e) {
        LOGGER.log(Level.FINE, "Ldap-login failed with Exception", e);
        LOGGER.log(
            Level.WARNING, "Could not login to LDAP: " + e.getMessage());
        return false;
      }
      return true;
    }

    public Iterable<String> searchEmails(String searchBase, String filter) {
      if (context == null) {
        LOGGER.log(Level.FINE, "Ldap-query without login");
        return null;
      }
      LOGGER.log(Level.FINE, 
          "Performing LDAP Query " + filter + " on base " + searchBase);
      SearchControls searchCtls = new SearchControls();
      searchCtls.setSearchScope(SearchControls.SUBTREE_SCOPE);
      searchCtls.setReturningAttributes(new String[]{"mail"});
      Set<String> result = new HashSet<String>();
      try {
        for (NamingEnumeration<SearchResult> answer = 
                 context.search(searchBase, filter, searchCtls); 
             answer.hasMoreElements(); ) {
          SearchResult sr = answer.next();
          Attributes attrs = sr.getAttributes();
          if (attrs != null && 
              attrs.get("mail") != null && 
              attrs.get("mail").get() != null) {
            result.add(attrs.get("mail").get().toString().toUpperCase());
          }
        }
      } catch (NamingException e) {
        LOGGER.log(Level.FINE, "Ldap-query failed with Exception", e);
        LOGGER.log(Level.WARNING, "Could not query LDAP: " + e.getMessage());
        return null;        
      }
      LOGGER.log(Level.FINE, "returning " + result.size() + " results");
      return result;
    }
    
    @Override
    public Connector clone() {
      return new StandardConnector();
    }    
  }

  private Connector connector;
  
  /**
   * Constructor visible for testing only
   */
  LdapUserFilter(Connector connector, Properties configuration) {
    super("ldap");
    this.connector = connector;    
    if (configuration != null) {
      setLocalConfig(configuration);
    }
    this.registerParameter(LDAP_URL, string);
    this.registerParameter(LDAP_AUTH, string, "DIGEST-MD5");
    this.registerParameter(LDAP_USER, string);
    this.registerParameter(LDAP_PASSWORD, string);
    this.registerParameter(LDAP_BASE, string);
    this.registerParameter(LDAP_BLACKLIST, string, NONE);
    this.registerParameter(LDAP_WHITELIST, string, NONE);
    this.registerParameter(LDAP_TRUST_ALL, bool, "false");
  }
  
  /**
   * Constructor
   */
  public LdapUserFilter() {
    this(null, null);
    if (this.connector == null) {
      this.connector = new StandardConnector();
    }
  }
  
  /** 
   * Helper: executes one of the queries
   */
  private boolean query(
      @Nullable String query, Set<String> result, Connector c) {
    
    // Nothing to do?
    if (query == null) {
      return true;
    }

    // Perform the query and see what happens
    Iterable<String> response = c.searchEmails(getString(LDAP_BASE), query);
    if (response == null) {
      return false;
    }
    
    // Refill the set
    result.clear();
    for (String email : response) {
      result.add(email.toUpperCase());
    }
    return true;
  }

  /**
   * Connects to ldap and performs the blacklist/whitelist queries.
   * Copies the results into the sets passed along. If one of the query
   * strings is null, the query will not be executed. If both are null,
   * no connection to LDAP will be made. If any of the querystrings is
   * null, the corresponding set may also be null 
   * Method is visible for testing
   * @return true if nothing bad happened while querying
   */
  boolean executeQuery(
      @Nullable String blacklistQuery, @Nullable Set<String> blacklist,
      @Nullable String whitelistQuery, @Nullable Set<String> whitelist) {
    if (blacklistQuery == null && whitelistQuery == null) {
      return true;
    }
    Connector c = connector.clone();
    if (!c.login(
        getString(LDAP_URL), getString(LDAP_AUTH), 
        getString(LDAP_USER), getString(LDAP_PASSWORD))) {
      return false;
    }
    try {
      if (!query(blacklistQuery, blacklist, c)) {
        return false;
      }
      if (!query(whitelistQuery, whitelist, c)) {
        return false;
      }
      return true;
    } finally {
      c.close();
    }
  }
  
  /**
   * Given a blacklist and whitelist of email addresses (all upper case),
   * iterate through other emails and call the remove-method on the iterator
   * whenever an email should not be in there. If blacklist or whitelist
   * should not be used, that set should be passed as null.
   */
  static void doFilter
      (@Nullable Set<String> blacklist, 
       @Nullable Set<String> whitelist,
       Iterator<String> emails) {
    while (emails.hasNext()) {
      String email = emails.next().toUpperCase();
      
      // Whitelist check
      if (whitelist != null && !whitelist.contains(email)) {
        emails.remove();
        continue;
      }
      
      // Blacklist check
      if (blacklist != null && blacklist.contains(email)) {
        emails.remove();
        continue;
      }      
    }
  }
  
  /**
   * Queries the ldap host for a current snapshot of the blacklist 
   * and whitelist and removes all emails from the given iterator that
   * do not match the pattern.
   * @param emails an iterator over emails to check, for instance a key set
   *   of a Map. This way, removing elements through the iterator will also
   *   remove them from the Map.
   * @return true if the operation completed successfully
   */
  public boolean doFilter(Iterator<String> emails) {
    
    // Load filters from config
    String blacklistQuery = getString(LDAP_BLACKLIST).trim();
    String whitelistQuery = getString(LDAP_WHITELIST).trim();
    Set<String> blacklist = new HashSet<String>();
    Set<String> whitelist = new HashSet<String>();
    if (blacklistQuery.equals(NONE)) {
      blacklistQuery = null;
      blacklist = null;
    }
    if (whitelistQuery.equals(NONE)) {
      whitelistQuery = null;
      whitelist = null;
    }
    
    // Perform query and filter
    if (!executeQuery(blacklistQuery, blacklist, whitelistQuery, whitelist)) {
      return false;
    }
    doFilter(blacklist, whitelist, emails);
    return true;
  }
  
  /**
   * Performs a quick check whether there is anything that prevents the 
   * component from functioning. Throws an exception if that is the case
   */
  public void selfTest() {
    String blacklistQuery = getString(LDAP_BLACKLIST).trim();
    String whitelistQuery = getString(LDAP_WHITELIST).trim();
    if (blacklistQuery.equals(NONE) && whitelistQuery.equals(NONE)) {
      System.out.println("LDAP filter not used");
      return;
    }
    Set<String> dummy = new HashSet<String>(Arrays.asList("A","B"));
    if (!doFilter(dummy.iterator())) {
      throw new RuntimeException("LDAP filter possibly misconfigured");
    }
  }
}
 
