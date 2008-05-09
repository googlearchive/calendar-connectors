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

import com.google.calendar.interoperability.connectorplugin.base.messages.util.Address;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.AddressList;
import com.google.calendar.interoperability.connectorplugin.base.messages.AdminCommand;
import com.google.calendar.interoperability.connectorplugin.base.messages.AppointmentCommand;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.BusyReport;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.DsExternalPostOffice;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.DsGroup;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.DsResource;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.DsUser;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.FileDescriptor;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.FileDescriptorList;
import com.google.calendar.interoperability.connectorplugin.base.messages.GwCommand;
import com.google.calendar.interoperability.connectorplugin.base.messages.MailCommand;
import com.google.calendar.interoperability.connectorplugin.base.messages.NoteCommand;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.NovellDate;
import com.google.calendar.interoperability.connectorplugin.base.messages.PassThroughCommand;
import com.google.calendar.interoperability.connectorplugin.base.messages.PhoneCommand;
import com.google.calendar.interoperability.connectorplugin.base.messages.SearchCommand;
import com.google.calendar.interoperability.connectorplugin.base.messages.util.StatusReport;
import com.google.calendar.interoperability.connectorplugin.base.messages.TaskCommand;
import com.google.calendar.interoperability.connectorplugin.base.messages.UnknownCommand;

import com.google.common.base.Function;

import java.io.IOException;
import java.util.List;
import java.util.logging.Level;
import java.util.logging.Logger;

/**
 * Parser for commands
 */
public class Parser implements Function<String, GwCommand>{
  
  private GwIo io;
  private static final Logger LOGGER = Logger.getLogger(Parser.class.getName());
  
  private static final String WHITE_SPACES = " \r\n\t\u3000\u00A0\u2007\u202F";
  
  private static String strip(String str) {
    if (str == null) {
      return null;
    }
    int limitLeft = 0;
    int limitRight = str.length() - 1;
    while (limitLeft <= limitRight &&
           WHITE_SPACES.indexOf(str.charAt(limitLeft)) >= 0) {
      limitLeft++;
    }
    while (limitRight >= limitLeft &&
           WHITE_SPACES.indexOf(str.charAt(limitRight)) >= 0) {
      limitRight--;
    }
    return str.substring(limitLeft, limitRight + 1);
  }
  
  private static boolean isEmpty(String s) {
    return (s == null) ? true : (s.length() == 0);
  }
  
  private class ParserState {
    private int currentLine = 0;
    String [] lines;

    public String getNextLine() throws IOException {
      if (lines == null) {
        throw new IOException();
      }
      
      if (currentLine >= lines.length) {
        throw new IOException();
      }
      
      String line = lines[currentLine];
      currentLine++;
      
      if (line.startsWith("#")) {
        return getNextLine();
      }
      
      String c = strip(line);
      if (isEmpty(c)) {
        return getNextLine();
      }
      
      return c;
    }

    public String peekNextLine() throws IOException {
      if (lines == null) {
        throw new IOException();
      }
      
      if (currentLine >= lines.length) {
        throw new IOException();
      }
      
      return lines[currentLine];
    }
    
    private GwCommand createCommand(
        String fileName,
        String asText) {
      String []lines = asText.split("\n");
      for (String line : lines) {
        line = line.trim();
        if (line.toUpperCase().startsWith("MSG-TYPE=")) {
          String []pair = line.split("=");
          if (pair.length != 2) {
            return new MailCommand(fileName, asText);
          }
          String value = pair[1].trim();
          if (value.length() < 2) {
            return new MailCommand(fileName, asText);
          }
          value = value.substring(0, value.length() - 1);
          value = value.trim();
          if ("Search".equalsIgnoreCase(value)) {
            LOGGER.log(Level.FINE, "Got search command");
            return new SearchCommand(fileName, asText);
          }
          if ("Mail".equalsIgnoreCase(value)) {
            LOGGER.log(Level.FINE, "Got mail command");
            return new MailCommand(fileName, asText);
          }
          if ("Appt".equalsIgnoreCase(value)) {
            LOGGER.log(Level.FINE, "Got appointment command");
            return new AppointmentCommand(fileName, asText);
          }
          if ("Note".equalsIgnoreCase(value)) {
            LOGGER.log(Level.FINE, "Got note command");
            return new NoteCommand(fileName, asText);
          }
          if ("Task".equalsIgnoreCase(value)) {
            LOGGER.log(Level.FINE, "Got task command");
            return new TaskCommand(fileName, asText);
          }
          if ("Phone".equalsIgnoreCase(value)) {
            LOGGER.log(Level.FINE, "Got phone command");
            return new PhoneCommand(fileName, asText);
          }
          if ("Admin".equalsIgnoreCase(value)) {
            LOGGER.log(Level.FINE, "Got admin command");
            return new AdminCommand(fileName, asText);
          }
          if ("PassThrough".equalsIgnoreCase(value)) {
            LOGGER.log(Level.FINE, "Got pass through command");
            return new PassThroughCommand(fileName, asText);
          }
          LOGGER.log(Level.FINE, "Got mail command");
          return new MailCommand(fileName, asText);
        }
      }
      LOGGER.log(Level.FINE, "Got mail command");
      return new MailCommand(fileName, asText);
    }
    
    
    /**
     * Get the value for a key whose value type is a String. This can be of two
     * forms:
     * a=b;
     * or
     * a=
     *  b;
     * @param keyValue String representing the current line
     * @return a String for the value of the key in the current line. If there is
     * no key, we return null
     * @throws IOException
     */
    private String getStringValue(String keyValue) throws IOException {
      int index = keyValue.indexOf('=');
      if (index == -1) {
        return null;
      }
      String value = 
        keyValue.substring(index + 1, keyValue.length());
      value = value.trim();
      if (!isEmpty(value)) {
        value = value.substring(0, value.length() - 1);
        value = value.trim();
        if (isEmpty(value)) {
          return null;
        }
        return value;
      } 
      // try next line
      String nextLine = getNextLine();
      nextLine = nextLine.trim();
      if (nextLine.length() < 2) {
        return null;
      } 
      nextLine = nextLine.substring(0, nextLine.length() - 1).trim();
      if (isEmpty(nextLine)) {
        return null;
      }
      return nextLine;
    }
    
    /**
     * We have reached the end of a block if we have two ending semicolons
     * @param inputLine Input String to check
     * 
     * @return true if this String ends with two semicolons
     */
    private boolean endOfBlock(final String inputLine) {
      String line = inputLine.trim();
      if (!line.endsWith(";")) {
        return false;
      }
      
      if (line.length() == 0) {
        return false;
      }
      line = line.substring(0, line.length() - 1);
      line = line.trim();
      if (!line.endsWith(";")) {
        return false;
      }
      
      return true;
    }
    /**
     * Takes in a line of the form a=b; or a=b;, or a=b;; and return <a,b>
     * 
     * @param line String from header file
     * @return Key value pair of Strings
     */
    private Tuple<String> getPair(String line) {
      String [] pair = line.split("=");
      if (pair.length != 2) {
        return Tuple.of("", "");
      }
      String first = pair[0].trim();
      
      String second = pair[1];
      second = second.trim();
      if (second.endsWith(",")) {
        second = second.substring(0, second.length() - 1);
        second = second.trim();
      }
      if (!second.endsWith(";")) {
        return Tuple.of("", "");
      }
      second = second.substring(0, second.length() - 1);
      second = second.trim();
      if (second.endsWith(";")) {
        second = second.substring(0, second.length() - 1);
        second = second.trim();
      }
      
      return Tuple.of(first, second);
    }
    
    private AddressList getAddressList() throws IOException {
      AddressList addressList = new AddressList();
      
      Address address = new Address();
      // get pair, add, if comma, start new one, if 2 semis return
      while (true) {
        
        if (!peekNextLine().startsWith(" ")) {
          return addressList;
        }
        
        String line = getNextLine();
        Tuple<String> keyValue = getPair(line);
        address.addPair(keyValue.first, keyValue.second);
        
        if (line.endsWith(",")) {
          addressList.add(address);
          address = new Address();
        }
        
        if (endOfBlock(line)) {
          addressList.add(address);
          return addressList;
        }

        if (";".equals(line)) {
          addressList.add(address);
          return addressList;
        }
      }
    }
    
    private NovellDate getDate(String line) {
      String []parts = line.split("=");
      if (parts.length != 2) {
        LOGGER.log(Level.FINE, "Line with date did not have 2 parts " + line);
        return null;
      }
      
      String d = parts[1];
      NovellDate date = new NovellDate();
      date.set(d.substring(0, d.length() - 1));
      
      return date;
    }
    
    private Character getCharacter(String line) {
      String []parts = line.split("=");
      if (parts.length != 2) {
        return null;
      }

      String c = parts[1];
      c = c.trim();
      // should not be empty, if nothing, the semi-colon should be there
      return c.charAt(0);
    }
    
    private Integer getInteger(String line) {
      String []parts = line.split("=");
      if (parts.length != 2) {
        return null;
      }

      String i = parts[1];
      i = i.trim();
      i = i.substring(0, i.length() - 1);
      // should not be empty, if nothing, the semi-colon should be there
      return Integer.parseInt(i);
    }
    
    private StatusReport getStatusReport() throws IOException {
      StatusReport statusReport = new StatusReport();

      String firstLine = getNextLine();
      Tuple<String> keyValue = getPair(firstLine);
      statusReport.set(keyValue.first, keyValue.second);
      if (endOfBlock(firstLine)) {
        return statusReport;
      }
      
      String secondLine = getNextLine();
      keyValue = getPair(firstLine);
      statusReport.set(keyValue.first, keyValue.second);
      if (endOfBlock(secondLine)) {
        return statusReport;
      }

      // hope this is just a comma and return...
      String comma = getNextLine();
      
      return statusReport;
    }
    
    private BusyReport getBusyReport() throws IOException {
      BusyReport busyReport = new BusyReport();
      
      // get pair, add, if comma, start new one, if 2 semis return
      while (true) {
        
        if (!peekNextLine().startsWith(" ")) {
          return busyReport;
        }
        
        String line = getNextLine();
        Tuple<String> keyValue = getPair(line);
        NovellDate startTime = new NovellDate();
        startTime.set(keyValue.second);
        
        line = getNextLine();
        keyValue = getPair(line);
        NovellDate endTime = new NovellDate();
        endTime.set(keyValue.second);

        busyReport.add(startTime, endTime);
        
        if (line.endsWith(",")) {
          continue;
        }
        
        if (endOfBlock(line)) {
          continue;
        }

        if (";".equals(line)) {
          return busyReport;
        }
      }    
    }
    
    private Address getAddress() throws IOException {
      Address address = new Address();
      
      // get pair, add, if 2 semis return
      while (true) {
        
        if (!peekNextLine().startsWith(" ")) {
          return address;
        }
        
        String line = getNextLine();
        Tuple<String> keyValue = getPair(line);
        address.addPair(keyValue.first, keyValue.second);
        
        if (endOfBlock(line)) {
          return address;
        }

        if (";".equals(line)) {
          return address;
        }
      }
    }
    
    private FileDescriptorList getFileDescriptor() throws IOException {
      FileDescriptorList fileDescriptorList = new FileDescriptorList();
      
      FileDescriptor fileDescriptor = new FileDescriptor();
      // get pair, add, if comma, start new one, if 2 semis return
      while (true) {
        
        if (!peekNextLine().startsWith(" ")) {
          return fileDescriptorList;
        }
        
        String line = getNextLine();
        Tuple<String> keyValue = getPair(line);
        fileDescriptor.addPair(keyValue.first, keyValue.second);
        
        if (line.endsWith(",")) {
          fileDescriptorList.add(fileDescriptor);
          fileDescriptor = new FileDescriptor();
        }
        
        if (endOfBlock(line)) {
          fileDescriptorList.add(fileDescriptor);
          return fileDescriptorList;
        }

        if (";".equals(line)) {
          fileDescriptorList.add(fileDescriptor);
          return fileDescriptorList;
        }
      }    
    }
    
    private void parse(GwCommand command) throws IOException {
      while (true) {
        String curLine = "";
        String curCapsLine = "";
        try {
          curLine = getNextLine();
          curCapsLine = curLine.toUpperCase();
        } catch (IOException e) {
          // Was not able to properly parse things, but lets return what we have
          LOGGER.log(Level.WARNING, "Could not parse till end of file");
          return;
        }
        
        // if starting with #, forget it
        if (curLine.startsWith("#")) {
          curLine = getNextLine();
          continue;
        }
        
        // if just a ';' or "-END-" return
        if (curCapsLine.startsWith("-END-")) {
          LOGGER.log(Level.FINE, "Got end! " + curCapsLine);
          return;
        }
        if (curCapsLine.startsWith("BUSY-FOR")) {
          command.setBusyFor(getStringValue(curLine)); 
          continue;
        }
        if (curCapsLine.startsWith("CALL-ACTION")) {
          command.setCallAction(getStringValue(curLine)); 
          continue;
        }
        if (curCapsLine.startsWith("CALLER-COMPANY")) {
          command.setCallerCompany(getStringValue(curLine)); 
          continue;
        }
        if (curCapsLine.startsWith("CALLER-NAME")) {
          command.setCallerName(getStringValue(curLine)); 
          continue;
        }
        if (curCapsLine.startsWith("CALLER-PHONE")) {
          command.setCallerPhone(getStringValue(curLine)); 
          continue;
        }
        if (curCapsLine.startsWith("FOLDER-NAME")) {
          command.setFolderName(getStringValue(curLine)); 
          continue;
        }      
        if (curCapsLine.startsWith("FROM-TEXT")) {
          command.setFromText(getStringValue(curLine)); 
          continue;
        } 
        if (curCapsLine.startsWith("-GET-DIRECTORY-")) {
          command.setGetDirectory(true); 
          continue;
        }
        if (curCapsLine.startsWith("HEADER-CHAR")) {
          command.setHeaderChar(getStringValue(curLine)); 
          continue;
        }
        if (curCapsLine.startsWith("LOCATION")) {
          command.setLocation(getStringValue(curLine)); 
          continue;
        }
        if (curCapsLine.startsWith("MSG-ACTION")) {
          command.setMsgAction(getStringValue(curLine)); 
          continue;
        }
        if (curCapsLine.startsWith("MSG-CHAR")) {
          command.setMsgChar(getStringValue(curLine)); 
          continue;
        }      
        if (curCapsLine.startsWith("MSG-FILE")) {
          command.setMsgFile(getStringValue(curLine)); 
          continue;
        } 
        if (curCapsLine.startsWith("MSG-ID")) {
          command.setMsgId(getStringValue(curLine)); 
          LOGGER.log(Level.FINE, "Got msg id! " + curCapsLine);
          continue;
        }       
        if (curCapsLine.startsWith("MSG-PRIORITY")) {
          command.setMsgPriority(getStringValue(curLine)); 
          continue;
        }      
        if (curCapsLine.startsWith("MSG-TYPE")) {        
          command.setMsgType(getStringValue(curLine));
          LOGGER.log(Level.FINE, "Got msg type! " + curCapsLine);
          continue;
        }
        if (curCapsLine.startsWith("MSG-VIEW")) {
          command.setMsgView(getStringValue(curLine)); 
          continue;
        }      
        if (curCapsLine.startsWith("ORIG-MSG-ID")) {
          command.setOrigMsgId(getStringValue(curLine)); 
          continue;
        } 
        if (curCapsLine.startsWith("ORIG-MSG-ID")) {
          command.setOrigMsgId(getStringValue(curLine)); 
          continue;
        } 
        if (curCapsLine.startsWith("SECURITY")) {
          command.setSecurity(getStringValue(curLine)); 
          continue;
        }       
        if (curCapsLine.startsWith("SEND-OPTIONS")) {
          command.setSendOptions(getStringValue(curLine)); 
          continue;
        }
        if (curCapsLine.startsWith("SET-STATUS")) {
          command.setSetStatus(getStringValue(curLine)); 
          continue;
        }
        if (curCapsLine.startsWith("STATUS-REQUEST")) {
          command.setStatusRequest(getStringValue(curLine)); 
          continue;
        }
        if (curCapsLine.startsWith("SUBJECT")) {
          command.setSubject(getStringValue(curLine)); 
          continue;
        }      
        if (curCapsLine.startsWith("TO-CC-TEXT")) {
          command.setToCCText(getStringValue(curLine)); 
          continue;
        }
        if (curCapsLine.startsWith("TO-TEXT")) {
          command.setToText(getStringValue(curLine)); 
          continue;
        }
        if (curCapsLine.startsWith("WPC-API")) {
          command.setWpcApi(getStringValue(curLine)); 
          LOGGER.log(Level.FINE, "Got wpcapi! " + curCapsLine);
          continue;
        }      
        
        // Address lists
        if (curCapsLine.startsWith("ALL-TO=")) {
          command.setAllTo(getAddressList()); 
          continue;
        }
        if (curCapsLine.startsWith("ALL-TO-CC=")) {
          command.setAllToCC(getAddressList()); 
          continue;
        }
        if (curCapsLine.startsWith("TO=")) {
          command.setTo(getAddressList()); 
          LOGGER.log(Level.FINE, "Got to! " + curCapsLine);
          continue;
        }
        if (curCapsLine.startsWith("TO-BC=")) {
          command.setToBC(getAddressList()); 
          continue;
        }
        if (curCapsLine.startsWith("TO-BC=")) {
          command.setToCC(getAddressList()); 
          continue;
        }

        // Dates
        if (curCapsLine.startsWith("BEGIN-TIME")) {
          command.setBeginTime(getDate(curLine)); 
          LOGGER.log(Level.FINE, "Got begin time! " + curCapsLine);
          continue;
        }
        if (curCapsLine.startsWith("DATE-SENT")) {
          command.setDateSent(getDate(curLine)); 
          continue;
        }
        if (curCapsLine.startsWith("DISTRIBUTE-DATE")) {
          command.setDistributeDate(getDate(curLine)); 
          continue;
        }
        if (curCapsLine.startsWith("END-TIME")) {
          command.setEndTime(getDate(curLine)); 
          LOGGER.log(Level.FINE, "Got end! " + curCapsLine);
          continue;
        }      
        if (curCapsLine.startsWith("RESPOND-BY")) {
          command.setRespondBy(getDate(curLine)); 
          continue;
        }
        
        // File descriptor
        if (curCapsLine.startsWith("ATTACH-FILE")) {
          command.setAttachFile(getFileDescriptor()); 
          continue;
        }

        // Busy Report
        if (curCapsLine.startsWith("BUSY-REPORT")) {
          command.setBusyReport(getBusyReport()); 
          continue;
        } 
        
        // Address
        if (curCapsLine.startsWith("FROM=")) {
          LOGGER.log(Level.FINE, "Got from! " + curCapsLine);
          command.setFrom(getAddress()); 
          continue;
        } 

        // Status report      
        if (curCapsLine.startsWith("STATUS-REPORT=")) {
          command.setStatusReport(getStatusReport()); 
          continue;
        } 
        
        // Character
        if (curCapsLine.startsWith("TASK-CATEGORY=")) {
          command.setTaskCategory(getCharacter(curLine)); 
          continue;
        } 

        // Integer
        if (curCapsLine.startsWith("TASK-PRIORITY=")) {
          command.setTaskPriority(getInteger(curLine)); 
          continue;
        } 
        
        List<DsExternalPostOffice> dsExternalPostOffice = null;
        List<DsGroup> dsGroup = null;
        List<DsResource> dsResource = null;
        List<DsUser> dsUser = null;
      }
    }
    
    private GwCommand getCommand(
        String fileName, 
        String asText) {

      GwCommand command = createCommand(fileName, asText);
      try {
        parse(command);
        return command;
      } catch (IOException e) {
        LOGGER.log(Level.WARNING, "Could not parse command", e);
      } catch (RuntimeException e) {
        LOGGER.log(Level.WARNING, "Could not parse command", e);
      }      
      return new UnknownCommand(fileName, asText);
    }
  }
  
  public Parser(GwIo io) {
    this.io = io;
  }

  public GwCommand apply(final String fileName) {
    
    // Has the file somehow been deleted?
    if (!io.exists(HEADERS_IN, fileName)) {
      return new UnknownCommand(fileName, "File not found: " + fileName);
    }
    
    // Can we fetch the header?
    final byte[] downloadedHeader = io.fetch(HEADERS_IN, fileName);
    if (downloadedHeader == null) {
      throw new NullPointerException("Cannot load header file: " + fileName);
    }
    
    // Convert to text
    final String asText = new String(downloadedHeader); 
    ParserState state = new ParserState();
    state.lines =  asText.split("\n");
      //List<KeyValue> keyValues = getKeyValues();
    return state.getCommand(fileName, asText);
  }

}
 
