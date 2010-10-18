﻿#region Using directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using log4net;

using Sharp3D.Math.Core;
using TreeDim.StackBuilder.Basics;
using TreeDim.StackBuilder.Graphics;
#endregion

namespace TreeDim.StackBuilder.Desktop
{
    public partial class DockContentTruckAnalysis :  DockContent, IView, IItemListener
    {
        #region Data members
        /// <summary>
        /// document
        /// </summary>
        private IDocument _document;
        /// <summary>
        /// analysis
        /// </summary>
        private TruckAnalysis _truckAnalysis;
        /// <summary>
        /// view parameters
        /// </summary>
        private const double _cameraDistance = 10000.0;
        private Vector3D _cameraPosition = Graphics3D.Corner_0;
        /// <summary>
        /// Currently selected truck solution
        /// </summary>
        private TruckSolution _sol;
        /// <summary>
        /// logger
        /// </summary>
        static readonly ILog _log = LogManager.GetLogger(typeof(DockContentAnalysis));
        #endregion

        #region Constructor
        public DockContentTruckAnalysis(IDocument document, TruckAnalysis truckAnalysis)
        {
            _document = document;
            _truckAnalysis = truckAnalysis;
            _truckAnalysis.AddListener(this);

            InitializeComponent();
        }
        #endregion

        #region Form override
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // text
            this.Text = _truckAnalysis.Name + "-" + _truckAnalysis.ParentDocument.Name;
            // fill grid
            FillGrid();

            gridSolutions.Selection.SelectionChanged += new SourceGrid.RangeRegionChangedEventHandler(onGridSolutionSelectionChanged);
        }


        private void FillGrid()
        {
            // fill grid solutions
            gridSolutions.Rows.Clear();

            // border
            DevAge.Drawing.BorderLine border = new DevAge.Drawing.BorderLine(Color.DarkBlue, 1);
            DevAge.Drawing.RectangleBorder cellBorder = new DevAge.Drawing.RectangleBorder(border, border);

            // views
            CellBackColorAlternate viewNormal = new CellBackColorAlternate(Color.LightBlue, Color.White);
            viewNormal.Border = cellBorder;
            CheckboxBackColorAlternate viewNormalCheck = new CheckboxBackColorAlternate(Color.LightBlue, Color.White);
            viewNormalCheck.Border = cellBorder;

            // column header view
            SourceGrid.Cells.Views.ColumnHeader viewColumnHeader = new SourceGrid.Cells.Views.ColumnHeader();
            DevAge.Drawing.VisualElements.ColumnHeader backHeader = new DevAge.Drawing.VisualElements.ColumnHeader();
            backHeader.BackColor = Color.LightGray;
            backHeader.Border = DevAge.Drawing.RectangleBorder.NoBorder;
            viewColumnHeader.Background = backHeader;
            viewColumnHeader.ForeColor = Color.White;
            viewColumnHeader.Font = new Font("Arial", 10, FontStyle.Bold);
            viewColumnHeader.ElementSort.SortStyle = DevAge.Drawing.HeaderSortStyle.None;

            // create the grid
            gridSolutions.BorderStyle = BorderStyle.FixedSingle;

            gridSolutions.ColumnsCount = 8;
            gridSolutions.FixedRows = 1;
            gridSolutions.Rows.Insert(0);

            // header
            SourceGrid.Cells.ColumnHeader columnHeader;
            columnHeader = new SourceGrid.Cells.ColumnHeader("Index");
            columnHeader.AutomaticSortEnabled = false;
            columnHeader.View = viewColumnHeader;
            gridSolutions[0, 0] = columnHeader;

            columnHeader = new SourceGrid.Cells.ColumnHeader("Layout");
            columnHeader.AutomaticSortEnabled = false;
            columnHeader.View = viewColumnHeader;
            gridSolutions[0, 1] = columnHeader;

            columnHeader = new SourceGrid.Cells.ColumnHeader("Pallet count");
            columnHeader.AutomaticSortEnabled = false;
            columnHeader.View = viewColumnHeader;
            gridSolutions[0, 2] = columnHeader;

            columnHeader = new SourceGrid.Cells.ColumnHeader("Case count");
            columnHeader.AutomaticSortEnabled = false;
            columnHeader.View = viewColumnHeader;
            gridSolutions[0, 3] = columnHeader; 

            columnHeader = new SourceGrid.Cells.ColumnHeader("Efficiency (%)");
            columnHeader.AutomaticSortEnabled = false;
            columnHeader.View = viewColumnHeader;
            gridSolutions[0, 4] = columnHeader;

            columnHeader = new SourceGrid.Cells.ColumnHeader("Load (kg)");
            columnHeader.AutomaticSortEnabled = false;
            columnHeader.View = viewColumnHeader;
            gridSolutions[0, 5] = columnHeader;

            columnHeader = new SourceGrid.Cells.ColumnHeader("Load height (mm)");
            columnHeader.AutomaticSortEnabled = false;
            columnHeader.View = viewColumnHeader;
            gridSolutions[0, 6] = columnHeader;

            columnHeader = new SourceGrid.Cells.ColumnHeader("Selected");
            columnHeader.AutomaticSortEnabled = false;
            columnHeader.View = viewColumnHeader;
            gridSolutions[0, 7] = columnHeader;

            // handling check box click
            SourceGrid.Cells.Controllers.CustomEvents solCheckboxClickEvent = new SourceGrid.Cells.Controllers.CustomEvents();
            solCheckboxClickEvent.Click += new EventHandler(clickEvent_Click);

            // data rows
            int iIndex = 0;
            foreach (TruckSolution sol in _truckAnalysis.Solutions)
            {
                ++iIndex;
                gridSolutions.Rows.Insert(iIndex);
                // index
                gridSolutions[iIndex, 0] = new SourceGrid.Cells.Cell(string.Format("{0}", iIndex));
                // Layout
                {
                    Graphics2DImage graphics = new Graphics2DImage(new Size(100, 50));
                    TruckSolutionViewer sv = new TruckSolutionViewer(sol);
                    sv.Draw(graphics);
                    gridSolutions[iIndex, 1] = new SourceGrid.Cells.Image(graphics.Bitmap);
                }
                // Pallet count
                gridSolutions[iIndex, 2] = new SourceGrid.Cells.Cell(string.Format("{0}", sol.PalletCount));
                // Case count
                gridSolutions[iIndex, 3] = new SourceGrid.Cells.Cell(string.Format("{0}", sol.BoxCount));
                // Efficiency
                gridSolutions[iIndex, 4] = new SourceGrid.Cells.Cell(string.Format("{0:F}", sol.Efficiency));
                // Load (kg)
                gridSolutions[iIndex, 5] = new SourceGrid.Cells.Cell(string.Format("{0:F}", sol.LoadWeight));
                // Load height (mm)
                gridSolutions[iIndex, 6] = new SourceGrid.Cells.Cell(string.Format("{0:F}", sol.LoadHeight));
                // Selected
                gridSolutions[iIndex, 7] = new SourceGrid.Cells.CheckBox(null, false);

                gridSolutions[iIndex, 0].View = viewNormal;
                gridSolutions[iIndex, 1].View = viewNormal;
                gridSolutions[iIndex, 2].View = viewNormal;
                gridSolutions[iIndex, 3].View = viewNormal;
                gridSolutions[iIndex, 4].View = viewNormal;
                gridSolutions[iIndex, 5].View = viewNormal;
                gridSolutions[iIndex, 6].View = viewNormal;
                gridSolutions[iIndex, 7].View = viewNormalCheck;

                gridSolutions[iIndex, 7].AddController(solCheckboxClickEvent);
            }

            gridSolutions.AutoStretchColumnsToFitWidth = true;
            gridSolutions.AutoSizeCells();
            gridSolutions.Columns.StretchToFit();


            // select first solution
            gridSolutions.Selection.SelectRow(1, true);
            if (_truckAnalysis.Solutions.Count > 0)
                _sol = _truckAnalysis.Solutions[0];
            Draw();
        }
        #endregion

        #region Public properties
        public TruckAnalysis TruckAnalysis
        {
            get { return _truckAnalysis; }
        }
        #endregion

        #region Event handlers
        // checkbox event handler
        void clickEvent_Click(object sender, EventArgs e)
        {
            SourceGrid.CellContext context = (SourceGrid.CellContext)sender;
            int iSel = context.Position.Row - 1;
        }
        // selection changed
        private void onGridSolutionSelectionChanged(object sender, SourceGrid.RangeRegionChangedEventArgs e)
        {
            SourceGrid.RangeRegion region = gridSolutions.Selection.GetSelectionRegion();
            int[] indexes = region.GetRowsIndex();
            // no selection -> exit
            if (indexes.Length == 0) return;
            // get selected solution
            _sol = _truckAnalysis.Solutions[indexes[0] - 1];
            // update select/unselect button text
            UpdateSelectButtonText();
            // redraw
            Draw();            
        }
        private void pictureBoxSolution_SizeChanged(object sender, EventArgs e)
        {
            // redraw
            Draw();
        }
        #endregion

        #region Solution selection
        private void UpdateGridCheckBoxes()
        { 
        }
        private void UpdateSelectButtonText()
        { 
        }
        private int GetCurrentSolutionIndex()
        {
            return 0;
        }
        #endregion

        #region IItemListener implementation
        public void Update(ItemBase item)
        {
            // update grid
            FillGrid();
            // select first solution
            if (gridSolutions.RowsCount > 0)
                gridSolutions.Selection.SelectRow(1, true);
            if (_truckAnalysis.Solutions.Count > 0)
                _sol = _truckAnalysis.Solutions[0];
            // draw
            Draw();
        }
        public void Kill(ItemBase item)
        {
            Close();
            _truckAnalysis.RemoveListener(this);
        }
        #endregion

        #region IView implementation
        public IDocument Document
        {
            get { return _document; }
        }
        #endregion

        #region Drawing
        private void Draw()
        {
            try
            {
                // sanity check
                if (pictureBoxTruckSolution.Size.Width < 1 || pictureBoxTruckSolution.Size.Height < 1)
                    return;
                // instantiate graphics
                Graphics3DImage graphics = new Graphics3DImage(pictureBoxTruckSolution.Size);
                // set camera position
                graphics.CameraPosition = _cameraPosition;
                // instantiate solution viewer
                if (null == _sol)
                    return;
                TruckSolutionViewer sv = new TruckSolutionViewer(_sol);
                sv.Draw(graphics);
                // show generated bitmap on picture box control
                pictureBoxTruckSolution.Image = graphics.Bitmap;
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString());
            }
        }
        #endregion

        #region Handlers to define point of view
        private void onViewCorner_0(object sender, EventArgs e)        {  _cameraPosition = Graphics3D.Corner_0;    Draw();     }
        private void onViewCorner_90(object sender, EventArgs e)       {  _cameraPosition = Graphics3D.Corner_90;   Draw();     }
        private void onViewCorner_180(object sender, EventArgs e)      {  _cameraPosition = Graphics3D.Corner_180;  Draw();     }
        private void onViewCorner_270(object sender, EventArgs e)      {  _cameraPosition = Graphics3D.Corner_270;  Draw();     }
        private void onViewSideFront(object sender, EventArgs e)       {  _cameraPosition = Graphics3D.Front;       Draw();     }
        private void onViewSideLeft(object sender, EventArgs e)        {  _cameraPosition = Graphics3D.Left;        Draw();     }
        private void onViewSideRear(object sender, EventArgs e)        {  _cameraPosition = Graphics3D.Back;        Draw();     }
        private void onViewSideRight(object sender, EventArgs e)       {  _cameraPosition = Graphics3D.Right;       Draw();     }
        private void onViewTop(object sender, EventArgs e)             {  _cameraPosition = Graphics3D.Top;         Draw();     }
        #endregion
    }
}