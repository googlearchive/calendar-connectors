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

import static com.google.calendar.interoperability.connectorplugin.base.Configurable.Type.string;
import static com.google.calendar.interoperability.connectorplugin.base.Configurable.Type.integer;

import com.google.common.base.Nullable;

import java.io.IOException;
import java.io.InputStream;
import java.util.logging.Level;
import java.util.logging.Logger;

/**
 * Logic to execute a task in the operating system 
 */
public class ShellUtility extends Configurable implements Runnable {

  private static final Logger LOGGER = 
    Logger.getLogger(ShellUtility.class.getName());
  
  private String base;
  
  /**
   * the external command to be executed, e.g. "ls -l". If left out,
   * the command will not be executed
   */
  public static final String COMMAND = "command";
  
  /**
   * the timeout in seconds that the command is allowed to take as a max
   *   (default: 120)
   */
  public static final String TIMEOUT = "timeout";
  
  /**
   * the frequency in seconds which to call the command (0 stands for "once")
   */
  public static final String FREQUENCY = "frequency";
  
  
  /**
   * Constructor
   * @param base both the short name of the command and the first part
   *   in the configuration hierarchy
   */
  public ShellUtility(String base) {
    super(base);
    this.base = base;
    this.registerParameter(COMMAND, string, null);
    this.registerParameter(TIMEOUT, integer, "120");
    this.registerParameter(FREQUENCY, integer, "0");
  }

  /**
   * Performs the (possibly cyclic) execution logic by parsing the values
   * from the configuration and interpreting them.
   */
  public void run() {
    while (!Thread.interrupted() && getString(COMMAND) != null) {
      
      // Execute the command
      LOGGER.log(Level.INFO, "Executing command " + base);
      int timeout = getInteger(TIMEOUT).intValue();
      StringBuilder out = new StringBuilder();
      StringBuilder err = new StringBuilder();
      try {
        int result = runCommand(getString(COMMAND), timeout, out, err);
        LOGGER.log(Level.INFO, 
                   "Command " + base + " terminated with exit code " + result);
      } catch (InterruptedException e) {
        LOGGER.log(Level.WARNING,
                   "Command " + base + " timed out and was terminated.");
      } catch (IOException e) {
        LOGGER.log(Level.WARNING,
            "Command could not be executed successfully", e);
      }
      
      // Log the output
      if (out.length() > 0) {
        LOGGER.log(Level.INFO, String.format(
            "Stdout of command %s:%n%s", base, out.toString()));        
      }
      if (err.length() > 0) {
        LOGGER.log(Level.INFO, String.format(
            "Stderr of command %s:%n%s", base, err.toString()));        
      }
      
      // Cyclic command?
      int frequency = getInteger(FREQUENCY).intValue();
      if (frequency <= 0 || !sleep(frequency * 1000)) {
        return;
      }
    }
  }
  
  /**
   * Sleeps a certain amount of time. Visible for testing.
   * @param timeInMilliseconds
   * @return true if the slumber was uninterrupted
   */
  boolean sleep(int timeInMilliseconds) {
    try {
      Thread.sleep(timeInMilliseconds);
      return true;
    } catch (InterruptedException e) {
      return false;
    }
  }
  
  /**
   * Executes a system command. Visible for testing.
   * @param command the command to execute, for instance "ls -l"
   * @param timeoutInSeconds the time to wait for execution. If time is
   *   exceeded, kill the process
   * @param out the output to stdout of the process
   * @param err the output to stderr of the process
   * @return the exit value of the executed command
   * @throws InterruptedException if the process did not terminate in time
   * @throws IOException if something went wrong while reading the process 
   *   feedback or launching it in the first place
   */
  int runCommand(
      String command, int timeoutInSeconds, 
      @Nullable StringBuilder out, @Nullable StringBuilder err) 
      throws InterruptedException, IOException {
    Integer exitValue = null;
    Process process = Runtime.getRuntime().exec(command);
    try {
      InputStream systemOut = process.getInputStream();
      InputStream systemErr = process.getErrorStream();
      for(long time = System.currentTimeMillis() + timeoutInSeconds * 1000L; 
          exitValue == null && time > System.currentTimeMillis();) {
        
        // Check the status of the process
        try {
          exitValue = process.exitValue();
        } catch (IllegalThreadStateException e) {
          // Process has not terminated yet, wait for 500 milliseconds
          if (!sleep(500)) {
            break;
          }
        }
        
        // Transfer as much data from the streams as possible
        while (out != null && systemOut.available() > 0) {
          out.append((char) systemOut.read());
        }
        while (err != null && systemErr.available() > 0) {
          out.append((char) systemOut.read());
        }
      }
      if (exitValue == null) {
        throw new InterruptedException("Process terminated");
      }  
      return exitValue;
    } finally {
      if (process != null) {
        process.destroy();
      }
    }
  }
}
 
