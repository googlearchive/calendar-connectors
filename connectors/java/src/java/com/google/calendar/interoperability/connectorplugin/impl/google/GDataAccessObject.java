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

package com.google.calendar.interoperability.connectorplugin.impl.google;

import static com.google.calendar.interoperability.connectorplugin.base.Configurable.Type.string;

import com.google.calendar.interoperability.connectorplugin.base.Configurable;
import com.google.common.base.Preconditions;
import com.google.gdata.client.GoogleService;
import com.google.gdata.client.appsforyourdomain.UserService;
import com.google.gdata.client.calendar.CalendarService;
import com.google.gdata.data.DateTime;
import com.google.gdata.data.Link;
import com.google.gdata.data.appsforyourdomain.AppsForYourDomainException;
import com.google.gdata.data.appsforyourdomain.provisioning.UserFeed;
import com.google.gdata.data.calendar.CalendarEventFeed;
import com.google.gdata.util.AuthenticationException;
import com.google.gdata.util.ServiceException;

import java.io.IOException;
import java.net.MalformedURLException;
import java.net.URL;
import java.util.ArrayList;
import java.util.List;
import java.util.logging.Level;
import java.util.logging.Logger;

/**
 * This object abstracts everything there is to know about gdata
 * connectivity
 */
class GDataAccessObject extends Configurable {
  
  private static final String APPS_FEEDS_URL_BASE =
    "https://www.google.com/a/feeds/";  
  private static final String USER = "username";
  private static final String PASS = "password";  
  private static final String DOMAIN = "domain";
  private static final Logger LOGGER = 
      Logger.getLogger(GDataAccessObject.class.getName());
  
  private CalendarService calendarService;
  private UserService userService;

  public GDataAccessObject() {
    super("gdata");
    registerParameter(USER, string);
    registerParameter(PASS, string);
    registerParameter(DOMAIN, string);
  }
  
  /**
   * Sets the user credentials for a given service as embedded in this
   * object's configuration
   */
  private<T extends GoogleService> T auth(T service) {
    try {
      service.setUserCredentials(getString(USER), getString(PASS));
    } catch (AuthenticationException e) {
      LOGGER.log(
          Level.SEVERE, 
          "Could not authenticate service " + service.getClass().getName(), e);
      return null;
    }
    return service;
  }
  
  private String getDomainBase() {
    return APPS_FEEDS_URL_BASE + getDomain() + "/";
  }
  
  public CalendarService getCalendarService() {
    if (calendarService == null) {
      calendarService = auth(new CalendarService("exchangeInteropCal"));
    }
    return calendarService;
  }
  
  public UserService getUserService() {
    if (userService == null) {
      userService = auth(new UserService("exchangeInteropUser"));
    }
    return userService;
  }
  
  /**
   * Gets the name of the domain this accessobject is for
   */
  public String getDomain() {
    return getString(DOMAIN);
  }
  
  /**
   * Retrieves all users in domain.  This method may be very slow for domains
   * with a large number of users.  Any changes to users, including creations
   * and deletions, which are made after this method is called may or may not be
   * included in the Feed which is returned.
   *
   * @return a UserFeed (or null if the feed cannot be retrieved)
   */
  public UserFeed retrieveAllUsers() {

    // Check prerequisites
    LOGGER.log(Level.INFO, "Retrieving all users.");
    URL retrieveUrl;
    try {
      retrieveUrl = new URL(getDomainBase() + "user/2.0/");
    } catch (MalformedURLException e) {
      LOGGER.log(Level.SEVERE, "Malformed feed url", e);
      return null;
    }    
    final UserService service = getUserService();
    if (service == null) {
      LOGGER.log(Level.SEVERE, "Could not retrieve service");
      return null;      
    }
    
    // Perform the query and any followup queries
    final UserFeed allUsers = new UserFeed();
    Link nextLink = null;
    do {
      
      // Current query
      Exception exception = null;
      UserFeed currentPage = null;
      try {
        LOGGER.log(Level.FINE, "Query: " + retrieveUrl);
        currentPage = service.getFeed(retrieveUrl, UserFeed.class);
      } catch (AppsForYourDomainException e) {
        exception = e;
      } catch (IOException e) {
        exception = e;
      } catch (ServiceException e) {
        exception = e;
      }
      if (exception != null) {
        LOGGER.log(Level.WARNING, "GData query failed");
        return null;    
      }
      
      // Any followup links?
      allUsers.getEntries().addAll(currentPage.getEntries());
      nextLink = currentPage.getLink(Link.Rel.NEXT, Link.Type.ATOM);
      if (nextLink != null) {
        try {
          retrieveUrl = new URL(nextLink.getHref());
        } catch (MalformedURLException e) {
          LOGGER.log(Level.SEVERE, "Malformed url in GData feedback: " + 
              nextLink.getHref());
          return null;
        }
       }
    } while (nextLink != null);

    // Done
    return allUsers;
  }
  
  private static final int FETCH_SIZE = 50;
  
  /** 
   * Retrieves the free/busy information for a particular user in a
   * particular timeframe.
   * @param userEmail the google email address of the user
   * @param fromUtc the lower bound of the search interval in Utc format
   * @param untilUtc the upper bound of the search interval in Utc format
   */
  public Iterable<CalendarEventFeed> 
      retrieveFreeBusy(String userEmail, long fromUtc, long untilUtc) {
    // Check prerequisites
    LOGGER.log(Level.INFO, "Retrieving free/busy feed for " + userEmail + ".");
    Preconditions.checkNotNull(userEmail);
    if (fromUtc > untilUtc) {
      throw new IllegalArgumentException("fromUtc > untilUtc");
    }
    final CalendarService service = getCalendarService();
    if (service == null) {
      LOGGER.log(Level.SEVERE, "Could not retrieve service");
      return null;      
    }
    URL feedUrl;
    String base = String.format(
        "https://www.google.com/calendar/feeds/%s/private/free-busy" +
        "?start-min=%s&start-max=%s&max-results=%s",
      userEmail,
      new DateTime(fromUtc).toString(),
      new DateTime(untilUtc).toString(),
      FETCH_SIZE
      );
    try {
      feedUrl = new URL(base);
      LOGGER.log(Level.FINE, "Base query: " + feedUrl);
    } catch (MalformedURLException e) {
      LOGGER.log(Level.SEVERE, "Malformed feed url", e);
      return null;      
    }
    base += "&start-index=";
    
    // Perform the queries
    List<CalendarEventFeed> result = new ArrayList<CalendarEventFeed>();
    for (int index = 1; (index - 1) % FETCH_SIZE == 0; ) {
      try {
        LOGGER.log(Level.FINE, "Fetching for start index: " + index);
        feedUrl = new URL(base + index);
        CalendarEventFeed feed = 
          service.getFeed(feedUrl, CalendarEventFeed.class);
        result.add(feed);
        if (feed.getEntries().size() == 0) {
          break;
        }
        index += feed.getEntries().size();
      } catch (IOException e) {
        LOGGER.log(Level.WARNING, "I/O communication failed", e);
        return null;
      } catch (ServiceException e) {
        LOGGER.log(Level.WARNING, 
            "Problem with accessing f/b data for " + userEmail, e);
        return null;
      }
    }
    LOGGER.log(Level.FINE, "All subqueries done");
    return result;
  }
}
 
