﻿using DominoPlanner.Core;
using DominoPlanner.Usage.HelperClass;
using DominoPlanner.Usage.Serializer;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace DominoPlanner.Usage
{
    public class NewProjectVM : ModelBase
    {
        #region CTOR
        public NewProjectVM()
        {
            SelectedPath = Properties.Settings.Default.StandardProjectPath;
            sPath = Properties.Settings.Default.StandardColorArray;
            ProjectName = "New Project";
            rbStandard = true;
            rbCustom = false; //damit die Labels passen
            SelectFolder = new RelayCommand(o => { SelectProjectFolder(); });
            SelectColor = new RelayCommand(o => { SelectColorArray(); });
            StartClick = new RelayCommand(o => { CreateNewProject(); });
        }
        #endregion

        #region Methods
        private void SelectProjectFolder()
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SelectedPath = fbd.SelectedPath;
            }
        }

        private void CreateNewProject()
        {
            try
            {
                if (Directory.Exists(Path.Combine(SelectedPath, ProjectName)))
                {
                    Errorhandler.RaiseMessage("This folder already exists. Please choose another project name.", "Existing Folder", Errorhandler.MessageType.Error);
                    return;
                }

                string projectpath = Path.Combine(SelectedPath, ProjectName);
                Directory.CreateDirectory(projectpath);
                Directory.CreateDirectory(Path.Combine(projectpath, "Source Image"));
                Directory.CreateDirectory(Path.Combine(projectpath, "Planner Files"));

                DominoAssembly main = new DominoAssembly();
                main.Save(Path.Combine(projectpath, ProjectName + Properties.Resources.ProjectExtension));

                if (File.Exists(sPath))
                {
                    string colorPath = Path.Combine(SelectedPath, ProjectName, "Planner Files", $"colors{Properties.Resources.ColorExtension}");
                    File.Copy(sPath, colorPath);
                    main.colorPath = Path.Combine("Planner Files", "colors" + Properties.Resources.ColorExtension);
                }

                main.Save();

                Errorhandler.RaiseMessage($"The project {ProjectName}{Properties.Resources.ProjectExtension} has been created", "Created", Errorhandler.MessageType.Info);
                Close = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Project creation failed: {0}", e.ToString());
            }
        }

        private void SelectColorArray()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            try
            {
                openFileDialog.InitialDirectory = sPath;
                openFileDialog.Filter = $"domino color files (*{Properties.Resources.ColorExtension})|*{Properties.Resources.ColorExtension}|All files (*.*)|*.*";
            }
            catch (Exception) { }

            if (openFileDialog.ShowDialog() == true)
            {
                sPath = openFileDialog.FileName;
            }
        }
        #endregion

        #region Prop
        private bool _Close;
        public bool Close
        {
            get { return _Close; }
            set
            {
                if (_Close != value)
                {
                    _Close = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _sPath;
        public string sPath
        {
            get { return _sPath; }
            set
            {
                if (_sPath != value)
                {
                    _sPath = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _SelectedPath;
        public string SelectedPath
        {
            get { return _SelectedPath; }
            set
            {
                if (_SelectedPath != value)
                {
                    _SelectedPath = value;
                    RaisePropertyChanged();
                }
            }
        }
        private string _ProjectName;
        public string ProjectName
        {
            get { return _ProjectName; }
            set
            {
                if (_ProjectName != value)
                {
                    _ProjectName = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _rbStandard;
        public bool rbStandard
        {
            get { return _rbStandard; }
            set
            {
                if (_rbStandard != value)
                {
                    _rbStandard = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _rbCustom;
        public bool rbCustom
        {
            get { return _rbCustom; }
            set
            {
                if (_rbCustom != value)
                {
                    _rbCustom = value;
                    RaisePropertyChanged();
                }
                if (value)
                    ColorVisibility = Visibility.Visible;
                else
                    ColorVisibility = Visibility.Hidden;
            }
        }
        private Visibility _ColorVisibility;
        public Visibility ColorVisibility
        {
            get { return _ColorVisibility; }
            set
            {
                if (_ColorVisibility != value)
                {
                    _ColorVisibility = value;
                    RaisePropertyChanged();
                }
            }
        }

        #endregion

        #region Command
        private ICommand _SelectFolder;
        public ICommand SelectFolder { get { return _SelectFolder; } set { if (value != _SelectFolder) { _SelectFolder = value; } } }

        private ICommand _SelectColor;
        public ICommand SelectColor { get { return _SelectColor; } set { if (value != _SelectColor) { _SelectColor = value; } } }

        private ICommand _StartClick;
        public ICommand StartClick { get { return _StartClick; } set { if (value != _StartClick) { _StartClick = value; } } }
        #endregion
    }
}
