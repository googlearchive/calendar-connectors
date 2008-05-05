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

import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.security.InvalidKeyException;
import java.security.Key;
import java.security.KeyStore;
import java.security.KeyStoreException;
import java.security.NoSuchAlgorithmException;
import java.security.PrivateKey;
import java.security.PublicKey;
import java.security.UnrecoverableKeyException;
import java.security.cert.CertificateException;
import java.util.ArrayList;
import java.util.Collections;
import java.util.Enumeration;
import java.util.List;
import java.util.Properties;
import java.util.logging.Level;
import java.util.logging.Logger;

import javax.crypto.BadPaddingException;
import javax.crypto.Cipher;
import javax.crypto.IllegalBlockSizeException;
import javax.crypto.NoSuchPaddingException;

import sun.misc.BASE64Encoder;
import sun.misc.BASE64Decoder;

/**
 * A class that can be used to encrypt clear-text passwords in configuration
 * files. The encryption mechanism is pluggable.
 */
public class PasswordLoader {
  
  private static final Logger LOGGER 
    = Logger.getLogger(PasswordLoader.class.getName());
  
  /**
   * Visible for testing
   */
  static PrivateKey MSCAPI_KEY_PRIVATE;
  
  /**
   * Visible for testing
   */
  static PublicKey MSCAPI_KEY_PUBLIC;
  
  /**
   * Strategy to be used for encryption/decryption of passwords.
   */
  public static interface EncryptionStrategy extends SelfTestable {
    
    /**
     * Encrypts a password using the given strategy
     * @param unencryptedPassword the password to be encrypted
     * @param p a set of properties with potential additional settings
     * @return the encrypted password
     */
    public String encryptPassword(String unencryptedPassword, Properties p);
   
    /**
     * Decrypts a password using the given strategy
     * @param encryptedPassword the password to be decrypted
     * @param p a set of properties with potential additional settings
     * @return the decrypted password
     */
    public String decryptPassword(String encryptedPassword, Properties p);
    
    /**
     * This encryption-strategy does not encode the password at all.
     * This is unsecure but can be used for debugging purposes.
     */
    EncryptionStrategy NONE = new EncryptionStrategy() {

      public String decryptPassword(String encryptedPassword, Properties p) {
        return encryptedPassword;
      }

      public String encryptPassword(String unencryptedPassword, Properties p) {
        return unencryptedPassword;
      }

      public void selfTest() {
        // this implementation does nothing
      }      
    };
    
    /**
     * This encryption strategy uses a simple XOR encryption to make the
     * password non-readable to someone accidentally opening up the file.
     * This does not provide any security whatsoever, and should not be
     * used to secure any data.
     */
    EncryptionStrategy OBFUSCATE = new EncryptionStrategy() {
      
      public String decryptPassword(String encryptedPassword, Properties p) {
        try {
          byte[] asBytes = new BASE64Decoder().decodeBuffer(encryptedPassword);
          for(int i = 0; i < asBytes.length; i++) {
            asBytes[i] = (byte) (asBytes[i] ^ 19);
          }
          return new String(asBytes);
        } catch (IOException e) {
          throw new RuntimeException("Password obfuscation failed.");
        }
      }

      public String encryptPassword(String unencryptedPassword, Properties p) {
        byte[] asBytes = unencryptedPassword.getBytes();
        for(int i = 0; i < asBytes.length; i++) {
          asBytes[i] = (byte) (asBytes[i] ^ 19);
        }
        return new BASE64Encoder().encode(asBytes);
      }
      
      public void selfTest() {
        // this implementation does nothing
      }      
    };
    
    /**
     * Uses the Microsoft Crypto API
     */
    EncryptionStrategy MSCAPI = new EncryptionStrategy() {
      
      private static final String DO_NOT_USE = "#####DO-NOT-USE#####";
      
      /**
       * Returns the key to look for the certificate at
       */
      private String getKeyName(Properties p) {
        final Configurable config = new Configurable("win") {{
          this.registerParameter(
              "certname", 
              Configurable.Type.string, 
              DO_NOT_USE);
        }};
        if (p != null) {
          config.setLocalConfig(p);
        }
        return config.getString("win.certname");
      }
      
      private PrivateKey getPrivateKey(String storeName, Properties p) {
        KeyStore store;
        try {
          final String keyName = getKeyName(p);
          if (keyName.equals(DO_NOT_USE)) {
            return null;
          }
          store = KeyStore.getInstance(storeName);
          store.load(null, null);
          return (PrivateKey) store.getKey(keyName, null);
        } catch (KeyStoreException e) {
          PasswordLoader.LOGGER.log(Level.INFO, "Exception", e);
          return null;
        } catch (UnrecoverableKeyException e) {
          PasswordLoader.LOGGER.log(Level.INFO, "Exception", e);
          return null;
        } catch (NoSuchAlgorithmException e) {
          PasswordLoader.LOGGER.log(Level.INFO, "Exception", e);
          return null;
        } catch (CertificateException e) {
          PasswordLoader.LOGGER.log(Level.INFO, "Exception", e);
          return null;
        } catch (IOException e) {
          PasswordLoader.LOGGER.log(Level.INFO, "Exception", e);
          return null;
        }
      }
      
      private PublicKey getPublicKey(String storeName, Properties p) {
        try {
          KeyStore store;
          final String keyName = getKeyName(p);
          if (keyName.equals(DO_NOT_USE)) {
            return null;
          }
          store = KeyStore.getInstance(storeName);
          store.load(null, null);
          return store.getCertificate(keyName).getPublicKey();
        } catch(RuntimeException e) {
          PasswordLoader.LOGGER.log(Level.INFO, "Exception", e);
          return null;
        } catch (KeyStoreException e) {
          PasswordLoader.LOGGER.log(Level.INFO, "Exception", e);
          return null;
        } catch (NoSuchAlgorithmException e) {
          PasswordLoader.LOGGER.log(Level.INFO, "Exception", e);
          return null;
        } catch (CertificateException e) {
          PasswordLoader.LOGGER.log(Level.INFO, "Exception", e);
          return null;
        } catch (IOException e) {
          PasswordLoader.LOGGER.log(Level.INFO, "Exception", e);
          return null;
        }
      }
      
      private synchronized PrivateKey getPrivateKey(Properties p) {
        if (MSCAPI_KEY_PRIVATE != null) {
          return MSCAPI_KEY_PRIVATE;
        }
        MSCAPI_KEY_PRIVATE = getPrivateKey("Windows-ROOT", p);
        
        // The following lines might not work if run as Windows Service,
        // unless it is setup properly
        if (MSCAPI_KEY_PRIVATE == null) {
          MSCAPI_KEY_PRIVATE = getPrivateKey("Windows-MY", p);
        }
        return MSCAPI_KEY_PRIVATE;        
      }
      
      private synchronized PublicKey getPublicKey(Properties p) {
        if (MSCAPI_KEY_PUBLIC != null) {
          return MSCAPI_KEY_PUBLIC;
        }
        MSCAPI_KEY_PUBLIC = getPublicKey("Windows-ROOT", p);
        
        // The following lines might not work if run as Windows Service,
        // unless it is setup properly
        if (MSCAPI_KEY_PUBLIC == null) {
          MSCAPI_KEY_PUBLIC = getPublicKey("Windows-MY", p);
        }
        return MSCAPI_KEY_PUBLIC;        
      }
      
      private String handle(int mode, byte[] asBytes, Properties p) {
        if (getPrivateKey(p) == null) {
          return null;
        }
        try {
          Key key = 
            (mode == Cipher.DECRYPT_MODE) ? getPrivateKey(p) : getPublicKey(p);
          if (key == null) {
            return null;
          }
          Cipher cipher = Cipher.getInstance(key.getAlgorithm());
          cipher.init(mode, key);
          byte[] bytes = cipher.doFinal(asBytes);
          return (mode == Cipher.DECRYPT_MODE) ? 
              new String(bytes) : new BASE64Encoder().encode(bytes);
        } catch (NoSuchAlgorithmException e) {
          PasswordLoader.LOGGER.log(Level.INFO, "Exception", e);
        } catch (NoSuchPaddingException e) {
          PasswordLoader.LOGGER.log(Level.INFO, "Exception", e);
        } catch (InvalidKeyException e) {
          PasswordLoader.LOGGER.log(Level.INFO, "Exception", e);
        } catch (IllegalBlockSizeException e) {
          PasswordLoader.LOGGER.log(Level.INFO, "Exception", e);
        } catch (BadPaddingException e) {
          PasswordLoader.LOGGER.log(Level.INFO, "Exception", e);
        }
        return null;
      }

      public String decryptPassword(String encryptedPassword, Properties p) {
        try {
          return handle(Cipher.DECRYPT_MODE, 
                        new BASE64Decoder().decodeBuffer(encryptedPassword), p);
        } catch (IOException e) {
          return null;
        }
      }

      public String encryptPassword(String unencryptedPassword, Properties p) {
        return handle(Cipher.ENCRYPT_MODE, 
            unencryptedPassword.getBytes(), p);
      }      
      
      public void selfTest() {
        final String keyName = getKeyName(null);
        if (keyName.equals(DO_NOT_USE)) {
          return;
        }
        if (getPrivateKey(null) != null && getPublicKey(null) != null) {
          final String original = "Encode This String :-)";
          if (!original.equals(
              decryptPassword(encryptPassword(original, null), null))) {
            PasswordLoader.LOGGER.log(
                Level.WARNING, "MSCAPI encryption/decryption failed");
            throw new RuntimeException("MSCAPI password encryption failed"); 
          }
          return;
        }
        if (getPublicKey(null) == null) {
          PasswordLoader.LOGGER.log(Level.WARNING, "Cannot load certificate");
        } else {
          PasswordLoader.LOGGER.log(Level.WARNING, 
              "Certificate has no private key");
        }
        KeyStore store;
        try {
          for (String storeName : new String[]{"Windows-ROOT", "Windows-MY"}) {
            store = KeyStore.getInstance(storeName);
            store.load(null, null);
            List<String> asList = new ArrayList<String>();
            for (Enumeration<String> aliases = store.aliases(); 
                 aliases.hasMoreElements(); ) {
              asList.add(aliases.nextElement());
            }
            Collections.sort(asList);
            PasswordLoader.LOGGER.log(
                Level.INFO, "The following certificates were found in : "
                    + storeName + ": " + asList);
          }
          
        } catch (KeyStoreException e) {
          PasswordLoader.LOGGER.log(
              Level.WARNING, "Exception during selftest", e);
        } catch (NoSuchAlgorithmException e) {
          PasswordLoader.LOGGER.log(
              Level.WARNING, "Exception during selftest", e);
        } catch (CertificateException e) {
          PasswordLoader.LOGGER.log(
              Level.WARNING, "Exception during selftest", e);
        } catch (IOException e) {
          PasswordLoader.LOGGER.log(
              Level.WARNING, "Exception during selftest", e);
        }
        throw new RuntimeException("MSCAPI password encryption misconfigured");
      }      
    };
    
    /**
     * This encryption strategy will choose the strongest algorith available
     */
    EncryptionStrategy FAILOVER = new EncryptionStrategy() {
      
      private String[] KEYS = {"-MS-", "-OB-", "-CL-"};
      private EncryptionStrategy[] VALUES = {
          MSCAPI, OBFUSCATE, NONE
      };

      public String decryptPassword(String encryptedPassword, Properties p) {
        for (int i = 0; i < KEYS.length; i++) {
          if (encryptedPassword.startsWith(KEYS[i])) {
            return VALUES[i].decryptPassword(
                encryptedPassword.substring(KEYS[i].length()), p);
          }
        }
        return null;
      }

      public String encryptPassword(String unencryptedPassword, Properties p) {
        for (int i = 0; i < KEYS.length; i++) {
          try {
            String encrypted = 
                VALUES[i].encryptPassword(unencryptedPassword, p);
            if (encrypted != null) {
              return KEYS[i] + encrypted;
            }
          } catch (RuntimeException e) {
            PasswordLoader.LOGGER.log(Level.INFO, "Exception", e);
          }
        }
        return null;
      }
      
      public void selfTest() {
        for (EncryptionStrategy strategy : VALUES) {
          strategy.selfTest();
        }
      }     

    };
  }
  
  private EncryptionStrategy strategy;
  
  /** 
   * Constructor
   * @param strategy the encryption strategy to be used
   */
  public PasswordLoader(EncryptionStrategy strategy) {
    Preconditions.checkNotNull(strategy);
    this.strategy = strategy;
  }
  
  /**
   * Loads a properties from a file. Checks if one or more unencrypted
   * passwords are given. If that is the case, encrypt the passwords and
   * (optionally) overwrite the file. Afterwards, take each of the passwords, 
   * decrypt them and add them to the properties
   * @param file the name of the file to load the properties from
   * @param saveChanges set to true if passwords that had to be encrypted
   *   should be written to the file (overwrites the original file)
   * @param passwordKeys a list of property-name tuples. The first entry
   *   is always the name of the unencrypted password, the second of the
   *   encrypted.
   * @return the loaded Properties object
   * @throws IOException if the properties cannot be read or written
   */
  public Properties loadWithEncryption(
      File file, boolean saveChanges, Tuple<String>... passwordKeys) 
      throws IOException {
    
    // First, load the properties file
    Properties props = new Properties();
    FileInputStream stream = new FileInputStream(file);
    props.load(stream);
    stream.close();    
    
    // Next, encrypt passwords if necessary
    boolean changed = false;
    for(Tuple<String> passwordKey : passwordKeys) {
      if (props.containsKey(passwordKey.first)) {
        changed = true;
        props.put(
            passwordKey.second, 
            strategy.encryptPassword(
                props.getProperty(passwordKey.first), props));
        props.remove(passwordKey.first);
      }
    }
    
    // Overwrite the file, if necessary
    if (changed && saveChanges) {
      FileOutputStream out = new FileOutputStream(file);
      props.store(out,"");
      stream.close();  
    }
    
    // Now, decrypt the passwords for internal use
    for(Tuple<String> passwordKey : passwordKeys) {
      if (props.containsKey(passwordKey.second)) {
        String decrypted = 
            strategy.decryptPassword(
                props.getProperty(passwordKey.second), props);
        if (decrypted != null) {
          props.put(passwordKey.first, decrypted);
        } else {
          throw new NullPointerException(
              "Could not decrypt password: " + passwordKey.first);
        }
      }
    }

    // Done
    return props;
  }
}
 
