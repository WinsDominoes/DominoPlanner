﻿using DominoPlanner.Core;
using DominoPlanner.Usage.HelperClass;
using DominoPlanner.Usage.Serializer;
using DominoPlanner.Usage.UserControls.ViewModel;
using Emgu.CV;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DominoPlanner.Usage
{
    public class NewObjectVM : ModelBase
    {
        #region CTOR
        public NewObjectVM(string folderpath, DominoAssembly parentProject)
        {

            this.parentProject = parentProject;
            FillObjects();
            ProjectPath = folderpath;
            CreateIt = new RelayCommand(o => { mCreateIt(); });
            SelectedType = 0;
        }
        #endregion

        #region fields
        private DominoAssembly parentProject;
        #endregion
        
        #region prop
        public IDominoWrapper ResultNode { get; private set; }
        public string ProjectPath { get; private set; }
        public string ObjectPath { get { return string.Format("{0}\\Planner Files\\{1}{2}", ProjectPath, _filename, _endung); } }

        private ObservableCollection<NewObjectEntry> _ViewModels;
        public ObservableCollection<NewObjectEntry> ViewModels
        {
            get => _ViewModels;
            set
            {
                if (_ViewModels != value)
                {
                    _ViewModels = value;
                    RaisePropertyChanged();
                }
            }
        }
        private object _CurrentViewModel;
        public object CurrentViewModel
        {
            get => _CurrentViewModel;
            set
            {
                if (_CurrentViewModel != value)
                {
                    _CurrentViewModel = value;
                    RaisePropertyChanged();
                }
            }
        }
        private ImageInformation _CurrentImageInformation;
        public ImageInformation CurrentImageInformation
        {
            get => _CurrentImageInformation;
            set
            {
                if (_CurrentImageInformation != value)
                {
                    _CurrentImageInformation = value;
                    RaisePropertyChanged();
                }
            }
        }
        private int _selectedType = -1;
        public int SelectedType
        {
            get { return _selectedType; }
            set
            {
                if (_selectedType != value)
                {
                    _selectedType = value;
                    Extension = ViewModels[value].Extension;
                    CurrentViewModel = ViewModels[value].ViewModel;
                    CurrentImageInformation = ViewModels[value].ImageInformation;
                    RaisePropertyChanged();
                }
            }
        }
        private string _filename;
        public string filename
        {
            get { return _filename; }
            set
            {
                if (_filename != value)
                {
                    _filename = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _endung = Properties.Resources.ObjectExtension;
        public string Extension
        {
            get { return _endung; }
            set
            {
                if (_endung != value)
                {
                    _endung = value;
                    RaisePropertyChanged();
                }
            }
        }
        private bool _Close;
        public bool Close
        {
            get { return _Close; }
            set
            {
                if (_Close != value)
                {
                    _Close = value;
                    CloseChanged(this, EventArgs.Empty);
                    RaisePropertyChanged();
                }
            }
        }

        public event EventHandler CloseChanged;

        #endregion

        #region command

        private ICommand _CreateIt;
        public ICommand CreateIt { get { return _CreateIt; } set { if (value != _CreateIt) { _CreateIt = value; } } }

        
        #endregion
        #region Methods

        private void FillObjects()
        {
            ViewModels = new ObservableCollection<NewObjectEntry>();
            CurrentImageInformation = new SingleImageInformation();
            string AbsoluteColorPath = Workspace.AbsolutePathFromReferenceLoseUpdate(parentProject.colorPath, parentProject);
            ViewModels.Add(new DominoProviderObjectEntry()
            {
                Name = "Field",
                Description = "Add a field based on an image",
                Icon = "/Icons/insert_table.ico",
                ImageInformation = CurrentImageInformation,
                Provider = new CreateFieldVM(
                    new FieldParameters(50, 50, System.Windows.Media.Colors.Transparent, AbsoluteColorPath,
                    8, 8, 24, 8, 5000, Emgu.CV.CvEnum.Inter.Lanczos4, new CieDe2000Comparison(), new Dithering(), new NoColorRestriction()), null)
                { BindSize = true }
            });
            ViewModels.Add(new DominoProviderObjectEntry()
            {
                Name = "Structure",
                Description = "Add a structure (e.g. Wall) based on an image",
                Icon = "/icons/insert_table.ico",
                ImageInformation = CurrentImageInformation,
                Provider = new CreateRectangularStructureVM(
                    new StructureParameters(5, 5, System.Windows.Media.Colors.Transparent, CreateRectangularStructureVM.StuctureTypes().Item1[0],
                    2000, AbsoluteColorPath, new CieDe2000Comparison(), new Dithering(), AverageMode.Corner, new NoColorRestriction()), null)
            });
            ViewModels.Add(new DominoProviderObjectEntry()
            {
                Name = "Circle",
                Description = "Add a circle bomb based on an image",
                Icon = "/Icons/insert_table.ico",
                ImageInformation = CurrentImageInformation,
                Provider = new CreateCircleVM(
                    new CircleParameters(5, 5, System.Windows.Media.Colors.Transparent, 10,
                    AbsoluteColorPath, new CieDe2000Comparison(), new Dithering(), AverageMode.Corner, new NoColorRestriction()), null)
            });
            ViewModels.Add(new DominoProviderObjectEntry()
            {
                Name = "Spiral",
                Description = "Add a spiral based on an image",
                Icon = "/Icons/insert_table.ico",
                ImageInformation = CurrentImageInformation,
                Provider = new CreateSpiralVM(
                    new SpiralParameters(5, 5, System.Windows.Media.Colors.Transparent, 10,
                    AbsoluteColorPath, new CieDe2000Comparison(), new Dithering(), AverageMode.Corner, new NoColorRestriction()), false)
            });
            ViewModels.Add(new NewAssemblyEntry(parentProject)
            {
                Name = "Subproject",
                Description = "Add a subproject with the same color list",
                Icon = "/Icons/folder_txt.ico",
                ImageInformation = new NoImageInformation()
            });
        }
        private void mCreateIt()
        {
            ResultNode = ViewModels[SelectedType].CreateIt(parentProject, filename, ProjectPath);
            if (ResultNode != null)
            {
                Close = true;
            }
        }
        #endregion
    }
    public class ImageInformation : ModelBase
    {
        public virtual void UpdateProvider(DominoProviderVM provider) { }
        public virtual bool FinalizeProvider(IDominoProvider provider, string filename)
        { return true; }
        public virtual void RevertChangesToFileSystem() { }
    }
    public class NoImageInformation : ImageInformation
    {

    }
    public class SingleImageInformation : ImageInformation
    {
        public SingleImageInformation()
        {
            LoadNewImage = new RelayCommand(x => SetNewImage((DominoProviderVM)x));
        }
        private string DefaultPictureName { get; set; } = "/Icons/add.ico";
        private string internPictureName;

        public string InternPictureName
        {
            get { return internPictureName; }
            set
            {
                internPictureName = value;
                RaisePropertyChanged();
            }
        }
        private string finalImagePath;
        
        #region Command
        private ICommand _LoadNewImage;
        public ICommand LoadNewImage { get { return _LoadNewImage; } set { if (value != _LoadNewImage) { _LoadNewImage = value; } } }
        #endregion

        #region Methods

        private void SetNewImage(DominoProviderVM provider)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.CheckPathExists = true;
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png)|*.jpg;*.jpeg;*.jpe;*.jfif;*.png|All Files|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                InternPictureName = openFileDialog.FileName;
            }
            try
            {
                var img = new Emgu.CV.Mat(InternPictureName, Emgu.CV.CvEnum.ImreadModes.Unchanged);
                img.Dispose();
                UpdateProvider(provider);
            }
            catch
            {
                Errorhandler.RaiseMessage("The image file is not readable, please select another file", "Invalid file", Errorhandler.MessageType.Error);
            }
            
        }
        public override void UpdateProvider(DominoProviderVM provider)
        {
            if (!string.IsNullOrEmpty(InternPictureName))
            {
                provider.CurrentProject.PrimaryImageTreatment = new NormalReadout(provider.CurrentProject, InternPictureName, AverageMode.Corner, true);
                if (provider.DominoCount > 0)
                {
                    provider.DominoCount = provider.DominoCount + 1; // if equal, setter would reject changes
                }
            }

        }
        public override bool FinalizeProvider(IDominoProvider provider, string savepath)
        {
            finalImagePath = InternPictureName;

            if (string.IsNullOrEmpty(finalImagePath) || string.IsNullOrWhiteSpace(finalImagePath))
            {
                Errorhandler.RaiseMessage("Please choose an image", "Missing Values", Errorhandler.MessageType.Error);
                InternPictureName = finalImagePath;
                return false;
            }

            finalImagePath = string.Format("{0}{1}", Path.GetFileNameWithoutExtension(savepath), Path.GetExtension(finalImagePath));
            int counter = 1;
            while (File.Exists(Path.Combine(Path.GetDirectoryName(savepath), @"..\Source Image", finalImagePath)))
            {
                finalImagePath = $"{Path.GetFileName(savepath)} ({counter}){Path.GetExtension(finalImagePath)}";
                counter++;
            }
            try
            {
                File.Copy(InternPictureName, Path.Combine(Path.GetDirectoryName(savepath), @"..\Source Image", finalImagePath));
            }
            catch (IOException)
            {
                Errorhandler.RaiseMessage("Copying the image into the project folder failed.\nPlease check the permissions to this file.", "", Errorhandler.MessageType.Warning);
                InternPictureName = finalImagePath;
                return false;
            }
            string relPicturePath = $@"..\Source Image\{finalImagePath}";
            if (provider is FieldParameters f)
            {
                provider.PrimaryImageTreatment = new FieldReadout(f, relPicturePath, Emgu.CV.CvEnum.Inter.Lanczos4);
            }
            else
            {
                provider.PrimaryImageTreatment = new NormalReadout(provider, relPicturePath, AverageMode.Corner, true);
            }
            return true;
        }
        public override void RevertChangesToFileSystem()
        {
            if (File.Exists(finalImagePath)) File.Delete(finalImagePath);
        }
        #endregion
    }

    public abstract class NewObjectEntry
    {
        public ImageInformation ImageInformation { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public abstract string Extension { get; }
        public abstract IDominoWrapper CreateIt(DominoAssembly parentProject, string filename, string ProjectPath);
        public abstract object ViewModel { get; }
        public abstract void UpdateImageInformation();
    }
    public class DominoProviderObjectEntry : NewObjectEntry
    {
        public override void UpdateImageInformation()
        {
            ImageInformation.UpdateProvider(Provider);
        }
        public override object ViewModel => Provider;
        public DominoProviderVM Provider { get; set; }

        public override string Extension => Properties.Resources.ObjectExtension;

        public bool Finalize(string filepath, DominoAssembly parentProject)
        {
            Provider.CurrentProject.Save(filepath);
            string colorlist = parentProject.colorPath;
            Provider.CurrentProject.ColorPath = $@"..\{colorlist}";
            return ImageInformation.FinalizeProvider(Provider.CurrentProject, filepath);
        }
        public override IDominoWrapper CreateIt(DominoAssembly parentProject, string filename, string ProjectPath)
        {
            try
            {
                string resultPath = Path.Combine(ProjectPath, Path.Combine("Planner Files", string.Format("{0}{1}", filename, Extension)));

                if (File.Exists(resultPath))
                {
                    Errorhandler.RaiseMessage("This name is already in use in this project.\n Please choose a different Name.", "Error!", Errorhandler.MessageType.Error);
                    return null;
                }
                if (string.IsNullOrEmpty(filename) || string.IsNullOrWhiteSpace(filename))
                {
                    Errorhandler.RaiseMessage("You forgot to choose a name.", "Missing Values", Errorhandler.MessageType.Error);
                    return null;
                }
                try
                {
                    bool finalizeResult = Finalize(resultPath, parentProject);

                    if (!finalizeResult)
                    {
                        throw new OperationCanceledException("Finalize failed");
                    }
                }
                catch (Exception ex)
                {
                    if (File.Exists(resultPath)) File.Delete(resultPath);
                    Workspace.CloseFile(Provider.CurrentProject);
                    ImageInformation.RevertChangesToFileSystem();
                    if (!(ex is OperationCanceledException))
                        Errorhandler.RaiseMessage("Project creation failed. Error mesage: \n" + ex.Message + "\n The created files have been deleted", "Failes creation", Errorhandler.MessageType.Error);
                    return null;
                }
                Provider.CurrentProject.Save();
                var ResultNode = (DocumentNode)IDominoWrapper.CreateNodeFromPath(parentProject, resultPath);
                parentProject.Save();
                return ResultNode;
            }
            catch (Exception es)
            {
                Errorhandler.RaiseMessage("Could not create a new Project!" + "\n" + es + "\n" + es.InnerException + "\n" + es.StackTrace, "Error!", Errorhandler.MessageType.Error);
                return null;
            }
        }
    }
    public class NewAssemblyEntry : NewObjectEntry
    {
        public override string Extension => Properties.Resources.ProjectExtension;
        public override object ViewModel => this;
        public string ColorPath { get; set; }
        DominoAssembly parentProject;
        public NewAssemblyEntry(DominoAssembly parentProject)
        {
            this.parentProject = parentProject;
            ColorPath = Workspace.AbsolutePathFromReferenceLoseUpdate(parentProject.colorPath, parentProject);
        }
        public override IDominoWrapper CreateIt(DominoAssembly parentProject, string filename, string ProjectPath)
        {
            string newProject = Path.Combine(ProjectPath, "Planner Files", filename);
            string PlannerFilesPath = Path.Combine(newProject, "Planner Files");
            string SourceImagesPath = Path.Combine(newProject, "Source Image");
            string assemblyPath = Path.Combine(newProject, filename + Extension);
            try
            {
                
                if (Directory.Exists(ProjectPath))
                {
                    if (Directory.Exists(newProject))
                    {
                        Errorhandler.RaiseMessage("A subassembly with this name already exists. Please choose a different name", "Error", 
                            Errorhandler.MessageType.Error);
                        return null;
                    }
                    if (string.IsNullOrEmpty(filename) || string.IsNullOrWhiteSpace(filename))
                    {
                        Errorhandler.RaiseMessage("You forgot to choose a name.", "Missing Values", Errorhandler.MessageType.Error);
                        return null;
                    }
                    Directory.CreateDirectory(newProject);
                    Directory.CreateDirectory(PlannerFilesPath);
                    Directory.CreateDirectory(SourceImagesPath);
                    
                    DominoAssembly dominoAssembly = new DominoAssembly() { };
                    dominoAssembly.Save(assemblyPath);
                    dominoAssembly.colorPath = Workspace.MakeRelativePath(assemblyPath,
                        Workspace.AbsolutePathFromReferenceLoseUpdate(parentProject.colorPath, parentProject));
                    dominoAssembly.Save();
                    AssemblyNode asn = new AssemblyNode(Workspace.MakeRelativePath(ProjectPath + "\\", assemblyPath), parentProject);
                    parentProject.Save();
                    return asn;
                }
            }
            catch
            {
                if (Directory.Exists(newProject)) Directory.Delete(newProject, true);
                Errorhandler.RaiseMessage("Project creation failed. The file system changes have been reverted", "Error", Errorhandler.MessageType.Error);
            }
            return null;
        }

        public override void UpdateImageInformation()
        {
            
        }
    }
    public class ImageSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (string.IsNullOrEmpty(item?.ToString()))
                return EmptyImageTemplate;
            return ImageTemplate;
        }
        public DataTemplate EmptyImageTemplate { get; set; }
        public DataTemplate ImageTemplate { get; set; }
    }
}