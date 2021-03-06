﻿using DominoPlanner.Usage.UserControls.ViewModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DominoPlanner.Usage.UserControls.View
{
    /// <summary>
    /// Interaction logic for EditProject.xaml
    /// </summary>
    public partial class EditProject : UserControl
    {
        public EditProject()
        {
            InitializeComponent();
            this.KeyDown += LiveBuildHelperV_KeyDown;
            DataContextChanged += EditProject_DataContextChanged;
        }

        private void EditProject_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(DataContext != null && DataContext is EditProjectVM editProjectVM)
            {
                editProjectVM.RefreshSize += EditProjectVM_RefreshSize;
                editProjectVM.DisplaySettingsTool.SizeChanged(ScrollViewer.RenderSize.Width, ScrollViewer.RenderSize.Height);
            }
        }

        private void EditProjectVM_RefreshSize(object sender, EventArgs e)
        {
            (sender as EditProjectVM).DisplaySettingsTool.SizeChanged(ScrollViewer.RenderSize.Width, ScrollViewer.RenderSize.Height);
        }

        private void LiveBuildHelperV_KeyDown(object sender, KeyEventArgs e)
        {
            ((EditProjectVM)DataContext).PressedKey(e.Key);
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if(DataContext != null && DataContext.GetType() == typeof(ViewModel.EditProjectVM))
            {
               ((ViewModel.EditProjectVM)DataContext).DisplaySettingsTool.SizeChanged(e.NewSize.Width, e.NewSize.Height);
            }
        }

        private void Grid_SizeChanged_1(object sender, SizeChangedEventArgs e)
        {
            ((Grid)sender).ColumnDefinitions[1].Width = new GridLength(e.NewSize.Width - 350);
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CanGrid.ColumnDefinitions[1].Width = new GridLength(10);
        }
    }

    public class ImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is System.Drawing.Bitmap bitmap)
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                          bitmap.GetHbitmap(),
                          IntPtr.Zero,
                          Int32Rect.Empty,
                          BitmapSizeOptions.FromEmptyOptions());
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
