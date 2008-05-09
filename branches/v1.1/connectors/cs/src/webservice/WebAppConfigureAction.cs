using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace Google.GCalExchangeSync.Web
{
  [RunInstaller(true)]
  public partial class WebAppConfigureAction : Installer
  {
    public WebAppConfigureAction()
    {
      InitializeComponent();
    }

    override public void Install(IDictionary savedState)
    {
      base.Install(savedState);

      //string[] p = Context.Parameters["Exchange"].Split('@');
      //string exchangeServer = p[0];
      //string activeDirectory = p[1].Split('=')[1];

      string directory = Path.GetDirectoryName(Context.Parameters["assemblypath"]);
      directory = Path.Combine(directory, "..");
      EventLog.WriteEntry("Directory", directory);

      string srcPath = Path.Combine(directory, "Web.template.config");
      string dstPath = Path.Combine(directory, "Web.config");

      // Make sure the Log Directory exists
      string absoluteDirectory = Path.GetFullPath(directory);
      EventLog.WriteEntry("AbsDirectory", absoluteDirectory);
      string rootPath = Path.GetPathRoot(absoluteDirectory);
      string logDirectory = Path.Combine(rootPath, "\\Google\\logs");
      logDirectory = Path.GetFullPath(logDirectory);
      Directory.CreateDirectory(logDirectory);

      XmlDocument doc = new XmlDocument();
      doc.Load(srcPath);

      setLogFileLocation(doc, Path.Combine(logDirectory, "WebService.log"));

      //setAppSetting(doc, "ActiveDirectory.DomainController", "ldap://" + activeDirectory);
      //setAppSetting(doc, "Exchange.ServerName", exchangeServer);
      //setAppSetting(doc, "GoogleApps.GCal.LogDirectory", logDirectory);

      doc.Save(dstPath);
    }

    protected void setAppSetting(XmlDocument doc, string key, string value) 
    {
      string path = 
          string.Format("//configuration/appSettings/add[@key='{0}']", key);
      XmlNode node = doc.SelectSingleNode(path);
      if (node != null)
      {
        node.Attributes["value"].Value = value;
      }
    }

    protected void setLogFileLocation(XmlDocument doc, string value)
    {
      string path = "//configuration/log4net/appender/file";
      XmlNode node = doc.SelectSingleNode(path);
      node.Attributes["value"].Value = value;
    }

    override public void Commit(IDictionary savedState)
    {
      base.Commit(savedState);
    }
  }
}