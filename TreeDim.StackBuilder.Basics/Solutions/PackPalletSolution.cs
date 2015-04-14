﻿#region Using directives
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sharp3D.Math.Core;
#endregion

namespace TreeDim.StackBuilder.Basics
{
    public class PackPalletSolution : IComparable
    {
        #region Data members
        /// <summary>
        /// Solution layer
        /// </summary>
        private BoxLayer _layer;
        /// <summary>
        /// List of layers
        /// </summary>
        private List<LayerDescriptor> _listLayers = new List<LayerDescriptor>();
        /// <summary>
        /// Parent analysis
        /// </summary>
        private PackPalletAnalysis _parentAnalysis;
        /// <summary>
        /// Solution title
        /// </summary>
        private string _title = string.Empty;
        /// <summary>
        /// laod bounding box
        /// </summary>
        private BBox3D _bbox = new BBox3D();
        #endregion

        #region Constructor
        public PackPalletSolution(PackPalletAnalysis analysis, string title, BoxLayer layer)
        {
            _parentAnalysis = analysis;
            _title = title;
            _layer = layer;
        }
        #endregion

        #region Add layer method
        public void AddLayer(bool swapped, bool hasInterlayer)
        {
            _listLayers.Add(new LayerDescriptor(swapped, hasInterlayer));
        }
        #endregion

        #region Accessing pack positions
        public BoxLayer GetBoxLayer(int iLayerIndex, ref bool hasInterlayer, ref double zInterlayer)
        {
            if (null == _parentAnalysis)
                throw new Exception("_parentAnalysis not set.");

            double interlayerThickness = (null != _parentAnalysis.InterlayerProperties) ? _parentAnalysis.InterlayerProperties.Thickness : 0.0;
            double packHeight = _parentAnalysis.PackProperties.Height;
            double zLow = _parentAnalysis.PalletProperties.Height;
            int i = 0;
            while (i <= iLayerIndex-1)
            {
                LayerDescriptor desc = _listLayers[i];
                zLow += (desc.HasInterlayer ? interlayerThickness : 0.0) + packHeight;
                ++i;
            }
            zInterlayer = zLow;
            hasInterlayer = _listLayers[iLayerIndex].HasInterlayer;
            zLow += hasInterlayer ? interlayerThickness : 0.0;
            // build BoxLayer
            BoxLayer layer = new BoxLayer(zLow + (hasInterlayer ? interlayerThickness : 0.0), _layer.PatternName);
            foreach (BoxPosition b in _layer)
                layer.Add(new BoxPosition(b.Position + zLow * Vector3D.ZAxis, b.DirectionLength, b.DirectionWidth));
            return layer;
        }
        #endregion

        #region BoundingBox
        public BBox3D BoundingBox
        {
            get
            {
                BBox3D bbox = new BBox3D();
                // --- extend
                // pallet
                bbox.Extend(Vector3D.Zero);
                bbox.Extend(new Vector3D(Analysis.PalletProperties.Length, Analysis.PalletProperties.Width, Analysis.PalletProperties.Height));
                // load
                bbox.Extend(LoadBoundingBox);
                return bbox;
            }
        }
        public BBox3D LoadBoundingBox
        {
            get
            {
                if (!_bbox.IsValid)
                    _bbox = ComputeLoadBBox3D();
                return _bbox;
            }
        }

        private BBox3D ComputeLoadBBox3D()
        {
            BBox3D bbox = new BBox3D();
            bool hasInterlayer = false;
            double zInterlayer = 0.0;
            BoxLayer layer0 = GetBoxLayer(0, ref hasInterlayer, ref zInterlayer);
            bbox.Extend(layer0.BoundingBox(Analysis.PackProperties));
            BoxLayer layerN = GetBoxLayer(LayerCount - 1, ref hasInterlayer, ref zInterlayer);
            bbox.Extend(layerN.BoundingBox(Analysis.PackProperties));

            return bbox;
        }
        #endregion

        #region IComparable
        public int CompareTo(object obj)
        {
            // cast
            PackPalletSolution sol = obj as PackPalletSolution;
            if (null == sol) return 0;
            // comparison
            if (this.PackCount > sol.PackCount) return -1;
            else if (this.PackCount < sol.PackCount) return 1;
            else return 0;
        }
        #endregion

        #region Public properties
        public PackPalletAnalysis Analysis {   get { return _parentAnalysis; }   set { _parentAnalysis = value; } }
        public int LayerCount { get { return _listLayers.Count; } }
        public int PackPerLayer { get { return _layer.Count; } }
        public int PackCount { get { return PackPerLayer * LayerCount; } }
        public int ConsumerSalesUnit { get { return PackCount * _parentAnalysis.PackProperties.Arrangement.Number; } }
        public double LayerWeight { get { return PackPerLayer * _parentAnalysis.PackProperties.Weight; } }
        public double PalletWeight { get { return LayerWeight * LayerCount + _parentAnalysis.PalletProperties.Weight; } }
        public double PalletNetWeight { get { return PackCount * _parentAnalysis.PackProperties.NetWeight.Value; } }
        public double PalletHeight
        {
            get
            {
                double height = _parentAnalysis.PalletProperties.Height;
                foreach (LayerDescriptor desc in _listLayers)
                { 
                    if (desc.HasInterlayer && (null != _parentAnalysis.InterlayerProperties))
                        height += _parentAnalysis.InterlayerProperties.Thickness;
                    height += _parentAnalysis.PackProperties.Height;
                }
                return height;
            }
        }
        public double MaximumSpace
        {
            get { return _layer.MaximumSpace; }
        }
        #endregion
    }
}