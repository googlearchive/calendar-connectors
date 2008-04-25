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
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

using log4net;
using Google.GCalExchangeSync.Library;

namespace GCalExchangeLookup
{
	public partial class ExchangeQuerier : System.Web.UI.Page
	{
        protected static readonly log4net.ILog _log =
          log4net.LogManager.GetLogger(typeof(ExchangeQuerier));

        private string _responseString = string.Empty;
		private bool _useSSL = false;

        protected string ResponseString
        {
            get { return _responseString; }
        }

        protected string GCalPostUrl
        {
            get 
			{
				// Try to make the scheme for  the Post URL
				// match the scheme the page was requested with

				_log.DebugFormat("Use SSL {0}", _useSSL);
				_log.DebugFormat("Request URL {0}", Request.Url);
				_log.DebugFormat("Referrer URL {0}", Request.UrlReferrer);

				return _useSSL ?
					ConfigCache.GCalPostUrl :
					ConfigCache.GCalPostUrl.Replace("https://", "http://");
			}
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                ProcessGCalRequest();
            }

			Response.Cache.SetNoStore();
			Response.Expires = 0;
        }

        private void ProcessGCalRequest()
        {
            GCalFreeBusyRequest gcalRequest = null;

			try
			{
				Uri referrer = new Uri(Request.ServerVariables["HTTP_REFERER"]);
				if (referrer != null)
				{
					_useSSL = referrer.Scheme.Equals("https");
				}
			}
			catch (System.ArgumentException)
			{
				// Missing referrer - use non-SSL
			}
			catch (System.UriFormatException)
			{
				// Bad referrer - use non-SSL
			}

            try
            {
                string requestString = Request.Form["text"];

                gcalRequest =
                    new GCalFreeBusyRequest(requestString);

                GCalFreeBusyResponse gcalResponse =
                    new GCalFreeBusyResponse(gcalRequest);

                _responseString = gcalResponse.GenerateResponse();
            }
            catch (GCalExchangeException ex)
            {
                GCalErrorResponse gcalResponse = 
                    new GCalErrorResponse(gcalRequest, ex);

                _responseString = gcalResponse.GenerateResponse();
                _log.Error( ex );
            }
            catch (Exception ex)
            {
                GCalErrorResponse gcalResponse = new GCalErrorResponse(ex);

                _responseString = gcalResponse.GenerateResponse();
                _log.Error( ex );
            }
        }
	}
}
