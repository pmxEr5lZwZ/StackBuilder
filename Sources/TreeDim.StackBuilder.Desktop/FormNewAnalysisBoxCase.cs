﻿#region Using directives
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using log4net;

using Sharp3D.Math.Core;

using treeDiM.StackBuilder.Engine;
using treeDiM.StackBuilder.Basics;
using treeDiM.StackBuilder.Graphics.Controls;
using treeDiM.StackBuilder.Desktop.Properties;
#endregion

namespace treeDiM.StackBuilder.Desktop
{
    public partial class FormNewAnalysisBoxCase : FormNewAnalysis, IItemBaseFilter
    {
        #region Constructor
        public FormNewAnalysisBoxCase(Document doc, AnalysisHomo analysis)
            : base(doc, analysis)
        {
            InitializeComponent();
        }
        #endregion

        #region FormNewBase overrides
        public override string ItemDefaultName
        { get { return Resources.ID_ANALYSIS; } }
        #endregion

        #region Form override
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            cbBoxes.Initialize(_document, this, AnalysisCast?.Content);
            cbCases.Initialize(_document, this, AnalysisCast?.Container);

            // event handling
            uCtrlLayerList.LayerSelected += OnLayerSelected;
            uCtrlLayerList.RefreshFinished += OnLayerSelected;
            uCtrlLayerList.ButtonSizes = new Size(100, 100);

            if (null == AnalysisCast)
            {
                uCtrlCaseOrientation.AllowedOrientations = new bool[]
                { Settings.Default.AllowVerticalX, Settings.Default.AllowVerticalY, Settings.Default.AllowVerticalZ };
                uCtrlOptMaximumWeight.Value = new OptDouble(false, Settings.Default.MaximumCaseWeight);
                uCtrlOptMaxNumber.Value = new OptInt(false, 1000);
            }
            else
            {
                if (AnalysisCast.ConstraintSet is ConstraintSetBoxCase constraintSet)
                {
                    uCtrlOptMaximumWeight.Value = constraintSet.OptMaxWeight;
                    uCtrlOptMaxNumber.Value = constraintSet.OptMaxNumber;
                }
            }
            checkBoxBestLayersOnly.Checked = Settings.Default.KeepBestSolutions;
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            Settings.Default.AllowVerticalX = uCtrlCaseOrientation.AllowedOrientations[0];
            Settings.Default.AllowVerticalY = uCtrlCaseOrientation.AllowedOrientations[1];
            Settings.Default.AllowVerticalZ = uCtrlCaseOrientation.AllowedOrientations[2];

            if (uCtrlOptMaximumWeight.Value.Activated)
                Settings.Default.MaximumCaseWeight = uCtrlOptMaximumWeight.Value.Value;

            Settings.Default.KeepBestSolutions = checkBoxBestLayersOnly.Checked;
        }
        #endregion

        #region FormNewAnalysis override
        public override void OnNext()
        {
            base.OnNext();
            try
            {
                List<LayerDesc> layerDescs = new List<LayerDesc>();
                foreach (ILayer2D layer2D in uCtrlLayerList.Selected)
                    layerDescs.Add(layer2D.LayerDescriptor);

                Solution.SetSolver(new LayerSolver());

                AnalysisBoxCase analysis = AnalysisCast;
                if (null == analysis)
                    AnalysisBase = _document.CreateNewAnalysisBoxCase(
                        ItemName, ItemDescription
                        , SelectedBoxProperties, SelectedCase
                        , new List<InterlayerProperties>()
                        , BuildConstraintSet()
                        , layerDescs
                        );
                else
                {
                    analysis.ID.SetNameDesc(ItemName, ItemDescription);
                    analysis.Content = SelectedBoxProperties;
                    analysis.CaseProperties = SelectedCase;
                    analysis.ConstraintSet = BuildConstraintSet();
                    analysis.AddSolution(layerDescs);

                    _document.UpdateAnalysis(analysis);
                }
                Close();
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString());
            }
        }
        private void OnBestCombinationClicked(object sender, EventArgs e)
        {
            try
            {
                // get case
                BoxProperties caseProperties = cbCases.SelectedType as BoxProperties;
                if (!(cbBoxes.SelectedType is PackableBrick packable) || null == caseProperties)
                    return;
                var constraintSet = BuildConstraintSet();
                // get best combination
                List<KeyValuePair<LayerDesc, int>> listLayer = new List<KeyValuePair<LayerDesc, int>>();
                LayerSolver.GetBestCombination(
                    packable.OuterDimensions,
                    caseProperties.GetStackingDimensions(constraintSet),
                    constraintSet,
                    ref listLayer);
                if (0 == listLayer.Count)
                {
                    _log.Warn("Failed to find a single valid layer");
                    return;
                }
                // select best layer
                List<LayerDesc> layerDesc = new List<LayerDesc>();
                foreach (KeyValuePair<LayerDesc, int> kvp in listLayer)
                    layerDesc.Add(kvp.Key);
                uCtrlLayerList.SelectLayers(layerDesc);

                _item = _document.CreateNewAnalysisBoxCase(
                    ItemName, ItemDescription
                    , SelectedBoxProperties, SelectedCase
                    , new List<InterlayerProperties>()
                    , BuildConstraintSet()
                    , layerDesc
                    );
                Close();
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString());
            }
        }
        #endregion

        #region IItemBaseFilter implementation
        public bool Accept(Control ctrl, ItemBase itemBase)
        {
            if (ctrl == cbBoxes)
            {
                return itemBase is PackableBrick packable
                    && (
                    (packable is BoxProperties)
                    || (packable is BundleProperties)
                    || (packable is PackProperties)
                    || (packable is LoadedCase)
                    );
            }
            else if (ctrl == cbCases)
            {
                return itemBase is Packable packable
                    && packable.IsCase
                    && (packable is BoxProperties)
                    && (packable != SelectedBoxProperties);
            }
            return false;
        }
        #endregion

        #region Private properties
        private Packable SelectedBoxProperties
        {
            get { return cbBoxes.SelectedType as Packable; }
        }
        private BoxProperties SelectedCase
        {
            get { return cbCases.SelectedType as BoxProperties; }
        }
        private AnalysisBoxCase AnalysisCast
        {
            get { return _item as AnalysisBoxCase; }
        }
        #endregion

        #region Event handlers
        private void OnBoxChanged(object sender, EventArgs e)
        {
            cbCases.Initialize(_document, this, null);
            OnInputChanged(sender, e);
        }

        private void OnInputChanged(object sender, EventArgs e)
        {
            try
            {
                // get box / case
                BoxProperties caseProperties = cbCases.SelectedType as BoxProperties;
                if (!(cbBoxes.SelectedType is PackableBrick packable) || null == caseProperties)
                    return;

                // update orientation control
                uCtrlCaseOrientation.BProperties = packable;

                // compute
                LayerSolver solver = new LayerSolver();
                List<Layer2D> layers = solver.BuildLayers(
                    packable.OuterDimensions
                    , new Vector2D(caseProperties.InsideLength, caseProperties.InsideWidth)
                    , 0.0 /* offsetZ */
                    , BuildConstraintSet()
                    , checkBoxBestLayersOnly.Checked
                    );
                // update control
                uCtrlLayerList.Packable = packable;
                uCtrlLayerList.ContainerHeight = caseProperties.InsideHeight;
                uCtrlLayerList.FirstLayerSelected = true;
                uCtrlLayerList.LayerList = layers.Cast<ILayer2D>().ToList();
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString());
            }
        }
        protected void OnLayerSelected(object sender, EventArgs e)
        {
            try
            {
                ILayer2D[] layers = uCtrlLayerList.Selected;
                UpdateStatus(layers.Length > 0 ? string.Empty : Resources.ID_SELECTATLEASTONELAYOUT);
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString());
            }
        }
        #endregion

        #region Helpers
        private ConstraintSetBoxCase BuildConstraintSet()
        { 
            // constraint set
            ConstraintSetBoxCase constraintSet = new ConstraintSetBoxCase(SelectedCase);
            constraintSet.SetAllowedOrientations(uCtrlCaseOrientation.AllowedOrientations);
            constraintSet.OptMaxWeight = uCtrlOptMaximumWeight.Value;
            constraintSet.OptMaxNumber = uCtrlOptMaxNumber.Value;
            return constraintSet;        
        }
        #endregion

        #region Data members
        static readonly ILog _log = LogManager.GetLogger(typeof(FormNewAnalysisBoxCase));
        #endregion
    }
}
