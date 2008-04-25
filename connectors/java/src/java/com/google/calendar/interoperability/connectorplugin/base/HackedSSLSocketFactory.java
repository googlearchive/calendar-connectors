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

import java.io.IOException;
import java.net.InetAddress;
import java.net.Socket;
import java.net.UnknownHostException;
import java.security.KeyManagementException;
import java.security.NoSuchAlgorithmException;
import java.security.cert.X509Certificate;
import java.util.logging.Level;
import java.util.logging.Logger;

import javax.net.SocketFactory;
import javax.net.ssl.SSLContext;
import javax.net.ssl.SSLSocketFactory;
import javax.net.ssl.TrustManager;
import javax.net.ssl.X509TrustManager;

/**
 * This class is a modified version of the standard SSL socket factory that
 * trusts any incoming certificate. This is used for LDAPS connection.
 */
public class HackedSSLSocketFactory extends SSLSocketFactory {
  
  private static final Logger LOGGER = 
    Logger.getLogger(HackedSSLSocketFactory.class.getName());
  
  /**
   * This instance represents a very trusting TrustManager: whatever
   * certificate you hand to his instance, it will accept. 
   */
  private static final X509TrustManager TRUST = new X509TrustManager() {
    public void checkClientTrusted(X509Certificate[] chain, String authType){
      // Do nothing
    }
    public void checkServerTrusted(X509Certificate[] chain, String authType){
      // Do nothing
    }
    public X509Certificate[] getAcceptedIssuers() {
      return new X509Certificate[0];
    }    
  };
  
  /**
   * Singleton instance of this factory
   */
  private static final HackedSSLSocketFactory INSTANCE = 
    new HackedSSLSocketFactory();
  
  /**
   * Inner factory, created by the getWrapped() method.
   */
  private static SSLSocketFactory WRAPPED; 
  
  /**
   * @return a singleton instance of this factory (used by the ssl package)
   */
  public static final SocketFactory getDefault() {
    return INSTANCE;
  }
  
  /**
   * Creates a special singleton instance of a SSLSocketFactory and
   * returns it each time this method is called. The HackedSSLSocketFactory
   * does not really "know" how to do secure connections, so it needs to
   * forward all those requests to another instance. When the instance
   * is created, it sets its TrustManager to the TRUST instance created
   * in this very class. This essentially removes the need for the
   * certificate to be in a keystore.
   * 
   * @return the singleton instance that this factory decorates
   */
  private static synchronized SSLSocketFactory getWrapped() {
    if (WRAPPED != null) {
      return WRAPPED;
    }
    String algorithm = "TLSv1";
    SSLContext sslCtx;
    try {
      sslCtx = SSLContext.getInstance(algorithm);
      sslCtx.init(
          null, new TrustManager[]{TRUST}, new java.security.SecureRandom());
      WRAPPED = sslCtx.getSocketFactory();
    } catch (NoSuchAlgorithmException e) {
      LOGGER.log(Level.SEVERE, "Cannot instantiate SSL Socket Factory");
      e.printStackTrace();
    } catch (KeyManagementException e) {
      LOGGER.log(Level.SEVERE, "Cannot instantiate SSL Socket Factory");
      e.printStackTrace();
    }
    return WRAPPED;    
  }

  @Override
  public Socket createSocket(
      Socket s, String host, int port, boolean autoClose) throws IOException {
    return getWrapped().createSocket(s, host, port, autoClose);
  }

  @Override
  public String[] getDefaultCipherSuites() {
    return getWrapped().getDefaultCipherSuites();
  }

  @Override
  public String[] getSupportedCipherSuites() {
    return getWrapped().getSupportedCipherSuites();
  }

  @Override
  public Socket createSocket(String host, int port) throws IOException,
      UnknownHostException {
    return getWrapped().createSocket(host, port);
  }

  @Override
  public Socket createSocket(InetAddress host, int port) throws IOException {
    return getWrapped().createSocket(host, port);
  }

  @Override
  public Socket createSocket(String host, int port, InetAddress localHost,
      int localPort) throws IOException, UnknownHostException {
    return getWrapped().createSocket(host, port, localHost, localPort);
  }

  @Override
  public Socket createSocket(InetAddress address, int port,
      InetAddress localAddress, int localPort) throws IOException {
    return getWrapped().createSocket(address, port, localAddress, localPort);
  }  
}
 
