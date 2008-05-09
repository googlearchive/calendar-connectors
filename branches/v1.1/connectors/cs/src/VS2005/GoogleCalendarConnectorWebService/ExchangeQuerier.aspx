<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ExchangeQuerier.aspx.cs" Inherits="GCalExchangeLookup.ExchangeQuerier" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
    <title>Free Busy Lookup Response</title>
    <meta http-equiv="Content-Type" content="text/html; charset=ISO-8859-1" />
</head>
<body>
  <form id="Form1" method="POST" action="<%= GCalPostUrl %>">
    <input name="text" value="<%= ResponseString %>" />
  </form>
  <script type="text/javascript">
    document.getElementById('Form1').submit();
  </script>
</body>
</html>