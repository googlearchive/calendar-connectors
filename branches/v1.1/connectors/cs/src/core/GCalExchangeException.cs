/* Copyright (c) 2008 Google Inc. All Rights Reserved
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
using System;
using System.Collections.Generic;
using System.Text;
using log4net;

namespace Google.GCalExchangeSync.Library
{
    /// <summary>
    /// Exceptions related to GCalExchangeSync
    /// </summary>
    public class GCalExchangeException : ApplicationException
    {
        /// <summary>
        /// Logger for GCalExchangeException
        /// </summary>
        protected static readonly log4net.ILog log =
          log4net.LogManager.GetLogger(typeof(GCalExchangeException));

        private GCalExchangeErrorCode errorCode = GCalExchangeErrorCode.GenericError;

        /// <summary>
        /// Error code for the exception
        /// </summary>
        public GCalExchangeErrorCode ErrorCode
        {
            get { return errorCode; }
        }

        /// <summary>
        /// Create a new exception
        /// </summary>
        /// <param name="errorCode">Error code of the exception</param>
        public GCalExchangeException(GCalExchangeErrorCode errorCode)
        {
            this.errorCode = errorCode;
        }

        /// <summary>
        /// Create new exception
        /// </summary>
        /// <param name="errorCode">Error code for the exception</param>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Root cause exception</param>
        public GCalExchangeException(GCalExchangeErrorCode errorCode, string message, Exception innerException)
            : base(message, innerException)
        {
            this.errorCode = errorCode;
        }

        /// <summary>
        /// Create a new exception
        /// </summary>
        /// <param name="errorCode">Error code for the exception</param>
        /// <param name="message">Error message</param>
        public GCalExchangeException(GCalExchangeErrorCode errorCode, string message) 
            : base(message)
        {
            this.errorCode = errorCode;
        }
    }

    /// <summary>
    /// Exception types
    /// </summary>
    public enum GCalExchangeErrorCode
    {
        /// <summary>
        /// General
        /// </summary>
        GenericError         = 0, 

        /// <summary>
        /// Could not connect to exchange server
        /// </summary>
        ExchangeUnreachable  = 1,

        /// <summary>
        /// Invalid version
        /// </summary>
        UnsupportedVersion   = 2,

        /// <summary>
        /// Request was malformed
        /// </summary>
        MalformedRequest     = 3,

        /// <summary>
        /// Problem communicating with Active Directory
        /// </summary>
        ActiveDirectoryError = 4,

        /// <summary>
        /// Error converting timezone
        /// </summary>
        OlsonTZError         = 5
    }
}
