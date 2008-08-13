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
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;

using Google.GCalExchangeSync.Library;
using Google.GCalExchangeSync.Library.Util;
using Google.GCalExchangeSync.Tests.Diagnostics;
using Google.GData.Calendar;

using TZ4Net;

namespace GCalExchangeLookup
{
    public partial class Diagnostics : System.Web.UI.Page
    {
        protected void verifyAllowed()
        {
            if (ConfigCache.RequireDiagnosticsLocalAccess && !Request.IsLocal)
            {
                throw new HttpException(403, "Diagnostics only available on local machine");
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            verifyAllowed();

            LabelAppsDomainName.Text = ConfigCache.GoogleAppsDomain;
            LabelAppsUser.Text = ConfigCache.GoogleAppsLogin;

            LabelDomainController.Text = ConfigCache.DomainController;
            LabelDomainLogin.Text = ConfigCache.DomainUserLogin;

            LabelExchServer.Text = ConfigCache.ExchangeServerUrl;
            LabelExchFBServer.Text = ConfigCache.ExchangeFreeBusyServerUrl;
            LabelExchQueryUser.Text = ConfigCache.ExchangeUserLogin;

            LabelExchAdminUser.Text = ConfigCache.ExchangeAdminLogin;

            LabelAdminGroup.Text = ConfigCache.AdminServerGroup;
            LabelMachineName.Text = Environment.MachineName;

            Server.ScriptTimeout = 600; // 10 mins

            Response.Cache.SetNoStore();
            Response.Expires = 0;

            Encrypt.Attributes["onclick"] = "return confirmEncrypt();";
        }

        protected void ButtonQueryGCalFB_Click(object sender, EventArgs e)
        {
            verifyAllowed();

            try
            {
                EventFeed feed =
                    GCalTester.QueryGCalFreeBusy(TextBoxQueryGCalEmail.Text);
                LabelGCalFBSummary.Text = "Verified";
                SyncServiceFreeBusy.CssClass = "verified";

                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("Busy Times for {0}:", TextBoxQueryGCalEmail.Text);
                sb.Append("<ul>");

                foreach( Google.GData.Calendar.EventEntry entry in feed.Entries )
                {
                    sb.AppendFormat( "<li>{0} to {1}</li>", entry.Times[ 0 ].StartTime, entry.Times[ 0 ].EndTime );
                }

                sb.Append("</ul>");
                LabelGCalFBDetail.Text = sb.ToString();
            }
            catch(Exception ex)
            {
                LabelGCalFBSummary.Text = ex.Message;
                SyncServiceFreeBusy.CssClass = "failed";
                LabelGCalFBDetail.Text = string.Format("<blockquote>{0}</blockquote>", ex.ToString());
            }
        }

        protected void ButtonLdapQuery_Click(object sender, EventArgs e)
        {
            verifyAllowed();

            try
            {
                ExchangeUserDict users =
                    ExchangeTester.QueryActiveDirectory(TextBoxLdapQuery.Text);

                if (users.Count > 0)
                {
                    WebServiceLdap.CssClass = "verified";
                    LabelLdapSummary.Text = string.Format("Verified - Located {0} users", users.Count);
                    StringBuilder sb = new StringBuilder();
                    int count = 0;

                    sb.Append("<ul>");

                    foreach (string userKey in users.Keys)
                    {
                        sb.AppendFormat("<li>User: {0}</li>", userKey);
                        count++;

                        if (count > 20)
                        {
                            sb.AppendFormat("<li>&lt;Truncated&gt;</li>");
                            break;
                        }
                    }

                    sb.Append("</ul>");

                    LabelLdapDetail.Text = sb.ToString();
                }
                else
                {
                    LabelLdapSummary.Text = "No users returned";
                    WebServiceLdap.CssClass = "failed";
                    LabelLdapDetail.Text = "";
                }
            }
            catch(Exception ex)
            {
                LabelLdapSummary.Text = ex.Message;
                WebServiceLdap.CssClass = "failed";
                LabelLdapDetail.Text = string.Format("<blockquote>{0}</blockquote>", ex.ToString());
            }
        }

        protected void EncryptSettings_Click(object sender, EventArgs e)
        {
            verifyAllowed();

            Configuration config = WebConfigurationManager.OpenWebConfiguration(Request.ApplicationPath);
            ConfigCache.EncryptAppSettings(config);
        }

        protected void ButtonQueryExchFB_Click(object sender, EventArgs e)
        {
            verifyAllowed();

            try
            {
                ExchangeUserDict users =
                    ExchangeTester.QueryFreeBusy(TextBoxQueryExchEmail.Text);

                if (users.Count > 0)
                {
                    LabelExchFBSummary.Text = "Verified";
                    WebServiceFreeBusy.CssClass = "verified";
                    StringBuilder sb = new StringBuilder();

                    foreach (ExchangeUser user in users.Values)
                    {
                        sb.AppendFormat("<ul>");
                        sb.AppendFormat("<li>Common Name: {0}</li>", user.CommonName);
                        sb.AppendFormat("<li>Email: {0}</li>", user.Email);
                        sb.AppendFormat("<li>Account Name: {0}</li>", user.AccountName);
                        sb.AppendFormat("<li>Mail Nickname: {0}</li>", user.MailNickname);

                        sb.AppendFormat("<li>Busy Times:");
                        sb.AppendFormat("<ul>");

                        foreach (FreeBusyTimeBlock tb in user.BusyTimes.Values)
                        {
                            sb.AppendFormat("<li>{0} to {1}</li>", tb.StartDate.ToLocalTime(), tb.EndDate.ToLocalTime());
                        }
                        sb.AppendFormat("</ul></li></ul>");
                    }

                    LabelExchFBDetail.Text = sb.ToString();
                }
                else
                {
                    LabelExchFBSummary.Text =
                        string.Format("User not found - {0}", TextBoxQueryExchEmail.Text);
                    LabelExchFBDetail.Text = "";
                    WebServiceFreeBusy.CssClass = "failed";
                }
            }
            catch (Exception ex)
            {
                LabelExchFBSummary.Text = ex.Message;
                WebServiceFreeBusy.CssClass = "failed";
                LabelExchFBDetail.Text = string.Format("<blockquote>{0}</blockquote>", ex.ToString());
            }
        }

        protected void ButtonWriteExchAppt_Click(object sender, EventArgs e)
        {
            verifyAllowed();

            try
            {
                ExchangeTester.WriteAppointment(TextBoxExchWriterEmail.Text, DateUtil.NowUtc);
                LabelWriteAppointmentSummary.Text = "Verified";
                SyncServiceWriteAppointment.CssClass = "verified";
                LabelWriteAppointmentDetail.Text = string.Format("Wrote Appointment for {0}", TextBoxFreeBusyName.Text);
            }
            catch (Exception ex)
            {
                LabelWriteAppointmentSummary.Text = ex.Message;
                SyncServiceWriteAppointment.CssClass = "failed";
                LabelWriteAppointmentDetail.Text = string.Format("<blockquote>{0}</blockquote>", ex.ToString());
            }
        }

        protected void ButtonWriteFreeBusy_Click(object sender, EventArgs e)
        {
            verifyAllowed();

            try
            {
                ExchangeTester.WriteFreeBusyMessage(TextBoxFreeBusyName.Text);
                LabelWriteFreeBusySummary.Text = "Verified";
                SyncServiceWriteFreeBusy.CssClass = "verified";
                LabelWriteFreeBusyDetail.Text = string.Format("Wrote Free/Busy Info for {0}", TextBoxFreeBusyName.Text);
            }
            catch (Exception ex)
            {
                LabelWriteFreeBusySummary.Text = ex.Message;
                SyncServiceWriteFreeBusy.CssClass = "failed";
                LabelWriteFreeBusyDetail.Text = string.Format("<blockquote>{0}</blockquote>", ex.ToString());
            }
        }
    }
}
