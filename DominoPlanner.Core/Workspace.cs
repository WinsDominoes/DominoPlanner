﻿using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DominoPlanner.Core
{
    public class Workspace : INotifyPropertyChanged
    {
        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        private ObservableCollection<Tuple<string, IWorkspaceLoadable>> _openedFiles;

        public ObservableCollection<Tuple<string, IWorkspaceLoadable>> openedFiles
        {
            get { return _openedFiles; }
            set { _openedFiles = value; RaisePropertyChanged(); }
        }
        
        // threadsicheres Singleton
        private static readonly Lazy<Workspace> _mySingleton = new Lazy<Workspace>(() => new Workspace());

        private string FileInWork = "";
        private bool ReferenceReplaced = false;
        private Workspace() {
            openedFiles = new ObservableCollection<Tuple<string, IWorkspaceLoadable>>();
        }
        public delegate string FileReplacementDelegate(string filename, string caller);

        public static FileReplacementDelegate del;

        public event PropertyChangedEventHandler PropertyChanged;

        public static Workspace Instance
        {
            get
            {
                return _mySingleton.Value;
            }

        }
        private static void Add(string absolutePath, IWorkspaceLoadable obj)
        {
            if (Find<IWorkspaceLoadable>(absolutePath) == null)
            {
                Instance.openedFiles.Add(new Tuple<string, IWorkspaceLoadable>(absolutePath, obj));
                Console.WriteLine($"File {absolutePath} added to workspace");
            }
        }
        public static string AbsolutePathFromReferenceLoseUpdate(string relativePath, IWorkspaceLoadable reference)
        {
            return AbsolutePathFromReference(ref relativePath, reference);
        }
        public static string AbsolutePathFromReference(ref string relativePath, IWorkspaceLoadable reference)
        {
            string absolutePath = "";
            var referenceTuple = Instance.openedFiles.Where(x => x.Item2 == reference).FirstOrDefault();
            if (new Uri(relativePath, UriKind.RelativeOrAbsolute).IsAbsoluteUri)
            {
                absolutePath = relativePath;
            }
            else if (reference != null)
            {
                string basepath = "";
                if (referenceTuple != null)
                {
                     basepath = referenceTuple.Item1;
                }
                else if (!String.IsNullOrEmpty(Instance.FileInWork))
                {
                    basepath = Instance.FileInWork;
                }
                if (basepath != "")
                {
                    string directoryofreference = Path.GetDirectoryName(basepath);
                    if (relativePath != null)
                        absolutePath = Path.GetFullPath(Path.Combine(directoryofreference, relativePath));
                    if (relativePath == null || !File.Exists(absolutePath))
                    {
                        string temp = del.Invoke(absolutePath, basepath);
                        relativePath = temp != "" ? temp : relativePath;
                        absolutePath = Path.GetFullPath(Path.Combine(directoryofreference, relativePath));
                        Instance.ReferenceReplaced = true;
                    }
                    if (!File.Exists(absolutePath))
                    {
                        throw new FileNotFoundException("File not found, update failed", absolutePath);
                    }
                }
            }
            
            else
            {
                throw new IOException("When not providing a reference, the path must be absolute");
            }
            return absolutePath;
        }
        public static T Load<T>(string absolutePath) where T : IWorkspaceLoadable
        {
            /*return Load<T>(absolutePath, null);*/
            return LoadInternal<T, T, T>(absolutePath, a => a, a => a);
        }
        private static Out LoadInternal<FullType, LoadType, Out>
            (string absolutePath, Func<LoadType, Out> func, Func<FullType, Out> func2) where FullType : IWorkspaceLoadable where LoadType : IWorkspaceLoadable
        {
            bool preview = typeof(LoadType) != typeof(FullType);
            var result = (FullType)Find<FullType>(absolutePath);
            string old_file_in_work = Instance.FileInWork;
            bool old_referenceReplaced = Instance.ReferenceReplaced;
            Console.WriteLine($"Datei {absolutePath} {(preview ? "als Vorschau" : "vollwertig")} öffnen");
            try
            {
                if (!preview) Instance.FileInWork = absolutePath;
                if (result == null)
                {
                    LoadType resultLoaded;
                    Console.WriteLine($"Datei noch nicht geöffnet, als {typeof(LoadType)} deserialisieren");
                    using (var file = File.OpenRead(absolutePath))
                    {
                        resultLoaded = Serializer.Deserialize<LoadType>(file);
                        if (!preview) Add(absolutePath, resultLoaded);
                    }
                    if (!preview && Instance.ReferenceReplaced)
                    {
                        Console.WriteLine($"Reference in file {absolutePath} replaced");
                        Save(resultLoaded, absolutePath);
                    }
                    return func.Invoke(resultLoaded);
                }
                Console.WriteLine("Datei bereits geöffnet, aus Workspace nehmen");
                
                return func2.Invoke(result);
            }
            finally
            {
                Instance.ReferenceReplaced = old_referenceReplaced;
                Instance.FileInWork = old_file_in_work;
            }
        }
        private static Out LoadInternal<FullType, LoadType, Out>
            (string relativePath, IWorkspaceLoadable reference, Func<LoadType, Out> func, Func<FullType, Out> func2) where FullType : IWorkspaceLoadable where LoadType : IWorkspaceLoadable
        {
            var absPath = AbsolutePathFromReference(ref relativePath, reference);
            return LoadInternal(absPath, func, func2);
        }
        public static T Load<T>(string relativePath, IWorkspaceLoadable reference) where T : IWorkspaceLoadable
        {
            return LoadInternal<T, T, T>(relativePath, reference, a => a, a => a);
        }
        public static void Save(IWorkspaceLoadable obj, string filepath = "")
        {
            // prüfen, ob Datei schon offen
            var index = Instance.openedFiles.FindIndex(x => x.Item2 == obj);
            bool addToList = false;
            if (index != -1)
            {
                // Datei schon offen, soll sie unter einem anderen Namen gespeichert werden? 
                if (filepath != Instance.openedFiles[index].Item1 && filepath != "")
                {
                    obj = Serializer.DeepClone(obj);
                    addToList = true;
                }
                filepath = Instance.openedFiles[index].Item1;
            }
            else
            {
                // Datei neu erstellt
                if (!new Uri(filepath, UriKind.RelativeOrAbsolute).IsAbsoluteUri)
                {
                    throw new IOException("If file will be newly created, save needs an absolute path");
                }
                addToList = true;
            }
            using (FileStream stream = new FileStream(filepath, FileMode.Create))
            {
                Serializer.Serialize(stream, obj);
                if (addToList)
                {
                    Add(filepath, obj);
                }
            }
        }
        public static ObservableCollection<ImageFilter> LoadImageFilters<T>(string absolutePath) where T : IWorkspaceLoadImageFilter
        {
            return LoadInternal<T, IDominoProviderImageFilter, ObservableCollection<ImageFilter>>(absolutePath, a => a.PrimaryImageTreatment?.ImageFilters, a => a.PrimaryImageTreatment?.ImageFilters);
        }
        public static ObservableCollection<ImageFilter> LoadImageFilters<T>(string relativePath, IWorkspaceLoadable reference) where T : IWorkspaceLoadImageFilter
        {
            return LoadImageFilters<T>(AbsolutePathFromReference(ref relativePath, reference));
        }
        public static Tuple<string, int[]> LoadColorList<T>(string absolutePath) where T: IWorkspaceLoadColorList
        {
            return LoadInternal<IDominoProvider, IDominoProviderPreview, Tuple<string, int[]>>(absolutePath,
                a => new Tuple<string, int[]>(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(absolutePath), a.ColorPath)), a.Counts),
                a => new Tuple<string, int[]>(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(absolutePath), a.ColorPath)), a.Counts)
                );
        }
        public static Tuple<string, int[]> LoadColorList<T>(string relativePath, IWorkspaceLoadable reference) where T : IWorkspaceLoadColorList
        {
            return LoadColorList<T>(AbsolutePathFromReference(ref relativePath, reference));
        }
        public static bool LoadEditingState<T>(string absolutePath) where T : IWorkspaceLoadColorList
        {
            return LoadInternal<IDominoProvider, IDominoProviderPreview, bool>(absolutePath, a => a.Editing, a => a.Editing);
        }
        public static bool LoadEditingState<T>(string relativePath, IWorkspaceLoadable reference) where T : IWorkspaceLoadColorList
        {
            return LoadEditingState<T>(AbsolutePathFromReference(ref relativePath, reference));
        }
        public static bool LoadHasProtocolDefinition<T>(string absolutePath) where T : IWorkspaceLoadColorList
        {
            return LoadInternal<IDominoProvider, IDominoProviderPreview, bool>(absolutePath, a => a.HasProtocolDefinition, 
                a => a.HasProtocolDefinition);
        }
        public static bool LoadHasProtocolDefinition<T>(string relativePath, IWorkspaceLoadable reference) where T : IWorkspaceLoadColorList
        {
            return LoadHasProtocolDefinition<T>(AbsolutePathFromReference(ref relativePath, reference));
        }
        public static byte[] LoadThumbnailFromStream(Stream stream)
        {
            return Serializer.Deserialize<IDominoProviderThumbnail>(stream).Thumbnail;
        }
        public static void CloseFile(string path)
        {
            if (new Uri(path, UriKind.RelativeOrAbsolute).IsAbsoluteUri)
            {
                Instance.openedFiles.RemoveAll(x => x.Item1 == path);
            }
        }
        public static void CloseFile(string relativePath, IWorkspaceLoadable reference)
        {
            CloseFile(AbsolutePathFromReference(ref 
                relativePath, reference));
        }
        public static void CloseFile(IWorkspaceLoadable reference)
        {
            Instance.openedFiles.RemoveAll(x => x.Item2 == reference);
        }
        public static void Clear()
        {
            Instance.openedFiles = new ObservableCollection<Tuple<string, IWorkspaceLoadable>>();
        }
        public static object Find<T>(string AbsolutePath)
        {
            var result = Instance.openedFiles.Where(x => Path.GetFullPath(x.Item1).Equals(Path.GetFullPath(AbsolutePath), StringComparison.OrdinalIgnoreCase) && x.Item2 is T);
            if (result.Count() == 0) return null;
            return result.First().Item2;
        }
        public static string Find(IWorkspaceLoadable obj) 
        {
            var result = Instance.openedFiles.Where(x => x.Item2 == obj);
            if (result.Count() == 0) return null;
            return result.First().Item1;
        }
        public static String MakeRelativePath(String fromPath, String toPath)
        {
            if (String.IsNullOrEmpty(fromPath)) throw new ArgumentNullException("fromPath");
            if (String.IsNullOrEmpty(toPath)) throw new ArgumentNullException("toPath");

            Uri fromUri = new Uri(fromPath);
            Uri toUri = new Uri(toPath);

            if (fromUri.Scheme != toUri.Scheme) { return toPath; } // path can't be made relative.

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            String relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (toUri.Scheme.Equals("file", StringComparison.InvariantCultureIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }
        
    }
    public static class ObservableCollectionExtensions
    {
        public static int RemoveAll<T>(
        this ObservableCollection<T> coll, Func<T, bool> condition)
        {
            var itemsToRemove = coll.Where(condition).ToList();

            foreach (var itemToRemove in itemsToRemove)
            {
                coll.Remove(itemToRemove);
            }

            return itemsToRemove.Count;
        }
        public static int FindIndex<T>(
        this ObservableCollection<T> coll, Func<T, bool> condition)
        {
            for (int i = 0; i < coll.Count; i++)
            {
                if (condition.Invoke(coll[i])) return i;
            }
            return -1;
        }
    }
}
