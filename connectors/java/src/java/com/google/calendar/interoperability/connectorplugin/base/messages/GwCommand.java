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


import com.google.calendar.interoperability.connectorplugin.base.messages.util.Address;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.AddressList;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.BusyReport;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.DsExternalPostOffice;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.DsGroup;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.DsResource;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.DsUser;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.FileDescriptorList;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.NovellDate;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.StatusReport;

import java.util.List;

/**
 * Base class for commands coming into the GroupWise connector. Every command
 * should contain at least the name of the header-file it was parsed from
 * and the body of the file (in case someone needs it for additional parsing
 * or logging)
 */
public abstract class GwCommand {
  
  // variable names correspond to the novell documentation
  AddressList allTo = null;
  AddressList allToCC = null;
  FileDescriptorList attachFile = null;
  NovellDate beginTime = null;
  String busyFor = null;
  BusyReport busyReport = null;
  String callAction = null;
  String callerCompany = null;
  String callerName = null;
  String callerPhone = null;
  NovellDate dateSent = null;
  NovellDate distributeDate = null;
  List<DsExternalPostOffice> dsExternalPostOffice = null;
  List<DsGroup> dsGroup = null;
  List<DsResource> dsResource = null;
  List<DsUser> dsUser = null;
  NovellDate endTime = null;
  String folderName = null;
  Address from = null;
  String fromText = null;
  boolean getDirectory = false;
  String headerChar = "T50";
  String location = null;
  String msgAction = null;
  String msgChar = null;
  String msgFile = null;
  String msgId = null;
  String msgPriority = null;
  String msgType = null;
  String msgView = null;
  String origMsgId = null;
  NovellDate respondBy = null;
  String security = null;
  String sendOptions = null;
  String setStatus = null;
  StatusReport statusReport = null;
  String statusRequest = null;
  String subject = null;
  Character taskCategory = null;
  Integer taskPriority = null;
  AddressList to = null;
  AddressList toBC = null;
  AddressList toCC = null;
  String toCCText = null;
  String toText = null;
  String wpcApi = null;
  
  public enum MessageType {
    SEARCH("Busy search request"),
    MAIL("Mail message"),
    APPT("Appointment message"),
    NOTE("Note message"),
    TASK("Task message"),
    PHONE("Phone message"),
    ADMIN("Directory service message"),
    PASS_THROUGH("Message to external groupwise");
    
    private String identifier;

    MessageType(String identifier) { 
      this.identifier = identifier; 
    }

    @Override
    public String toString() { 
      return identifier; 
    }
  }

  private String wpc_api;
  
  // initialized to default value: MAIL
  private MessageType messageType = MessageType.MAIL;

  private final String headerName;
  private String headerContent;
  
  public GwCommand(String headerName, String headerContent) {
    super();
    this.headerName = headerName;
    this.headerContent = headerContent;
  }

  public String getHeaderName() {
    return headerName;
  }

  public String getHeaderContent() {
    return headerContent;
  }
  
  
  public void setHeaderContent(String content) {
    this.headerContent = content;
  }

  public String getWpc_api() {
    return wpc_api;
  }

  public void setWpc_api(String wpc_api) {
    this.wpc_api = wpc_api;
  }

  public MessageType getMessageType() {
    return messageType;
  }

  public void setMessageType(MessageType messageType) {
    this.messageType = messageType;
  }

  public AddressList getAllTo() {
    return allTo;
  }

  public void setAllTo(AddressList allTo) {
    this.allTo = allTo;
  }

  public AddressList getAllToCC() {
    return allToCC;
  }

  public void setAllToCC(AddressList allToCC) {
    this.allToCC = allToCC;
  }

  public FileDescriptorList getAttachFile() {
    return attachFile;
  }

  public void setAttachFile(FileDescriptorList attachFile) {
    this.attachFile = attachFile;
  }

  public NovellDate getBeginTime() {
    return beginTime;
  }

  public void setBeginTime(NovellDate beginTime) {
    this.beginTime = beginTime;
  }

  public String getBusyFor() {
    return busyFor;
  }

  public void setBusyFor(String busyFor) {
    this.busyFor = busyFor;
  }

  public BusyReport getBusyReport() {
    return busyReport;
  }

  public void setBusyReport(BusyReport busyReport) {
    this.busyReport = busyReport;
  }

  public String getCallAction() {
    return callAction;
  }

  public void setCallAction(String callAction) {
    this.callAction = callAction;
  }

  public String getCallerCompany() {
    return callerCompany;
  }

  public void setCallerCompany(String callerCompany) {
    this.callerCompany = callerCompany;
  }

  public String getCallerName() {
    return callerName;
  }

  public void setCallerName(String callerName) {
    this.callerName = callerName;
  }

  public String getCallerPhone() {
    return callerPhone;
  }

  public void setCallerPhone(String callerPhone) {
    this.callerPhone = callerPhone;
  }

  public NovellDate getDateSent() {
    return dateSent;
  }

  public void setDateSent(NovellDate dateSent) {
    this.dateSent = dateSent;
  }

  public NovellDate getDistributeDate() {
    return distributeDate;
  }

  public void setDistributeDate(NovellDate distributeDate) {
    this.distributeDate = distributeDate;
  }

  public List<DsExternalPostOffice> getDsExternalPostOffice() {
    return dsExternalPostOffice;
  }

  public void setDsExternalPostOffice(
      List<DsExternalPostOffice> dsExternalPostOffice) {
    this.dsExternalPostOffice = dsExternalPostOffice;
  }

  public List<DsGroup> getDsGroup() {
    return dsGroup;
  }

  public void setDsGroup(List<DsGroup> dsGroup) {
    this.dsGroup = dsGroup;
  }

  public List<DsResource> getDsResource() {
    return dsResource;
  }

  public void setDsResource(List<DsResource> dsResource) {
    this.dsResource = dsResource;
  }

  public List<DsUser> getDsUser() {
    return dsUser;
  }

  public void setDsUser(List<DsUser> dsUser) {
    this.dsUser = dsUser;
  }

  public String getFolderName() {
    return folderName;
  }

  public void setFolderName(String folderName) {
    this.folderName = folderName;
  }

  public Address getFrom() {
    return from;
  }

  public void setFrom(Address from) {
    this.from = from;
  }

  public String getFromText() {
    return fromText;
  }

  public void setFromText(String fromText) {
    this.fromText = fromText;
  }

  public boolean getGetDirectory() {
    return getDirectory;
  }

  public void setGetDirectory(boolean getDirectory) {
    this.getDirectory = getDirectory;
  }

  public String getHeaderChar() {
    return headerChar;
  }

  public void setHeaderChar(String headerChar) {
    this.headerChar = headerChar;
  }

  public String getLocation() {
    return location;
  }

  public void setLocation(String location) {
    this.location = location;
  }

  public String getMsgAction() {
    return msgAction;
  }

  public void setMsgAction(String msgAction) {
    this.msgAction = msgAction;
  }

  public String getMsgChar() {
    return msgChar;
  }

  public void setMsgChar(String msgChar) {
    this.msgChar = msgChar;
  }

  public String getMsgFile() {
    return msgFile;
  }

  public void setMsgFile(String msgFile) {
    this.msgFile = msgFile;
  }

  public String getMsgId() {
    return msgId;
  }

  public void setMsgId(String msgId) {
    this.msgId = msgId;
  }

  public String getMsgPriority() {
    return msgPriority;
  }

  public void setMsgPriority(String msgPriority) {
    this.msgPriority = msgPriority;
  }

  public String getMsgType() {
    return msgType;
  }

  public void setMsgType(String msgType) {
    this.msgType = msgType;
  }

  public String getMsgView() {
    return msgView;
  }

  public void setMsgView(String msgView) {
    this.msgView = msgView;
  }

  public String getOrigMsgId() {
    return origMsgId;
  }

  public void setOrigMsgId(String origMsgId) {
    this.origMsgId = origMsgId;
  }

  public NovellDate getRespondBy() {
    return respondBy;
  }

  public void setRespondBy(NovellDate respondBy) {
    this.respondBy = respondBy;
  }

  public String getSecurity() {
    return security;
  }

  public void setSecurity(String security) {
    this.security = security;
  }

  public String getSendOptions() {
    return sendOptions;
  }

  public void setSendOptions(String sendOptions) {
    this.sendOptions = sendOptions;
  }

  public String getSetStatus() {
    return setStatus;
  }

  public void setSetStatus(String setStatus) {
    this.setStatus = setStatus;
  }

  public StatusReport getStatusReport() {
    return statusReport;
  }

  public void setStatusReport(StatusReport statusReport) {
    this.statusReport = statusReport;
  }

  public String getStatusRequest() {
    return statusRequest;
  }

  public void setStatusRequest(String statusRequest) {
    this.statusRequest = statusRequest;
  }

  public String getSubject() {
    return subject;
  }

  public void setSubject(String subject) {
    this.subject = subject;
  }

  public Character getTaskCategory() {
    return taskCategory;
  }

  public void setTaskCategory(Character taskCategory) {
    this.taskCategory = taskCategory;
  }

  public Integer getTaskPriority() {
    return taskPriority;
  }

  public void setTaskPriority(Integer taskPriority) {
    this.taskPriority = taskPriority;
  }

  public AddressList getTo() {
    return to;
  }

  public void setTo(AddressList to) {
    this.to = to;
  }

  public AddressList getToBC() {
    return toBC;
  }

  public void setToBC(AddressList toBC) {
    this.toBC = toBC;
  }

  public AddressList getToCC() {
    return toCC;
  }

  public void setToCC(AddressList toCC) {
    this.toCC = toCC;
  }

  public String getToCCText() {
    return toCCText;
  }

  public void setToCCText(String toCCText) {
    this.toCCText = toCCText;
  }

  public String getToText() {
    return toText;
  }

  public void setToText(String toText) {
    this.toText = toText;
  }

  public String getWpcApi() {
    return wpcApi;
  }

  public void setWpcApi(String wpcApi) {
    this.wpcApi = wpcApi;
  }

  public NovellDate getEndTime() {
    return endTime;
  }

  public void setEndTime(NovellDate endTime) {
    this.endTime = endTime;
  }
}
 
