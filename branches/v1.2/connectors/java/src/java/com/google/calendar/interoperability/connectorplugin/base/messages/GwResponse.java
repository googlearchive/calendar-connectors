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

import com.google.common.base.Preconditions;

/**
 * General superclass for responses to an incoming GwCommand.
 */
public class GwResponse {
  
  private static final char[] ESCAPE_THESE = {'\\', ',', ';'};
  
  private static final String REGULAR_RESPONSE =
    "INCOMING COMMAND:%n" +
    "=================%n" +
    "%s%n%n" +
    "RESPONSE:%n" +
    "=========%n" +
    "%s%n";
  
  private static final String EMPTY_RESPONSE =
    "INCOMING COMMAND:%n" +
    "=================%n" +
    "%s%n%n" +
    "==========================%n" +
    "(NO RESPONSE TO GROUPWISE)%n" +
    "==========================%n";
  
  private GwCommand originalCommand;
  
  public GwResponse(GwCommand originalCommand) {
    Preconditions.checkNotNull(originalCommand);
    this.originalCommand = originalCommand;
  }

  public final GwCommand getOriginalCommand() {
    return originalCommand;
  }
  
  /**
   * Renders the response in a format that GroupWise understands.
   * @return the rendered response or null if no response to 
   *   GroupWise is required
   */
  public String renderResponse() {
    return null;
  }
  
  /**
   * Suggests a filename for the rendered response. Default behavior is
   * to use the header name of the original command 
   */
  public String suggestFilename() {
    return originalCommand.getHeaderName();
  }
  
  /**
   * Renders the response in a way it should be logged (usually contains
   * the original command)
   */
  public String renderLog() {
    final String resp = renderResponse();
    return 
      (resp == null) ?
      String.format(EMPTY_RESPONSE, originalCommand.getHeaderContent()) :
      String.format(REGULAR_RESPONSE, 
                    originalCommand.getHeaderContent(), 
                    resp);
  }
  
  /**
   * Suggests a filename for the rendered log. Default behavior is
   * to use the header name of the original command and prepend the
   * word processed and an underscore 
   */
  public String suggestLogFilename() {
    return "processed_" + originalCommand.getHeaderName();
  }

  /**
   * Tool-method escapes a string for GroupWise
   */
  protected String escape(final String toEscape) {
    String result = toEscape;
    
    // Regex-replace seems to not always the way we need it to, so we
    // do the replacement manually
    for (char c : ESCAPE_THESE) {
      StringBuilder sb = new StringBuilder(result);
      String toSearch = String.valueOf(c);
      for (int i = sb.indexOf(toSearch, 0); i >= 0 && i < sb.length(); 
          i = sb.indexOf(toSearch, i)) {
        sb.insert(i, '\\');
        i += 2;
      }
      result = sb.toString();
    }
    
    // Done :-)
    return result;    
  }

}
 
