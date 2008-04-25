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

import java.util.regex.Pattern;

/**
 * This interface abstracts IO-based operations necessary in the
 * GroupWise API directory structure. By mocking this interface, unit
 * tests can verify that IO-based classes work without having to call
 * external resources. The real implementation will either rely on a file
 * based system or a network folder.
 */
public interface GwIo {
  
  /**
   * This enumeration represents the folders accessible to I/O
   * operations
   */
  public enum FOLDER {
    
    HEADERS_IN("API_IN", true, false, ".*api"),
    HEADERS_OUT("API_OUT", false, true, ".*api"),
    CONTENT_IN("ATT_IN", true, false, ".*"),
    CONTENT_OUT("ATT_OUT", false, true, ".*"),
    LOG("WPCSIN", false, true, ".*")
    ;
    
    
    private final String nameInGroupWise;
    private final boolean canRead;
    private final boolean canWrite;
    private final Pattern filePattern;
    
    public String getNameInGroupWise() {
      return nameInGroupWise;
    }
    
    /**
     * @return true if files in this folder can be read and the directory
     *   structure of this folder can be accessed
     */
    public boolean canRead() {
      return canRead;
    }
    
    /**
     * @return true if files in this folder can be deleted
     */
    public boolean canDelete() {
      return canRead;
    }
    
    /**
     * @return true if this folder can be written to (this does not mean
     *   that the files written can be re-read or that the directory content
     *   can be listed!)
     */
    public boolean canWrite() {
      return canWrite;
    }
    
    /**
     * @return true if the given filename would be valid for this folder
     */
    public boolean verify(String fileName) {
      return filePattern.matcher(fileName).matches();
    }
    
    FOLDER(String name, boolean read, boolean write, String pattern) {
      nameInGroupWise = name;
      canRead = read;
      canWrite = write;
      filePattern = Pattern.compile(pattern, Pattern.CASE_INSENSITIVE);
    }
  }
  
  /**
   * Scans a given folder for all files (limited by the pattern acceptable
   * for a certain folder)
   * 
   * @return an Iterable of file names in String format. The file names are
   *   stripped of any folder information and it is guaranteed that repeating
   *   the same operation will not return one file with a different 
   *   representation (upcase/lowercase changes)
   * @exception java.security.InvalidParameterException if there is no
   *   read access to this folder 
   */
  public Iterable<String> listFiles(FOLDER folder);
  
  /**
   * Retrieves a file from a given folder and returns it in binary format. 
   * 
   * @return the downloaded file, null if the download failed
   */
  byte[] fetch(FOLDER folder, String name);
  
  /**
   * Tests whether a certain file exists in a certain folder
   */
  boolean exists(FOLDER folder, String name);
  
  /**
   * Deletes a file from the storage
   * 
   * @return true if the deletion was successful or the file had already been
   *   deleted
   */
  boolean delete(FOLDER folder, String name);
  
  /**
   * Stores data in a folder. If the data could not be stored due to
   * connectivity problems, an existing file with the same name or
   * lack of access rights, the method will return false
   * 
   * @return true if the data was stored successfully
   */
  boolean store(FOLDER folder, String name, byte[] data);

}
 
