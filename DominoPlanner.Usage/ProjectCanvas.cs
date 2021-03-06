﻿using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Linq;
using System.IO;

namespace DominoPlanner.Usage
{
    public class ProjectCanvas : Canvas
    {
        public List<DominoInCanvas> Stones = new List<DominoInCanvas>();

        public Color UnselectedBorderColor { get; set; }

        public Color SelectedBorderColor { get; set; }
        public double BorderSize;

        public Image<Emgu.CV.Structure.Bgra, byte> OriginalImage;

        public bool above;
        private double opacity_value;

        public double OpacityValue
        {
            get { return opacity_value; }
            set { opacity_value = value; }
        }
        

        protected override void OnRender(DrawingContext dc)
        {
            BitmapSource bit = null;
            if (opacity_value != 0)
            {
                var reduced = OriginalImage.Clone();
                Core.ImageExtensions.OpacityReduction(reduced, opacity_value);
                bit = BitmapSourceConvert.ToBitmapSource(reduced);
            }
            base.OnRender(dc);


            if (!above && bit != null)
                dc.DrawImage(bit, new Rect(0, 0, Width, Height));
            foreach (DominoInCanvas dic in Stones)
            {
                Point point1 = dic.canvasPoints[0];
                Point point2 = dic.canvasPoints[1];
                Point point3 = dic.canvasPoints[2];
                Point point4 = dic.canvasPoints[3];

                StreamGeometry streamGeometry = new StreamGeometry();
                using (StreamGeometryContext geometryContext = streamGeometry.Open())
                {
                    geometryContext.BeginFigure(point1, true, true);
                    var points = new System.Windows.Media.PointCollection
                                             {
                                                 point2, point3, point4
                                             };
                    geometryContext.PolyLineTo(points, true, true);
                }

                Pen pen = new Pen();
                if (dic.isSelected)
                {
                    pen.Brush = new SolidColorBrush(SelectedBorderColor);
                    pen.Thickness = BorderSize;
                }
                else if (dic.PossibleToPaste)
                {
                    pen.Brush = Brushes.Plum;
                    pen.Thickness = BorderSize;
                }
                else
                {
                    pen.Brush = new SolidColorBrush(UnselectedBorderColor);
                    pen.Thickness = BorderSize / 2;
                }

                dc.DrawGeometry(new SolidColorBrush(dic.StoneColor), pen, streamGeometry);
            }
            if (above && bit != null)
                dc.DrawImage(bit, new Rect(0, 0, Width, Height));

        }
    }
    public static class BitmapSourceConvert
    {
        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        public static BitmapSource ToBitmapSource(IImage image)
        {
            using (System.Drawing.Bitmap source = image.Bitmap)
            {
                IntPtr ptr = source.GetHbitmap();

                BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    ptr,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(ptr);
                return bs;
            }
        }
    }
}
