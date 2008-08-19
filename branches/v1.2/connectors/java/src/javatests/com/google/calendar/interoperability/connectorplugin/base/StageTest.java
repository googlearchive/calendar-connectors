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

import com.google.calendar.interoperability.connectorplugin.base.Sink;
import com.google.calendar.interoperability.connectorplugin.base.Stage;
import com.google.common.base.Function;

import junit.framework.TestCase;

import org.jmock.Expectations;
import org.jmock.Mockery;

/**
 * Tests the anonymous stage class
 */
public class StageTest extends TestCase {
  
  private Mockery context;
  private Sink<String> in;
  private Sink<Integer> out;
  private Function<String, Integer> processor;
  private Stage<String, Integer> stage;
  private RuntimeException exception;
  
  @SuppressWarnings("unchecked")
  @Override
  public void setUp() {
    context = new Mockery();
    in = context.mock(Sink.class, "in_sink");
    out = context.mock(Sink.class, "out_sink");
    processor = context.mock(Function.class);
    stage = new Stage(in, out, processor){};
    exception = new RuntimeException("expected");
  }
  
  public void testRegularCase() {
    context.checking(new Expectations(){{
      exactly(1).of(in).checkOut();
      will(returnValue("A"));
      exactly(1).of(processor).apply("A");
      will(returnValue(1));
      exactly(1).of(out).accept(1);
      exactly(1).of(in).reportSuccess("A");
    }});
    stage.processSingleElement();
    context.assertIsSatisfied();
  }
  
  public void testNullInput() {
    context.checking(new Expectations(){{
      exactly(1).of(in).checkOut();
      will(returnValue(null));
    }});
    stage.processSingleElement();
    context.assertIsSatisfied();    
  }
  
  public void testProcessingFailure() {
    context.checking(new Expectations(){{
      exactly(1).of(in).checkOut();
      will(returnValue("A"));
      exactly(1).of(processor).apply("A");
      will(throwException(exception));
      exactly(1).of(in).reportFailure("A", exception);
    }});
    stage.processSingleElement();
    context.assertIsSatisfied();    
  }

}
 
