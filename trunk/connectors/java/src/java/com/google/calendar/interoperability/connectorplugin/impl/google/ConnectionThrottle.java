/* Copyright (c) 20087 Google Inc.
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

import static com.google.calendar.interoperability.connectorplugin.base.Tuple.of;

import com.google.calendar.interoperability.connectorplugin.base.Tuple;
import com.google.common.collect.Lists;

import java.util.List;
import java.util.concurrent.DelayQueue;
import java.util.concurrent.Delayed;
import java.util.concurrent.TimeUnit;
import java.util.logging.Level;
import java.util.logging.Logger;

/**
 * A utility class used by the GDataAccessObject to control how quickly
 * in succession connections can be established. Internally, this object uses
 * a DelayQueue to remember a timeout for every "connection" established.
 * If the amount of errors increase, the timeouts will get rewound with
 * a higher value.
 */
class ConnectionThrottle {
  
  // List of Tuples <x,y>. If x or more consecutive requests fail,
  // wait for about y seconds (+/- randomTime) before retrying anything
  private static final List<Tuple<Integer>> escalationScale = 
    Lists.immutableList(of(2, 30), of(4, 60), of(6, 120), of(8, 300), 
                        of(10, 1500), of(12, 3000));
  
  private static final Logger LOGGER = 
    Logger.getLogger(ConnectionThrottle.class.getName());

  
  /**
   * A delayed object that can stay in a queue as long as we need to.
   * Visible for unit tests.
   */
  class Timer implements Delayed {
    
    private long utcTime;
    
    public Timer(long utcTime) {
      this.utcTime = utcTime;
    }

    public long getDelay(TimeUnit unit) {
      final long result = Math.max(utcTime - getTime(), 0); 
      return unit.convert(result, TimeUnit.MILLISECONDS);
    }

    public int compareTo(Delayed o) {
      return (int) 
          (getDelay(TimeUnit.MILLISECONDS) - o.getDelay(TimeUnit.MILLISECONDS));
    }    
  }
  
  // A queue of timers -- whenever a timer is expired, it will be possible
  // to get it out of the queue (== become available)
  // Visible for testing
  DelayQueue<Timer> queue;
  
  // Timeout to block for a timer to become available (in seconds) in the queue
  int blockTimeInMilliSeconds = 360000;
  
  // Amount of consecutive errors recorded in this object
  int numErrors;
  
  // How much deviation should be in the random retries?
  double maxRandomTimeDeviationInPercent = 25;
  
  // What's the permissible maximum number of requests per second ?
  private int maxRequestsPerSecond;
  
  /**
   * This method is called whenever the delay in the DelayQueue should be
   * rewound (usually if the failures go through a certain threshold)
   */
  void rebuildQueue(boolean refillAfterError) {
    LOGGER.log(Level.INFO, 
        "Changing connection throttle to " + (getDelayInMillis() / 1000) +
        " seconds.");
    int numEntries = Math.min(queue.size(), getTargetQueueSize());
    if (refillAfterError) {
      numEntries = queue.isEmpty() ? 
          getTargetQueueSize() - 1 : getTargetQueueSize();
    }
    queue.clear();
    for(int i = 0; i < numEntries; i++) {
      rewindTimer();
    }
  }
  
  /**
   * Gets the current system time. Will be overwritten for unit tests.
   */
  long getTime() {
    return System.currentTimeMillis();
  }
  
  /**
   * Gets the delay in milliseconds that should be applied to a blocker
   * that gets put into the queue (depending on the currently recorded
   * amounto of errors)
   */
  int getDelayInMillis() {
    final int border = numErrors;
    if (border < 0) { // possible integer overflow
      return escalationScale.get(escalationScale.size() - 1).second * 1000;
    }
    int result = 1000;
    for(Tuple<Integer> entry : escalationScale) {
      if (entry.first > border) {
        return result;
      }
      result = entry.second * 1000;
    }
    return result;
  }
  
  int getTargetQueueSize() {
    if (this.numErrors < 0) {
      return 1;
    }
    if (this.numErrors < escalationScale.get(0).first) {
      return this.maxRequestsPerSecond;
    }
    return 1;
  }
  
  /**
   * Sets the maximum amount of requests permitted per second. 
   * This method should only be used during the initialization period of
   * the object, since it erases all currently known timers
   */
  public synchronized void setMaxRequestsPerSecond(int max) {
    this.maxRequestsPerSecond = max;
    final long timeout = getTime();
    if (queue == null) {
      queue = new DelayQueue<Timer>();
    } else {
      queue.clear();
    }
    for (int i = 0; i < max; i++) {
      queue.add(new Timer(timeout));
    }
  }
  
  /** 
   * using the internal queue, make sure that the overall amount of requests
   * is properly throttled
   */
  public void checkoutTimer() {
    if (queue == null) {
      return;
    }
    long time = getTime();
    try {
      Timer object = queue.poll(
          blockTimeInMilliSeconds, TimeUnit.MILLISECONDS);
      if (object == null) {
        throw new RuntimeException("Could not get connection in time");        
      }
    } catch (InterruptedException e) {
      throw new RuntimeException("Could not get connection in time");
    }
    finally {
      LOGGER.log(Level.FINE, 
          "Checkout took " + (getTime() - time) + " milliseconds.");
    }
  }
  
  /** 
   * using the internal queue, make sure that the overall amount of requests
   * is properly throttled
   */
  public synchronized void rewindTimer() {
    if (queue == null) {
      return;
    }
    if (queue.size() >= getTargetQueueSize()) {
      return;
    }
    final int delay = getDelayInMillis();
    final int randomDeviation = (int)
        (delay * maxRandomTimeDeviationInPercent * (0.5 - Math.random()) / 50);
    queue.add(new Timer(getTime() + delay + randomDeviation));
  }
  
  /**
   * This method is called whenever a call went through successfully, thus
   * "decreasing" the potential wait level
   */
  public synchronized void reportSuccess() {
    if (queue == null) {
      return;
    }
    final int oldDelay = getDelayInMillis();
    numErrors = 0;
    if (getDelayInMillis() != oldDelay) {
      rebuildQueue(true);
    }
  }
  
  /**
   * This method is called whenever a call went through successfully, thus
   * potentially increasing the wait level
   */
  public synchronized void reportFailure() {
    if (queue == null) {
      return;
    }
    final int oldDelay = getDelayInMillis();
    numErrors++;
    if (getDelayInMillis() != oldDelay) {
      rebuildQueue(false);
    }
  }

}
 
