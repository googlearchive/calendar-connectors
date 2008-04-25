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

package com.google.calendar.interoperability.connectorplugin.base.messages.util;

import java.util.Iterator;
import java.util.LinkedHashSet;
import java.util.Set;

/**
 * A novell groupwise file descriptor used in message headers
 */
public class FileDescriptorList {
  private Set<FileDescriptor> fileDescriptors;
  
  public FileDescriptorList(Set<FileDescriptor> fileDescriptors) {
    this.fileDescriptors = fileDescriptors;
  }

  public FileDescriptorList() {
    this.fileDescriptors = new LinkedHashSet<FileDescriptor>();
  }  
  
  /**
   * Returns the set of file descriptors. We use a LinkedHashSet to maintain
   * order of file descriptors.
   * 
   * @return A Set of descriptors
   */
  public Set<FileDescriptor> getFileDescriptors() {
    return fileDescriptors;
  }
  
  public void add(FileDescriptor fileDescriptor) {
    fileDescriptors.add(fileDescriptor);
  }
  
  @Override
  public String toString() {
    
    String fileDescriptorList = "";
    
    int numFileDescriptors = fileDescriptors.size();
    if (numFileDescriptors == 0) {
      return fileDescriptorList;
    }
    
    Iterator<FileDescriptor> iterator = fileDescriptors.iterator();
    fileDescriptorList = iterator.next().toString();
    
    while (iterator.hasNext()) {
      fileDescriptorList = fileDescriptorList + "  ,\n" + iterator.next().toString(); 
    }
    return fileDescriptorList;
  }
}
