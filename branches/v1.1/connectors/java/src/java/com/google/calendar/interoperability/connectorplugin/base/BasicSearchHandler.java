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

import com.google.calendar.interoperability.connectorplugin.base.messages.GwResponse;
import com.google.calendar.interoperability.connectorplugin.base.messages.ProbeResponse;
import com.google.calendar.interoperability.connectorplugin.base.messages.SearchCommand;
import com.google.calendar.interoperability.connectorplugin.base.messages.VoidResponse;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.Address;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.AddressList;
import com.google.common.base.Function;

import java.util.Iterator;
import java.util.logging.Level;
import java.util.logging.Logger;

/**
 * Convenience-class for implementing free/busy search handlers.
 * Will handle probes and general input validation
 */
public abstract class BasicSearchHandler 
    implements Function<SearchCommand, GwResponse>  {

  protected final Logger logger = Logger.getLogger(getClass().getName());
  
  protected abstract GwResponse handleSearch(
      SearchCommand command, Address requestor, Address searchFor);
  
  public final GwResponse apply(SearchCommand searchCommand) {
    // Make sure that basic data fields are set
    if (searchCommand.getMsgId() == null) {
      logger.log(Level.WARNING, "Could not get message id");
      return VoidResponse.invalid(searchCommand);
    }
    if (searchCommand.getBeginTime() == null) {
      logger.log(Level.WARNING, "Could not get getBeginTime");
      return VoidResponse.invalid(searchCommand);
    }
    if (searchCommand.getEndTime() == null) {
      logger.log(Level.WARNING, "Could not get getEndTime");
      return VoidResponse.invalid(searchCommand);
    }
    if (searchCommand.getFrom() == null) {
      logger.log(Level.WARNING, "Could not get getFrom");
      return VoidResponse.invalid(searchCommand);
    }
    if (searchCommand.getTo() == null) {
      logger.log(Level.WARNING, "Could not get getTo");
      return VoidResponse.invalid(searchCommand);
    }
    
    Address fromAddress = searchCommand.getFrom();
    AddressList to = searchCommand.getTo();
    Iterator<Address> iterator = to.getAddresses().iterator();
    if (!iterator.hasNext()){
      logger.log(Level.WARNING, "Could not get any toAddress ");
      return VoidResponse.invalid(searchCommand);
    }
    Address toAddress = iterator.next();
    if (toAddress == null) {
      logger.log(Level.WARNING, "Could not get toAddress");
      return VoidResponse.invalid(searchCommand);
    }
    
    // Case: F/B probe
    if (searchCommand.getHeaderContent().toUpperCase()
        .indexOf("FB-PROBE") >= 0) {
      return new ProbeResponse(searchCommand);
    }
    
    // Call custom method
    return handleSearch(searchCommand, fromAddress, toAddress);
  }
}
 
