using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using VAPPCT.DA;

public class CItemCollectionData : CData
{
    public CItemCollectionData(CData Data)
        : base(Data)
    {
        //constructors are not inherited in c#!
    }

    /// <summary>
    /// method to run logic over a colletion
    ///     1. all items in multi select must be have an option selected
    ///     2. all free text items must have data entered
    ///     3. labs and note titles are ignored
    ///     4. items that are not ACTIVE are ignored
    /// </summary>
    public CStatus RunCollectionLogic(string strPatientID,
                                      long lPatChecklistID,
                                      long lChecklistID,
                                      long lCollectionItemID)
    {
        //Get the patient checklist item data
        CPatChecklistItemData PatChecklistItem = new CPatChecklistItemData(this);
        CPatChecklistItemDataItem pdi = null;
        PatChecklistItem.GetPatCLItemDI(lPatChecklistID,
                                        lCollectionItemID,
                                        out pdi);

        //get all the items in the collection
        CItemCollectionData coll = new CItemCollectionData(this);
        DataSet dsColl = null;
        CStatus status = coll.GetItemCollectionDS(lCollectionItemID, out dsColl);

        //get all the patient item components in the collection
        DataSet dsPatItemComps = null;
        status = coll.GetItemColMostRecentPatICDS(lCollectionItemID, strPatientID, out dsPatItemComps);

        //if this collection item is overridden just return and don't update the state
        if (pdi.IsOverridden == k_TRUE_FALSE_ID.True)
        {
            bool bReturn = true;

            //check to see if there is a new result and override if there is...
            foreach (DataTable tableI in dsColl.Tables)
            {
                foreach (DataRow drI in tableI.Rows)
                {
                    //get values from the the item in the collection (not the patient items, the collection items)
                    long lItemID = CDataUtils.GetDSLongValue(drI, "ITEM_ID");
                    long lItemTypeID = CDataUtils.GetDSLongValue(drI, "ITEM_TYPE_ID");
                    long lActiveID = CDataUtils.GetDSLongValue(drI, "ACTIVE_ID");

                    //get the item so we can check entry date against TS etc...
                    CPatientItemDataItem patItemDI = null;
                    CPatientItemData patData = new CPatientItemData(this);
                    patData.GetMostRecentPatientItemDI(strPatientID, lItemID, out patItemDI);
                    DateTime dtEntryDate = patItemDI.EntryDate;

                    if (dtEntryDate > pdi.OverrideDate)
                    {
                        pdi.OverrideDate = CDataUtils.GetNullDate();
                        pdi.IsOverridden = k_TRUE_FALSE_ID.False;
                        PatChecklistItem.UpdatePatChecklistItem(pdi);
                        bReturn = false;
                        break;
                    }
                }
            }

            if(bReturn)
            {
                //return if this item is overridden and there are no new results
                return new CStatus();
            }
        }
            
        //get the checklist item di
        CChecklistItemDataItem cidDI = null;
        CChecklistItemData cid = new CChecklistItemData(this);
        cid.GetCLItemDI(lChecklistID, lCollectionItemID, out cidDI);

        //calculate how far the TS can go back
        long lTS = cidDI.CLITSTimePeriod;
        DateTime dtNow = DateTime.Now;
        DateTime dtTSCompare = dtNow.AddDays(-1 * lTS);
       
        //keeps the overall OS state of the items
        k_STATE_ID kOverallOSStateID = k_STATE_ID.NotSelected;

        //keeps the overall TS state of the items
        k_STATE_ID kOverallTSStateID = k_STATE_ID.NotSelected;

        //loop over the collection items
        foreach (DataTable table in dsColl.Tables)
        {
            foreach (DataRow dr in table.Rows)
            {
                //get values from the the item in the collection (not the patient items, the collection items)
                long lItemID = CDataUtils.GetDSLongValue(dr, "ITEM_ID");
                long lItemTypeID = CDataUtils.GetDSLongValue(dr, "ITEM_TYPE_ID");
                long lActiveID = CDataUtils.GetDSLongValue(dr, "ACTIVE_ID");

                //get the item so we can check entry date against TS etc...
                CPatientItemDataItem patItemDI = null;
                CPatientItemData patData = new CPatientItemData(this);
                patData.GetMostRecentPatientItemDI(strPatientID, lItemID, out patItemDI);
                DateTime dtEntryDate = patItemDI.EntryDate;

                //only interested in ACTIVE items
                if ((k_ACTIVE)lActiveID == k_ACTIVE.ACTIVE)
                {
                    //check the TS and set overall TS state
                    k_COMPARE kTS = CDataUtils.CompareDates(dtEntryDate, dtTSCompare);
                    if (kTS == k_COMPARE.GREATERTHAN ||
                        kTS == k_COMPARE.EQUALTO)
                    {
                        //good
                        if (kOverallTSStateID != k_STATE_ID.Bad)
                        {
                            kOverallTSStateID = k_STATE_ID.Good;
                        }
                    }
                    else
                    {
                        //bad
                        kOverallTSStateID = k_STATE_ID.Bad;
                    }

                    //list to hold the created component items
                    CPatientItemCompList PatItemCompList = new CPatientItemCompList();

                    //build a pat item component list loaded with the most recent
                    //values
                    foreach (DataTable tableComp in dsPatItemComps.Tables)
                    {
                        foreach (DataRow drComp in tableComp.Rows)
                        {
                            //values to load the component item
                            long lCompItemID = CDataUtils.GetDSLongValue(drComp, "ITEM_ID");
                            long lComponentID = CDataUtils.GetDSLongValue(drComp, "ITEM_COMPONENT_ID");
                            string strComponentValue = CDataUtils.GetDSStringValue(drComp, "COMPONENT_VALUE");
                            long lPatItemID = CDataUtils.GetDSLongValue(drComp, "PAT_ITEM_ID");
                            //only this item
                            if (lCompItemID == lItemID)
                            {
                                CPatientItemComponentDataItem diComp = new CPatientItemComponentDataItem();
                                diComp.PatientID = strPatientID;
                                diComp.ItemID = lCompItemID;
                                diComp.ComponentID = lComponentID;
                                diComp.ComponentValue = strComponentValue;
                                diComp.PatItemID = lPatItemID;

                                PatItemCompList.Add(diComp);
                            }
                        }

                        //we now have a list of item components for this item
                        //loop and get status for this item and update the overall status
                        bool bHasSelectedValue = false;
                        foreach (CPatientItemComponentDataItem diPatItemComp in PatItemCompList)
                        {
                            //get the state id for this component
                            CICStateDataItem sdi = null;
                            CItemComponentData icd = new CItemComponentData(this);
                            icd.GetICStateDI(lItemID, diPatItemComp.ComponentID, out sdi);

                            //switch on the type and get the value
                            switch ((k_ITEM_TYPE_ID)lItemTypeID)
                            {
                                case k_ITEM_TYPE_ID.Laboratory:
                                    {
                                        bHasSelectedValue = true;

                                        //get the ranges
                                        CICRangeDataItem rdi = null;
                                        icd.GetICRangeDI(lItemID, diPatItemComp.ComponentID, out rdi);
                                        if (String.IsNullOrEmpty(diPatItemComp.ComponentValue))
                                        {
                                            //does not have a value?
                                            kOverallOSStateID = k_STATE_ID.Unknown;
                                        }
                                        else
                                        {
                                            try
                                            {
                                                double dblValue = Convert.ToDouble(diPatItemComp.ComponentValue);

                                                //max/high check
                                                if (dblValue >= rdi.LegalMax)
                                                {
                                                    if (kOverallOSStateID != k_STATE_ID.Bad)
                                                    {
                                                        kOverallOSStateID = k_STATE_ID.Unknown;
                                                    }
                                                }
                                                else
                                                {
                                                    if (dblValue >= rdi.High)
                                                    {
                                                        kOverallOSStateID = k_STATE_ID.Bad;
                                                    }
                                                    else
                                                    {
                                                        if (kOverallOSStateID != k_STATE_ID.Bad)
                                                        {
                                                            kOverallOSStateID = k_STATE_ID.Good;
                                                        }
                                                    }
                                                }

                                                //min/low check
                                                if (dblValue <= rdi.LegalMin)
                                                {
                                                    if (kOverallOSStateID != k_STATE_ID.Bad)
                                                    {
                                                        kOverallOSStateID = k_STATE_ID.Unknown;
                                                    }
                                                }
                                                else
                                                {
                                                    if (dblValue <= rdi.Low)
                                                    {
                                                        kOverallOSStateID = k_STATE_ID.Bad;
                                                    }
                                                    else
                                                    {
                                                        if (kOverallOSStateID != k_STATE_ID.Bad)
                                                        {
                                                            kOverallOSStateID = k_STATE_ID.Good;
                                                        }
                                                    }
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                if (kOverallOSStateID != k_STATE_ID.Bad)
                                                {
                                                    kOverallOSStateID = k_STATE_ID.Unknown;
                                                }
                                            }
                                        }

                                        break;
                                    }
                                case k_ITEM_TYPE_ID.NoteTitle:
                                    {
                                        //note titles are excluded from quick entry
                                        //so if we have one our os state is unknown
                                        bHasSelectedValue = true;
                                        kOverallOSStateID = k_STATE_ID.Unknown;
                                        break;
                                    }
                                case k_ITEM_TYPE_ID.QuestionFreeText:
                                    {
                                        bHasSelectedValue = true;
                                        if (diPatItemComp.ComponentValue.Length < 1)
                                        {
                                            //if they did not enter a value 
                                            //then the overall state is bad!
                                            kOverallOSStateID = k_STATE_ID.Bad;
                                        }
                                        break;
                                    }
                                case k_ITEM_TYPE_ID.QuestionSelection:
                                    if (!String.IsNullOrEmpty(diPatItemComp.ComponentValue))
                                    {
                                        //only interested in the one they selected
                                        if ((k_TRUE_FALSE_ID)Convert.ToInt64(diPatItemComp.ComponentValue) != k_TRUE_FALSE_ID.False)
                                        {
                                            bHasSelectedValue = true;
                                            if (kOverallOSStateID != k_STATE_ID.Bad)
                                            {
                                                if ((k_STATE_ID)sdi.StateID == k_STATE_ID.Bad)
                                                {
                                                    kOverallOSStateID = k_STATE_ID.Bad;
                                                }
                                                else
                                                {
                                                    if ((k_STATE_ID)sdi.StateID == k_STATE_ID.Good)
                                                    {
                                                        if (kOverallOSStateID != k_STATE_ID.Unknown)
                                                        {
                                                            kOverallOSStateID = k_STATE_ID.Good;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        kOverallOSStateID = k_STATE_ID.Unknown;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    break;
                            }
                        }//for each

                        //if there is nothing selected then the 
                        //overall TS state is bad and the OS state is unknown
                        if (!bHasSelectedValue)
                        {
                            kOverallOSStateID = k_STATE_ID.Unknown;
                            kOverallTSStateID = k_STATE_ID.Bad;
                        }
                    }
                }
            }
        }

        //
        //now update the collection item states.
        //

        //get the ts,os and ds possible values
        CChecklistItemData dCL = new CChecklistItemData(this);
        DataSet dsTS = null;
        dCL.GetTemporalStateDS(lChecklistID, lCollectionItemID, out dsTS);
        long lGoodTSID = dCL.GetTSDefaultStateID(dsTS, k_STATE_ID.Good);
        long lBadTSID = dCL.GetTSDefaultStateID(dsTS, k_STATE_ID.Bad);
        long lUnknownTSID = dCL.GetTSDefaultStateID(dsTS, k_STATE_ID.Unknown);

        DataSet dsOS = null;
        dCL.GetOutcomeStateDS(lChecklistID, lCollectionItemID, out dsOS);
        long lGoodOSID = dCL.GetOSDefaultStateID(dsOS, k_STATE_ID.Good);
        long lBadOSID = dCL.GetOSDefaultStateID(dsOS, k_STATE_ID.Bad);
        long lUnknownOSID = dCL.GetOSDefaultStateID(dsOS, k_STATE_ID.Unknown);

        DataSet dsDS = null;
        dCL.GetDecisionStateDS(lChecklistID, lCollectionItemID, out dsDS);
        long lGoodDSID = dCL.GetDSDefaultStateID(dsDS, k_STATE_ID.Good);
        long lBadDSID = dCL.GetDSDefaultStateID(dsDS, k_STATE_ID.Bad);
        long lUnknownDSID = dCL.GetDSDefaultStateID(dsDS, k_STATE_ID.Unknown);

        //update the TS state on the data item
        if (kOverallTSStateID == k_STATE_ID.Bad)
        {
            pdi.TSID = lBadTSID;
        }
        else if (kOverallTSStateID == k_STATE_ID.Good)
        {
            pdi.TSID = lGoodTSID;
        }
        else
        {
            pdi.TSID = lUnknownTSID;
        }

        //update the OS state on the data item
        if (kOverallOSStateID == k_STATE_ID.Bad)
        {
            pdi.OSID = lBadOSID;
        }
        else if (kOverallOSStateID == k_STATE_ID.Good)
        {
            pdi.OSID = lGoodOSID;
        }
        else
        {
            pdi.OSID = lUnknownOSID;
        }

        //update the ds state on the item data
        if (kOverallTSStateID == k_STATE_ID.Good &&
            kOverallOSStateID == k_STATE_ID.Good)
        {
            pdi.DSID = lGoodDSID;
        }
        else
        {
            pdi.DSID = lBadDSID;
        }

        //update the checklist item state
        status = PatChecklistItem.UpdatePatChecklistItem(pdi);

        return status;
    }

    /// <summary>
    /// US:1883 insert an item collection
    /// </summary>
    /// <param name="di"></param>
    /// <returns></returns>
    public CStatus InsertItemCollection(CItemCollectionDataItem di)
    {
        //create a status object and check for valid dbconnection
        CStatus status = DBConnValid();
        if (!status.Status)
        {
            return status;
        }

        //load the paramaters list
        CParameterList pList = new CParameterList(SessionID, ClientIP, UserID);

        // procedure specific parameters
        pList.AddInputParameter("pi_nCollectionItemID", di.CollectionItemID);
        pList.AddInputParameter("pi_nItemID", di.ItemID);
        pList.AddInputParameter("pi_nSortOrder", di.SortOrder);

        //execute the SP
        status = DBConn.ExecuteOracleSP("PCK_ITEM_COLLECTION.InsertItemCollection", pList);
        if (!status.Status)
        {
            return status;
        }

        return new CStatus();
    }

    /// <summary>
    /// US:1883 get a collection dataset
    /// </summary>
    /// <param name="ds"></param>
    /// <returns></returns>
    public CStatus GetItemCollectionDS(out DataSet ds)
    {
        //initialize parameters
        ds = null;

        //create a status object and check for valid dbconnection
        CStatus status = DBConnValid();
        if (!status.Status)
        {
            return status;
        }

        //load the paramaters list
        CParameterList pList = new CParameterList(SessionID, ClientIP, UserID);

        //get the dataset
        CDataSet cds = new CDataSet();
        status = cds.GetOracleDataSet(
            DBConn,
            "PCK_ITEM_COLLECTION.GetItemCollectionRS",
            pList,
            out ds);
        if (!status.Status)
        {
            return status;
        }

        return new CStatus();
    }

    /// <summary>
    /// US:1883 get an items collection dataset
    /// </summary>
    /// <param name="lCollectionItemID"></param>
    /// <param name="ds"></param>
    /// <returns></returns>
    public CStatus GetItemCollectionDS(long lCollectionItemID, out DataSet ds)
    {
        //initialize parameters
        ds = null;

        //create a status object and check for valid dbconnection
        CStatus status = DBConnValid();
        if (!status.Status)
        {
            return status;
        }

        //load the paramaters list
        CParameterList pList = new CParameterList(SessionID, ClientIP, UserID);

        pList.AddInputParameter("pi_nCollectionItemID", lCollectionItemID);

        //get the dataset
        CDataSet cds = new CDataSet();
        status = cds.GetOracleDataSet(
            DBConn,
            "PCK_ITEM_COLLECTION.GetItemCollectionRS",
            pList,
            out ds);
        if (!status.Status)
        {
            return status;
        }

        return new CStatus();
    }

    /// <summary>
    /// US:1883 get the most recent item
    /// </summary>
    /// <param name="lCollectionItemID"></param>
    /// <param name="strPatientID"></param>
    /// <param name="ds"></param>
    /// <returns></returns>
    public CStatus GetItemColMostRecentPatItemDS(long lCollectionItemID, string strPatientID, out DataSet ds)
    {
        //initialize parameters
        ds = null;

        //create a status object and check for valid dbconnection
        CStatus status = DBConnValid();
        if (!status.Status)
        {
            return status;
        }

        //load the paramaters list
        CParameterList pList = new CParameterList(SessionID, ClientIP, UserID);

        pList.AddInputParameter("pi_nCollectionItemID", lCollectionItemID);
        pList.AddInputParameter("pi_vPatientID", strPatientID);

        //get the dataset
        CDataSet cds = new CDataSet();
        status = cds.GetOracleDataSet(
            DBConn,
            "PCK_ITEM_COLLECTION.GetItemColPatItemRS",
            pList,
            out ds);
        if (!status.Status)
        {
            return status;
        }

        return new CStatus();
    }

    /// <summary>
    /// US:1883 get the patients most recent collection item
    /// </summary>
    /// <param name="lCollectionItemID"></param>
    /// <param name="strPatientID"></param>
    /// <param name="ds"></param>
    /// <returns></returns>
    public CStatus GetItemColMostRecentPatICDS(long lCollectionItemID, string strPatientID, out DataSet ds)
    {
        //initialize parameters
        ds = null;

        //create a status object and check for valid dbconnection
        CStatus status = DBConnValid();
        if (!status.Status)
        {
            return status;
        }

        //load the paramaters list
        CParameterList pList = new CParameterList(SessionID, ClientIP, UserID);
        pList.AddInputParameter("pi_nCollectionItemID", lCollectionItemID);
        pList.AddInputParameter("pi_vPatientID", strPatientID);

        //get the dataset
        CDataSet cds = new CDataSet();
        status = cds.GetOracleDataSet(
            DBConn,
            "PCK_ITEM_COLLECTION.GetItemColPatICRS",
            pList,
            out ds);
        if (!status.Status)
        {
            return status;
        }

        return new CStatus();
    }

    /// <summary>
    /// US:1883 get the item collection DI
    /// </summary>
    /// <param name="lCollectionItemID"></param>
    /// <param name="lItemID"></param>
    /// <param name="di"></param>
    /// <returns></returns>
    public CStatus GetItemCollectionDI(long lCollectionItemID, long lItemID, out CItemCollectionDataItem di)
    {
        //initialize parameters
        di = null;

        //create a status object and check for valid dbconnection
        CStatus status = DBConnValid();
        if (!status.Status)
        {
            return status;
        }

        //load the paramaters list
        CParameterList pList = new CParameterList(SessionID, ClientIP, UserID);

        // procedure specific parameters
        pList.AddInputParameter("pi_nCollectionItemID", lCollectionItemID);
        pList.AddInputParameter("pi_nItemID", lItemID);

        //get the dataset
        CDataSet cds = new CDataSet();
        DataSet ds = null;
        status = cds.GetOracleDataSet(
            DBConn,
            "PCK_ITEM_COLLECTION.GetItemCollectionDI",
            pList,
            out ds);
        if (!status.Status)
        {
            return status;
        }

        di = new CItemCollectionDataItem(ds);

        return new CStatus();
    }

    /// <summary>
    /// US:1883 update a collection item
    /// </summary>
    /// <param name="di"></param>
    /// <returns></returns>
    public CStatus UpdateItemCollection(CItemCollectionDataItem di)
    {
        //create a status object and check for valid dbconnection
        CStatus status = DBConnValid();
        if (!status.Status)
        {
            return status;
        }

        //load the paramaters list
        CParameterList pList = new CParameterList(SessionID, ClientIP, UserID);

        // procedure specific parameters
        pList.AddInputParameter("pi_nCollectionItemID", di.CollectionItemID);
        pList.AddInputParameter("pi_nItemID", di.ItemID);
        pList.AddInputParameter("pi_nSortOrder", di.SortOrder);

        //execute the SP
        status = DBConn.ExecuteOracleSP("PCK_ITEM_COLLECTION.UpdateItemCollection", pList);
        if (!status.Status)
        {
            return status;
        }

        return new CStatus();
    }

    /// <summary>
    /// US:1883 delete a collection item
    /// </summary>
    /// <param name="lCollectionItemID"></param>
    /// <param name="lItemID"></param>
    /// <returns></returns>
    public CStatus DeleteItemCollection(long lCollectionItemID, long lItemID)
    {
        //create a status object and check for valid dbconnection
        CStatus status = DBConnValid();
        if (!status.Status)
        {
            return status;
        }

        //load the paramaters list
        CParameterList pList = new CParameterList(SessionID, ClientIP, UserID);

        // procedure specific parameters
        pList.AddInputParameter("pi_nCollectionItemID", lCollectionItemID);
        pList.AddInputParameter("pi_nItemID", lItemID);

        //execute the SP
        status = DBConn.ExecuteOracleSP("PCK_ITEM_COLLECTION.DeleteItemCollection", pList);
        if (!status.Status)
        {
            return status;
        }

        return new CStatus();
    }

    /// <summary>
    /// US:1883 delete item collection
    /// </summary>
    /// <param name="lCollectionItemID"></param>
    /// <returns></returns>
    public CStatus DeleteItemCollection(long lCollectionItemID)
    {
        //create a status object and check for valid dbconnection
        CStatus status = DBConnValid();
        if (!status.Status)
        {
            return status;
        }

        //load the paramaters list
        CParameterList pList = new CParameterList(SessionID, ClientIP, UserID);

        // procedure specific parameters
        pList.AddInputParameter("pi_nCollectionItemID", lCollectionItemID);

        //execute the SP
        status = DBConn.ExecuteOracleSP("PCK_ITEM_COLLECTION.DeleteItemCollection", pList);
        if (!status.Status)
        {
            return status;
        }

        return new CStatus();
    }

    /// <summary>
    /// US:1883 delete item collection
    /// </summary>
    /// <param name="lCollectionItemID"></param>
    /// <param name="strItemIDs"></param>
    /// <returns></returns>
    public CStatus DeleteItemCollection(long lCollectionItemID, string strItemIDs)
    {
        //create a status object and check for valid dbconnection
        CStatus status = DBConnValid();
        if (!status.Status)
        {
            return status;
        }

        //load the paramaters list
        CParameterList pList = new CParameterList(SessionID, ClientIP, UserID);

        // procedure specific parameters
        pList.AddInputParameter("pi_nCollectionItemID", lCollectionItemID);
        pList.AddInputParameter("pi_vItemIDs", strItemIDs);

        //execute the SP
        status = DBConn.ExecuteOracleSP("PCK_ITEM_COLLECTION.DeleteItemCollection", pList);
        if (!status.Status)
        {
            return status;
        }

        return new CStatus();
    }
}
