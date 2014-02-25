<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="dashboard.aspx.cs" Inherits="WebRole1.dashboard" %>

<%@ Register assembly="System.Web.DataVisualization, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" namespace="System.Web.UI.DataVisualization.Charting" tagprefix="asp" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div style="height: 31px">
        


        <asp:Label ID="label9" runat="server" Text="CPU% : "></asp:Label>
        <asp:Label ID="cpu_lbl" runat="server" Text="Label           "></asp:Label>
        <asp:Label ID="Label10" runat="server" Text="|    Mem Available :   "></asp:Label>
        <asp:Label ID="mem_lbl" runat="server" Text="Label"></asp:Label>
        <br />
        <br />
        


    </div>
        <p>
            <asp:Button ID="start_btn" runat="server" Height="46px" OnClick="start_btn_Click" Text="Start" Width="107px" />
        </p>
        <p>
            <asp:Button ID="stop_btn" runat="server" Height="46px" OnClick="stop_btn_Click" Text="Stop" Width="107px" />
        </p>
        <asp:Button ID="update_btn" runat="server" OnClick="update_btn_Click" Text="Update Info" />
        <p>
            <asp:Label ID="Label1" runat="server" Text="Size of Queue : "></asp:Label>
            <asp:Label ID="queueSize_lbl" runat="server" Text="unvisited_lbl"></asp:Label>
        </p>
        <p>
            <asp:Label ID="Label2" runat="server" Text="#Urls Crawled :   "></asp:Label>
            <asp:Label ID="crawled_lbl" runat="server" Text="0"></asp:Label>
        </p>
        <p>
            <asp:Label ID="Label3" runat="server" Text="State : "></asp:Label>
            <asp:Label ID="state_lbl" runat="server" Text="state_lbl"></asp:Label>
        </p>
        <p>
            <asp:Label ID="Label5" runat="server" Text="Url : "></asp:Label>
            <asp:TextBox ID="url_txtbox" runat="server" OnTextChanged="TextBox1_TextChanged" Width="663px"></asp:TextBox>
            <asp:Button ID="ok_btn" runat="server" OnClick="ok_btn_Click" Text="ok" />
        </p>
        <p>
            <asp:Label ID="title_lbl" runat="server" Text="    title"></asp:Label>
        </p>
        <p>
            <asp:Label ID="Label4" runat="server" Text="Recent 10 Urls : "></asp:Label>
            <asp:Label ID="recent_lbl" runat="server" Text="0"></asp:Label>
        </p>
    </form>
</body>
</html>
