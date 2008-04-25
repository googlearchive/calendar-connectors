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

package com.google.calendar.interoperability.connectorplugin;

import com.google.calendar.interoperability.connectorplugin.base.LdapUserFilter;
import com.google.calendar.interoperability.connectorplugin.base.PasswordLoader;

/**
 * Program to verify that the program has been configured correctly.
 * If the program exists with a code != 0, something went wrong
 */
public class SelfTest {
  
  public static void main(String[] args) {
    
    try {
    
      // Start with loading the configuration file
      System.out.println("Loading configuration...");
      String confFile = (args.length > 0) ? args[0] : 
        System.getProperties().getProperty("config", null);
      Main.setGlobalConfig(confFile, false);
      System.out.println("   ... Done");
      
      // See whether we can basically initialize the program
      System.out.println("Test base configuration...");
      Main connector = new Main();
      connector.setUp();
      if (connector.realInfrastructure == null) {
        System.out.println(
            "Connector is not configured to use the " +
            "Google Calendar Connector Plugin!!!");
        System.exit(1);
      }
      System.out.println("   ... Done");
      
      // Check the password encryption or obfuscation module module
      PasswordLoader.EncryptionStrategy.FAILOVER.selfTest();
      
      // Check the base directory
      System.out.println("Evaluating base directory...");
      if (connector.baseDirectory.startsWith("\\\\")) {
        System.out.println(
            "Base directory will not be accessible by windows service " +
            "(use local folder, not mapped drive): " + 
            connector.baseDirectory);
        System.exit(2);
      } else {
        System.out.println(
            "(Make sure that the Exchange side also uses " + 
            connector.baseDirectory + ")");     
      }    
      System.out.println("   ... Done");
      
      // Check the connectivity to Google
      System.out.println("Evaluating Connectivity to GData");
      connector.realInfrastructure.selfTest();
      System.out.println("   ... Done");
      
      // Check the LDAP filter
      System.out.println("Evaluating LDAP filtering");
      new LdapUserFilter().selfTest();
      System.out.println("   ... Done");
      
      // All tests passed :-)
      System.out.println("All tests passed -- you're good to go :-)");
      System.exit(0);

    } catch (Throwable t) {
      t.printStackTrace();
      System.exit(3);
    }
  }

}
 
