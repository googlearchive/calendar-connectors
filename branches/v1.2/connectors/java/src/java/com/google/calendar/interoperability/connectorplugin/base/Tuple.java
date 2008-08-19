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

/**
 * A pair of two elements of the same kind. This is a very simple
 * container -- it does not do toString, comparison or hashcode
 * correctly. Do not use it as key in collections.
 */
public class Tuple<T> {
  
  public final T first, second;
  
  public Tuple(T firstElement, T secondElement) {
    this.first = firstElement;
    this.second = secondElement;
  }

  public T getFirst() {
    return first;
  }

  public T getSecond() {
    return second;
  }

  /**
   * Factory-method, saves some typing compared to contructor call.
   */
  public static<T> Tuple<T> of(T first, T second) {
    return new Tuple<T>(first, second);
  }
}
 
