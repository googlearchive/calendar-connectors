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

import java.util.concurrent.BlockingQueue;
import java.util.concurrent.LinkedBlockingQueue;

/**
 * Base class for simple sink implementations: assumes that elements should
 * be processed in the order in which they arrive. If an element fails to
 * process, it will be re-enqueued at the end of the queue. This sink is
 * non-blocking for accept but blocking for checkOut
 */
public class SimpleSink<S> implements Sink<S> {
  
  private int penaltyInMilliseconds = 5000;
  private BlockingQueue<S> queue = new LinkedBlockingQueue<S>();

  public void accept(S t) {
    try {
      queue.put(t);
    } catch (InterruptedException e) {
      throw new AssertionError
          ("LinkedBlockingQueue should have a non-blocking put");
    }
  }

  public S checkOut() {
    try {
      return queue.take();
    } catch (InterruptedException e) {
      return null;
    }
  }

  /** 
   * Re-enqueues the failed object to try it again later. Before re-enqueuing
   * the obejct, it will wait a short time (defined in the hidden field
   * "penaltyInMilliseconds", default is 5 seconds) to give the component
   * that caused the problem some time to recover.
   */
  public void reportFailure(final S processedObject, @Nullable Throwable t) {
    new Thread() {
      @Override
      public void run() {
        try {
          Thread.sleep(penaltyInMilliseconds);
        } catch (InterruptedException e) {
          
          // No special handling required if we get interrupted
        }
        accept(processedObject);
      }
    }.start();    
  }

  /**
   * Currently, this method does not do anything. It can be overwritten
   * for special handling.
   */
  public void reportSuccess(S processedObject) {
    // Do nothing
  }

  /**
   * Sets the "penalty" (time in milliseconds) that this sink should wait
   * before putting a failed command back into the queue. 
   * Default is five seconds.
   */
  public void setPenaltyInMilliseconds(int penaltyInMilliseconds) {
    this.penaltyInMilliseconds = penaltyInMilliseconds;
  }

}
 
