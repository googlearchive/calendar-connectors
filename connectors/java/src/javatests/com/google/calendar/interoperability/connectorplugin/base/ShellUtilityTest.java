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

import com.google.common.base.Nullable;

import junit.framework.TestCase;

import java.util.Properties;

/**
 * Unit tests for the ShellUtility
 */
public class ShellUtilityTest extends TestCase {
  
  private Properties config;
  private int runCount = 0;
  
  @Override
  public void setUp() {
    config = new Properties();
    config.setProperty("tst.command", "ls -l");
    config.setProperty("tst.timeout", "50000");
    config.setProperty("tst.frequency", "1");
  }
  
  /**
   * Stubs out the runCommand method to make the class independent of
   * threading and java.lang.Runtime
   */
  private class StubbedUtility extends ShellUtility {

    private int testCase = 0;
    
    public StubbedUtility(String base, int testNumber) {
      super(base);
      this.testCase = testNumber;
    }
    
    @Override
    boolean sleep(int timeInMilliseconds) {
      if (timeInMilliseconds != 500) {
        assertEquals(
            config.getProperty("tst.frequency"), 
            "" + (timeInMilliseconds / 1000));
      }
      return true;
    }
    
    @Override
    int runCommand(
        String command, int timeoutInSeconds, 
        @Nullable StringBuilder out, @Nullable StringBuilder err) {      
      runCount++;      
      switch(testCase) {
        case 1: // we expect the method to be never called
          fail("Statement should not be reached");
          break;
        case 2: // the command should not be called any more
          config.setProperty("tst.frequency", "0");
          testCase--;
          break;
        case 3: // the command should be called only twice
          testCase--;
          break;
        case 4: // the command should only be called once
          testCase = 1;
          break;
      }
      
      // No special handling for the test case
      return 0;
    }    
  }
  
  public void testDoNothingIfCommandIsNotSet() {
    config.remove("tst.command");
    StubbedUtility util = new StubbedUtility("tst", 1);
    util.setLocalConfig(config);
    util.run();
    assertEquals(0, runCount);
  }
  
  public void testRegularCycle() {    
    StubbedUtility util = new StubbedUtility("tst", 3);
    util.setLocalConfig(config);
    util.run();
    assertEquals(2, runCount);
  }
  
  public void testCallOnlyOnce() {
    config.setProperty("tst.frequency", "0");
    StubbedUtility util = new StubbedUtility("tst", 4);
    util.setLocalConfig(config);
    util.run();    
    assertEquals(1, runCount);
  }

}
 
