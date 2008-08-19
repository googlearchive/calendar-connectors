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

import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.util.logging.Level;
import java.util.logging.Logger;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

/**
 * A class that uses a simple templating scheme to render
 * responses. It starts with a regular string and then 
 * does additional replacements:
 * 
 * $(methodName) will call an argumentless method on the original command
 *   and replace the string with its result
 * ${methodName} will call an argumentless method on this object and replace
 *   the string with its result
 *   
 *   This class should be subclassed to have the template hard-coded.
 */
abstract class TemplateResponse extends GwResponse {
  
  private static final Logger LOG = 
    Logger.getLogger(TemplateResponse.class.getName());
  
  // Visible for unit tests only  
  static final String PATTERN_ONTHIS = "\\$\\{[^\\$\\{\\}]*\\}";
  
  //Visible for unit tests only
  static final String PATTERN_ONCOMMAND = "\\$\\([^\\$\\{\\}]*\\)";
  
  private final String template;
  
  public TemplateResponse(GwCommand originalCommand, String template) {
    super(originalCommand);
    this.template = template;
  }
  
  //Visible for unit tests only
  String replace(final String original, String pattern, Object object) 
    throws 
      NoSuchMethodException, 
      IllegalAccessException, 
      InvocationTargetException {
    Pattern regex = Pattern.compile(pattern);
    String replaceHere = original;
    for (
        Matcher m = regex.matcher(replaceHere); 
        m.find(); 
        m = regex.matcher(replaceHere)) {
      final String start = replaceHere.substring(0, m.start());
      final String end = replaceHere.substring(m.end());
      final String methodName = replaceHere.substring(m.start() + 2, m.end() - 1);
      final Method method = 
        object.getClass().getMethod(methodName, new Class[0]);
      replaceHere = start + String.valueOf(method.invoke(object)) + end;
    }
    return replaceHere;
  }
  
  /**
   * Tool-method, performs ${}-replacement for arbitrary strings
   */
  protected String replace(String templateString, Object parameterObject) 
      throws RuntimeException {
    try {
      return replace(templateString, PATTERN_ONTHIS, parameterObject);
    } catch (NoSuchMethodException e) {
      throw new IllegalArgumentException(e);
    } catch (IllegalAccessException e) {
      throw new IllegalArgumentException(e);
    } catch (InvocationTargetException e) {
      throw new IllegalArgumentException(e);
    }
  }
  
  @Override
  public String renderResponse() {
    String result = template;
    Exception ex = null;
    try {
      result = replace(result, PATTERN_ONTHIS, this);
      result = replace(result, PATTERN_ONCOMMAND, getOriginalCommand());
    } catch (NoSuchMethodException e) {
      ex = e;
    } catch (IllegalAccessException e) {
      ex = e;
    } catch (InvocationTargetException e) {
      ex = e;
    }
    if (ex != null) {
      LOG.log(Level.SEVERE, "Illegal template definition", ex);
      return null;
    }
    return result;
  }
  
}
 
