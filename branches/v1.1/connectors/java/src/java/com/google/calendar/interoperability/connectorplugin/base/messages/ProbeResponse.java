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

import com.google.calendar.interoperability.connectorplugin.base.messages.util.NovellDate;

/**
 * Responds to a Free/Busy probe from Exchange
 */
public class ProbeResponse extends TemplateResponse {

  public ProbeResponse(GwCommand originalCommand) {
    super(originalCommand,
        "WPC-API= 1.2; \r\n" +
        "Header-Char= T50; \r\n" +
        "Msg-Type= SEARCH; \r\n" +
        "From-Text= ${renderFromText}; \r\n" +
        "From= \r\n" +
        "${renderFrom}" +
        "To= \r\n" +
        "${renderTo}" +
        "All-To= \r\n" +
        "${renderAllto}" +
        "Msg-Id= 3FCD7DD9.2A0A.000B.000; \r\n" +
        "To-Text= ${renderToText}; \r\n" +
        "Date-Sent= ${dateSent}; \r\n" +
        "Send-Options= None; \r\n" +
        "Status-Request= None; \r\n" +
        "Begin-Time= $(getBeginTime); \r\n" +
        "End-Time= $(getEndTime); \r\n" +
        "-END-\r\n"
        );
    }
  
  public String renderFrom() {
    return replace(
        "    WPD= ${getWPD}; \r\n" +
        "    WPPO= ${getWPPO}; \r\n" +
        "    WPU= ${getWPU}; ; \r\n",
        getOriginalCommand().getTo().getAddresses().iterator().next());
  }
  
  public String renderTo() {
    return replace(
        "    WPD= ${getWPD}; \r\n" +
        "    WPPO= ${getWPPO}; \r\n" +
        "    WPU= ${getWPU}; \r\n" +
        "    WPPONUM= 1; \r\n" +
        "    WPUNUM= 1; \r\n" +
        "    CDBA= ${getCDBA}; ; \r\n",
        getOriginalCommand().getFrom());
  }
  
  public String renderAllto() {
    return replace(
        "    WPD= ${getWPD}; \r\n" +
        "    WPPO= ${getWPPO}; \r\n" +
        "    WPU= ${getWPU}; \r\n" +
        "    WPPONUM= 1; \r\n" +
        "    WPUNUM= 1; ; \r\n",
        getOriginalCommand().getFrom());
  }
  
  public String renderToText() {
    return getOriginalCommand().getFrom().getCDBA()
      .replaceAll("\\.FB-PROBE", "(FB-PROBE)");
  }
  
  public String renderFromText() {
    return getOriginalCommand().getTo().getAddresses()
        .iterator().next().getCDBA();
  }
  
  public String dateSent() {
    return new NovellDate().toString();
  }
}
 
