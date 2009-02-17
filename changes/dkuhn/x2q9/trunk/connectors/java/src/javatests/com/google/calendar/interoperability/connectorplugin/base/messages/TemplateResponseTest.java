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

package com.google.calendar.interoperability.connectorplugin.base.messages;

import com.google.calendar.interoperability.connectorplugin.base.messages.TemplateResponse;
import com.google.calendar.interoperability.connectorplugin.base.messages.UnknownCommand;

import junit.framework.TestCase;

import java.lang.reflect.InvocationTargetException;

/**
 * Unit tests for TemplateResponse
 */
public class TemplateResponseTest extends TestCase {
  
  private UnknownCommand command;
  private TemplateResponse response;
  
  @Override
  public void setUp() {
    command = new UnknownCommand("A", "B");
    response = new TemplateResponse(command, "D$(toString)E"){};    
  }
  
  public void testReplace() throws 
    NoSuchMethodException, IllegalAccessException, InvocationTargetException {
    assertEquals(
        "1abc1.0def1", response.replace(
            "${toString}abc${doubleValue}def${intValue}", 
            TemplateResponse.PATTERN_ONTHIS, 1));
    assertEquals(
        "1ab$(d)c1.0def1", response.replace(
            "${toString}ab$(d)c${doubleValue}def${intValue}", 
            TemplateResponse.PATTERN_ONTHIS, 1));
    assertEquals(
        "${toString}ab1c${doubleValue}def${intValue}", 
        response.replace(
            "${toString}ab$(toString)c${doubleValue}def${intValue}", 
            TemplateResponse.PATTERN_ONCOMMAND, 1));
  }
  
  public void testRenderCommandValues() {
    assertEquals(
        String.format("D%sE", command.toString()),
        response.renderResponse());
  }

  public void testRenderThisValues() {
    UnknownCommand cmd = new UnknownCommand("A", "B");
    TemplateResponse response = 
      new TemplateResponse(cmd, "D${extraMethod}E") {
      @SuppressWarnings("unused")
      public String extraMethod() {
        return "EXTRA";
      }
    };
    assertEquals(
        String.format("DEXTRAE", cmd.toString()),
        response.renderResponse());
  }
}
 
