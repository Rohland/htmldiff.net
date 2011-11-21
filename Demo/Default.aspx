<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Demo._Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    <h2>Old Text</h2>
    <hr />
    <asp:Literal runat="server" ID="litOldText"></asp:Literal>
    <h2>New Text</h2>
    <hr />
    <asp:Literal runat="server" ID="litNewText"></asp:Literal>
    <h2>Html Diff Visualisation</h2>
    <hr />
    <asp:Literal runat="server" ID="litDiffText"></asp:Literal>
    </div>
    </form>
</body>
</html>
