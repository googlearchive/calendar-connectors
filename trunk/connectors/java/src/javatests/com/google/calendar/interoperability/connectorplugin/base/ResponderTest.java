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

import static com.google.calendar.interoperability.connectorplugin.base.GwIo.FOLDER.HEADERS_OUT;
import static com.google.calendar.interoperability.connectorplugin.base.GwIo.FOLDER.LOG;

import com.google.calendar.interoperability.connectorplugin.base.GwIo;
import com.google.calendar.interoperability.connectorplugin.base.Responder;
import com.google.calendar.interoperability.connectorplugin.base.messages.GwResponse;
import com.google.calendar.interoperability.connectorplugin.base.messages.UnknownCommand;
import com.google.calendar.interoperability.connectorplugin.base.messages.VoidResponse;

import junit.framework.TestCase;

import org.jmock.Expectations;
import org.jmock.Mockery;

/**
 * Unit tests for the "responder" function
 */
public class ResponderTest extends TestCase {
  
  private Responder responder;
  private GwIo io;
  private Mockery context;
  private GwResponse response;
  
  @Override
  public void setUp() throws Exception {
    super.setUp();
    context = new Mockery();
    io = context.mock(GwIo.class);
    responder = new Responder(io, false);
    response = new GwResponse(new UnknownCommand("A", "B")) {
      @Override
      public String renderResponse() {
        return "response";
      }
    };
  }
  
  public void testNonVoid() {
    context.checking(new Expectations(){{
      exactly(1).of(io).store(
          with(equal(HEADERS_OUT)),
          with(equal("A")),
          with(aNonNull(byte[].class)));
      will(returnValue(true));
    }});
    responder.apply(response);
    context.assertIsSatisfied();
  }

  public void testVoid() {
    context.checking(new Expectations(){{}});
    responder.apply(
        new VoidResponse(response.getOriginalCommand(), "C", "D"));
    context.assertIsSatisfied();
  }
  
  public void testWriteFails() {
    context.checking(new Expectations(){{
      exactly(1).of(io).store(
          with(equal(HEADERS_OUT)),
          with(equal("A")),
          with(aNonNull(byte[].class)));
      will(returnValue(false));
    }});
    try {
      responder.apply(response);
      fail("Expected an exception");  // COV_NF_LINE
    } catch (RuntimeException e) {
      assertEquals("Could not write response, I/O problem?", e.getMessage());
    }
    context.assertIsSatisfied();
  }
  
  public void testLog() {
    responder = new Responder(io, true);
    context.checking(new Expectations(){{
      exactly(1).of(io).store(
          with(equal(LOG)),
          with(equal("processed_A")),
          with(aNonNull(byte[].class)));
      will(returnValue(true));
      exactly(1).of(io).store(
          with(equal(HEADERS_OUT)),
          with(equal("A")),
          with(aNonNull(byte[].class)));
      will(returnValue(true));
    }});
    responder.apply(response);
    context.assertIsSatisfied();    
  }
  
}
 
