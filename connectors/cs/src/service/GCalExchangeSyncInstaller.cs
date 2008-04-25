using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Xml;

namespace Google.GCalExchangeSync.Service
{
    [RunInstaller(true)]
    public class GCalExchangeSyncInstaller : Installer
    {
        public GCalExchangeSyncInstaller()
        {
            InitializeComponent();
        }

        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.si = new System.ServiceProcess.ServiceInstaller();
            this.spi = new System.ServiceProcess.ServiceProcessInstaller();
            // 
            // si
            // 
            this.si.DisplayName = "Google Calendar Sync Service";
			this.si.ServiceName = "GcalSyncSvc";
            this.si.Description = "Synchronizes Google Calendar Free/Busy information with Exchange Server.";
            this.si.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // spi
            // 
            this.spi.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.spi.Password = null;
            this.spi.Username = null;
            // 
            // GCalExchangeSyncInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.si,
            this.spi});

        }

        override public void Install(IDictionary savedState)
        {
          base.Install(savedState);

          string directory = Path.GetDirectoryName(Context.Parameters["assemblypath"]);
          EventLog.WriteEntry("Directory", directory);

          string srcPath = Path.Combine(directory, "GoogleCalendarSyncService.exe.config");
          string dstPath = Path.Combine(directory, "GoogleCalendarSyncService.exe.config");

          string absoluteDirectory = Path.GetFullPath(directory);
          string rootPath = Path.GetPathRoot(absoluteDirectory);
          
          // Make sure the Log Directory exists
		  string logDirectory = Path.Combine(rootPath, "\\Google\\logs");
          logDirectory = Path.GetFullPath(logDirectory);
          Directory.CreateDirectory(logDirectory);

          // Make sure the XML Storage Directory exists
          string xmlStorage = Path.Combine(rootPath, "\\Google\\data");
          xmlStorage = Path.GetFullPath(xmlStorage);
          Directory.CreateDirectory(xmlStorage);

		  XmlDocument doc = new XmlDocument();
          doc.Load(srcPath);

          EventLog.WriteEntry("Doc", doc == null ? "NULL" : "Not Null");
          setLogFileLocation(doc, Path.Combine(logDirectory, "SyncService.log"));

		  setValue(doc, "SyncService.XmlStorageDirectory", xmlStorage);

          doc.Save(dstPath);
        }

        protected void setValue(XmlDocument doc, string key, string value)
        {
          string path =
              string.Format("//configuration/appSettings/add[@key='{0}']", key);
          XmlNode node = doc.SelectSingleNode(path);
          node.Attributes["value"].Value = value;
        }

        protected void setLogFileLocation(XmlDocument doc, string value)
        {
          string path = "//configuration/log4net/appender/file";
          XmlNode node = doc.SelectSingleNode(path);
          if (node != null)
          {
            node.Attributes["value"].Value = value;
          }
        }

        override public void Commit(IDictionary savedState)
        {
          base.Commit(savedState);
        }

        private ServiceInstaller si;
        private ServiceProcessInstaller spi;
    }
}