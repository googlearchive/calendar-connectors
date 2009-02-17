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
 * A class that can be used as an endpoint of a SEDA queue
 * (simply discards anything added).
 */
public final class NullSink<S> implements Sink<S> {

  public void accept(S t) {    
    // Nothing to do; the sink just discards the element
  }

  public S checkOut() {
    throw new UnsupportedOperationException();
  }

  public void reportFailure(S processedObject, @Nullable Throwable t) {
    throw new UnsupportedOperationException();
  }

  public void reportSuccess(S processedObject) {
    throw new UnsupportedOperationException();
  }

}
 
