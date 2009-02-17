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

import com.google.common.base.Preconditions;

import java.io.BufferedInputStream;
import java.io.BufferedOutputStream;
import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.OutputStream;
import java.util.ArrayList;
import java.util.List;
import java.util.logging.Level;
import java.util.logging.Logger;

/**
 * This implementation if the GwIo interfaces uses a directory on the
 * local file system.
 */
public class GwFileIo implements GwIo {
  
  private static final Logger LOGGER 
      = Logger.getLogger(GwFileIo.class.getName());
  private File base;
  
  /**
   * Constructor. Checks whether the subdirectories necessary for the
   * API exist and creates them if necessary.
   * 
   * @param baseDirectory the name of the root of the api folders. The
   *   user executing this program should have full write and read access
   *   to this directory.
   */
  public GwFileIo(File baseDirectory) {
    Preconditions.checkNotNull(baseDirectory);
    this.base = baseDirectory;
    
    // Make sure that we can access everything
    if (!testRoot()) {
      throw new IllegalArgumentException("Invalid root: " + base);
    }
    for (FOLDER folder : FOLDER.values()) {
      if (!testFolder(folder)) {
        throw new IllegalArgumentException
            ("Validation failed for subfolder " + folder);
      }
    }
  }

  /**
   * Tests whether a folder can be accessed and all permissions necessary
   * to operate on it (read/write access depending on folder) are there.
   * Creates the folder if necessary and possible. 
   * 
   * @return true if the FOLDER object is accessible and usable
   */
  public boolean testFolder(FOLDER testThis) {
    
    // Basic checks
    Preconditions.checkNotNull(testThis);
    if (!testRoot()) {
      return false;
    }
    
    // Test for existence
    File subFolder = new File(base, testThis.getNameInGroupWise());
    if (!subFolder.exists() && !subFolder.mkdir()) {
      LOGGER.log(
          Level.FINE, "Cannot create subfolder: " + testThis);
      return false;
    }
    if (subFolder.isFile()) {
      LOGGER.log(
          Level.FINE, "Not a directory: " + testThis);
      return false;
    }
    
    // Test for writing permissions
    if (testThis.canWrite() || testThis.canDelete()) {
      File testFile = 
        new File(subFolder, "tst" + Math.random() + ".tmp");
      try {
        testFile.createNewFile();
        testFile.delete();
      } catch (IOException e) {
        LOGGER.log(
            Level.FINE, "No write permission: " + testThis);
        return false;
      }      
    }    
    return true;
  }

  /**
   * @return true if the root of the directory hierarchy can be accessed
   */
  private boolean testRoot() {
    if (base.exists()) {
      LOGGER.log(
          Level.FINE, "Base found, is directory: " + base.isDirectory());
      return base.isDirectory();
    }
    LOGGER.log(Level.FINE, "Base not found, trying to create directory");
    return base.mkdirs();
  }

  public Iterable<String> listFiles(FOLDER folder) {
    if (!folder.canRead()) {
      return null;
    }
    File subFolder = new File(base, folder.getNameInGroupWise());
    List<String> result = new ArrayList<String>();
    for (File file : subFolder.listFiles()) {
      if (file.isDirectory()) {
        continue;
      }
      final String nameOnly = file.getName();
      if (!folder.verify(nameOnly)) {
        continue;
      }
      result.add(nameOnly);
    }
    return result;
  }
  
  private File toFile(final FOLDER folder, final String name) {
    Preconditions.checkNotNull(folder);
    Preconditions.checkNotNull(name);
    File subFolder = new File(base, folder.getNameInGroupWise());
    return new File(subFolder, name);
  }

  public boolean delete(FOLDER folder, String name) {
    if (!folder.canDelete()) {
      return false;
    }
    final File file = toFile(folder, name);
    if (!file.exists()) {
      return true;
    }
    file.delete();
    return true;
  }

  public boolean exists(FOLDER folder, String name) {
    if (!folder.canRead()) {
      return false;
    }
    return toFile(folder, name).exists();
  }

  public byte[] fetch(FOLDER folder, String name) {
    if (!folder.canRead()) {
      return null;
    }
    try {
      return read(toFile(folder, name));
    } catch (IOException e) {
      return null;
    }
  }

  public boolean store(FOLDER folder, String name, byte[] data) {
    if (!folder.canWrite()) {
      return false;
    }
    final File file = toFile(folder, name);
    if (file.exists()) {
      return false;
    }
    try {
      write(file, data);
      return true;
    } catch (IOException e) {
      return false;
    }
  }
  
  /**
   * Reads the entire contents of the specified file into a {@code byte[]}.
   */
  private static byte[] read(File file) throws IOException {
    ByteArrayOutputStream boas = new ByteArrayOutputStream();
    BufferedInputStream bis = new BufferedInputStream(
        new FileInputStream(file));
    try {
      byte[] buffer = new byte[4096];
      int n;
      while ((n = bis.read(buffer, 0, buffer.length)) > 0) {
        boas.write(buffer, 0, n);
      }
    } finally {
      bis.close();
    }
    return boas.toByteArray();
  }

  /**
   * Writes the specified contents to the specified file.
   */
  private static void write(File file, byte[] content)
      throws IOException {
    OutputStream os = new BufferedOutputStream(new FileOutputStream(file));
    try {
      os.write(content);
    } finally {
      os.close();
    }
  }  
}
 
