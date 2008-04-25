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

import com.google.common.base.Function;
import com.google.common.base.Preconditions;

/**
 * A simple stage implementation that uses a bunch of parallel daemon threads
 * and assumes they never crash
 */
public class SimpleStage<S, T> extends Stage<S, T> {
  
  private ThreadGroup threadGroup;

  public SimpleStage(
      Sink<S> inQueue, 
      Sink<T> outQueue, 
      Function<S, T> processor,
      int numberOfThreads,
      String threadGroupName) {
    super(inQueue, outQueue, processor);
    Preconditions.checkNotNull(threadGroupName);
    threadGroup = new ThreadGroup(threadGroupName);
    threadGroup.setDaemon(true);
    for (int i = 0; i < numberOfThreads; i++) {
      new Thread(threadGroup, threadGroupName + '@' + i) {
        @Override
        public void run() {
          runForever();
        }
      }.start();
    }
  }
  
  /**
   * Processes elements in an endless loop until the thread gets
   * interruped
   */
  private void runForever() {
    while (!Thread.interrupted()) {
      if (!processSingleElement()) {
        try {
          Thread.sleep(500);
        } catch (InterruptedException e) {
          return;
        }
      }
    }
  }

}
 
