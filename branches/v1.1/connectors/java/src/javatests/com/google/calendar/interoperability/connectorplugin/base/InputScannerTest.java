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

import static com.google.calendar.interoperability.connectorplugin.base.GwIo.FOLDER.HEADERS_IN;

import com.google.calendar.interoperability.connectorplugin.base.GwIo;
import com.google.calendar.interoperability.connectorplugin.base.InputScanner;
import com.google.calendar.interoperability.connectorplugin.base.Sink;
import com.google.common.collect.Lists;

import junit.framework.TestCase;

import org.jmock.Expectations;
import org.jmock.Mockery;

import java.util.List;

/**
 * Unit tests for the InputScanner class
 */
public class InputScannerTest extends TestCase {
  
  private List<String> folder1 = Lists.newArrayList("1");
  private List<String> folder2 = Lists.newArrayList("2");
  private List<String> folderAll = Lists.newArrayList("1", "2");
  private List<String> folderNone = Lists.newArrayList();
  
  private Sink<String> sink;
  private GwIo io;
  private InputScanner scanner;
  private Mockery context;
  
  @Override
  @SuppressWarnings("unchecked")
  public void setUp() {
    context = new Mockery();
    sink = context.mock(Sink.class);
    io = context.mock(GwIo.class);
    scanner = new InputScanner(io, sink);
  }
  
  public void testAddsNewFile() {
    context.checking(new Expectations(){{
      exactly(1).of(io).listFiles(HEADERS_IN);
      will(returnValue(folder1));
      exactly(1).of(sink).accept("1");
    }});
    scanner.scan();
    context.assertIsSatisfied();
  }

  public void testAddsMultipleFiles() {
    context.checking(new Expectations(){{
      exactly(1).of(io).listFiles(HEADERS_IN);
      will(returnValue(folderAll));
      exactly(1).of(sink).accept("1");
      exactly(1).of(sink).accept("2");
    }});
    scanner.scan();
    context.assertIsSatisfied();
  }
  
  public void testAddsFilesOnlyOnce() {
    testAddsNewFile();
    context.checking(new Expectations(){{
      exactly(1).of(io).listFiles(HEADERS_IN);
      will(returnValue(folder1));
    }});
    scanner.scan();
    context.assertIsSatisfied();
  }
  
  public void testAddsNewFilesButNotOld() {
    testAddsNewFile();
    context.checking(new Expectations(){{
      exactly(1).of(io).listFiles(HEADERS_IN);
      will(returnValue(folder2));
      exactly(1).of(sink).accept("2");
    }});
    scanner.scan();
    context.assertIsSatisfied();
  }
  
  public void testAddsFileAgainAfterDelete() {
    testAddsNewFile();
    context.checking(new Expectations(){{
      exactly(1).of(io).listFiles(HEADERS_IN);
      will(returnValue(folderNone));
    }});
    scanner.scan();
    context.assertIsSatisfied();
    testAddsNewFile();
  }
}
 
