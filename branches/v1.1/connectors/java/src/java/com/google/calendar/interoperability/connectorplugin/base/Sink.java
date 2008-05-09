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

/**
 * Accepts incoming objects and enqueues them for a SEDA stage
 * to process.
 */
public interface Sink<T> {
  
  /**
   * Accepts an object for further processing
   */
  public void accept(T t);
  
  /**
   * Requests an object to work on from the sink. The sink should mark
   * the object as "being processed" and not return it again until either
   * reportSuccess or reportFailure were called. Returns null if there
   * is currently nothing that could be checked out. A sink may choose to
   * block a requesting thread until something becomes available.
   */
  public T checkOut();
  
  /**
   * Reports that an object was processed successfully
   */
  public void reportSuccess(T processedObject);
  
  /**
   * Reports that an object could not be processed
   */
  public void reportFailure(T processedObject, @Nullable Throwable t);

}
 
