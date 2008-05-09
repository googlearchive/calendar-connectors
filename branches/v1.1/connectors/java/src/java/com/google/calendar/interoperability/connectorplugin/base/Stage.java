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

import java.util.logging.Level;
import java.util.logging.Logger;

/**
 * A stage in a SEDA architecture. Takes elements out of a sink and processes
 * them. This class abstracts the logic of processing a single element but
 * does not contain the logic of staging things in parallel (single-threaded
 * vs multi-threaded, keeping things in order etc). See subclasses of the
 * stage for details.
 */
public abstract class Stage<S, T> {
  
  private static final Logger LOGGER = Logger.getLogger(Stage.class.getName());
  
  private Sink<S> inQueue;
  private Sink<T> outQueue;
  private Function<S, T> processor;
  
  /**
   * @param inQueue the queue that the stage should fetch elements to process
   *   from
   * @param outQueue the queue that the stage puts elements after having
   *   processed them
   * @param processor a function that encapsulates the processing logic. This
   *   function should be free of side effects in order to facilitate parallel
   *   processing. If the function is not free of side effects, this should be
   *   clearly stated in order to make the user of the function choose a
   *   single threaded stage model
   */
  public Stage(Sink<S> inQueue, Sink<T> outQueue, Function<S, T> processor) {
    super();
    Preconditions.checkNotNull(inQueue);
    Preconditions.checkNotNull(outQueue);
    Preconditions.checkNotNull(processor);
    this.inQueue = inQueue;
    this.outQueue = outQueue;
    this.processor = processor;
  }
  
  /**
   * Fetches a single element from the inqueue, processes it and puts it into
   * the outqueue. This method catches all exceptions that could happen
   * internally and logs them (without propagating them). It depends on
   * the implementation of the sink to decide what to do when an error is
   * reported.
   * 
   * @return true if an element from the sink was processed
   */
  protected boolean processSingleElement() {
    
    // Check out an element to process
    S processThis = null;
    try {
      processThis = inQueue.checkOut();
    } catch (Throwable t) {
      LOGGER.log(Level.WARNING, "Could not check out from sink", t);
      return false;
    }
    if (processThis == null) {
      return false;
    }
    
    // Process the element and enqueue the result
    boolean ok = false;
    Throwable problem = null;
    try {
      T processingResult = processor.apply(processThis);
      outQueue.accept(processingResult);
      ok = true;
    } catch (Throwable t) {
      LOGGER.log(Level.WARNING, "Processing of object failed", t);
      problem = t;
    }
    
    // Notify the incoming sink
    try {
      if (ok) {
        inQueue.reportSuccess(processThis);
      } else {
        inQueue.reportFailure(processThis, problem);
      }
    } catch (Throwable t) {
      LOGGER.log(Level.WARNING, "Could not notify incoming sink", t);
    }
    return true;     
  }

}
 
