/* Copyright (c) 2008 Google Inc.
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
package com.google.calendar.interoperability.connectorplugin.impl.google;


import junit.framework.TestCase;

import java.util.concurrent.TimeUnit;


/**
 * Unit tests for the ConnectionThrottle
 * 
 */
public class ConnectionThrottleTest extends TestCase {
  
  private class TestThrottle extends ConnectionThrottle {    
    @Override
    public long getTime() {
      return ConnectionThrottleTest.this.time;
    }    
  }
  
  private TestThrottle testObject;
  private long time;
  
  @Override
  public void setUp() {
    testObject = new TestThrottle();
    testObject.blockTimeInMilliSeconds = 1;
    testObject.maxRandomTimeDeviationInPercent = 0;
  }
  
  public void testMaxRequestThrottling() {
    
    // Initialize the queue. All timeouts should be set to "now" (time 0)
    assertNull(testObject.queue);
    final int amount = 5;
    testObject.setMaxRequestsPerSecond(amount);
    assertEquals(amount, testObject.queue.size());
    for(int i = 0; i < amount; i++) {
      assertEquals(0, testObject.queue.peek().getDelay(TimeUnit.SECONDS));
      testObject.checkoutTimer();
    }
    assertEquals(0, testObject.queue.size());
    
    // Add a new element to the queue. This should now be 1000 milliseconds
    // in the future
    time = 10;
    testObject.rewindTimer();
    assertEquals(1, testObject.queue.size());
    assertEquals(1000, testObject.queue.peek().getDelay(TimeUnit.MILLISECONDS));
    time = 20;
    assertEquals(990, testObject.queue.peek().getDelay(TimeUnit.MILLISECONDS));
    testObject.rewindTimer();
    assertEquals(2, testObject.queue.size());
    
    // Currently, all connections should be blocked. Make sure that is the case
    try {
      testObject.checkoutTimer();
    } catch (RuntimeException e) {
      assertEquals(RuntimeException.class, e.getClass());
    }
    
    // Add a second object and make sure that it deviates 10 seconds from the
    // first one
    time = 1010;
    assertEquals(0, testObject.queue.peek().getDelay(TimeUnit.MILLISECONDS));
    testObject.checkoutTimer();
    assertEquals(1, testObject.queue.size());
    assertEquals(10, testObject.queue.peek().getDelay(TimeUnit.MILLISECONDS));
  }
  
  /**
   * Tests that the expected delay changes, depending on how many consecutive
   * errors have been reported.
   */
  public void testGetDelayInMillisAndTargetQueueSize() {
    testObject.setMaxRequestsPerSecond(3);
    final int[][] errorsAndDelays = {
        {0, 1000, 3},
        {1, 10, 1},
        {2, 20, 1},
        {3, 40, 1},
        {4, 80, 1},
        {5, 160, 1},
        {6, 320, 1},
        {7, 640, 1},
        {8, 1280, 1},
        {9, 2560, 1},
        {10, 5120, 1},
        {11, 10240, 1},
        {12, 20480, 1},
        {13, 40960, 1},
        {14, 40960, 1},
        {-1, 40960, 1},
        {1000000000, 40960, 1},
    };
    for(int[] testCase : errorsAndDelays) {
      testObject.numErrors = testCase[0];
      assertEquals("Failed for " + testCase[0] + " errors.", 
          testCase[1], testObject.getDelayInMillis());
      assertEquals("Failed for " + testCase[0] + " errors.", 
          testCase[2], testObject.getTargetQueueSize());
    }
  }
  
  /**
   * Tests that the timer-objects in the queue change their delay, depending
   * on how many successes and failures get reported
   */
  public void testReportFailureAndSuccess() {
    
    // Initialize
    testObject.setMaxRequestsPerSecond(2);
    testObject.checkoutTimer();
    testObject.checkoutTimer();
    testObject.rewindTimer();
    testObject.rewindTimer();
    assertEquals(2, testObject.queue.size());
    assertEquals(1, testObject.queue.peek().getDelay(TimeUnit.SECONDS));
    
    // Success should not change the queue size or time
    testObject.reportSuccess();
    assertEquals(2, testObject.queue.size());
    assertEquals(1, testObject.queue.peek().getDelay(TimeUnit.SECONDS));

    // The first failure should shrink the queue to 1 and modify the timeout     
    testObject.reportFailure();
    assertEquals(1, testObject.getTargetQueueSize());
    assertEquals(1, testObject.queue.size());
    assertEquals(10, testObject.queue.peek().getDelay(TimeUnit.MILLISECONDS));

    // The second failure should increase the timeout        
    testObject.reportFailure();
    assertEquals(1, testObject.queue.size());
    assertEquals(20, testObject.queue.peek().getDelay(TimeUnit.MILLISECONDS));
    
    // Another success should get the queue into the original state...
    testObject.reportSuccess();
    assertEquals(2, testObject.queue.size());
    assertEquals(1, testObject.queue.peek().getDelay(TimeUnit.SECONDS));
    
    // ...but does this also work when an object was checked out?
    testObject.reportFailure();
    testObject.reportFailure();
    testObject.reportFailure();
    assertEquals(1, testObject.getTargetQueueSize());
    assertEquals(1, testObject.queue.size());
    this.time += 30000;
    testObject.checkoutTimer();
    assertEquals(0, testObject.queue.size());
    testObject.reportSuccess();
    assertEquals(2, testObject.getTargetQueueSize());
    assertEquals(1, testObject.queue.size());
    testObject.rewindTimer();
    assertEquals(2, testObject.queue.size());
    
    // And are we sure that we cannot "overstuff" the queue?
    testObject.rewindTimer();
    assertEquals(2, testObject.queue.size());
  }
  
  /**
   * Tests how the throttle behaves if setMaxRequestsPerSecond was not called.
   * We should be able to "check out" and "rewind" as many timers as we please.
   */
  public void testSemiInitializedObject() {
    for (int i = 0; i < 10000; i++) {
      testObject.checkoutTimer();
    }
    for (int i = 0; i < 10000; i++) {
      testObject.rewindTimer();
    }
    for (int i = 0; i < 10000; i++) {
      testObject.reportSuccess();
    }
    for (int i = 0; i < 10000; i++) {
      testObject.reportFailure();
    }
  }
  
  /**
   * Tests that there is random deviation in the request timers
   */
  public void testRandomDeviation() {
    testObject.maxRandomTimeDeviationInPercent = 25;
    testObject.setMaxRequestsPerSecond(2);
    testObject.checkoutTimer();
    testObject.checkoutTimer();
    testObject.rewindTimer();
    testObject.rewindTimer();
    assertEquals(2, testObject.queue.size());
    for (int i = 0; i < 6; i++) {
      testObject.reportFailure();
    }
    assertEquals(1, testObject.queue.size());
    
    // The "standard" delay is now 320 milliseconds, but that does not mean
    // that the regular delay should be...
    assertEquals(320, testObject.getDelayInMillis());
    for(int i = 0; i < 100; i++) {
      long time = testObject.queue.peek().getDelay(TimeUnit.MILLISECONDS);
      assertTrue(time > 0);
      if (time == testObject.getDelayInMillis()) {
        testObject.rebuildQueue(false);
        continue;
      }
      final double deviation = Math.abs(120000.0 - time) / 120000.0;
      assertTrue("Too close to the average time", deviation > 0);
      assertTrue("Too far off", deviation <= 25);
    }
  }

}
 
