﻿<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="FixContacts.aspx.cs" Inherits="Sitecore.Support.FixContacts" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <asp:Button ID="Button2" runat="server" OnClick="Button2_Click" Text="Check" />
        </div>
        <asp:Button ID="Button1" runat="server" OnClick="Button1_Click" Text="Fix" />
        <p>
            <asp:Label ID="Label1" runat="server"></asp:Label>
        </p>
    </form>
</body>
</html>
