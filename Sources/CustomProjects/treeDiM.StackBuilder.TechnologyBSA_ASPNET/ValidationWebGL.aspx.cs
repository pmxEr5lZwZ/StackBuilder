﻿#region Using directives
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Sharp3D.Math.Core;
using treeDiM.StackBuilder.Basics;
#endregion

namespace treeDiM.StackBuilder.TechnologyBSA_ASPNET
{
    public partial class Validation : Page
    {
        #region Page_Load 
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                ChkbMirrorLength.Checked = LayersMirrorLength;
                ChkbMirrorWidth.Checked = LayersMirrorWidth;
                TBFileName.Text = FileName;
                var interlayerArray = Interlayers.Select(p => p == '1' ? true : false).ToArray();
                var listInterlayers = new List<InterlayerDetails>();
                PalletStacking.InitializeInterlayers(DimCase, DimPallet, MaxPalletHeight, string.Empty, ref listInterlayers);
                for (var i = 0; i < interlayerArray.Length; ++i)
                {
                    if (i < listInterlayers.Count)
                        listInterlayers[i].Activated = interlayerArray[i];
                }

                listInterlayers.Reverse();
                LVInterlayers.DataSource = listInterlayers;
                LVInterlayers.DataBind();

                // clear output directory
                DirectoryHelpers.ClearDirectory(Output);
            }
            ExecuteKeyPad();
            UpdateImage();
        }
        private void ExecuteKeyPad()
        {
            if (ConfigSettings.ShowVirtualKeyboard)
                ScriptManager.RegisterStartupScript(this.Page, Page.GetType(), "VKeyPad", "ActivateVirtualKeyboard();", true);
        }
        #endregion

        #region Update image
        protected void UpdateImage()
        {
            // clear output directory
            DirectoryHelpers.ClearDirectory(Output);

            int caseCount = 0, layerCount = 0;
            double weightLoad = 0.0, weightTotal = 0.0;
            var bbLoad = Vector3D.Zero;
            var bbTotal = Vector3D.Zero;
            string fileGuid = Guid.NewGuid().ToString() + ".glb";

            PalletStacking.GenerateExport(
                DimCase, WeightCase, BitmapTexture,
                DimPallet, WeightPallet,
                MaxPalletHeight,
                BoxPositions,
                ChkbMirrorLength.Checked, ChkbMirrorWidth.Checked,
                InterlayersBoolArray,
                Path.Combine(Output, fileGuid),
                ref caseCount, ref layerCount,
                ref weightLoad, ref weightTotal,
                ref bbLoad, ref bbTotal
            );

            XModelDiv.InnerHtml = $"<x-model class=\"x-model\" src=\"./Output/{fileGuid}\"/>";

            loadedPallet.Update();
        }
        #endregion

        #region Event handlers
        protected void OnInputChanged(object sender, EventArgs e)
        {
            UpdateImage();
        }
        protected void OnExport(object sender, EventArgs e)
        {
            try
            {
                string fileName = TBFileName.Text;
                fileName = Path.ChangeExtension(fileName, "csv");

                byte[] fileBytes = null;
                PalletStacking.Export(
                    DimCase, WeightCase,
                    DimPallet, WeightPallet,
                    MaxPalletHeight, BoxPositions,
                    ChkbMirrorLength.Checked, ChkbMirrorWidth.Checked,
                    InterlayersBoolArray,
                    ref fileBytes);

                if (FtpHelpers.Upload(fileBytes, ConfigSettings.FtpDirectory, fileName, ConfigSettings.FtpUsername, ConfigSettings.FtpPassword))
                {
                    ScriptManager.RegisterStartupScript(this, GetType(), "alertMessage", $"alert('{fileName} was successfully exported!');", true);
                }
            }
            catch (Exception ex)
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "alertMessage", $"alert('{ex.Message}');", true);
            }
        }
        protected void OnPrevious(object sender, EventArgs e)
        {
            // clear output directory
            DirectoryHelpers.ClearDirectory(Output);

            if (LayerEdited)
                Response.Redirect("LayerEdition.aspx");
            else
                Response.Redirect("LayerSelectionWebGL.aspx");
        }
        #endregion

        #region Private variables
        private List<bool> InterlayersBoolArray
        {
            get
            {
                var list = new List<bool>();
                foreach (var item in LVInterlayers.Items)
                {
                    if (item.FindControl("LayerCheckBox") is CheckBox chkBox)
                        list.Add(chkBox.Checked);
                }
                list.Reverse();
                return list;
            }
        }
        private Vector3D DimCase=> Vector3D.Parse((string)Session[SessionVariables.DimCase]);
        private double WeightCase => (double)Session[SessionVariables.WeightCase];
        private Vector3D DimPallet => Vector3D.Parse((string)Session[SessionVariables.DimPallet]);
        private double WeightPallet => (double)Session[SessionVariables.WeightPallet];
        private double MaxPalletHeight => (double)Session[SessionVariables.MaxPalletHeight];
        private bool LayersMirrorLength => (bool)Session[SessionVariables.LayersMirrorLength];
        private bool LayersMirrorWidth => (bool)Session[SessionVariables.LayersMirrorWidth];
        private bool LayerEdited => (bool)Session[SessionVariables.LayerEdited];
        private string FileName => (string)Session[SessionVariables.FileName];
        private List<BoxPosition> BoxPositions => (List<BoxPosition>)Session[SessionVariables.BoxPositions];
        private Bitmap BitmapTexture => (Bitmap)Session[SessionVariables.BitmapTexture];
        private string Output => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
        private string Interlayers => (string) Session[SessionVariables.Interlayers];

        #endregion

    }
}