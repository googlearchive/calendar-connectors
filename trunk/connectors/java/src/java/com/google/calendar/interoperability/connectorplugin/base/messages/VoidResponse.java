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

import com.google.common.base.Nullable;
import com.google.common.base.Preconditions;

/**
 * This class represents a response that does not result in any
 * feedback to the GroupWise client.
 */
public class VoidResponse extends GwResponse {
  
  private static final String EMPTY_RESPONSE =
    "INCOMING COMMAND:%n" +
    "=================%n" +
    "%s%n%n" +
    "REASON FOR NOT RESPONDING:%n" +
    "==========================%n" +
    "%s%n";
  
  private String reason;
  private String prepend;

  /** 
   * Constructor
   * @param originalCommand the command this response was generated for
   * @param reason the reason no response to the client is required
   */
  public VoidResponse(GwCommand originalCommand, String reason) {
    this(originalCommand, reason, null);
  }
  
  /** 
   * Constructor
   * @param originalCommand the command this response was generated for
   * @param reason the reason no response to the client is required
   * @param prepend the string prepended to the suggested log filename
   *   (nonrespond if left null)
   */
  public VoidResponse
      (GwCommand originalCommand, String reason, @Nullable String prepend) {
    super(originalCommand);
    Preconditions.checkNotNull(reason);
    this.reason = reason;
    this.prepend = (prepend == null) ? "nonrespond" : prepend;
  }
  
  
  @Override
  public String renderLog() {
    return String.format(
        EMPTY_RESPONSE, getOriginalCommand().getHeaderContent(), reason);
  }
  
  @Override
  public String suggestLogFilename() {
    return prepend + "_" + getOriginalCommand().getHeaderName();
  }
  
  /**
   * Helper: creates a void response for "invalid" commands (with missing
   * arguments et cetera)
   */
  public static VoidResponse invalid(GwCommand originalCommand) {
    return new VoidResponse(originalCommand, "invalid command", "invalid");
  }

}
 
