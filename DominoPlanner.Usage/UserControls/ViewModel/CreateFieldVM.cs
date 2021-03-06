﻿using DominoPlanner.Core;
using DominoPlanner.Usage.HelperClass;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DominoPlanner.Usage.UserControls.ViewModel
{
    class CreateFieldVM : DominoProviderVM
    {
        #region CTOR
        public CreateFieldVM(FieldParameters dominoProvider, bool? AllowRegenerate) : base(dominoProvider, AllowRegenerate)
        {
            // Regeneration kurz sperren, dann wieder auf Ursprungswert setzen
            AllowRegeneration = false;
            field_templates = new List<StandardSize>();
            field_templates.Add(new StandardSize("8mm", new Sizes(8, 8, 24, 8)));
            field_templates.Add(new StandardSize("Tortoise", new Sizes(0, 48, 24, 0)));
            field_templates.Add(new StandardSize("User Size", new Sizes(10, 10, 10, 10)));
            Click_Binding = new RelayCommand((x) => BindSize = !BindSize);  
            ReloadSizes();
            AllowRegeneration = AllowRegenerate;
            Refresh();
            if (fieldParameters.Counts != null) RefreshColorAmount();
            UnsavedChanges = false;
            TargetSizeAffectedProperties = new string[] {"Length", "Height" };
        }
        #endregion

        #region fields
        Progress<String> progress = new Progress<string>(pr => Console.WriteLine(pr));
        FieldParameters fieldParameters
        {
            get { return CurrentProject as FieldParameters; }
            set
            {
                if (CurrentProject != value)
                {
                    CurrentProject = value;
                }
            }
        }
        #endregion

        #region prope
        public override TabItemType tabType
        {
            get
            {
                return TabItemType.CreateField;
            }
        }
        private FieldParameters FieldParameters
        {
            get => CurrentProject as FieldParameters;
        }
        private bool _EditState = true;
        public bool EditState
        {
            get { return _EditState; }
            set
            {
                if (_EditState != value)
                {
                    _EditState = value;
                    RaisePropertyChanged();
                }
            }
        }
        public int Length
        {
            get { return FieldParameters.Length; }
            set
            {
                if (FieldParameters.Length != value)
                {
                    PropertyValueChanged(this, value, PostAction: () => {
                        if (BindSize)
                        {
                            double fieldWidth = Length * (fieldParameters.HorizontalDistance + fieldParameters.HorizontalSize);
                            double stoneHeightWidhSpace = fieldParameters.VerticalDistance + fieldParameters.VerticalSize;
                            fieldParameters.Height = (int)(fieldWidth / (double)fieldParameters.PrimaryImageTreatment.Width * fieldParameters.PrimaryImageTreatment.Height / stoneHeightWidhSpace);
                        }
                        Refresh();
                    }, PostUndoAction: () => Refresh(), ChangesSize: true );
                    FieldParameters.Length = value;
                    
                    RaisePropertyChanged();
                }
            }
        }

        public int Height
        {
            get { return FieldParameters.Height; }
            set
            {
                if (FieldParameters.Height != value)
                {
                    PropertyValueChanged(this, value, PostAction: () => {
                        if (BindSize)
                        {
                            double fieldHeight = Height * (fieldParameters.VerticalDistance + fieldParameters.VerticalSize);
                            double stoneWidthWidthSpace = fieldParameters.HorizontalDistance + fieldParameters.HorizontalSize;
                            fieldParameters.Length = (int)(fieldHeight / (double)fieldParameters.PrimaryImageTreatment.Height * fieldParameters.PrimaryImageTreatment.Width / stoneWidthWidthSpace);
                        }
                        Refresh();
                    }, PostUndoAction: () => Refresh(), ChangesSize: true);
                    fieldParameters.Height = value;
                    
                    RaisePropertyChanged();
                }
            }
        }
        public int HorizontalDistance
        {
            get { return Horizontal ? FieldParameters.HorizontalDistance : FieldParameters.VerticalDistance; }
            set
            {
                if ((Horizontal ? FieldParameters.HorizontalDistance : FieldParameters.VerticalDistance) != value)
                {
                    PropertyValueChanged(this, value);
                    if (Horizontal)
                        FieldParameters.HorizontalDistance = value;
                    else FieldParameters.VerticalDistance = value;
                    RaisePropertyChanged();
                }
            }
        }
        public int HorizontalSize
        {
            get { return Horizontal ? FieldParameters.HorizontalSize : FieldParameters.VerticalSize; }
            set
            {
                if ((Horizontal ? FieldParameters.HorizontalSize : FieldParameters.VerticalSize) != value)
                {
                    PropertyValueChanged(this, value);
                    if (Horizontal)
                        FieldParameters.HorizontalSize = value;
                    else FieldParameters.VerticalSize = value;
                    RaisePropertyChanged();
                }
            }
        }
        public int VerticalSize
        {
            get { return !Horizontal ? FieldParameters.HorizontalSize : FieldParameters.VerticalSize; }
            set
            {
                if ((!Horizontal ? FieldParameters.HorizontalSize : FieldParameters.VerticalSize) != value)
                {
                    PropertyValueChanged(this, value);
                    if (!Horizontal)
                        FieldParameters.HorizontalSize = value;
                    else FieldParameters.VerticalSize = value;
                    RaisePropertyChanged();
                }
            }
        }
        public int VerticalDistance
        {
            get { return !Horizontal ? FieldParameters.HorizontalDistance : FieldParameters.VerticalDistance; }
            set
            {
                if ((!Horizontal ? FieldParameters.HorizontalDistance : FieldParameters.VerticalDistance) != value)
                {
                    PropertyValueChanged(this, value);
                    if (!Horizontal)
                        FieldParameters.HorizontalDistance = value;
                    else FieldParameters.VerticalDistance = value;
                    RaisePropertyChanged();
                }
            }
        }

        private List<StandardSize> _field_templates;
        public List<StandardSize> field_templates
        {
            get { return _field_templates; }
            set
            {
                if (_field_templates != value)
                {
                    _field_templates = value;
                    RaisePropertyChanged();
                }
            }
        }


        private bool _CanChange;
        public bool CanChange
        {
            get { return _CanChange; }
            set
            {
                if (_CanChange != value)
                {
                    _CanChange = value;
                    RaisePropertyChanged();
                }
            }
        }

        private StandardSize _SelectedItem;
        public StandardSize SelectedItem
        {
            get { return _SelectedItem; }
            set
            {
                if (_SelectedItem != value)
                {
                    var tempSelItem = _SelectedItem;
                    if (_SelectedItem?.Name=="User Size")
                    {
                        _SelectedItem.Sizes.a = HorizontalDistance;
                        _SelectedItem.Sizes.b = HorizontalSize;
                        _SelectedItem.Sizes.c = VerticalSize;
                        _SelectedItem.Sizes.d = VerticalDistance;
                    }
                    
                    if (_SelectedItem != null)
                    {
                        PropertyValueChanged(this, value, PostAction: () => { UpdateSizes(); RefreshTargetSize(); Refresh(); },
                            PostUndoAction: () => { UpdateSizes(); Refresh(); }, ChangesSize: true);
                    }
                    _SelectedItem = value;
                    if (SelectedItem.Name.Equals("User Size"))
                        CanChange = true;
                    else
                        CanChange = false;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _BindSize = true;
        public bool BindSize
        {
            get { return _BindSize; }
            set
            {
                if (_BindSize != value)
                {
                    PropertyValueChanged(this, value);
                    _BindSize = value;
                    RaisePropertyChanged();
                }
            }
        }
        public bool Horizontal
        {
            get { return CurrentProject.FieldPlanDirection == Core.Orientation.Horizontal; }
            set
            {
                if ((CurrentProject.FieldPlanDirection == Core.Orientation.Horizontal) != value)
                {
                    PropertyValueChanged(this, value, ChangesSize: true, PostAction: () => { UpdateSizes(); RefreshTargetSize(); Refresh(); },
                    PostUndoAction: () => { UpdateSizes(); Refresh(); });
                    CurrentProject.FieldPlanDirection = value ? Core.Orientation.Horizontal : Core.Orientation.Vertical;
                    RaisePropertyChanged();
                }
            }
        }
        #endregion

        #region Methods

        public override bool Save()
        {
            try
            {
                fieldParameters.Save();
                UnsavedChanges = false;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        protected override void PostCalculationUpdate()
        {
            TabPropertyChanged("Length", ProducesUnsavedChanges: false);
            TabPropertyChanged("Height", ProducesUnsavedChanges: false);
            TabPropertyChanged("DominoCount", ProducesUnsavedChanges: false);
        }
        private void UpdateSizes()
        {
            HorizontalDistance = SelectedItem.Sizes.a;
            HorizontalSize = SelectedItem.Sizes.b;
            VerticalSize = SelectedItem.Sizes.c;
            VerticalDistance = SelectedItem.Sizes.d;

        }

        private void ReloadSizes()
        {
            Sizes currentSize = new Sizes(HorizontalDistance, HorizontalSize, VerticalSize, VerticalDistance);
            bool found = false;
            foreach (StandardSize sSize in field_templates)
            {
                if (sSize.Sizes.a == currentSize.a && sSize.Sizes.b == currentSize.b && sSize.Sizes.c == currentSize.c && sSize.Sizes.d == currentSize.d)
                {
                    SelectedItem = sSize;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                field_templates.Last<StandardSize>().Sizes = currentSize;
                SelectedItem = field_templates.Last<StandardSize>();
            }
            UnsavedChanges = true;
        }
        #endregion

        private ICommand _Click_Binding;
        public ICommand Click_Binding { get { return _Click_Binding; } set { if (value != _Click_Binding) { _Click_Binding = value; } } }
        
        
    }

    class StandardSize : ModelBase
    {
        public StandardSize(string name, Sizes sizes)
        {
            this.Name = name;
            this.Sizes = sizes;
        }

        #region prop
        public string Name { get; set; }
        
        public Sizes Sizes { get; set; }
        #endregion

        public override string ToString()
        {
            return Name;
        }
    }
    class Sizes : ModelBase
    {
        public Sizes(int a, int b, int c, int d)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }

        #region prop
        public int a;
        public int b;
        public int c;
        public int d;
        #endregion
    }
}
