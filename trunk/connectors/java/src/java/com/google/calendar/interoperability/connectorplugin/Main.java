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

package com.google.calendar.interoperability.connectorplugin;

import static com.google.calendar.interoperability.connectorplugin.base.Configurable.Type.bool;
import static com.google.calendar.interoperability.connectorplugin.base.Configurable.Type.string;
import static com.google.calendar.interoperability.connectorplugin.base.Tuple.of;

import com.google.calendar.interoperability.connectorplugin.base.CommandHandler;
import com.google.calendar.interoperability.connectorplugin.base.Configurable;
import com.google.calendar.interoperability.connectorplugin.base.GarbageCollector;
import com.google.calendar.interoperability.connectorplugin.base.GwFileIo;
import com.google.calendar.interoperability.connectorplugin.base.GwIo;
import com.google.calendar.interoperability.connectorplugin.base.InputScanner;
import com.google.calendar.interoperability.connectorplugin.base.NullSink;
import com.google.calendar.interoperability.connectorplugin.base.Parser;
import com.google.calendar.interoperability.connectorplugin.base.PasswordLoader;
import com.google.calendar.interoperability.connectorplugin.base.ShellUtility;
import com.google.calendar.interoperability.connectorplugin.base.PasswordLoader.EncryptionStrategy;
import com.google.calendar.interoperability.connectorplugin.base.Responder;
import com.google.calendar.interoperability.connectorplugin.base.SimpleSink;
import com.google.calendar.interoperability.connectorplugin.base.SimpleStage;
import com.google.calendar.interoperability.connectorplugin.base.Sink;
import com.google.calendar.interoperability.connectorplugin.base.Stage;
import com.google.calendar.interoperability.connectorplugin.base.messages.GwCommand;
import com.google.calendar.interoperability.connectorplugin.base.messages.GwResponse;
import com.google.calendar.interoperability.connectorplugin.impl.google.GDataConnector;
import com.google.calendar.interoperability.connectorplugin.impl.mock.MockInfrastructure;
import com.google.common.base.Function;
import com.google.common.base.Preconditions;

import java.io.File;
import java.io.IOException;
import java.util.logging.FileHandler;
import java.util.logging.Level;
import java.util.logging.Logger;
import java.util.logging.SimpleFormatter;

/**
 * Main class that runs a connection from Exchange to Google
 */
public class Main extends Configurable implements Runnable {
  
  private static final Logger LOGGER = 
    Logger.getLogger(Main.class.getName());
  
  InputScanner scanner;
  MockInfrastructure mockInfratsructure;
  GDataConnector realInfrastructure;
  GwIo io;
  String baseDirectory;
  
  public Main() {
    super("general");
    this.registerParameter("baseDirectory", string);
    this.registerParameter("connector", string, "google");
    this.registerParameter("logFile", string, "");
    this.registerParameter("verboseLogging", bool, "false");
    this.registerParameter("logMessages", bool, "true");
    this.registerParameter("httpProxy", string, "<NONE>");
    this.registerParameter("httpsProxy", string, "<NONE>");
  }
  
  /**
   * Configures java to use an http/https proxy if the external settings
   * are configured accordingly.
   * @param property the property to read the configuration from the entry
   *   has to be in the format <proxyHost>:<proxyPort>
   * @param httpProp the name of the system property variable that the
   *   proxyhost should be written to (http.proxyHost or https.proxyHost)
   * @param portProp the name of the system environment variable that the
   *   proxyport should be written to (http.proxyPort or https.proxyPort)
   */
  private void setProxy(String property, String httpProp, String portProp) {
    if (getString(property).equals("<NONE>") || 
        getString(property).trim().equals("")) {
      LOGGER.log(Level.INFO, property + " not set, will not use proxy");
      return;
    }
    String[] setting = getString(property).split(":");
    if (setting.length != 2) {
      LOGGER.log(
          Level.SEVERE, 
          String.format(
              "Invalid setting for %s: %s", property, getString(property)));
      throw new IllegalArgumentException("Invalid format");
    }
    int port = 0;
    try {
      port = Integer.parseInt(setting[1]);
    } catch (NumberFormatException e) {
      LOGGER.log(
          Level.SEVERE, 
          String.format(
              "Invalid port for %s: %s", property, getString(property)));      
      throw new IllegalArgumentException("Invalid format");
    }
    LOGGER.log(Level.INFO, String.format(
        "Setting %s to host %s with port %s", property, setting[0], port));
    System.setProperty(httpProp, setting[0]);
    System.setProperty(portProp, setting[1]);
  }
  
  /**
   * Initializes this object from the main thread
   */
  void setUp() throws SecurityException, IOException {
    // Setup logging
    if (getBoolean("verboseLogging")) {
      
      // This setting does not affect the console, just the file
      Logger.getLogger("").setLevel(Level.ALL);
    }
    if (getString("logFile").trim().length() > 0) {
      File parent = new File(getString("logFile")).getParentFile();
      if (parent != null && !parent.exists()) {
        parent.mkdirs();
      }
      FileHandler handler = new FileHandler(getString("logFile"), true);
      handler.setFormatter(new SimpleFormatter());
      Logger.getLogger("").addHandler(handler);
    }
    
    // Set proxy settings
    setProxy("httpProxy", "http.proxyHost", "http.proxyPort");
    setProxy("httpsProxy", "https.proxyHost", "https.proxyPort");
    
    // Build directory io
    baseDirectory = getString("baseDirectory");
    LOGGER.log(Level.INFO, "base directory is " + baseDirectory);
    io = new GwFileIo(new File(baseDirectory));
    
    // Build directory scanner
    LOGGER.log(Level.INFO, "Building scanner...");
    Sink<String> filenameSink = new SimpleSink<String>(); 
    scanner = new InputScanner(io, filenameSink);
    
    // Build Stage 1 (message parsing)
    LOGGER.log(Level.INFO, "Building stage 1...");
    Sink<GwCommand> messageInSink = new SimpleSink<GwCommand>(); 
    Function<String, GwCommand> parser = new Parser(io);
    Stage<String, GwCommand> stage1 = new SimpleStage<String, GwCommand>(
        filenameSink, messageInSink, parser, 10, "messageParsing");
    
    // Build Stage 2 (message handling)
    LOGGER.log(Level.INFO, "Building stage 2...");
    Sink<GwResponse> responseSink = new SimpleSink<GwResponse>();
    CommandHandler handler = new CommandHandler();
    final String connector = getString("connector").toUpperCase().trim();
    if (connector.equals("MOCK")) {
      mockInfratsructure = new MockInfrastructure(handler);
    } else if (connector.equals("GOOGLE")) {
      realInfrastructure = new GDataConnector(handler);
    }
    Stage<GwCommand, GwResponse> stage2 = 
      new SimpleStage<GwCommand, GwResponse>(
        messageInSink, responseSink, handler, 100, "messageExecution");
    
    // Build Stage 3 (message feedback)
    LOGGER.log(Level.INFO, "Building stage 3...");
    Sink<GwCommand> cleanupSink = new SimpleSink<GwCommand>();
    Responder responder = new Responder(io, getBoolean("logMessages"));
    Stage<GwResponse, GwCommand> stage3 = 
      new SimpleStage<GwResponse, GwCommand>(
        responseSink, cleanupSink, responder, 10, "responseCreation");
    
    // Build Stage 4 (cleanup of in-folder)
    LOGGER.log(Level.INFO, "Building stage 4...");
    Sink<Object> terminator = new NullSink<Object>();
    GarbageCollector cleanupCrew = new GarbageCollector(io);
    Stage<GwCommand, Object> stage4 = new SimpleStage<GwCommand, Object>(
        cleanupSink, terminator, cleanupCrew, 5, "cleanup");    
  }

  /**
   * Constructs the SEDA stages and runs the directory scan
   */
  public void run() {
    try {
    
      // Construct whatever is required within this object
      setUp();
      
      // Start scanning the incoming directory
      LOGGER.log(Level.INFO, "Starting to scan");
      scanner.scanForever();
      
    } catch(Throwable e) {
      LOGGER.log(Level.SEVERE, "Main thread died with an exception", e);
    }
  }
  
  /**
   * Helper: sets the main config file and anonymizes it if necessary
   */
  public static void setGlobalConfig(String fileName, boolean encode) 
      throws IOException {
    Preconditions.checkNotNull(fileName);
    PasswordLoader loader = 
      new PasswordLoader(EncryptionStrategy.FAILOVER);
    if (encode) {
      Configurable.setGlobalConfig(loader.loadAndEncrypt(new File(fileName), 
          of("ldap.password","_LDP_"), of("gdata.password","_GDP_")));
    } else {
      Configurable.setGlobalConfig(loader.loadAndEncrypt(new File(fileName)));
    }
  }
  
  /**
   * Main method, called when the service starts
   */
  public static void main(String[] args) throws IOException {
    // Parse args
    String confFile = (args.length > 0) ? args[0] : 
      System.getProperties().getProperty("config", null);
    setGlobalConfig(confFile, true);
    
    // Create the cyclic command that reconfigures the free/busy lookup
    // in Exchange
    ShellUtility fbFixer = new ShellUtility("fbfix");
    Thread fbFixerThread = new Thread(fbFixer);
    fbFixerThread.start();
    
    // Create scanner thread
    Thread mainThread = new Thread(new Main());
    mainThread.setDaemon(false);
    mainThread.start();
  }
}
 
