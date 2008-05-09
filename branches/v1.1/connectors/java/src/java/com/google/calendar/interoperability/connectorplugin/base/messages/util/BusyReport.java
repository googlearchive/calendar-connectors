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

import com.google.calendar.interoperability.connectorplugin.base.Tuple;

import java.util.ArrayList;
import java.util.Iterator;
import java.util.List;

/**
 * A Novell groupwise Busy-Report data structure containing a list of
 * begin and end times
 */
public class BusyReport {

  List<Tuple<NovellDate>> busyTimes;
  
  public BusyReport() {
    busyTimes = new ArrayList<Tuple<NovellDate>>();
  }
  
  public void add(NovellDate start, NovellDate end) {
    busyTimes.add(Tuple.of(start, end));
  }
  
  @Override
  public String toString() {
    String str = "BUSY-REPORT=";
    

    Iterator<Tuple<NovellDate>> iterator = busyTimes.iterator();
    if (!iterator.hasNext()) {
      return str + "\n" + "    ;";
    }
    
    Tuple<NovellDate> pair = iterator.next();
    str = str + "\n" +
      "    " + pair.first.toString() + ";\n" +
      "    " + pair.second.toString() + ";";
    
    while (iterator.hasNext()) {
      pair = iterator.next();
      
      str = str + ",\n" +
        "    " + pair.first.toString() + ";" + "\n" +
        "    " + pair.second.toString() + ";";
 
    }
    
    return str;
  }
}
