﻿using DominoPlanner.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DominoPlanner.Usage
{
    class LiveBuildHelperVM : ModelBase
    {
        private ICommand _OpenPopup;
        public ICommand OpenPopup
        {
            get
            {
                return _OpenPopup;
            }
            set { if (value != _OpenPopup) { _OpenPopup = value; } }
        }

        #region CTOR
        public LiveBuildHelperVM(IDominoProvider pFParameters, int pBlockSize, Core.Orientation orientation, bool MirrorX, bool MirrorY)
        {
            blockSize = pBlockSize;
            fParameters = pFParameters;
            intField = fParameters.GetBaseField(orientation, MirrorX, MirrorY);
            NextN = 500;
            CountRow = intField.GetLength(1);
            stonesPerLine = intField.GetLength(0);
            CountBlock = Convert.ToInt32(Math.Ceiling(((double)stonesPerLine / blockSize)));
            SizeChanged = new RelayCommand(o => { RefreshCanvas(); });
            MouseDown = new RelayCommand(o => { currentBlock.Focus(); });
            ColumnConfig = new ColumnConfig();

            var columns = new ObservableCollection<Column>();
            columns.Add(new Column() { DataField = "DominoColor.mediaColor", Header = "" });
            columns.Add(new Column() { DataField = "DominoColor.name", Header = "Name" });
            columns.Add(new Column() { DataField = "ProjectCount[0]", Header = "Total used" });
            columns.Add(new Column() { DataField = "ProjectCount[1]", Header = "Remaining" });
            columns.Add(new Column() { DataField = "ProjectCount[2]", Header = "Next " + NextN });

            ColumnConfig.Columns = columns;

            OpenPopup = new RelayCommand(x => { FillColorList(); PopupOpen = true; });
        }
        #endregion

        #region fields
        private int blockSize;
        private IDominoProvider fParameters;
        private int stonesPerLine;
        private int[,] intField;
        private int space = 2;
        private int stoneWidth = 0;
        #endregion

        #region prope
        private string _BatState;
        public string BatState
        {
            get { return _BatState; }
            set
            {
                if (_BatState != value)
                {
                    _BatState = value;
                    RaisePropertyChanged();
                }
            }
        }


        private Canvas _currentBlock = new Canvas();
        public Canvas currentBlock
        {
            get { return _currentBlock; }
            set
            {
                if (_currentBlock != value)
                {
                    _currentBlock = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int _SelectedRow = 1;
        public int SelectedRow
        {
            get { return _SelectedRow; }
            set
            {
                if (_SelectedRow != value)
                {
                    _SelectedRow = value;
                    RaisePropertyChanged();
                    RefreshCanvas();
                    currentBlock.Focus();
                }
            }
        }

        private int _CountRow;
        public int CountRow
        {
            get { return _CountRow; }
            set
            {
                if (_CountRow != value)
                {
                    _CountRow = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int _SelectedBlock = 1;
        public int SelectedBlock
        {
            get { return _SelectedBlock; }
            set
            {
                if (_SelectedBlock != value)
                {
                    _SelectedBlock = value;
                    RaisePropertyChanged();
                    RefreshCanvas();
                    currentBlock.Focus();
                }
            }
        }

        private bool _ShowHistory;
        public bool ShowHistory
        {
            get { return _ShowHistory; }
            set
            {
                if(_ShowHistory != value)
                {
                    _ShowHistory = value;
                    RaisePropertyChanged();
                    RefreshCanvas();
                }
            }
        }

        private int _CountBlock;
        public int CountBlock
        {
            get { return _CountBlock; }
            set
            {
                if (_CountBlock != value)
                {
                    _CountBlock = value;
                    RaisePropertyChanged();
                }
            }
        }
        private bool _popupOpen;
        public bool PopupOpen
        {
            get
            {
                return _popupOpen;
            }
            set
            {
                _popupOpen = value; RaisePropertyChanged();
            }
        }

        private ColumnConfig _columnConfig;

        public ColumnConfig ColumnConfig
        {
            get { return _columnConfig; }
            set { _columnConfig = value; RaisePropertyChanged(); }
        }
        private ObservableCollection<ColorListEntry> _colors;

        public ObservableCollection<ColorListEntry> Colors
        {
            get { return _colors; }
            set { _colors = value; RaisePropertyChanged(); }
        }
        private int _nextN;

        public int NextN
        {
            get { return _nextN; }
            set { _nextN = value; }
        }


        #endregion

        #region methods
        private void RefreshCanvas()
        {
            BatState = "Battery: " + System.Windows.Forms.SystemInformation.PowerStatus.BatteryLifePercent * 100 + " % "
            + (((System.Windows.Forms.SystemInformation.PowerStatus.BatteryChargeStatus & System.Windows.Forms.BatteryChargeStatus.Charging) == System.Windows.Forms.BatteryChargeStatus.Charging) ? ", charging" : "");

            currentBlock.Children.RemoveRange(0, currentBlock.Children.Count);
            
            stoneWidth = (((int)currentBlock.ActualWidth) - (2 * 2 * space) - ((blockSize - 1) * space)) / blockSize;
            int stoneHeight = 250;
            int marginHeight = (((int)currentBlock.ActualHeight) / 2);

            int firstBlockStone = blockSize * (SelectedBlock - 1);

            int lastColor = intField[(SelectedBlock - 1) * blockSize, SelectedRow - 1];
            string lastColorName = lastColor > 0 ? fParameters.colors[lastColor].name : "";
            int lastLeftMargin = 2 * space;
            int countColor = 0;
            
            for (int i = 0; i < blockSize; i++)
            {
                if (firstBlockStone + i < stonesPerLine)
                {
                    if(ShowHistory && SelectedRow - 2 >= 0)
                    {
                        int oldStoneIndex = intField[firstBlockStone + i, SelectedRow - 2];
                        currentBlock.Children.Add(new DominoInCanvas(stoneWidth, stoneHeight / 4, ((i + 2) * space) + (i * stoneWidth), 10, fParameters.colors[oldStoneIndex].mediaColor));
                    }

                    int stoneindex = intField[firstBlockStone + i, SelectedRow - 1];
                    if (stoneindex < 0) continue;
                    currentBlock.Children.Add(new DominoInCanvas(stoneWidth, stoneHeight, ((i + 2) * space) + (i * stoneWidth), marginHeight - stoneHeight, fParameters.colors[stoneindex].mediaColor));
                    
                    if (lastColor != intField[firstBlockStone + i, SelectedRow - 1])
                    {
                        _DrawText(lastColorName, countColor, lastLeftMargin, marginHeight);
                        lastColor = intField[firstBlockStone + i, SelectedRow - 1];
                        lastColorName = lastColor > 0 ? fParameters.colors[lastColor].name : "";
                        lastLeftMargin = ((i + 1) * space) + (i * stoneWidth);
                        countColor = 1;
                    }
                    else
                    {
                        countColor++;
                    }
                }
            }
            _DrawText(lastColorName, countColor, lastLeftMargin, marginHeight);
        }

        private void _DrawText(string colorName, int colorAmount, int margin_left, int margin_top)
        {
            int stonesWidth = (((colorAmount + 1) * space) + (colorAmount * stoneWidth));

            TextBlock tb = new TextBlock();
            tb.FontSize = 16;
            tb.Text = colorName + Environment.NewLine + colorAmount;
            tb.FontWeight = System.Windows.FontWeights.Bold;
            tb.TextAlignment = System.Windows.TextAlignment.Center;
            tb.Margin = new System.Windows.Thickness(margin_left, margin_top + 20, 0, 0);
            tb.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            tb.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            tb.Width = ((colorAmount + 1) * space) + (colorAmount * stoneWidth);

            System.Drawing.Size textSize = System.Windows.Forms.TextRenderer.MeasureText(colorName, new System.Drawing.Font(tb.FontFamily.FamilyNames.ToString(), (float)tb.FontSize));

            if (stonesWidth < textSize.Width)
            {
                if (stoneWidth < tb.FontSize * 1.4)
                {
                    tb.FontSize = stoneWidth / 1.4;
                }
                tb.Text = colorName + " " + colorAmount;
                textSize = System.Windows.Forms.TextRenderer.MeasureText(tb.Text, new System.Drawing.Font(tb.FontFamily.FamilyNames.ToString(), (float)tb.FontSize));
                tb.TextAlignment = System.Windows.TextAlignment.Left;
                tb.Width = textSize.Width;
                tb.TextWrapping = System.Windows.TextWrapping.NoWrap;
                tb.Margin = new System.Windows.Thickness(margin_left + (stonesWidth / 2) + (tb.FontSize * 1.4 / 2), margin_top + 20, 0, 0);
                tb.RenderTransform = new RotateTransform() { Angle = 90 };
            }

            currentBlock.Children.Add(tb);
        }

        private void RefreshRemainingColors()
        {

        }
        private void FillColorList()
        {
            Colors = new ObservableCollection<ColorListEntry>();

            int counter = 0;

            if (fParameters.colors.RepresentionForCalculation.OfType<EmptyDomino>().Count() == 1)
            {
                Colors.Add(new ColorListEntry() { DominoColor = fParameters.colors.RepresentionForCalculation.OfType<EmptyDomino>().First(), SortIndex = -1 });
            }
            foreach (DominoColor domino in fParameters.colors.RepresentionForCalculation.OfType<DominoColor>())
            {
                Colors.Add(new ColorListEntry() { DominoColor = domino, SortIndex = fParameters.colors.Anzeigeindizes[counter] });
                counter++;
            }

            RefreshColorAmount();
        }
        private void RefreshColorAmount()
        {
            int firstBlockStone = blockSize * (SelectedBlock - 1);
            int firstRow = SelectedRow - 1;
            int[] RemainingCount = new int[Colors.Count];
            int[] NextNCount = new int[Colors.Count];
            int counter = 0;
            for (int i = firstRow; i < intField.GetLength(1); i++)
            {
                int startj = (i == firstRow) ? firstBlockStone : 0; 
                for (int j = startj; j < intField.GetLength(0); j++)
                {
                    if (counter < NextN)
                        NextNCount[intField[j, i]]++;
                    RemainingCount[intField[j, i]]++;
                    counter++;
                }
            }
            for (int i = 0; i < Colors.Count; i++)
            {
                if (fParameters.Counts.Length > i)
                {
                    Colors[i].ProjectCount = new ObservableCollection<int>();
                    Colors[i].ProjectCount.Add(fParameters.Counts[i]);

                    Colors[i].ProjectCount.Add(RemainingCount[i]);
                    Colors[i].ProjectCount.Add(NextNCount[i]);
                    
                }
                else
                {
                    Colors[i].ProjectCount.Add(fParameters.Counts[0]);
                }
            }
            Colors = new ObservableCollection<ColorListEntry>(Colors.Where(x => x.ProjectCount[0] > 0).OrderBy(x => x.SortIndex));
        }

        internal void PressedKey(Key pressedKey)
        {
            switch (pressedKey)
            {
                case Key.Space:
                    if (SelectedBlock == CountBlock)
                    {
                        if (SelectedRow < CountRow)
                        {
                            SelectedBlock = 1;
                            SelectedRow++;
                        }
                    }
                    else
                    {
                        SelectedBlock++;
                    }
                    break;
                case Key.Left:
                    if (SelectedBlock == 1)
                    {
                        if (SelectedRow > 1)
                        {
                            SelectedRow--;
                            SelectedBlock = CountBlock;
                        }
                    }
                    else
                    {
                        SelectedBlock--;
                    }
                    break;
                case Key.Right:
                    if (SelectedBlock == CountBlock)
                    {
                        if (SelectedRow < CountRow)
                        {
                            SelectedBlock = 1;
                            SelectedRow++;
                        }
                    }
                    else
                        SelectedBlock++;
                    break;
                case Key.Up:
                    if (SelectedRow > 1)
                        SelectedRow--;
                    break;
                case Key.Down:
                    if (SelectedRow < CountRow)
                        SelectedRow++;
                    break;
                default:
                    break;
            }
        }
        #endregion



        private ICommand _MouseDown;
        public ICommand MouseDown { get { return _MouseDown; } set { if (value != _MouseDown) { _MouseDown = value; } } }


        private ICommand _SizeChanged;
        public ICommand SizeChanged { get { return _SizeChanged; } set { if (value != _SizeChanged) { _SizeChanged = value; } } }

    }
}
