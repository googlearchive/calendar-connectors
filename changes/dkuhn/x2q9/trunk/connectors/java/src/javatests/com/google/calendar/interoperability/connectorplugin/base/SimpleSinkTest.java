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

import com.google.calendar.interoperability.connectorplugin.base.SimpleSink;

import junit.framework.TestCase;

/**
 * Unit tests for the simple sink
 */
public class SimpleSinkTest extends TestCase {
  
  public void testClass() {
    SimpleSink<String> sink = new SimpleSink<String>();

    // There is no race condition here -- thanks to the BlockingQueue, the
    // penalty can be as high as we like. However, setting it to a single
    // millisecond makes this test run much faster :-)
    sink.setPenaltyInMilliseconds(1);   
    sink.accept("Hello");
    sink.accept("World");
    assertEquals("Hello", sink.checkOut());
    assertEquals("World", sink.checkOut());
    sink.reportSuccess("Hello");
    sink.reportFailure("World", null);
    assertEquals("World", sink.checkOut());
  }

}
 
