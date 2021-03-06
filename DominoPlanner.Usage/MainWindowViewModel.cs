﻿using DominoPlanner.Core;
using DominoPlanner.Usage.HelperClass;
using DominoPlanner.Usage.Serializer;
using DominoPlanner.Usage.UserControls.ViewModel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DominoPlanner.Usage
{
    class MainWindowViewModel : ModelBase
    {
        #region CTOR
        public MainWindowViewModel()
        {
            Properties.Settings.Default.Upgrade();
            Properties.Settings.Default.StructureTemplates = Properties.Settings.Default.Properties["StructureTemplates"].DefaultValue.ToString();
            if (Properties.Settings.Default.FirstStartup)
            {
                Properties.Settings.Default.StandardColorArray = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Appdata", "Local", "DominoPlanner", "colors" + Properties.Resources.ColorExtension);
                Properties.Settings.Default.StandardProjectPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Appdata", "Local", "DominoPlanner");
                Properties.Settings.Default.OpenProjectList = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Appdata", "Local", "DominoPlanner", "OpenProjects.xml");
                Directory.CreateDirectory(Path.GetDirectoryName(Properties.Settings.Default.StandardColorArray));
                OpenProjectSerializer.Create();
                Properties.Settings.Default.FirstStartup = false;
            }
            Properties.Settings.Default.Save();

            while (!File.Exists(Properties.Settings.Default.StandardColorArray))
            {
                Errorhandler.RaiseMessage("Please create a defaultcolortable.", "Missing Color Table", Errorhandler.MessageType.Info);
                new SetStandardV().ShowDialog();
            }

            NewFieldStruct = new RelayCommand(o => { NewFieldStructure(); });
            MenuSetStandard = new RelayCommand(o => { new SetStandardV().ShowDialog(); });
            AddExistingProject = new RelayCommand(o => { AddProject_Exists(); });
            AddExistingItem = new RelayCommand(o => { AddItem_Exists(); });
            NewProject = new RelayCommand(o => { CreateNewProject(); });
            SaveAll = new RelayCommand(o => { SaveAllOpen(); });
            SaveCurrentOpen = new RelayCommand(o => { SaveCurrentOpenProject(); });
            FileListClickCommand = new RelayCommand(o => { OpenItemFromPath(o); });
            Tabs = new ObservableCollection<TabItem>();
            Workspace.del = UpdateReference;
            loadProjectList();
        }

        internal bool CloseAllTabs()
        {
            while (Tabs.Count > 0)
            {
                if (!RemoveItem(Tabs.First()))
                    return false;
            }
            return true;
        }
        private string UpdateReference(string absolutePath, string parentPath)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = Path.GetDirectoryName(absolutePath);
            ofd.Title = $"Locate file {Path.GetFileName(absolutePath)}";
            Errorhandler.RaiseMessage($"The object {parentPath} contains a reference to the file {absolutePath}," +
                $"which could not be located. Please find the file.", "Missing file", Errorhandler.MessageType.Error);
            string extension = Path.GetExtension(absolutePath);
            ofd.Filter = $"{extension} files|*{extension}|all files|*.*";
            if (ofd.ShowDialog() == true && File.Exists(ofd.FileName))
            {
                return Workspace.MakeRelativePath(parentPath, ofd.FileName);
            }

            return "";
        }
        private void RegisterNewViewModel(DominoProviderTabItem oldViewModel, DominoProviderTabItem newViewModel)
        {
            TabItem tabItem = Tabs.Where(x => x.Content == oldViewModel).FirstOrDefault();
            tabItem.Content = newViewModel;
        }
        private void RegisterReplacementViewModel(DominoProviderTabItem oldVM, DominoProviderTabItem newVM)
        {
            TabItem tabItem = Tabs.Where(x => x.Content == oldVM).FirstOrDefault();
            tabItem.Content = newVM;
        }
        private DominoProviderTabItem GetNewViewModel(DominoProviderTabItem oldVM)
        {
            TabItem tabItem = Tabs.Where(x => x.Content == oldVM).FirstOrDefault();
            return TabItem.ViewModelGenerator(((DominoProviderTabItem) tabItem.Content).CurrentProject, tabItem.Path);
        }
        #endregion

        #region prop
        public ObservableCollection<TabItem> Tabs { get; set; }
        private TabItem _SelectedTab;
        public TabItem SelectedTab
        {
            get { return _SelectedTab; }
            set
            {
                if (_SelectedTab != value)
                {
                    if (_SelectedTab != null)
                    {
                        //if (_SelectedTab.Content is ColorListControlVM colorList)
                        //{
                        //    //hässlich aber tut... :D
                        //    //colorList.DifColumns.Clear();
                        //}
                    }
                    _SelectedTab = value;
                    if (SelectedTab != null)
                        _SelectedTab.Content.ResetContent();
                    RaisePropertyChanged();
                }
            }
        }
        private AssemblyNodeVM _SelectedProject;
        public AssemblyNodeVM SelectedProject
        {
            get { return _SelectedProject; }
            set
            {
                if (_SelectedProject != value)
                {
                    _SelectedProject = value;
                    RaisePropertyChanged();
                }
            }
        }
        public static ObservableCollection<AssemblyNodeVM> _Projects;
        public ObservableCollection<AssemblyNodeVM> Projects
        {
            get { return _Projects; }
            set
            {
                if (_Projects != value)
                {
                    _Projects = value;
                    RaisePropertyChanged();
                }
            }
        }
        #endregion

        #region Command
        private ICommand _AddExistingProject;
        public ICommand AddExistingProject { get { return _AddExistingProject; } set { if (value != _AddExistingProject) { _AddExistingProject = value; } } }

        private ICommand _AddExistingItem;
        public ICommand AddExistingItem { get { return _AddExistingItem; } set { if (value != _AddExistingItem) { _AddExistingItem = value; } } }

        private ICommand _NewProject;
        public ICommand NewProject { get { return _NewProject; } set { if (value != _NewProject) { _NewProject = value; } } }

        private ICommand _NewFieldStruct;
        public ICommand NewFieldStruct { get { return _NewFieldStruct; } set { if (value != _NewFieldStruct) { _NewFieldStruct = value; } } }

        private ICommand _MenuSetStandard;
        public ICommand MenuSetStandard { get { return _MenuSetStandard; } set { if (value != _MenuSetStandard) { _MenuSetStandard = value; } } }

        private ICommand _SaveAll;
        public ICommand SaveAll { get { return _SaveAll; } set { if (value != _SaveAll) { _SaveAll = value; } } }

        private ICommand _SaveCurrentOpen;
        public ICommand SaveCurrentOpen { get { return _SaveCurrentOpen; } set { if (value != _SaveCurrentOpen) { _SaveCurrentOpen = value; } } }

        private ICommand _FileListClickCommand;
        public ICommand FileListClickCommand { get { return _FileListClickCommand; } set { if (value != _FileListClickCommand) { _FileListClickCommand = value; } } }

        #endregion

        #region Methods
        #region Eventmethods
        /// <summary>
        /// Remove selected Project
        /// </summary>
        //private void RemoveMI_Click(object sender, System.Windows.RoutedEventArgs e)
        //{
        //    List<TabItem> removeList = Tabs.Where(x => x.ProjectID == ((ProjectListComposite)((System.Windows.Controls.MenuItem)sender).DataContext).OwnID).ToList<TabItem>();
        //    for (int i = 0; i < removeList.Count; i++)
        //    {
        //        RemoveItem(removeList[0]);
        //    }

        //    if (OpenProjectSerializer.RemoveOpenProject(((ProjectListComposite)((System.Windows.Controls.MenuItem)sender).DataContext).OwnID))
        //    {
        //        Projects.Remove(((ProjectListComposite)((System.Windows.Controls.MenuItem)sender).DataContext));
        //    }
        //    else
        //    {
        //        Errorhandler.RaiseMessage("Could not remove the project!", "Error", Errorhandler.MessageType.Error);
        //    }
        //}

        /// <summary>
        /// Clickevent wenn in der Baumstruktur ein Projektnode geklickt wird
        /// </summary>
        /// <param name="sender">Gecklicktes Node</param>
        /// <param name="e"></param>
        private void Item_IsClicked(object sender, EventArgs e)
        {
            OpenItem(TabItem.TabItemGenerator(sender as NodeVM));
        }
        private void OpenItemFromPath(object param)
        {
            string path = param.ToString();
            string ex = Path.GetExtension(path).ToLower();
            if (ex == Properties.Resources.ColorExtension.ToLower() || ex == Properties.Resources.ObjectExtension.ToLower())
            {
                OpenItem(GetTab(path) ?? new TabItem(path));
            }
        }
        private TabItem GetTab(NodeVM toOpen)
        {
            return GetTab(toOpen.AbsolutePath);
        }
        private TabItem GetTab(string toOpen)
        {
            return Tabs.FirstOrDefault(x => Path.GetFullPath(x.Path).Equals(Path.GetFullPath(toOpen), StringComparison.OrdinalIgnoreCase));
        }
        private void OpenItem(TabItem toOpen)
        {
            if (!Tabs.Contains(toOpen))
            {
                Tabs.Add(toOpen);
                toOpen.CloseIt = MainWindowViewModel_CloseIt;
                if (toOpen.Content is DominoProviderTabItem ti && ti.CurrentProject != null)
                {
                    ti.GetNewViewModel = GetNewViewModel;
                    ti.RegisterNewViewModel = RegisterNewViewModel;
                }
                if (toOpen.Content is EditProjectVM v)
                {
                    v.DisplaySettingsTool.ResetCanvas();
                }
            }
            SelectedTab = toOpen;
        }
        public void OpenFile(string filename)
        {
            foreach (string s in filename.Split('\n'))
            {
                var fn = s.Trim();
                var ext = Path.GetExtension(fn).ToLower();
                if (ext == Properties.Resources.ObjectExtension.ToLower() || ext == Properties.Resources.ColorExtension.ToLower())
                {
                    OpenItemFromPath(fn);
                }
                else if (ext == Properties.Resources.ProjectExtension.ToLower())
                {
                    AssemblyNodeVM res = null;
                    foreach (AssemblyNodeVM p in Projects)
                    {
                        if (Path.GetFullPath(p.AbsolutePath).Equals(Path.GetFullPath(fn), StringComparison.OrdinalIgnoreCase))
                        {
                            res = p;
                            res.IsExpanded = true;
                            break;
                        }
                    }
                    if (res != null) continue;
                    if (File.Exists(fn))
                    {
                        OpenProject openProject = OpenProjectSerializer.AddOpenProject(Path.GetFileNameWithoutExtension(fn), Path.GetDirectoryName(fn));
                        loadProject(openProject);
                    }
                }
            }
        }
        /// <summary>
        /// Selection Changed in der Baumstruktur (damit das akteuelle Item refreshed werden kann)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindowViewModel_SelectedEvent(object sender, EventArgs e)
        {
            if (sender is NodeVM node && node.IsSelected)
            {
                if (sender is AssemblyNodeVM assy)
                {
                    SelectedProject = assy;
                }
                else
                {
                    SelectedProject = node.parent;
                }
            }
        }
        /// <summary>
        /// Aktuelles TabItem schließen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private bool MainWindowViewModel_CloseIt(TabItem tabItem)
        {
            return RemoveItem(tabItem);
        }
        private bool RemoveFileFromTabs(string path)
        {
            bool result = true;
            foreach (TabItem tabItem in Tabs.Where(x => x.Path == path).ToArray())
            {
                result = result && RemoveItem(tabItem);
            }
            return result;
        }
        private bool RemoveNodeFromTabs(NodeVM node)
        {
            return RemoveFileFromTabs(node.AbsolutePath);
        }
        private bool RemoveItem(TabItem tabItem)
        {
            bool remove = false;
            if (tabItem.Content.UnsavedChanges)
            {
                System.Windows.Forms.DialogResult result = System.Windows.Forms.MessageBox.Show($"Save unsaved changes of {tabItem.Header.TrimEnd('*')}?", "Warning", System.Windows.Forms.MessageBoxButtons.YesNoCancel, System.Windows.Forms.MessageBoxIcon.Warning);
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    tabItem.Content.Save();
                    remove = true;
                }
                if (result == System.Windows.Forms.DialogResult.No)
                {
                    remove = true;
                }
            }
            else
            {
                remove = true;
            }
            if (remove) Tabs.Remove(tabItem);
            return remove;
        }

        #endregion
        /// <summary>
        /// Projektliste laden
        /// </summary>
        private void loadProjectList()
        {
            Projects = new ObservableCollection<AssemblyNodeVM>();
            List<OpenProject> OpenProjects = OpenProjectSerializer.GetOpenProjects();
            if (OpenProjects != null)
            {
                foreach (OpenProject curOP in OpenProjects)
                {
                    loadProject(curOP);
                }
            }
            else
            {
                Errorhandler.RaiseMessage("Error loading opened projects!", "Error", Errorhandler.MessageType.Error);
                OpenProjectSerializer.Create();
            }
        }

        private void loadProject(OpenProject newProject)
        {
            bool remove = true;
            string projectpath = Path.Combine(newProject.path, $"{newProject.name}{Properties.Resources.ProjectExtension}");
            if (File.Exists(projectpath))
            {
                remove = false;
                
                AssemblyNodeVM node = null;
                
                try
                {
                    AssemblyNode mainnode = new AssemblyNode(projectpath);
                    // check if the file can be deserialized properly
                    node = new AssemblyNodeVM(mainnode, OpenItem, RemoveNodeFromTabs, GetTab);
                }
                catch
                {
                    try
                    {
                        AssemblyNode restored = AssemblyNodeVM.RestoreAssembly(projectpath);
                        node = new AssemblyNodeVM(restored, OpenItem, RemoveNodeFromTabs, GetTab);
                    }
                    catch (FileNotFoundException)
                    {
                        remove = true;
                    }
                    catch
                    {
                        Errorhandler.RaiseMessage($"The main project file of project {projectpath} was damaged. An attempt to restore the file has been unsuccessful. \nThe project will be removed from the list of opened projects.", "Damaged File", Errorhandler.MessageType.Error);
                        remove = true;
                    }
                }
                if (!remove)
                {
                    Projects.Add(node);
                }
            }
            if (remove)
            {
                Errorhandler.RaiseMessage($"Unable to load project {newProject.name}. It might have been moved or damaged. \nPlease re-add it at its current location.\n\nThe project has been removed from the list of opened projects.", "Error!", Errorhandler.MessageType.Error);
                OpenProjectSerializer.RemoveOpenProject(newProject.id);
            }
        }
        private List<ProjectElement> getProjects(DominoAssembly dominoAssembly)
        {
            List<ProjectElement> returnList = new List<ProjectElement>();

            if (dominoAssembly != null)
            {
                ProjectElement color = new ProjectElement(dominoAssembly.colorPath, @".\Icons\colorLine.ico", null);
                returnList.Add(color);
            }

            foreach (DocumentNode dominoWrapper in dominoAssembly.children.OfType<DocumentNode>().ToList())
            {
                try
                {
                    string relativePath = dominoWrapper.relativePath;
                    string filepath = Workspace.AbsolutePathFromReference(ref relativePath, dominoWrapper.parent);
                    dominoWrapper.relativePath = relativePath;
                    string picturepath = ImageHelper.GetImageOfFile(filepath);
                    ProjectElement project = new ProjectElement(filepath,
                        picturepath, dominoWrapper);
                    returnList.Add(project);
                    

                }
                catch (FileNotFoundException)
                {
                    // Remove file from Project
                    dominoAssembly.children.Remove(dominoWrapper);
                    Errorhandler.RaiseMessage($"The file {dominoWrapper.relativePath} doesn't exist at the current location. \nIt has been removed from the project.", "Missing file", Errorhandler.MessageType.Error);
                    dominoAssembly.Save();
                }
            }
            dominoAssembly.Save();
            return returnList;
        }

        /// <summary>
        /// Neues Unterprojekt starten
        /// </summary>
        private void NewFieldStructure()
        {
            if (SelectedProject == null)
            {
                Errorhandler.RaiseMessage("Please choose a project folder.", "Please choose", Errorhandler.MessageType.Error);
                return;
            }
            SelectedProject.NewFieldStructure();
        }

        private void AddProject_Exists()
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Filter = $"project files (*{Properties.Resources.ProjectExtension})|*{Properties.Resources.ProjectExtension}";
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (File.Exists(openFileDialog.FileName))
                {
                    OpenProject openProject = OpenProjectSerializer.AddOpenProject(Path.GetFileNameWithoutExtension(openFileDialog.FileName), Path.GetDirectoryName(openFileDialog.FileName));
                    loadProject(openProject);
                }
            }
        }

        private void AddItem_Exists()
        {
            if (SelectedProject == null)
            {
                Errorhandler.RaiseMessage("Please choose a project folder.", "Please choose", Errorhandler.MessageType.Error);
                return;
            }
            SelectedProject.AddExistingItem();
        }
        private void CreateNewProject()
        {
            NewProjectVM curNPVM = new NewProjectVM();
            new NewProject(curNPVM).ShowDialog();
            if (curNPVM.Close == true)
            {
                OpenProject newProj = OpenProjectSerializer.AddOpenProject(curNPVM.ProjectName, string.Format(@"{0}\{1}", curNPVM.SelectedPath, curNPVM.ProjectName));
                if (newProj == null)
                {
                    Errorhandler.RaiseMessage("Could not create new Project!", "Error!", Errorhandler.MessageType.Error);
                    return;
                }
                loadProject(newProj);
            }
        }

        /// <summary>
        /// Save all open projects
        /// </summary>
        private void SaveAllOpen()
        {
            foreach (TabItem curTI in Tabs)
            {
                if (curTI.Content.UnsavedChanges)
                {
                    if (!curTI.Content.Save())
                    {
                        Errorhandler.RaiseMessage("Error Saving files!", string.Format("Stop saving, because could not save {0}", curTI.Header), Errorhandler.MessageType.Error);
                        return;
                    }
                }
            }
            Errorhandler.RaiseMessage("Save all files", "Saves all files!", Errorhandler.MessageType.Info);
        }
        /// <summary>
        /// Save current project
        /// </summary>
        private void SaveCurrentOpenProject()
        {
            if (SelectedTab.Content.Save())
                Errorhandler.RaiseMessage("Save all changes!", "Save all changes", Errorhandler.MessageType.Info);
            else
                Errorhandler.RaiseMessage("Error!", "Error saving changes!", Errorhandler.MessageType.Error);
        }
        #endregion
    }
}