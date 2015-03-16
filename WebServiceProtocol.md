# Google Calendar Web Service Protocol #

## Introduction ##

The Google Calendar Connector Web Service relies on a feature in Google Calendar to allow external Free Busy information to be included in the meeting scheduler dialog. This requires a Google Apps domain to have this feature enabled and for the endpoint of the Google Calendar Connector Web Service to be configured. This document describes the protocol used when a request is issued by Google Calendar.

## Details ##

When the Google Calendar client needs free/busy information for a user whose calendar does not reside in Google Calendar, a POST request will be made to the URL configured for the domain.

### Request ###
```
POST /free-busy-endpoint HTTP/1.1
Host: example.org
Content-type: application/x-www-form-urlencoded
...
text=[<version>,<messageId>,[<emails>],<start>/<end>,<since>,<tz>]
```

where:
  * 

&lt;version&gt;

**: Version # - currently 1
  ***

&lt;messageId&gt;

**: ID for the request
  ***

&lt;emails&gt;

**: comma separated list of emails to get free/busy info for
  ***

&lt;start&gt;

**: Start of range to get free/busy info for (as YYYYMMDD)
  ***

&lt;end&gt;

**: End of range to get free/busy info for (as YYYMMDD)
  ***

&lt;since&gt;

**: Return free/busy info modified since this time (as YYMMDDTHHMMSS)
  ***

&lt;tz&gt;

**: Timezone for the request as an [Olson Time zone](http://www.twinsun.com/tz/tz-link.htm)**

### Response ###
```
HTTP/1.1 200 OK
Content-type: text/html
...
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
    <meta http-equiv="Content-Type" content="text/html; charset=ISO-8859-1" />
</head>
<body>
  <form id="Form1" method="POST" action="<submitUrl>">
    <input name="text" value="<freeBusyResponse>" />
  </form>
  <script type="text/javascript">
    document.getElementById('Form1').submit();
  </script>
</body>
</html>
```

where:
  * 

&lt;submitUrl&gt;

**: URL for a Google Apps domain specific mailslot URL
  ***

&lt;freeBusyResponse&gt;

**: A free/busy response JSON expression.**

### Free/Busy JSON Expression ###

```
  [<version>,<messageId>,
    ['_ME_AddData', '<start>/<end>','<since>',
      [
        <user1>,<email>,<accessLevel>, // for each user
          [
            [<subject>,<start>,<end>,<location>,<organizer>,<status>] // for each appointment
          ],
       <user2>,<email>,<accessLevel>, // for each user
          [
            [<subject>,<start>,<end>,<location>,<organizer>,<status>] // for each appointment
          ]
      ]
    ]
  ]
```

where:
  * 

&lt;version&gt;

**: Version # - currently 1
  ***

&lt;messageId&gt;

**: ID from the request
  ***

&lt;start&gt;

**: Start of range for free/busy request (as YYYYMMDD)
  ***

&lt;end&gt;

**: End of range for free/busy request (as YYYMMDD)
  ***

&lt;since&gt;

**: Since time from the request (as YYMMDDTHHMMSS)
  ***

&lt;userName&gt;

**: Display name of the user the Free/Busy results are for
  ***

&lt;email&gt;

**: Email address of the user the Free/Busy results are for
  ***

&lt;accessLevel&gt;

**: Access level the user has (not currently used)
  ***

&lt;subject&gt;

**: Subject of the event (if available)
  ***

&lt;start&gt;

**: Start date time for the free busy block (as YYMMDDTHHMMSS)
  ***

&lt;end&gt;

**: End date time for the free busy block (as YYMMDDTHHMMSS)
  ***

&lt;location&gt;

**: Location of the event (if available)
  ***

&lt;organizer&gt;

**: Organizer of the event
  ***

&lt;status&gt;

**: Free/Busy status**

_Note:_ All times in the response should be in the same timezone specified in the request. Some information is only available if the Web Service is configured to return appointment detail AND the event is not marked as private.

