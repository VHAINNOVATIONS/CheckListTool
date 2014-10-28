<%@ Control Language="C#" AutoEventWireup="true" CodeFile="sp_ucPatItemEditor.ascx.cs"
    Inherits="sp_ucPatItemEditor" %>
<%@ Reference Control="~/app_ucTimer.ascx" %>
<%@ Register TagPrefix="uc" TagName="TimePicker" Src="~/app_ucTimePicker.ascx" %>
<%@ Register TagPrefix="uc" TagName="Collection" Src="~/sp_ucCollection.ascx" %>
<%@ Register TagPrefix="uc" TagName="NoteTitle" Src="~/sp_ucNoteTitle.ascx" %>

<!--panel for collection items-->
<asp:Panel ID="pnlCollection" runat="server" Width="860">
    <asp:Label ID="lblForCollection" runat="server"
        AssociatedControlID="lblColl"
        CssClass="app_label2"
        Text="Collection:">
    </asp:Label>
    <asp:Label ID="lblColl" runat="server"
        CssClass="app_label4">
    </asp:Label>
    <div class="app_horizontal_spacer">
    </div>
    <div class="patient_item_fields">
        <div class="pif_left_padding_top">
            <asp:Label ID="lblColItems" CssClass="app_label2" runat="server"
                AssociatedControlID="ddlColItems"
                Text="Collection Item(s):">
            </asp:Label>
        </div>
        <div>
            <asp:DropDownList ID="ddlColItems" runat="server"
                AutoPostBack="true" 
                OnSelectedIndexChanged="OnSelIndexChangedColItem">
            </asp:DropDownList>
        </div>
        <div class="app_horizontal_spacer">
        </div>
    </div>
</asp:Panel>
<!--end of panel for collection items-->

<!--panel for patient items-->
<div class="patient_item_fields">
    <asp:Panel ID="pnlPatItems" runat="server" Width="860">
    <div>
        <asp:Label ID="lblForItem" runat="server"
            AssociatedControlID="lblItem"
            CssClass="app_label2"
            Text="Item:">
        </asp:Label>
        <asp:Label ID="lblItem" runat="server"
            CssClass="app_label4">
        </asp:Label>
    </div>
    <div class="app_horizontal_spacer">
    </div>
    <div class="pif_left_padding_top">
        <asp:Label ID="lblResults" runat="server"
            AccessKey="R"
            AssociatedControlID="ddlItems"
            CssClass="app_label2"
            Text="<span span class=access_key>R</span>esult(s):">
        </asp:Label>
    </div>
    <div class="patient_item_fields_right">
        <asp:DropDownList ID="ddlItems" runat="server"
            AutoPostBack="true"
            OnSelectedIndexChanged="OnSelIndexChangedItem">
        </asp:DropDownList>
    </div>
</asp:Panel>
<!--end of panel for patient items-->

<div class="app_horizontal_spacer">
</div>
    
<!--panel for mapped items-->
<asp:Panel ID="pnlMapped" Visible="false" runat="server"
    CssClass="app_panel"
    Width="860"
    Height="305"
    ScrollBars="None">
    This is a mapped item and results cannot be entered or edited. 
    Please choose from the list of historical result(s) above or 
    enter new values in the source system.    
</asp:Panel>
<!--end of mapped items panel-->

<!--panel empty coll-->
<asp:Panel ID="pnlEmptyColl" Visible="false" runat="server"
    CssClass="app_panel"
    Width="860"
    Height="305"
    ScrollBars="None">
    Please choose from the list above to enter or view values
     in this collection.    
</asp:Panel>
<!--end of empty coll panel-->

<!--entry date-->    
<div class="pif_left_padding_top">
    <asp:Label ID="lblDate" runat="server"
            AccessKey="D"
            AssociatedControlID="txtEntryDate"
            CssClass="app_label2"
            Text="Result&nbsp;<span class=access_key>D</span>ate:">
        </asp:Label>
    </div>
    <div class="patient_item_fields_right">
        <asp:TextBox ID="txtEntryDate" runat="server"
            AutoPostBack="true"
            OnTextChanged="OnEntryDateChanged"
            Width="70">
        </asp:TextBox>
        <asp:CalendarExtender ID="calEntryDate" runat="server"
            DefaultView="Days"
            Format="MM/dd/yyyy" 
            TargetControlID="txtEntryDate">
        </asp:CalendarExtender>
        <uc:TimePicker ID="ucTimePicker" runat="server" />
    </div>
    <div class="app_horizontal_spacer">
    </div>
</div>
<!--end of entry date-->

<asp:Label CssClass="app_label2" ID="lblItemComps" runat="server" Text="Item Component(s)"></asp:Label>

<!--single item components panel-->
<asp:Panel ID="pnlComponents" runat="server"
    CssClass="app_panel"
    Width="860"
    Height="175"
    ScrollBars="Vertical">
    <asp:GridView ID="gvComponents" runat="server"
        DataKeyNames="item_component_id, legal_min, critical_low, low, high, critical_high, legal_max"
        OnRowDataBound="OnRowDataBoundComp"
        Height="50"
        ShowHeader="false">
        <Columns>
            <asp:TemplateField>
                <ItemTemplate>
                    <asp:Label ID="lblItemDescr" runat="server"
                        Width="840">
                    </asp:Label>
                    <asp:RadioButton ID="rbSelComponent" runat="server"
                        GroupName="gnComponent"
                        OnClick="javascript:ToggleGVRadio(this.id,'rbSelComponent');"
                        Width="695" />
                    <asp:Label ID="lblComponent" runat="server"
                        AssociatedControlID="txtValue"
                        Width="200">
                    </asp:Label>
                    <asp:TextBox ID="txtValue" runat="server"
                        MaxLength="4000"
                        Width="200">
                    </asp:TextBox>
                    <asp:Label ID="lblUnits" runat="server"
                        Width="100">
                    </asp:Label>
                    <div class="app_horizontal_spacer">
                    </div>
                    <asp:Label ID="lblRanges" runat="server"
                        Width="695">
                    </asp:Label>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
</asp:Panel>
<!--end of single item components panel

<!--quick entry panel-->
<asp:Panel ID="pnlQuickEntry" runat="server"
    CssClass="app_panel"
    Width="860"
    Height="255"
    ScrollBars="Vertical">
    <asp:GridView ID="gvQuickEntry" runat="server"
        DataKeyNames="item_component_id, legal_min, critical_low, low, high, critical_high, legal_max, item_id, item_type_id"
        OnRowDataBound="OnRowDataBoundQuickEntry"
        Height="50"
        ShowHeader="false">
        <Columns>
            <asp:TemplateField>
                <ItemTemplate>
                    <asp:Label ID="lblItemDescr" runat="server"
                        Width="840">
                    </asp:Label>
                    <asp:RadioButton ID="rbSelComponent" runat="server"
                        GroupName="gnComponent"
                        OnClick="javascript:ToggleGVRadio(this.id,'rbSelComponent');"
                        Width="840" />
                    <asp:Label ID="lblComponent" runat="server"
                        AssociatedControlID="txtValue"
                        Width="200">
                    </asp:Label>
                    <asp:TextBox ID="txtValue" runat="server"
                        MaxLength="4000"
                        Width="200">
                    </asp:TextBox>
                    <asp:Label ID="lblUnits" runat="server"
                        Width="100">
                    </asp:Label>
                    <div class="app_horizontal_spacer">
                    </div>
                    <asp:Label ID="lblRanges" runat="server"
                        Width="695">
                    </asp:Label>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
</asp:Panel>
<!--end of quick entry panel-->

<!--note title control-->
<uc:NoteTitle ID="ucNoteTitle" runat="server"
    Height="100"
    Width="860"
    Visible="false" />
<!--end of note title control-->

<div class="app_horizontal_spacer">
</div>

<!--new comment-->
<asp:Label CssClass="app_label2" ID="lblNewComment" runat="server" Text="New Comment"></asp:Label>
<asp:TextBox ID="txtComment" runat="server"
    MaxLength="4000"
    Rows="2"
    TextMode="MultiLine"
    Width="860">
</asp:TextBox>
<!--end of new comment-->

<!--spacer-->
<div class="app_horizontal_spacer">
</div>

<asp:Label CssClass="app_label2" ID="lblCommentHistory" runat="server" Text="Comment History"></asp:Label>
<!--comments panel-->
<asp:Panel ID="pnlComments" runat="server"
    CssClass="app_panel"
    Height="50"
    ScrollBars="Vertical"
    Width="860">
    <asp:GridView ID="gvComments" runat="server"
        DataKeyNames="pat_item_id">
        <Columns>
            <asp:BoundField AccessibleHeaderText="Date"
                ControlStyle-CssClass="gv_truncated"
                ControlStyle-Width="150"
                DataField="comment_date" 
                HeaderStyle-Width="150"
                HeaderText="Date" />
            <asp:BoundField AccessibleHeaderText="Comment"
                ControlStyle-CssClass="gv_truncated"
                ControlStyle-Width="415"
                DataField="comment_text" 
                HeaderStyle-Width="420"
                HeaderText="Comment" />
            <asp:BoundField AccessibleHeaderText="User"
                ControlStyle-CssClass="gv_truncated"
                ControlStyle-Width="150"
                DataField="user_name" 
                HeaderStyle-Width="150"
                HeaderText="User" />
        </Columns>
    </asp:GridView>
</asp:Panel>
<!--end of comments panel-->

<!--spacer-->
<div class="app_horizontal_spacer">
</div>
<asp:Button ID="btnOK" runat="server"
    OnClick="OnClickOK"
    Text="Save" />
<asp:Button ID="btnCancel" runat="server"
    OnClick="OnClickCancel"
    Text="Cancel" />

