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

import static com.google.calendar.interoperability.connectorplugin.base.PasswordLoader.EncryptionStrategy.*;

import junit.framework.TestCase;

import java.security.KeyPair;
import java.security.KeyPairGenerator;
import java.security.PrivateKey;
import java.util.Properties;

import javax.crypto.Cipher;

/**
 * Unit tests for PasswordLoader
 */
public class PasswordLoaderTest extends TestCase {
  
  private final static String TEST_STRING =
    "This is a really nice String, that we will en- and decode :-)";
  
  private final static Properties PROPS = new Properties();
  
  public void testNone() {
    assertEquals(TEST_STRING, NONE.encryptPassword(TEST_STRING, PROPS));
    assertEquals(TEST_STRING, 
        NONE.decryptPassword(NONE.encryptPassword(TEST_STRING, PROPS), PROPS));
  }

  public void testObfuscate() {
    assertNotSame(TEST_STRING, OBFUSCATE.encryptPassword(TEST_STRING, PROPS));
    assertEquals(TEST_STRING, 
        OBFUSCATE.decryptPassword(
            OBFUSCATE.encryptPassword(TEST_STRING, PROPS), PROPS));
  }
  
  public void testKey() throws Exception {
    KeyPairGenerator generator = KeyPairGenerator.getInstance("RSA");
    KeyPair pair = generator.generateKeyPair();
    PrivateKey key = pair.getPrivate();
    byte[] original = TEST_STRING.getBytes();
    Cipher cipher = Cipher.getInstance(key.getAlgorithm());
    cipher.init(Cipher.ENCRYPT_MODE, pair.getPublic());
    byte[] encoded = cipher.doFinal(original);
    cipher = Cipher.getInstance(key.getAlgorithm());
    cipher.init(Cipher.DECRYPT_MODE, key);
    byte[] decoded = cipher.doFinal(encoded);
  }
  
  public void testMscapi() throws Exception {
    KeyPairGenerator generator = KeyPairGenerator.getInstance("RSA");
    KeyPair pair = generator.generateKeyPair();
    PasswordLoader.MSCAPI_KEY_PRIVATE = pair.getPrivate();
    PasswordLoader.MSCAPI_KEY_PUBLIC = pair.getPublic();
    String encrypted = MSCAPI.encryptPassword(TEST_STRING, PROPS);
    assertNotSame(TEST_STRING, encrypted);
    assertEquals(TEST_STRING, MSCAPI.decryptPassword(encrypted, PROPS));      
  }
  
  public void testFailover() {
    PasswordLoader.MSCAPI_KEY_PRIVATE = null;
    PasswordLoader.MSCAPI_KEY_PUBLIC = null;
    String encrypted = FAILOVER.encryptPassword(TEST_STRING, PROPS);
    assertNotSame(TEST_STRING, encrypted);
    assertEquals(TEST_STRING, FAILOVER.decryptPassword(encrypted, PROPS));     
  }
  
}
 
