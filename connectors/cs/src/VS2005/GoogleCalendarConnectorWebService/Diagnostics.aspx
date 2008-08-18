<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Diagnostics.aspx.cs" Inherits="GCalExchangeLookup.Diagnostics" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" >
  <head id="Head1" runat="server">
    <title>Google Calendar Connector Diagnostics</title>
    <meta http-equiv="Content-Type" content="text/html; charset=ISO-8859-1" />
    <style type="text/css">
      body { font-family: Tahoma; font-size: 10pt; }
      td { font-family: Tahoma; font-size: 10pt; }
      td.label { font-weight: bold; text-align: right; vertical-align: top; }
      input.button {
        background-color: #cccccc;
        color: black;
        font-weight: bold;
        font-size: 90%;
        font-family: sans-serif;
        text-align: center;
        border-style: outset;
        border-width: 2px;
        margin: 3px;
        padding: 2px;
        cursor: hand;
        vertical-align: middle;
      }

    .unverified { background-color: #F5FFC6; }
    .verified { background-color: #D1FFCE; }
    .failed { background-color: #FFA4A0; }
    </style>
    <script language="javascript">
      function isEnterKeypress(event)
      {
        var key;
        if (window.event)
          key = window.event.keyCode;
        else if (event)
          key = event.which;
        else
          return false;

        return key == 13;
      }

      function blockEnterKey(event)
      {
        return !isEnterKeypress(event);
      }

      function submitOnEnterKey(event, buttonId) {
        if (isEnterKeypress(event))
          document.getElementById(buttonId).click();
      }

      var encryptionWarning =
          "This will encrypt the entire appSettings node of the XML\n" +
          "configuration file, rendering it unreadable. Once encrypted,\n" +
          "the settings are no longer in plain text on the file system.\n" +
          "However, settings can still be changed through the Internet\n" +
          "Information Services (IIS) Manager.\n" +
          "\n" +
          "Are you sure you want to encrypt your config settings now?";

      function confirmEncrypt() {
        return confirm(encryptionWarning);
      }
    </script>
  </head>
<body onkeydown="return blockEnterKey(event);">
  <h1>Google Calendar Connector Diagnostics</h1>
  <form id="form1" runat="server">
    <div>

  <fieldset>
    <legend>Settings from Configuration File:</legend>
    <ul>
      <li>ActiveDirectory.DomainController: <asp:Label ID="LabelDomainController" Runat="server"/></li>
      <li>ActiveDirectory.DomainUser.Login: <asp:Label ID="LabelDomainLogin" Runat="server"/></li>
      <li>Exchange.ServerName: <asp:Label ID="LabelExchServer" Runat="server"/></li>
      <li>Exchange.FreeBusyServerName: <asp:Label ID="LabelExchFBServer" Runat="server"/></li>
      <li>Exchange.GCalQueryUser.Login: <asp:Label ID="LabelExchQueryUser" Runat="server"/></li>
      <li>Exchange.GCalAdminUser.Login: <asp:Label ID="LabelExchAdminUser" Runat="server"/></li>
      <li>GoogleApps.DomainName: <asp:Label ID="LabelAppsDomainName" Runat="server"/></li>
      <li>GoogleApps.AdminUser.Login: <asp:Label ID="LabelAppsUser" Runat="server"/></li>
      <li id="MachineName">Local Machine Name: <asp:Label ID="LabelMachineName" Runat="server"/></li>
    </ul>
      <asp:Button ID="Encrypt" Text="Encrypt Config Settings" runat="server" onkeydown="submitOnEnterKey(event, 'Encrypt');" OnClick="EncryptSettings_Click" CssClass="button" /><br />
  </fieldset>
  <br />
    <h2>WebService Diagnostics</h2>
  <asp:Table Runat="server" id="WebServiceDiagnostics" Width="90%" CellPadding="5" CellSpacing="2" HorizontalAlign="Center">
    <asp:TableRow Runat="server" id="WebServiceLdap" CssClass="unverified">
      <asp:TableCell>
        <b>Verify users can be found in Active Directory</b><br />
        <asp:TextBox ID="TextBoxLdapQuery" Text="(cn=*)" Columns="30" onkeydown="submitOnEnterKey(event, 'ButtonLdapQuery');" runat="server" />
        <asp:Button ID="ButtonLdapQuery" Text="Verify" runat="server" onkeydown="submitOnEnterKey(event, 'ButtonLdapQuery');" OnClick="ButtonLdapQuery_Click" CssClass="button" /><br />
        <div id="WebServiceLdapResultSummary"><b><asp:Label ID="LabelLdapSummary" Runat="server">Not verified</asp:Label></b></div>
        <div id="WebServiceLdapResultDetail"><asp:Label ID="LabelLdapDetail" Runat="server">Not verified</asp:Label></div>
      </asp:TableCell>
    </asp:TableRow>
    <asp:TableRow Runat="server" id="WebServiceFreeBusy" CssClass="unverified">
      <asp:TableCell>
        <b>Verify Free / Busy information can be found in Exchange (+/- 7 days)</b><br />
        <asp:TextBox ID="TextBoxQueryExchEmail" Text="ExchangeUser@example.com" Columns="30" onkeydown="submitOnEnterKey(event, 'ButtonQueryExchFB');" runat="server" />
        <asp:Button ID="ButtonQueryExchFB" Text="Verify" runat="server" CssClass="button" onkeydown="submitOnEnterKey(event, 'ButtonQueryExchFB');" OnClick="ButtonQueryExchFB_Click" /><br />
        <div id="WebServiceFBResultSummary"><b><asp:Label ID="LabelExchFBSummary" Runat="server">Not verified</asp:Label></b></div>
        <div id="WebServiceFBResultDetail"><asp:Label ID="LabelExchFBDetail" Runat="server"></asp:Label></div>
      </asp:TableCell>
    </asp:TableRow>
  </asp:Table>

    <h2>Sync Service Diagnostics</h2>
  <asp:Table Runat="server" id="SyncServiceDiagnostics" Width="90%" CellPadding="2" CellSpacing="2" HorizontalAlign="Center">
    <asp:TableRow Runat="server" id="SyncServiceFreeBusy" CssClass="unverified">
      <asp:TableCell>
        <b>Verify Free / Busy information can be found in Google Calendar (+/- 7 Days)</b><br />
        <asp:TextBox ID="TextBoxQueryGCalEmail" Text="GoogleUser@example.com" Columns="30" onkeydown="submitOnEnterKey(event, 'ButtonQueryGCalFB');" runat="server" />
        <asp:Button ID="ButtonQueryGCalFB" Text="Verify" runat="server" onkeydown="submitOnEnterKey(event, 'ButtonQueryGCalFB');" OnClick="ButtonQueryGCalFB_Click" CssClass="button" /><br />
        <div id="SyncServiceQueryGCalFBSummary"><b><asp:Label ID="LabelGCalFBSummary" Runat="server">Not verified</asp:Label></b></div>
        <div id="SyncServiceQueryGCalFBDetail"><asp:Label ID="LabelGCalFBDetail" Runat="server"></asp:Label></div>
      </asp:TableCell>
    </asp:TableRow>
    <asp:TableRow Runat="server" id="SyncServiceWriteAppointment" CssClass="unverified">
      <asp:TableCell>
        <b>Verify Appointment can be written to Exchange</b><br />
        <asp:TextBox ID="TextBoxExchWriterEmail" Text="ExchangeUser@example.com" Columns="30" onkeydown="submitOnEnterKey(event, 'ButtonWriteExchAppt');" runat="server" />
        <asp:Button ID="ButtonWriteExchAppt" Text="Verify" runat="server" CssClass="button" onkeydown="submitOnEnterKey(event, 'ButtonWriteExchAppt');" OnClick="ButtonWriteExchAppt_Click" /><br />
        <div id="SyncServiceWriteAppointmentSummary"><b><asp:Label ID="LabelWriteAppointmentSummary" Runat="server">Not verified</asp:Label></b></div>
        <div id="SyncServiceWriteAppointmentDetail"><asp:Label ID="LabelWriteAppointmentDetail" Runat="server"></asp:Label></div>
      </asp:TableCell>
    </asp:TableRow>
    <asp:TableRow Runat="server" id="SyncServiceWriteFreeBusy" CssClass="unverified">
      <asp:TableCell>
        <b>Verify Free/Busy can be written to Exchange</b><br />
        <asp:TextBox ID="TextBoxFreeBusyName" Text="ExchangeUser@example.com" Columns="30" onkeydown="submitOnEnterKey(event, 'ButtonWriteFreeBusy');" runat="server" />
        <asp:Button ID="ButtonWriteFreeBusy" Text="Verify" runat="server" CssClass="button" onkeydown="submitOnEnterKey(event, 'ButtonWriteFreeBusy');" OnClick="ButtonWriteFreeBusy_Click" /><br />
        <div id="SyncServiceWriteFreeBusySummary"><b><asp:Label ID="LabelWriteFreeBusySummary" Runat="server">Not verified</asp:Label></b></div>
        <div id="SyncServiceWriteFreeBusyDetail"><asp:Label ID="LabelWriteFreeBusyDetail" Runat="server"></asp:Label></div>
      </asp:TableCell>
    </asp:TableRow>
  </asp:Table>
    </div>
  </form>
</body>
</html>
