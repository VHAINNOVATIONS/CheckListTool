using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using VAPPCT.DA;
using VAPPCT.UI;

public partial class sp_ucPatItemEditor : CAppUserControlPopup 
{
    //constants for gridview key positions
    private const int nItemComponentIDIndex = 0;
    private const int nLegalMinIndex = 1;
    private const int nCriticalLowIndex = 2;
    private const int nLowIndex = 3;
    private const int nHighIndex = 4;
    private const int nCriticalHighIndex = 5;
    private const int nLegalMaxIndex = 6;
    private const int nItemIDIndex = 7;
    private const int nItemTypeIDIndex = 8;

    //constant for quick entry option in ddl
    private const int nQuickEntry = -2;

    //constant for null map id
    private const int nNullMapID = -1;

    /// <summary>
    /// property to get/set the patient id
    /// </summary>
    public string PatientID
    {
        get
        {
            object obj = ViewState[ClientID + "PatientID"];
            return (obj != null) ? obj.ToString() : string.Empty;
        }
        set { ViewState[ClientID + "PatientID"] = value; }
    }

    /// <summary>
    /// property
    /// gets/sets a checklist id for the page
    /// </summary>
    public long ChecklistID
    {
        get
        {
            object obj = ViewState[ClientID + "ChecklistID"];
            return (obj != null) ? Convert.ToInt64(obj) : 0;
        }
        set { ViewState[ClientID + "ChecklistID"] = value; }
    }

    /// <summary>
    /// property
    /// gets/sets a iscollection for the page
    /// </summary>
    public bool IsCollection
    {
        get
        {
            object obj = ViewState[ClientID + "IsCollection"];
            return (obj != null) ? Convert.ToBoolean(obj) : false;
        }
        set { ViewState[ClientID + "IsCollection"] = value; }
    }

    /// <summary>
    /// property
    /// gets/sets a checklist id for the page
    /// </summary>
    public long PatientChecklistID
    {
        get
        {
            object obj = ViewState[ClientID + "PatChecklistID"];
            return (obj != null) ? Convert.ToInt64(obj) : 0;
        }
        set { ViewState[ClientID + "PatChecklistID"] = value; }
    }

    /// <summary>
    /// property
    /// gets/sets a checklist item id
    /// </summary>
    public long ItemID
    {
        get
        {
            object obj = ViewState[ClientID + "ItemID"];
            return (obj != null) ? Convert.ToInt64(obj) : 0;
        }
        set { ViewState[ClientID + "ItemID"] = value; }
    }

    /// <summary>
    /// property
    /// gets/sets a checklist item id
    /// </summary>
    public long ChecklistItemID
    {
        get
        {
            object obj = ViewState[ClientID + "CLItemID"];
            return (obj != null) ? Convert.ToInt64(obj) : 0;
        }
        set { ViewState[ClientID + "CLItemID"] = value; }
    }

    /// <summary>
    /// item type id property
    /// </summary>
    private long ItemTypeID
    {
        get
        {
            object obj = ViewState[ClientID + "ItemTypeID"];
            return (obj != null) ? Convert.ToInt64(obj) : 0;
        }
        set { ViewState[ClientID + "ItemTypeID"] = value; }
    }

    protected void ClearControls2()
    {
        //clear the date
        txtEntryDate.Text = string.Empty;
        calEntryDate.SelectedDate = null;
        ucTimePicker.SetTime(0, 0, 0);

        //clear the gridviews
        gvComponents.DataSource = null;
        gvComponents.DataBind();
        gvQuickEntry.DataSource = null;
        gvQuickEntry.DataBind();
        gvComments.DataSource = null;
        gvComments.DataBind();

        //hide controls
        pnlComponents.Visible = false;
        pnlCollection.Visible = false;
        pnlQuickEntry.Visible = false;
        pnlComments.Visible = false;
        pnlPatItems.Visible = false;
        pnlMapped.Visible = false;
        ucNoteTitle.Visible = false;
        txtComment.Visible = false;

        lblItemComps.Visible = false;
        lblCommentHistory.Visible = false;
        lblNewComment.Visible = false;
        ucTimePicker.Visible = false;
        txtEntryDate.Visible = false;
        lblDate.Visible = false;
    }

    /// <summary>
    /// override load control
    /// </summary>
    /// <param name="lEditMode"></param>
    /// <returns></returns>
    public override CStatus LoadControl(k_EDIT_MODE lEditMode)
    {
        ((app_ucTimer)UCTimer).StopRefresh();

        //clear the collection and items ddl
        ddlColItems.Items.Clear();
        ddlItems.Items.Clear();

        ClearControls2();

        //start with not a collection
        IsCollection = false;

        //get the item type id
        CItemData Item = new CItemData(BaseMstr.BaseData);
        CItemDataItem di = null;
        CStatus status = Item.GetItemDI(ItemID, out di);
        if (!status.Status)
        {
            return status;
        }
        ItemTypeID = di.ItemTypeID;

        //switch on the item type id
        if (di.ItemTypeID == (long)k_ITEM_TYPE_ID.Collection)
        {
            //this item is a collection
            IsCollection = true;

            pnlEmptyColl.Visible = true;

            //collection label and description
            lblColl.Text = di.ItemLabel + " - " + di.ItemDescription;

            //load the collection item ddl
            status = CItem.LoadItemCollectionDDL(
                BaseMstr.BaseData,
                ddlColItems,
                ItemID);
            if (!status.Status)
            {
                return status;
            }

            //insert quick entry item into the ddl
            ListItem itm = new ListItem();
            itm.Text = "Quick Entry";
            itm.Value = Convert.ToString(nQuickEntry);
            ddlColItems.Items.Insert(1, itm);

            //show the collection options
            pnlCollection.Visible = true;
        }
        else
        {
            //not a collection

            //show the pat items
            pnlPatItems.Visible = true;
            
            //load the gridview
            status = LoadItemAndComponents(di);
            if (!status.Status)
            {
                return status;
            }
        }

        return new CStatus();
    }

    /// <summary>
    /// load item and components
    /// </summary>
    /// <param name="di"></param>
    /// <returns></returns>
    protected CStatus LoadItemAndComponents(CItemDataItem di)
    {
        if (di == null)
        {
            return new CStatus(false, k_STATUS_CODE.Failed, "TODO");
        }

        lblItem.Text = di.ItemLabel + " - " + di.ItemDescription;

        //load a ddl with all results ordered newest to oldest
        CStatus status = CPatientItem.LoadPatientItemsDDL(
            BaseMstr.BaseData,
            ddlItems,
            PatientID,
            ItemID);
        if (!status.Status)
        {
            return status;
        }

        //cannot add new results for mapped items
        if (di.MapID != Convert.ToString(nNullMapID))
        {
            ddlItems.Items[0].Text = "[Select Result]";
            txtEntryDate.ReadOnly = true;
            calEntryDate.Enabled = false;
            ucTimePicker.Enabled = false;
            txtEntryDate.Text = string.Empty;
            calEntryDate.SelectedDate = null;
            ucTimePicker.SetTime(0, 0, 0);
            txtComment.Enabled = false;

            ShowEditOptions(false);
        }
        else
        {
            //new result
            ddlItems.Items[0].Text = "[New Result]";
            txtEntryDate.ReadOnly = false;
            calEntryDate.Enabled = true;
            ucTimePicker.Enabled = true;
            txtEntryDate.Text = CDataUtils.GetDateAsString(DateTime.Now);
            calEntryDate.SelectedDate = DateTime.Now;
            ucTimePicker.SetTime(DateTime.Now);
            txtComment.Enabled = true;

            ShowEditOptions(true);
        }

        //note title
        if (di.ItemTypeID == (long)k_ITEM_TYPE_ID.NoteTitle)
        {
            pnlComponents.Visible = false;

            ucNoteTitle.Visible = false;
            ucNoteTitle.Clear();
                
            if (ddlItems.Items[0].Text != "[Select Result]")
            {
                if (ddlItems.Items[0].Text != "")
                {
                    ucNoteTitle.Visible = true;
                }
            }
        }
        else
        {
            //not a note title
            pnlComponents.Visible = false;
            if (ddlItems.Items[0].Text != "[Select Result]")
            {
                if (ddlItems.Items[0].Text != "")
                {
                    pnlComponents.Visible = true;
                }
            }

            ucNoteTitle.Visible = false;

            status = LoadComponents();
            if (!status.Status)
            {
                return status;
            }
        }

        //clear and bind comments
        txtComment.Text = string.Empty;
        gvComments.DataSource = null;
        gvComments.DataBind();

        return new CStatus();
    }

    /// <summary>
    /// load components
    /// </summary>
    /// <returns></returns>
    protected CStatus LoadComponents()
    {
        //get the item components
        DataSet dsComponents = null;
        CItemComponentData icd = new CItemComponentData(BaseMstr.BaseData);
        CStatus status = icd.GetItemComponentOJDS(
            ItemID,
            k_ACTIVE_ID.Active,
            out dsComponents);
        if (!status.Status)
        {
            return status;
        }

        gvComponents.DataSource = dsComponents;
        gvComponents.DataBind();

        return new CStatus();
    }

    /// <summary>
    /// load pat item components
    /// </summary>
    /// <returns></returns>
    protected CStatus LoadPatItemAndComponents()
    {
        CPatientItemData dta = new CPatientItemData(BaseMstr.BaseData);
        CPatientItemDataItem di = null;
        CStatus status = dta.GetPatientItemDI(
            PatientID,
            ItemID,
            Convert.ToInt64(ddlItems.SelectedValue),
            out di);
        if (!status.Status)
        {
            return status;
        }

        // set date/time
        txtEntryDate.Text = CDataUtils.GetDateAsString(di.EntryDate);
        calEntryDate.SelectedDate = di.EntryDate;
        ucTimePicker.SetTime(di.EntryDate);

        if (di.ItemTypeID == (long)k_ITEM_TYPE_ID.NoteTitle)
        {
            ucNoteTitle.ItemID = di.ItemID;
            ucNoteTitle.PatientItemID = Convert.ToInt64(ddlItems.SelectedValue);
            ucNoteTitle.PatientID = PatientID;
            status = ucNoteTitle.LoadControl(k_EDIT_MODE.UPDATE);
            if (!status.Status)
            {
                return status;
            }
        }
        else
        {
            status = LoadPatComponents();
            if (!status.Status)
            {
                return status;
            }
        }

        status = LoadPatComments();
        if (!status.Status)
        {
            return status;
        }

        return new CStatus();
    }

    /// <summary>
    /// load pat components
    /// </summary>
    /// <returns></returns>
    protected CStatus LoadPatComponents()
    {
        CPatientItemData pid = new CPatientItemData(BaseMstr.BaseData);
        DataSet dsComps = null;
        CStatus status = pid.GetPatientItemComponentDS(
            PatientID,
            Convert.ToInt64(ddlItems.SelectedValue),
            ItemID,
            out dsComps);
        if (!status.Status)
        {
            return status;
        }

        DataTable dtComps = dsComps.Tables[0];
        if (dtComps == null || dtComps.Rows.Count != gvComponents.Rows.Count)
        {
            return new CStatus(false, k_STATUS_CODE.Failed, "TODO");
        }

        foreach (GridViewRow gvr in gvComponents.Rows)
        {
            RadioButton rbSelect = (RadioButton)gvr.FindControl("rbSelComponent");
            TextBox txtVal = (TextBox)gvr.FindControl("txtValue");
            if (rbSelect == null || txtVal == null)
            {
                return new CStatus(false, k_STATUS_CODE.Failed, "TODO");
            }

            DataRow drComp = dtComps.Select("ITEM_COMPONENT_ID = " + gvComponents.DataKeys[gvr.DataItemIndex][nItemComponentIDIndex].ToString())[0];
            switch ((k_ITEM_TYPE_ID)ItemTypeID)
            {
                case k_ITEM_TYPE_ID.Laboratory:
                    txtVal.Text = drComp["COMPONENT_VALUE"].ToString();
                    break;
                case k_ITEM_TYPE_ID.QuestionFreeText:
                    txtVal.Text = drComp["COMPONENT_VALUE"].ToString();
                    break;
                case k_ITEM_TYPE_ID.QuestionSelection:
                    rbSelect.Checked = (drComp["COMPONENT_VALUE"].ToString() == "1") ? true : false;
                    break;
                default:
                    return new CStatus(false, k_STATUS_CODE.Failed, "TODO");
            }
        }

        return new CStatus();
    }

    /// <summary>
    /// load pat comments
    /// </summary>
    /// <returns></returns>
    protected CStatus LoadPatComments()
    {
        CPatientItemData itemData = new CPatientItemData(BaseMstr.BaseData);
        DataSet dsComments = null;
        CStatus status = itemData.GetPatientItemCommmentDS(
            Convert.ToInt64(ddlItems.SelectedValue),
            ItemID,
            out dsComments);
        if (!status.Status)
        {
            return status;
        }

        gvComments.DataSource = dsComments;
        gvComments.DataBind();

        return new CStatus();
    }

    /// <summary>
    /// event
    /// loads grid view row data for non header rows
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void OnRowDataBoundComp(object sender, GridViewRowEventArgs e)
    {
        GridViewRow gvr = (GridViewRow)e.Row;
        if (gvr == null)
        {
            ShowStatusInfo(new CStatus(false, k_STATUS_CODE.Failed, "TODO"));
            return;
        }

        if (gvr.RowType == DataControlRowType.DataRow)
        {
            CStatus status = LoadGridViewRow(gvr);
            if (!status.Status)
            {
                ShowStatusInfo(status);
                return;
            }
        }
    }

    /// <summary>
    /// method
    /// loads all of the item's component data into the gridview
    /// </summary>
    /// <param name="gvr"></param>
    /// <returns></returns>
    protected CStatus LoadGridViewRow(GridViewRow gvr)
    {
        DataRowView drv = (DataRowView)gvr.DataItem;
        if (drv == null)
        {
            return new CStatus(false, k_STATUS_CODE.Failed, "TODO");
        }

        DataRow dr = drv.Row;
        if (dr == null)
        {
            return new CStatus(false, k_STATUS_CODE.Failed, "TODO");
        }

        RadioButton rbSelect = (RadioButton)gvr.FindControl("rbSelComponent");
        Label lblComponent = (Label)gvr.FindControl("lblComponent");
        TextBox txtVal = (TextBox)gvr.FindControl("txtValue");
        Label lblUnit = (Label)gvr.FindControl("lblUnits");
        Label lblRanges = (Label)gvr.FindControl("lblRanges");

        if (rbSelect == null
            || lblComponent == null
            || txtVal == null
            || lblUnit == null
            || lblRanges == null)
        {
            return new CStatus(false, k_STATUS_CODE.Failed, "TODO");
        }

        if (ddlItems.Items[0].Text == "[Select Result]")
        {
            rbSelect.Enabled = false;
            txtVal.ReadOnly = true;
        }

        switch ((k_ITEM_TYPE_ID)Convert.ToInt64(dr["ITEM_TYPE_ID"]))
        {
            case k_ITEM_TYPE_ID.Laboratory:
                rbSelect.Visible = false;
                lblComponent.Visible = true;
                txtVal.Visible = true;
                lblUnit.Visible = true;
                lblRanges.Visible = true;

                lblComponent.Text = dr["ITEM_COMPONENT_LABEL"].ToString();
                lblUnit.Text = dr["UNITS"].ToString();
                lblRanges.Text = " (Legal Min: " + dr["LEGAL_MIN"].ToString()
                    + " Critical Low: " + dr["CRITICAL_LOW"].ToString()
                    + " Low: " + dr["LOW"].ToString()
                    + " High: " + dr["HIGH"].ToString()
                    + " Critical High: " + dr["CRITICAL_HIGH"].ToString()
                    + " Legal Max: " + dr["LEGAL_MAX"].ToString() + ") ";
                txtVal.Width = 200;
                break;

            case k_ITEM_TYPE_ID.QuestionFreeText:
                rbSelect.Visible = false;
                lblComponent.Visible = true;
                txtVal.Visible = true;
                lblUnit.Visible = false;
                lblRanges.Visible = false;

                lblComponent.Text = dr["ITEM_COMPONENT_LABEL"].ToString();
                txtVal.Width = 500;
                break;
            case k_ITEM_TYPE_ID.QuestionSelection:
                rbSelect.Visible = true;
                lblComponent.Visible = false;
                txtVal.Visible = false;
                lblUnit.Visible = false;
                lblRanges.Visible = false;

                rbSelect.Text = dr["ITEM_COMPONENT_LABEL"].ToString();
                break;
            default:
                return new CStatus(false, k_STATUS_CODE.Failed, "TODO");
        }

        return new CStatus();
    }

    /// <summary>
    /// page load
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void Page_Load(object sender, EventArgs e)
    {
        ucNoteTitle.BaseMstr = BaseMstr;

        if (!IsPostBack)
        {
            Title = "Single Patient Item Editor";
        }

    }
      
    /// <summary>
    /// US:1883 method
    /// builds a list of all the item components for an item from the controls on the page
    /// </summary>
    /// <param name="PatItemCompList"></param>
    /// <returns></returns>
    private CStatus BuildPatItemCompList(out CPatientItemCompList PatItemCompList)
    {
        PatItemCompList = new CPatientItemCompList();

        //insert components
        foreach (GridViewRow gr in gvComponents.Rows)
        {
            // create patient item component data item for the grid view row
            CPatientItemComponentDataItem di = new CPatientItemComponentDataItem();
            di.PatientID = PatientID;
            di.ItemID = (ItemTypeID == (long)k_ITEM_TYPE_ID.Collection) ? Convert.ToInt64(ddlColItems.SelectedValue) : ItemID;
            di.ComponentID = Convert.ToInt64(gvComponents.DataKeys[gr.DataItemIndex][nItemComponentIDIndex]);

            RadioButton rbSelect = (RadioButton)gr.FindControl("rbSelComponent");
            TextBox txtVal = (TextBox)gr.FindControl("txtValue");

            if (rbSelect == null || txtVal == null)
            {
                return new CStatus(false, k_STATUS_CODE.Failed, "TODO");
            }

            //switch on the type and get the value
            switch ((k_ITEM_TYPE_ID)ItemTypeID)
            {
                // laboratory and question free text components are handled the same
                case k_ITEM_TYPE_ID.Laboratory:
                case k_ITEM_TYPE_ID.QuestionFreeText:
                    // get the value from the text box
                    di.ComponentValue = txtVal.Text;
                    PatItemCompList.Add(di);
                    break;
                case k_ITEM_TYPE_ID.QuestionSelection:
                    // get the value from the radio button
                    di.ComponentValue = Convert.ToInt64((rbSelect.Checked) ? k_TRUE_FALSE_ID.True : k_TRUE_FALSE_ID.False).ToString();
                    PatItemCompList.Add(di);
                    break;
                default:
                    PatItemCompList = null;
                    return new CStatus(false, k_STATUS_CODE.Failed, "TODO");
            }
        }

        return new CStatus(true, k_STATUS_CODE.Success, "TODO");
    }

    /// <summary>
    ///US:1883 Abstract save control method
    /// </summary>
    /// <returns></returns>
    public override CStatus SaveControl()
    {
        CPatientItemData itemData = new CPatientItemData(BaseMstr.BaseData);
        CStatus status = new CStatus();
        long lPatItemID = -1;

        if (ddlItems.SelectedItem.Text == "[New Result]")
        {
            //load an item for insert
            CPatientItemDataItem di = new CPatientItemDataItem();
            di.PatientID = PatientID;
            di.ItemID = ItemID;
            di.SourceTypeID = (long)k_SOURCE_TYPE_ID.VAPPCT;

            //get the date time, which is a combination of the 2 controls
            di.EntryDate = CDataUtils.GetDate(txtEntryDate.Text, ucTimePicker.HH, ucTimePicker.MM, ucTimePicker.SS);

            // build a list of all the item components in the grid view
            CPatientItemCompList PatItemCompList = null;
            status = BuildPatItemCompList(out PatItemCompList);
            if (!status.Status)
            {
                return status;
            }

            // insert the patient item and all of its item components
            status = itemData.InsertPatientItem(di, PatItemCompList, out lPatItemID);
            if (!status.Status)
            {
                return status;
            }
        }
        else
        {
            lPatItemID = CDropDownList.GetSelectedLongID(ddlItems);
        }

        // update the comments if there is a new one
        if (!string.IsNullOrEmpty(txtComment.Text))
        {
            status = itemData.InsertPatientItemComment(lPatItemID, ItemID, txtComment.Text);
            if (!status.Status)
            {
                return status;
            }
        }

        //show status
        return new CStatus();
    }

    /// <summary>
    /// US:1880
    /// override
    /// Abstract validate user input method
    /// </summary>
    /// <param name="plistStatus"></param>
    /// <returns></returns>
    public override CStatus ValidateUserInput(out CParameterList plistStatus)
    {
        plistStatus = new CParameterList();
        CStatus status = new CStatus();

        if (ddlItems.SelectedItem.Text != "[New Result]")
        {
            return status;
        }

        //make sure date range is valid
        DateTime dtEntryDate = CDataUtils.GetDate(
            txtEntryDate.Text,
            ucTimePicker.HH,
            ucTimePicker.MM,
            ucTimePicker.SS);

        if (dtEntryDate > DateTime.Now)
        {
            plistStatus.AddInputParameter("ERROR_FUTURE_DATE", Resources.ErrorMessages.ERROR_FUTURE_DATE);
        }
        
        //validate components
        bool bHasSelectedValue = false;
        foreach (GridViewRow gr in gvComponents.Rows)
        {
            TextBox txtValue = (TextBox)gr.FindControl("txtValue");
            Label lblComponent = (Label)gr.FindControl("lblComponent");
            RadioButton rbSelect = (RadioButton)gr.FindControl("rbSelComponent");

            if (txtValue == null
                || lblComponent == null
                || rbSelect == null)
            {
                return new CStatus(false, k_STATUS_CODE.Failed, "TODO");
            }
            
            //switch on the type and get the value
            switch ((k_ITEM_TYPE_ID)ItemTypeID)
            {
                case k_ITEM_TYPE_ID.Laboratory:
                    {
                        bHasSelectedValue = true;

                        //get the min and max from the keys
                        double dblLegalMin = Convert.ToDouble(gvComponents.DataKeys[gr.DataItemIndex][nLegalMinIndex]);
                        double dblLegalMax = Convert.ToDouble(gvComponents.DataKeys[gr.DataItemIndex][nLegalMaxIndex]);

                        string strError = "Please enter a valid '"
                            + lblComponent.Text
                            + "' value, valid range is from"
                            + dblLegalMin.ToString()
                            + " to "
                            + dblLegalMax.ToString()
                            + ".";

                        // if the value is not numeric
                        // or if the value is outside the legal ranges
                        double dblValue = 0;
                        if (!double.TryParse(txtValue.Text, out dblValue)
                            || dblValue < dblLegalMin
                            || dblValue > dblLegalMax)
                        {
                            plistStatus.AddInputParameter("ERROR", strError);
                        }
                        break;
                    }

                case k_ITEM_TYPE_ID.NoteTitle:
                    bHasSelectedValue = true;
                    break;

                case k_ITEM_TYPE_ID.QuestionFreeText:
                    {
                        bHasSelectedValue = true;
                        if (txtValue.Text.Length < 1)
                        {
                            string strError = "Please enter a valid '"
                                + lblComponent.Text
                                + "' value!";

                            plistStatus.AddInputParameter("ERROR", strError);
                        }
                        break;
                    }

                case k_ITEM_TYPE_ID.QuestionSelection:
                    if (rbSelect.Checked)
                    {
                        bHasSelectedValue = true;
                    }
                    break;
             }
        }
        
        //make sure the user selected a radio button
        if(!bHasSelectedValue)
        {
            plistStatus.AddInputParameter("ERROR", "Please select a valid option!");
        }

        if (plistStatus.Count > 0)
        {
            status.Status = false;
            status.StatusCode = k_STATUS_CODE.Failed;
        }

        return status;
    }
          
    /// <summary>
    /// user clicked the ok button
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void OnClickOK(object sender, EventArgs e)
    {
        //make sure we have something selected that can be saved if collection...
        if (IsCollection)
        {
            if (ddlColItems.SelectedIndex < 1)
            {
                ShowMPE();
                return;
            }

            //get the item id
            ItemID = Convert.ToInt64(ddlColItems.SelectedValue);

            //quick entry
            if (ItemID == nQuickEntry)
            {
                //validate user input
                CParameterList plistStatusQE = null;
                CStatus statusQE = ValidateUserInputQuickEntry(out plistStatusQE);
                if (!statusQE.Status)
                {
                    ShowMPE();
                    ShowStatusInfoScroll(
                        statusQE.StatusCode,
                        plistStatusQE);
                    return;
                }

                //save the data
                statusQE = SaveControlQuickEntry();
                if (!statusQE.Status)
                {
                    ShowMPE();
                    ShowStatusInfo(statusQE);
                    return;
                }

                Visible = false;
                ShowParentMPE();
                ((app_ucTimer)UCTimer).StartRefresh();
                return;
            }
        }

        //
        //not quick entry
        //

        //validate user input
        CParameterList plistStatus = null;
        CStatus status = ValidateUserInput(out plistStatus);
        if (!status.Status)
        {
            ShowMPE();
            ShowStatusInfoScroll(
                status.StatusCode,
                plistStatus);
            return;
        }

        //save the data
        status = SaveControl();
        if (!status.Status)
        {
            ShowMPE();
            ShowStatusInfo(status);
            return;
        }

        Visible = false;
        ShowParentMPE();
        ((app_ucTimer)UCTimer).StartRefresh();
    }

    /// <summary>
    /// event
    /// starts the timer after before closing the dialog
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void OnClickCancel(object sender, EventArgs e)
    {
        Visible = false;
        ShowParentMPE();
        ((app_ucTimer)UCTimer).StartRefresh(false);
    }


    /// <summary>
    /// method
    /// loads all of the item's component data into the gridview
    /// </summary>
    /// <param name="gvr"></param>
    /// <returns></returns>
    protected CStatus LoadGridViewRowQuickEntry(GridViewRow gvr)
    {
        DataRowView drv = (DataRowView)gvr.DataItem;
        if (drv == null)
        {
            return new CStatus(false, k_STATUS_CODE.Failed, "TODO");
        }

        DataRow dr = drv.Row;
        if (dr == null)
        {
            return new CStatus(false, k_STATUS_CODE.Failed, "TODO");
        }

        RadioButton rbSelect = (RadioButton)gvr.FindControl("rbSelComponent");
        Label lblComponent = (Label)gvr.FindControl("lblComponent");
        TextBox txtVal = (TextBox)gvr.FindControl("txtValue");
        Label lblUnit = (Label)gvr.FindControl("lblUnits");
        Label lblRanges = (Label)gvr.FindControl("lblRanges");
        Label lblItemDescr = (Label)gvr.FindControl("lblItemDescr");

        if (rbSelect == null
            || lblComponent == null
            || txtVal == null
            || lblUnit == null
            || lblRanges == null
            || lblItemDescr == null)
        {
            return new CStatus(false, k_STATUS_CODE.Failed, "TODO");
        }

       // if (ddlItems.Items[0].Text == "[Select Result]")
       // {
       //     rbSelect.Enabled = false;
       //     txtVal.ReadOnly = true;
       // }

        switch ((k_ITEM_TYPE_ID)Convert.ToInt64(dr["ITEM_TYPE_ID"]))
        {
            case k_ITEM_TYPE_ID.ItemLabel:
                txtVal.Width = 0;
                
                lblItemDescr.Visible = true;
                lblComponent.Visible = false;
                rbSelect.Visible = false;
                txtVal.Visible = false;
                lblUnit.Visible = false;
                lblRanges.Visible = false;

                lblItemDescr.Text = dr["ITEM_COMPONENT_LABEL"].ToString();
                break;
               

            case k_ITEM_TYPE_ID.Laboratory:
                lblItemDescr.Visible = false;
                rbSelect.Visible = false;
                lblComponent.Visible = true;
                txtVal.Visible = true;
                lblUnit.Visible = true;
                lblRanges.Visible = true;

                lblComponent.Text = dr["ITEM_COMPONENT_LABEL"].ToString();
                lblUnit.Text = dr["UNITS"].ToString();
                lblRanges.Text = " (Legal Min: " + dr["LEGAL_MIN"].ToString()
                    + " Critical Low: " + dr["CRITICAL_LOW"].ToString()
                    + " Low: " + dr["LOW"].ToString()
                    + " High: " + dr["HIGH"].ToString()
                    + " Critical High: " + dr["CRITICAL_HIGH"].ToString()
                    + " Legal Max: " + dr["LEGAL_MAX"].ToString() + ") ";
                txtVal.Width = 200;
                break;

            case k_ITEM_TYPE_ID.QuestionFreeText:
                lblItemDescr.Visible = false;
                rbSelect.Visible = false;
                lblComponent.Visible = true;
                txtVal.Visible = true;
                lblUnit.Visible = false;
                lblRanges.Visible = false;

                lblComponent.Text = dr["ITEM_COMPONENT_LABEL"].ToString();
                txtVal.Width = 500;
                break;
            case k_ITEM_TYPE_ID.QuestionSelection:
                lblItemDescr.Visible = false;
                rbSelect.Visible = true;
                lblComponent.Visible = false;
                txtVal.Visible = false;
                lblUnit.Visible = false;
                lblRanges.Visible = false;

                rbSelect.Text = dr["ITEM_COMPONENT_LABEL"].ToString();
                rbSelect.GroupName += dr["ITEM_ID"].ToString();
                break;
            default:
                return new CStatus(false, k_STATUS_CODE.Failed, "TODO");
        }

        return new CStatus();
    }


    /// <summary>
    /// event
    /// loads grid view row data for non header rows
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void OnRowDataBoundQuickEntry(object sender, GridViewRowEventArgs e)
    {
        GridViewRow gvr = (GridViewRow)e.Row;
        if (gvr == null)
        {
            ShowStatusInfo(new CStatus(false, k_STATUS_CODE.Failed, "TODO"));
            return;
        }

        if (gvr.RowType == DataControlRowType.DataRow)
        {
            CStatus status = LoadGridViewRowQuickEntry(gvr);
            if (!status.Status)
            {
                ShowStatusInfo(status);
                return;
            }
        }
    }

    /// <summary>
    /// load the quick entry
    /// </summary>
    protected void LoadQuickEntry()
    {
        DataSet dsComponentsAll = new DataSet();
        pnlPatItems.Visible = false;

        ShowEditOptions(true);
        ucNoteTitle.Visible = false;

        txtEntryDate.ReadOnly = false;
        calEntryDate.Enabled = true;
        ucTimePicker.Enabled = true;
        txtEntryDate.Text = CDataUtils.GetDateAsString(DateTime.Now);
        calEntryDate.SelectedDate = DateTime.Now;
        ucTimePicker.SetTime(DateTime.Now);
        txtComment.Enabled = true;

        //loop over all the items in the ddl
        foreach (ListItem itm in ddlColItems.Items)
        {
            //only interested in items that have a value greater than zero
            //this weeds out the empty and 'quick entry'
            if (Convert.ToInt64(itm.Value) > 0)
            {
                CItemData Item = new CItemData(BaseMstr.BaseData);
                CItemDataItem di = null;

                ItemID = Convert.ToInt64(itm.Value);
                CStatus status = Item.GetItemDI(ItemID, out di);
                if (!status.Status)
                {
                    ShowStatusInfo(status);
                    return;
                }

                ItemTypeID = di.ItemTypeID;
                              
                //get the item components, dont allow quick entry on mapped items
                bool bDoQuickEntry = true;
                if (!String.IsNullOrEmpty(di.MapID))
                {
                    if (di.MapID != "-1")
                    {
                        bDoQuickEntry = false;
                    }
                }

                //dont allow quick entry on note titles
                if (di.ItemTypeID == (long)k_ITEM_TYPE_ID.NoteTitle)
                {
                    bDoQuickEntry = false;
                }

                if (bDoQuickEntry)
                {
                    DataSet dsComponents = null;
                    CItemComponentData icd = new CItemComponentData(BaseMstr.BaseData);
                    status = icd.GetItemComponentOJDS(
                        ItemID,
                        k_ACTIVE_ID.Active,
                        out dsComponents);

                    //add a row to the dataset for the item label
                    DataRow rowItem = dsComponents.Tables[0].NewRow();
                    rowItem["ITEM_TYPE_ID"] = k_ITEM_TYPE_ID.ItemLabel;
                    rowItem["ITEM_COMPONENT_LABEL"] = "<br><b>" + di.ItemLabel + ": " + di.ItemDescription + "</b>";
                    dsComponents.Tables[0].Rows.InsertAt(rowItem, 0);

                    //merge this dataset with the dataset that has all components for all items
                    dsComponentsAll.Merge(dsComponents);
                }
            }
        }

        //show hide controls
        pnlComponents.Visible = false;
        pnlQuickEntry.Visible = true;
        gvQuickEntry.Visible = true;

        //bind the gridview to the full dataset
        gvQuickEntry.DataSource = dsComponentsAll;
        gvQuickEntry.DataBind();

        //now load the quick entry with the latest values
        LoadQuickEntryGridView();

    }

    /// <summary>
    /// load the quick entry grid with the latest values
    /// </summary>
    protected void LoadQuickEntryGridView()
    {
        //get the count of items in the gridview
        int nItemCount = GetItemQuickEntryCount();
        for (int i = 0; i < nItemCount; i++)
        {
            int nIndex = i;

            //starting row index
            int nRow = -1;

            //get components for this index
            foreach (GridViewRow gr in gvQuickEntry.Rows)
            {
                Label lblDescr = (Label)gr.FindControl("lblItemDescr");
                if (!String.IsNullOrEmpty(lblDescr.Text))
                {
                    nRow++;
                }
                else
                {
                    if (nRow == nIndex)
                    {
                        long lItemID = Convert.ToInt64(gvQuickEntry.DataKeys[gr.DataItemIndex][nItemIDIndex]);
                        long lComponentID = Convert.ToInt64(gvQuickEntry.DataKeys[gr.DataItemIndex][nItemComponentIDIndex]);
                        long lItemTypeID = Convert.ToInt64(gvQuickEntry.DataKeys[gr.DataItemIndex][nItemTypeIDIndex]);

                        string strValue = String.Empty;

                        DataSet dsColl = null;
                        CItemCollectionData coll = new CItemCollectionData(BaseMstr.BaseData);
                        coll.GetItemColMostRecentPatICDS(ChecklistItemID, PatientID, out dsColl);
                        foreach (DataTable table in dsColl.Tables)
                        {
                            foreach (DataRow dr in table.Rows)
                            {
                                //get values from the the item in the collection (not the patient items, the collection items)
                                long lItemComponentID = CDataUtils.GetDSLongValue(dr, "ITEM_COMPONENT_ID");

                                if (lItemComponentID == lComponentID)
                                {
                                    strValue = CDataUtils.GetDSStringValue(dr, "COMPONENT_VALUE");
                                }
                            }
                        }
                     

                        //get the radio button and text box
                        RadioButton rbSelect = (RadioButton)gr.FindControl("rbSelComponent");
                        TextBox txtVal = (TextBox)gr.FindControl("txtValue");

                        //switch on the type and get the value
                        switch ((k_ITEM_TYPE_ID)lItemTypeID)
                        {
                            // laboratory and question free text components are handled the same
                            case k_ITEM_TYPE_ID.Laboratory:
                            case k_ITEM_TYPE_ID.QuestionFreeText:
                                // get the value from the text box
                                txtVal.Text = strValue;
                                break;
                            case k_ITEM_TYPE_ID.QuestionSelection:
                                // get the value from the radio button
                                rbSelect.Checked = (strValue == "1") ? true : false;
                                break;
                        }
                    }
                }
            }
         }
    }

    /// <summary>
    /// US:1883 the selected item in the collection ddl was changed
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void OnSelIndexChangedColItem(object sender, EventArgs e)
    {
        //clear the comment text
        txtComment.Text = string.Empty;

        //chose empty optio so reset
        if (ddlColItems.SelectedIndex == 0)
        {
            //clear
            ClearControls2();

            //show
            pnlEmptyColl.Visible = true;
            pnlCollection.Visible = true;
            txtComment.Enabled = false;
        }

        //chose an option we can enter values for
        if (ddlColItems.SelectedIndex > 0)
        {
            pnlEmptyColl.Visible = false;

            txtComment.Enabled = true;

            ItemID = Convert.ToInt64(ddlColItems.SelectedValue);

            //quick entry
            if (ItemID == nQuickEntry)
            {
                IsCollection = true;
                pnlQuickEntry.Visible = true;
                pnlPatItems.Visible = false;
                LoadQuickEntry();
                ShowMPE();
                return;
            }
            else
            {
                IsCollection = false;
            }

            pnlPatItems.Visible = true;
            pnlQuickEntry.Visible = false;

            CItemData Item = new CItemData(BaseMstr.BaseData);
            CItemDataItem di = null;
            CStatus status = Item.GetItemDI(ItemID, out di);
            if (!status.Status)
            {
                ShowStatusInfo(status);
                return;
            }

            ItemTypeID = di.ItemTypeID;

            status = LoadItemAndComponents(di);
            if (!status.Status)
            {
                ShowStatusInfo(status);
                return;
            }
        }
        else
        {
            txtComment.Enabled = false;
        }

        ShowMPE();
    }

    /// <summary>
    /// show or hide options for editing
    /// </summary>
    /// <param name="bShow"></param>
    protected void ShowEditOptions(bool bShow)
    {
        //show hide the panels and labels for edit
        lblDate.Visible = bShow;
        txtEntryDate.Visible = bShow;
        ucTimePicker.Visible = bShow;

        pnlComments.Visible = bShow;
        pnlComponents.Visible = bShow;

        lblItemComps.Visible = bShow;
        lblNewComment.Visible = bShow;
        lblCommentHistory.Visible = bShow;
        txtComment.Visible = bShow;

        if (!bShow)
        {
            pnlMapped.Visible = true;
        }
        else
        {
            pnlMapped.Visible = false;
        }
    }

    /// <summary>
    /// event
    /// loads the selected patient item into the grid view
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void OnSelIndexChangedItem(object sender, EventArgs e)
    {
        ShowMPE();

        if (ddlItems.SelectedIndex == 0)
        {
            foreach (GridViewRow gvr in gvComponents.Rows)
            {
                RadioButton rbSelect = (RadioButton)gvr.FindControl("rbSelComponent");
                TextBox txtValue = (TextBox)gvr.FindControl("txtValue");

                if (rbSelect == null || txtValue == null)
                {
                    ShowStatusInfo(new CStatus(false, k_STATUS_CODE.Failed, "TODO"));
                }
                rbSelect.Checked = false;
                rbSelect.Enabled = true;
                txtValue.Text = string.Empty;
                txtValue.ReadOnly = false;
            }

            if (ddlItems.SelectedItem.Text == "[New Result]")
            {
                ShowEditOptions(true);

                // set date/time
                txtEntryDate.ReadOnly = false;
                calEntryDate.Enabled = true;
                ucTimePicker.Enabled = true;
                txtEntryDate.Text = CDataUtils.GetDateAsString(DateTime.Now);
                calEntryDate.SelectedDate = DateTime.Now;
                ucTimePicker.SetTime(DateTime.Now);
                txtComment.Enabled = true;
            }
            else if (ddlItems.SelectedItem.Text == "[Select Result]")
            {
                txtEntryDate.ReadOnly = true;
                calEntryDate.Enabled = false;
                ucTimePicker.Enabled = false;
                txtEntryDate.Text = string.Empty;
                calEntryDate.SelectedDate = null;
                ucTimePicker.SetTime(0, 0, 0);
                txtComment.Enabled = false;

                ucNoteTitle.Clear();

                ShowEditOptions(false);
            }

            txtComment.Text = string.Empty;
            gvComments.DataSource = null;
            gvComments.DataBind();
        }
        else
        {
            ShowEditOptions(true);

            foreach (GridViewRow gvr in gvComponents.Rows)
            {
                RadioButton rbSelect = (RadioButton)gvr.FindControl("rbSelComponent");
                TextBox txtValue = (TextBox)gvr.FindControl("txtValue");
                if (rbSelect == null || txtValue == null)
                {
                    ShowStatusInfo(new CStatus(false, k_STATUS_CODE.Failed, "TODO"));
                }
                rbSelect.Enabled = false;
                txtValue.ReadOnly = true;
            }

            txtEntryDate.ReadOnly = true;
            calEntryDate.Enabled = false;
            ucTimePicker.Enabled = false;
            txtComment.Enabled = true;
            CStatus status = LoadPatItemAndComponents();
            if (!status.Status)
            {
                ShowStatusInfo(status);
                return;
            }
        }
    }

    /// <summary>
    /// event
    /// maintains the entry date across postbacks
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void OnEntryDateChanged(object sender, EventArgs e)
    {
        ShowMPE();
        try
        {
            calEntryDate.SelectedDate = Convert.ToDateTime(txtEntryDate.Text);
        }
        catch (Exception)
        {
            calEntryDate.SelectedDate = null;
            txtEntryDate.Text = string.Empty;
        }
    }

    ///////////////////////////////////////////////////////////////////////
    //quick entry
    /////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// loop over components and build a patitem list for insert
    /// </summary>
    /// <param name="nIndex"></param>
    /// <param name="PatItemCompList"></param>
    /// <returns></returns>
    private CStatus BuildPatItemCompListQE(int nIndex,
                                            out long lItemID,
                                            out long lItemTypeID,
                                            out CPatientItemCompList PatItemCompList)
    {
        //new patitemcomponent list returned in out variable
        PatItemCompList = new CPatientItemCompList();
        lItemID = 0;
        lItemTypeID = 0;

        //starting row index
        int nRow = -1;

        //get components for this index
        foreach (GridViewRow gr in gvQuickEntry.Rows)
        {
            Label lblDescr = (Label)gr.FindControl("lblItemDescr");
            if (!String.IsNullOrEmpty(lblDescr.Text))
            {
                nRow++;
            }
            else
            {
                if (nRow == nIndex)
                {
                    // create patient item component data item for the grid view row
                    CPatientItemComponentDataItem di = new CPatientItemComponentDataItem();

                    //patient id
                    di.PatientID = PatientID;

                    //item id, also returned from the call
                    di.ItemID = Convert.ToInt64(gvQuickEntry.DataKeys[gr.DataItemIndex][nItemIDIndex]);
                    lItemID = di.ItemID;

                    //component id
                    di.ComponentID = Convert.ToInt64(gvQuickEntry.DataKeys[gr.DataItemIndex][nItemComponentIDIndex]);

                    //item type id
                    lItemTypeID = Convert.ToInt64(gvQuickEntry.DataKeys[gr.DataItemIndex][nItemTypeIDIndex]);

                    //get the radio button and text box
                    RadioButton rbSelect = (RadioButton)gr.FindControl("rbSelComponent");
                    TextBox txtVal = (TextBox)gr.FindControl("txtValue");

                    //switch on the type and get the value
                    switch ((k_ITEM_TYPE_ID)lItemTypeID)
                    {
                        // laboratory and question free text components are handled the same
                        case k_ITEM_TYPE_ID.Laboratory:
                        case k_ITEM_TYPE_ID.QuestionFreeText:
                            // get the value from the text box
                            di.ComponentValue = txtVal.Text;
                            PatItemCompList.Add(di);
                            break;
                        case k_ITEM_TYPE_ID.QuestionSelection:
                            // get the value from the radio button
                            di.ComponentValue = Convert.ToInt64((rbSelect.Checked) ? k_TRUE_FALSE_ID.True : k_TRUE_FALSE_ID.False).ToString();
                            PatItemCompList.Add(di);
                            break;
                        default:
                            PatItemCompList = null;
                            return new CStatus(false, k_STATUS_CODE.Failed, "TODO");
                    }
                }
            }
        }

        //success
        return new CStatus(true, k_STATUS_CODE.Success, "");
    }

    /// <summary>
    /// validate quick entry
    /// </summary>
    /// <param name="plistStatus"></param>
    /// <returns></returns>
    public CStatus ValidateUserInputQuickEntry( out CParameterList plistStatus)
    {
        CStatus status = new CStatus();

        //parameter list to hold all errors
        plistStatus = new CParameterList();

        //make sure date range is valid
        DateTime dtEntryDate = CDataUtils.GetDate(
            txtEntryDate.Text,
            ucTimePicker.HH,
            ucTimePicker.MM,
            ucTimePicker.SS);

        if (dtEntryDate > DateTime.Now)
        {
            plistStatus.AddInputParameter("ERROR_FUTURE_DATE", Resources.ErrorMessages.ERROR_FUTURE_DATE);
        }

        //get the collection data item 
        CItemData idColl = new CItemData(BaseMstr.BaseData);
        CItemDataItem idiColl = null;
        idColl.GetItemDI(ChecklistItemID, out idiColl);
                
        //make sure all the items have at least 1 value selected.

        //get the count of items in the gridview
        int nItemCount = GetItemQuickEntryCount();
        for (int i = 0; i < nItemCount; i++)
        {
            // build a list of all the item components in the grid view
            long lItemID = 0;
            long lItmTypeID = 0;
            CPatientItemCompList PatItemCompList = null;
            status = BuildPatItemCompListQE(i, out lItemID, out lItmTypeID, out PatItemCompList);
            if (!status.Status)
            {
                return status;
            }
            
            bool bHasSelectedValue = false;
            
            //get the item for errors, ts etc..
            CItemData id = new CItemData(BaseMstr.BaseData);
            CItemDataItem idi = null;
            id.GetItemDI(lItemID, out idi);
            string strItemLabel = idi.ItemLabel;

            //loop over the component and get errors and check entry
            foreach (CPatientItemComponentDataItem diPatItemComp in PatItemCompList)
            {
                //get the state id for this component
                CICStateDataItem sdi = null;
                CItemComponentData icd = new CItemComponentData(BaseMstr.BaseData);
                icd.GetICStateDI(lItemID, diPatItemComp.ComponentID, out sdi);
                
                //switch on the type and get the value
                switch ((k_ITEM_TYPE_ID)lItmTypeID)
                {
                    case k_ITEM_TYPE_ID.Laboratory:
                        {
                            bHasSelectedValue = true;
                            //todo:
                            break;
                        }

                    case k_ITEM_TYPE_ID.NoteTitle:
                        bHasSelectedValue = true;
                        break;

                    case k_ITEM_TYPE_ID.QuestionFreeText:
                        {
                            bHasSelectedValue = true;
                            if (diPatItemComp.ComponentValue.Length < 1)
                            {
                                string strError = "Please enter a valid '" + strItemLabel + "' value";
                                plistStatus.AddInputParameter("ERROR", strError);
                            }
                            break;
                        }

                    case k_ITEM_TYPE_ID.QuestionSelection:
                        if (!String.IsNullOrEmpty(diPatItemComp.ComponentValue))
                        {
                            if ((k_TRUE_FALSE_ID)Convert.ToInt64(diPatItemComp.ComponentValue) != k_TRUE_FALSE_ID.False)
                            {
                                bHasSelectedValue = true;
                            }
                        }
                        break;
                }
            }//for each

            //make sure the user selected a radio button
            if (!bHasSelectedValue)
            {
                string strErr = "Please select a valid '" + strItemLabel + "' option.";
                plistStatus.AddInputParameter("ERROR", strErr);
            }
        }

        //if any errors update the overall status
        if (plistStatus.Count > 0)
        {
            status.Status = false;
            status.StatusCode = k_STATUS_CODE.Failed;
        }
       
        return status;
    }

    /// <summary>
    /// get the item count of all items in the quick entry grid
    /// </summary>
    /// <returns></returns>
    private int GetItemQuickEntryCount()
    {
        //loop over gridview items and get a count of 
        //all items by counting our item description rows
        int nItemCount = 0;
        foreach (GridViewRow gr in gvQuickEntry.Rows)
        {
            Label lblDescr = (Label)gr.FindControl("lblItemDescr");
            if (!String.IsNullOrEmpty(lblDescr.Text))
            {
                nItemCount++;
            }
        }
        
        return nItemCount;
    }

    /// <summary>
    /// save the quick entry
    /// </summary>
    /// <returns></returns>
    public CStatus SaveControlQuickEntry()
    {
        //get the count of items in the gridview
        int nItemCount = GetItemQuickEntryCount();

        //status object
        CStatus status = new CStatus();

        //loop and insert each item
        for (int i = 0; i < nItemCount; i++)
        {
            // build a list of all the item components in the grid view
            long lItemID = 0;
            long lItmTypeID = 0;
            CPatientItemCompList PatItemCompList = null;
            status = BuildPatItemCompListQE(i, out lItemID, out lItmTypeID, out PatItemCompList);
            if (!status.Status)
            {
                return status;
            }

            //load an item for insert
            CPatientItemDataItem di = new CPatientItemDataItem();
            di.PatientID = PatientID;
            di.ItemID = lItemID;
            di.SourceTypeID = (long)k_SOURCE_TYPE_ID.VAPPCT;

            //get the date time, which is a combination of the 2 controls
            di.EntryDate = CDataUtils.GetDate(txtEntryDate.Text, ucTimePicker.HH, ucTimePicker.MM, ucTimePicker.SS);

            // insert the patient item and all of its item components
            long lPatItemID = -1;
            CPatientItemData itemData = new CPatientItemData(BaseMstr.BaseData);
            status = itemData.InsertPatientItem(di, PatItemCompList, out lPatItemID);
            if (!status.Status)
            {
                return status;
            }

            // update the comments if there is a new one. 1 comment will be tied to all results!
            if (!string.IsNullOrEmpty(txtComment.Text))
            {
                status = itemData.InsertPatientItemComment(lPatItemID, lItemID, txtComment.Text);
                if (!status.Status)
                {
                    return status;
                }
            }
        }

        ddlColItems.SelectedIndex = 0;

        //show status
        return new CStatus();
    }
}
