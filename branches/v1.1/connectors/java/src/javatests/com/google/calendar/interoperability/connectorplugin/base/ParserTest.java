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
import com.google.calendar.interoperability.connectorplugin.base.Parser;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.Address;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.AddressList;
import com.google.calendar.interoperability.connectorplugin.base.messages.AdminCommand;
import com.google.calendar.interoperability.connectorplugin.base.messages.GwCommand;
import com.google.calendar.interoperability.connectorplugin.base.messages.MailCommand;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.NovellDate;
import com.google.calendar.interoperability.connectorplugin.base.messages.SearchCommand;

import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Iterator;
import java.util.Set;

import junit.framework.TestCase;

import org.jmock.Expectations;
import org.jmock.Mockery;

public class ParserTest extends TestCase {
  
  private Parser parser;
  private GwIo io;
  private Mockery context;
  
  @Override
  public void setUp() {
    context = new Mockery();
    io = context.mock(GwIo.class);
    parser = new Parser(io);
  }
  
  private static final String FREE_BUSY = 
    "MSG-TYPE= Search;\n" + 
    "-END-"; 

  public void testTypeFreeBusyCommand() {
    context.checking(new Expectations(){{
      exactly(1).of(io).exists(HEADERS_IN, "1");
      will(returnValue(true));
      exactly(1).of(io).fetch(HEADERS_IN, "1");
      will(returnValue(FREE_BUSY.getBytes()));
    }});
    assertTrue(parser.apply("1") instanceof SearchCommand);
    context.assertIsSatisfied();
  }

  private static final String MAIL = 
    "MSG-TYPE= Mail;\n" + 
    "-END-"; 

  public void testTypeMailCommand() {
    context.checking(new Expectations(){{
      exactly(1).of(io).exists(HEADERS_IN, "1");
      will(returnValue(true));
      exactly(1).of(io).fetch(HEADERS_IN, "1");
      will(returnValue(MAIL.getBytes()));
    }});
    assertTrue(parser.apply("1") instanceof MailCommand);
    context.assertIsSatisfied();
  }

  private static final String PROBE = 
    "WPC-API= 1.2;\n" +
    "MSG-TYPE= Search;\n" +
    "Msg-ID= FB-PROBE:2007.8.23.15.59:2007.8.23.15.59:2007.8.23.15.59.9;\n" +
    "From= \n" +
    "    WPD= FAKE;\n" + 
    "    WPPO= Exchange Gateway;\n" + 
    "    WPU= FB-PROBE; \n" +
    "    CDBA= FAKE.Exchange Gateway.FB-PROBE; ;\n" + 
    "To= \n" +
    "    WPD= FAKE to;\n" + 
    "    WPPO= Exchange Gateway to;\n" + 
    "    WPU= FB-PROBE to; \n" +
    "    CDBA= FAKE.Exchange Gateway.FB-PROBE to; ;\n" + 
    "Begin-Time= 23/8/2007 15:59;\n" +
    "End-Time= 23/8/2007 15:59;\n" +
    "-END-";

  public void testParseSearchCommand() {
    context.checking(new Expectations(){{
      exactly(1).of(io).exists(HEADERS_IN, "1");
      will(returnValue(true));
      exactly(1).of(io).fetch(HEADERS_IN, "1");
      will(returnValue(PROBE.getBytes()));
    }});
    GwCommand command = parser.apply("1");
    assertTrue(command instanceof SearchCommand);
    context.assertIsSatisfied();
    
    assertTrue("1.2".equals(command.getWpcApi()));
    assertTrue(
        "FB-PROBE:2007.8.23.15.59:2007.8.23.15.59:2007.8.23.15.59.9".equals(
            command.getMsgId()));
    Address address = command.getFrom();
    assertEquals(
        "    WPD = FAKE;\n" +
        "    WPPO = Exchange Gateway;\n" +
        "    WPU = FB-PROBE;\n" +
        "    CDBA = FAKE.Exchange Gateway.FB-PROBE;", address.toString());
    
    AddressList to = command.getTo();
    Set<Address> toAddresses = to.getAddresses();
    assertEquals(toAddresses.size(), 1);
    
    Iterator<Address> iterator = toAddresses.iterator();
    Address toAddress = iterator.next();
    assertEquals(
        "    WPD = FAKE to;\n" +
        "    WPPO = Exchange Gateway to;\n" +
        "    WPU = FB-PROBE to;\n" +
        "    CDBA = FAKE.Exchange Gateway.FB-PROBE to;", toAddress.toString());
 
    assertNotNull(command.getBeginTime());
    assertNotNull(command.getEndTime());
  }
  
  private static final String MAIL_MUL = 
    "Wpc-Api= 1.2;\n" +
    "Header-Char= T50; \n" +
    "Msg-Type= MAIL; \n" +
    "From= \n" +
    "    WPD= Exchange;\n" + 
    "    WPPO= First Administrative Group;\n" + 
    "    WPU= ausername1234; \n" +
    "    CDBA= ^¡addrExchange.First Administrative Group.ausername1234^¡sbjtFW: 2XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX^¡osnt22/8/2007 15:23:19^¡strqNone;  ;\n" +
    "From-Text= ausername1234; \n" +
    "To= \n" +
    "    WPD= GWDOMAIN;\n" + 
    "    WPPO= GWPO; \n" +
    "    WPU= GWGWUser2; \n" +
    "    CDBA= GWDOMAIN.GWPO.GWGWUser2;  ,\n" +
    "    WPD= GWDOMAIN; \n" +
    "    WPPO= GWPO; \n" +
    "    WPU= GWGWUser1; \n" +
    "    CDBA= GWDOMAIN.GWPO.GWGWUser1;  ;\n" +
    "All-To= \n" +
    "    WPD= GWDOMAIN;\n" + 
    "    WPPO= GWPO; \n" +
    "    WPU= GWGWUser1; \n" +
    "    CDBA= GWDOMAIN.GWPO.GWGWUser1;  ,\n" +
    "    WPD= GWDOMAIN; \n" +
    "    WPPO= GWPO; \n" +
    "    WPU= GWGWUser2; \n" +
    "    CDBA= GWDOMAIN.GWPO.GWGWUser2;  ;\n" +
    "To-Text= \n" +
    "    GW GWUser1, GW GWUser2;\n" + 
    "Msg-Char= T50; \n" +
    "Msg-File= a7f7cb6f.bdy;\n" + 
    "Subject= FW: 2XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX;\n" + 
    "Msg-Id= c=US\\;a= \\;p=goolab\\;l=LABEXCH1-070822222319Z-8\\;gwcnt=0007; \n" +
    "Date-Sent= 22/8/2007 15:23:19; \n" +
    "Security= Normal; \n" +
    "Msg-Priority= Normal; \n" +
    "Status-Request= None; \n" +
    "-END-\n";
  
  public void testParseMailMulCommand() {
    context.checking(new Expectations(){{
      exactly(1).of(io).exists(HEADERS_IN, "1");
      will(returnValue(true));
      exactly(1).of(io).fetch(HEADERS_IN, "1");
      will(returnValue(MAIL_MUL.getBytes()));
    }});
    GwCommand command = parser.apply("1");
    assertTrue(command instanceof MailCommand);
    context.assertIsSatisfied();
    
    assertTrue("1.2".equals(command.getWpcApi()));
    assertTrue(
      "c=US\\;a= \\;p=goolab\\;l=LABEXCH1-070822222319Z-8\\;gwcnt=0007".equals(
        command.getMsgId()));
    Address address = command.getFrom();
    assertEquals(
        "    WPD = Exchange;\n" +
        "    WPPO = First Administrative Group;\n" +
        "    WPU = ausername1234;\n" +
        "    CDBA = ^¡addrExchange.First Administrative Group.ausername1234^¡sbjtFW: 2XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX^¡osnt22/8/2007 15:23:19^¡strqNone;", address.toString());
    
    AddressList to = command.getTo();
    Set<Address> toAddresses = to.getAddresses();
    assertEquals(toAddresses.size(), 2);
    
    Iterator<Address> iterator = toAddresses.iterator();
    Address toAddress = iterator.next();
    assertEquals(
        "    WPD = GWDOMAIN;\n" +
        "    WPPO = GWPO;\n" +
        "    WPU = GWGWUser2;\n" +
        "    CDBA = GWDOMAIN.GWPO.GWGWUser2;", toAddress.toString());
    
    toAddress = iterator.next();
    assertEquals(
        "    WPD = GWDOMAIN;\n" +
        "    WPPO = GWPO;\n" +
        "    WPU = GWGWUser1;\n" +
        "    CDBA = GWDOMAIN.GWPO.GWGWUser1;", toAddress.toString());   
    
    AddressList allTo = command.getAllTo();
    Set<Address> allToAddresses = allTo.getAddresses();
    assertEquals(allToAddresses.size(), 2);
    
    iterator = allToAddresses.iterator();
    Address allToAddress = iterator.next();
    assertEquals(
        "    WPD = GWDOMAIN;\n" +
        "    WPPO = GWPO;\n" +
        "    WPU = GWGWUser1;\n" +
        "    CDBA = GWDOMAIN.GWPO.GWGWUser1;", allToAddress.toString());
    
    allToAddress = iterator.next();
    assertEquals(
        "    WPD = GWDOMAIN;\n" +
        "    WPPO = GWPO;\n" +
        "    WPU = GWGWUser2;\n" +
        "    CDBA = GWDOMAIN.GWPO.GWGWUser2;", allToAddress.toString());   

    assertEquals(command.getToText(), "GW GWUser1, GW GWUser2");
    assertEquals(command.getMsgChar(), "T50");
    assertEquals(command.getSubject(), "FW: 2XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX");
    assertEquals(command.getMsgId(), "c=US\\;a= \\;p=goolab\\;l=LABEXCH1-070822222319Z-8\\;gwcnt=0007");
    assertNotNull(command.getDateSent());  
    assertEquals(command.getSecurity(), "Normal");
    assertEquals(command.getMsgPriority(), "Normal");
    assertEquals(command.getStatusRequest(), "None");
  }  
  
  
  private static final String GET_DIR = 
    "WPC-API= 1.2; \r\n" + 
    "Msg-Type= Admin; \r\n" + 
    "-GET-DIRECTORY- \r\n" +
    "-END- \r\n";

  public void testParseGetDirCommand() {
    context.checking(new Expectations(){{
      exactly(1).of(io).exists(HEADERS_IN, "1");
      will(returnValue(true));
      exactly(1).of(io).fetch(HEADERS_IN, "1");
      will(returnValue(GET_DIR.getBytes()));
    }});
    GwCommand command = parser.apply("1");
    assertTrue(command instanceof AdminCommand);
    context.assertIsSatisfied();
    
    assertEquals("1.2", command.getWpcApi());
    assertTrue(command.getGetDirectory());
  }
    
  String BAD_COMMAND = 
    "dfgdjgdgdfgdfgdfgdf";
  
  public void testParseBadCommand() {
    context.checking(new Expectations(){{
      exactly(1).of(io).exists(HEADERS_IN, "1");
      will(returnValue(true));
      exactly(1).of(io).fetch(HEADERS_IN, "1");
      will(returnValue(BAD_COMMAND.getBytes()));
    }});
    GwCommand command = parser.apply("1");
    assertTrue(command instanceof MailCommand);
    context.assertIsSatisfied();    
  }
  
  private static final String FREE_BUSY2 =
    "WPC-API= 1.2;\r\n" +
    "MSG-TYPE= Search;\r\n" +
    "Msg-ID= AAKDJPCA:2007.9.20.17.8:2007.11.19.16.8:2007.9.21.0.8.59;\r\n" +
    "From= \r\n" +
    "    WPD= googleDOTcom; \r\n" +
    "    WPPO= Exchange Gateway; \r\n" +
    "    WPU= Microsoft System Attendant; \r\n" +
    "    CDBA= googleDOTcom.Exchange Gateway." +
    "Microsoft System Attendant; ; \r\n" +
    "To= \r\n" +
    "    WPD= postmaster32; \r\n" +
    "    WPPO= brgr; \r\n" +
    "    WPU= name..a3@postmaster32.brgr.name; \r\n" +
    "    CDBA= postmaster32.brgr.name..a3@postmaster32.brgr.name; ; \r\n" +
    "Begin-Time= 20/9/2007 17:8;\r\n" +
    "End-Time= 19/11/2007 16:8;\r\n" +
    "-END-\r\n";
  
  private static String format(NovellDate date) {
    assertNotNull(date);
    return new SimpleDateFormat("yyyyMMddHHmmss").
      format(new Date(date.getTimeInUtc()));
  }
  
  public void testTypeFreeBusyCommand2() {
    
    // Step 1: parse
    context.checking(new Expectations(){{
      exactly(1).of(io).exists(HEADERS_IN, "1");
      will(returnValue(true));
      exactly(1).of(io).fetch(HEADERS_IN, "1");
      will(returnValue(FREE_BUSY2.getBytes()));
    }});
    SearchCommand command = (SearchCommand) parser.apply("1");
    context.assertIsSatisfied();
    
    // Step 2: validate some of the parsed values
    assertEquals("20070920170800", format(command.getBeginTime()));
    assertEquals("20071119160800", format(command.getEndTime()));
  }
  
}

