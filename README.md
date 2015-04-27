#### calendar-connectors
Automatically exported from code.google.com/p/google-calendar-connectors

#####IMPORTANT UPDATE: Posted on 2014-11-18 

<b>Google Calendar Connector Sync Service relies on a deprecated version of the Google Calendar API and worked with Exchange 2003, a product that is no longer supported by Microsoft. On November 17th 2014, Google stopped servicing the deprecated Calendar API's requests causing this product to stop working. As a result, we have removed the Sync Service, however the Web Service should remain functional. Users that need Sync Service functionality should take one of the following options:
 - Update to use the Calendar Interop functionality built into the Google Apps Admin Console with a supported version of Microsoft Exchange, or
 - Use a custom-built connector that syncs with your version of Microsoft Exchange using the latest version of the Google Calendar API (v3 at the time of writing).</b>

---

The [Google Calendar Connectors](connectors/docs/wiki/Overview.wiki) are a set of services that allow Google Calendar to interoperate with Exchange 2000, Exchange 2003 or Exchange 2007 servers through the bi-directional sharing of Calendar Free / Busy information. Each of the tools is installed in the local Exchange environment and must be maintained / configured by an Exchange Administrator.

**February 2, 2011**: Version 1.3.1 of GCC Sync Service and Version 1.3.3 of GCC Web Service have just been released.

This open source project is a developer and partner release and is not targeted for direct customer or end-user installation. The Google Calendar Connectors represent a set of tools and should not be considered native functionality of Google Apps Premier & Education Editions.

- [Google Calendar Connector Web Service](connectors/docs/wiki/WebServiceGuide.wiki): This connector allows users in Google Calendar to see free/busy information for users who maintain their calendars in Exchange. It is a .NET web service that takes requests sent from the browser with Google Calendar and returns free/busy obtained from a Microsoft Exchange 2003\2007 server.
- [Google Calendar Connector Sync Service](connectors/docs/wiki/SyncServiceGuide.wiki): This connector allows users of Microsoft Exchange to see free/busy information for users who maintain their calendars in Google Calendar. It is a Windows Service that periodically queries the Google Calendar GData API to get updated free/busy information and publishes this information as free/busy information in Exchange.
Notice: The Google Calendar Connectors represent a set of tools for interoperation, and not a self-contained solution. Bi-directional free/busy scheduling requires a Google Apps Premier Edition domain which has been enabled for calendar interoperability.

A special thanks to Bill Mers for his contributions to the project.
