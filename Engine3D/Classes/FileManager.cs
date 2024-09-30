using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public enum FileType
    {
        Textures,
        Fonts,
        Audio,
        Models,
        Animations
    }

    public static class FileManager
    {
        private static List<Stream> openedStreams = new List<Stream>();
        private static Dictionary<string, int> fileFolderCount = new Dictionary<string, int>();
        private static bool first = true;

        public static Stream? GetFileStream(string fullpath)
        {
            Stream? s = Stream.Null;

            string? folderName = Path.GetDirectoryName(fullpath);
            if (folderName == null)
                return s;

            string fileName = Path.GetFileName(fullpath);

            if (!Directory.Exists(folderName))
            {
                s = GetFileStreamFromResource(fileName);
            }
            else
            {
                string[] files = Directory.GetFiles(folderName);
                if (!files.Any(x => Path.GetFileName(x) == fileName))
                {
                    s = GetFileStreamFromResource(fileName);
                }
                else
                {
                    string foundFile = Directory.GetFiles(folderName).Where(x => Path.GetFileName(x) == fileName).First();
                    s = File.OpenRead(foundFile);
                }
            }

            return s;
        }

        public static string GetFilePath(string fileName, string folder)
        {
            string filePath = Environment.CurrentDirectory + "\\Assets\\" + folder + "\\" + fileName;
            if (File.Exists(filePath))
                return filePath;

            string resourceName = GetResourceNameByNameEnd(fileName);
            if (resourceName != null && resourceName != "")
                return resourceName;

            return "";
        }

        private static string GetResourceNameByNameEnd(string nameEnd)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            foreach (string resourceName in assembly.GetManifestResourceNames())
            {
                if (resourceName.EndsWith(nameEnd, StringComparison.OrdinalIgnoreCase))
                {
                    return resourceName;
                }
            }
            return ""; // or throw an exception if the resource is not found
        }

        private static Stream? GetFileStreamFromResource(string file)
        {
            string resourceName = GetResourceNameByNameEnd(file);
            if (resourceName == "")
                throw new Exception("'" + file + "' doesn't exist!");


            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream? s = assembly.GetManifestResourceStream(resourceName);
            return s;
        }

        public static Dictionary<string,Stream> GetFontStreams()
        {
            Dictionary<string, Stream> fontDict = new Dictionary<string, Stream>();

            Assembly assembly = Assembly.GetExecutingAssembly();
            foreach (string resourceName in assembly.GetManifestResourceNames().Where(x => x.EndsWith(".ttf")))
            {
                var split = resourceName.Split('.');
                string name = split[split.Length - 2] + ".ttf";

                Stream? s = assembly.GetManifestResourceStream(resourceName);

                if (s == null)
                    Engine.consoleManager.AddLog("Font: " + name + " failed to load!", LogType.Warning);
                else
                    fontDict.Add(name, s);
            }

            return fontDict;
        }

        public static void GetAllAssets(ref AssetManager assetManager)
        {
            if (first)
            {
                foreach (var type in Enum.GetValues(typeof(FileType)))
                {
                    string fileLocation = Environment.CurrentDirectory + "\\Assets\\" + type.ToString();

                    RecursiveAllAssets(fileLocation, (FileType)type, ref assetManager, true);
                }
                first = false;
            }
            else
            {
                foreach (var type in Enum.GetValues(typeof(FileType)))
                {
                    string fileLocation = Environment.CurrentDirectory + "\\Assets\\" + type.ToString();

                    RecursiveAllAssets(fileLocation, (FileType)type, ref assetManager, false);
                }
            }
        }

        private static void RecursiveAllAssets(string fileLocation, FileType type, ref AssetManager assetManager, bool first)
        {
            if (first)
            {
                if (Directory.Exists(fileLocation))
                {
                    var files = Directory.GetFiles(fileLocation);
                    foreach (var file in files)
                    {
                        IncreaseFileTypeCount(fileLocation);
                        Asset asset = new Asset(Asset.CurrentId + 1, Path.GetFileName(file), file, GetAssetType((FileType)type), GetAssetTypeEditor((FileType)type), type);
                        assetManager.Add(asset);
                    }

                    var dirs = Directory.GetDirectories(fileLocation);
                    foreach (var dir in dirs)
                    {
                        RecursiveAllAssets(dir, type, ref assetManager, first);
                    }
                }
            }
            else
            {
                if (Directory.Exists(fileLocation))
                {
                    var files = Directory.GetFiles(fileLocation);

                    if (!AreFilesTheSameCount(fileLocation, files.Count()))
                    {
                        foreach (var file in files)
                        {
                            if (assetManager.loaded.Contains(file) || assetManager.toLoadString.Contains(file))
                                continue;

                            IncreaseFileTypeCount(fileLocation);
                            Asset asset = new Asset(Asset.CurrentId + 1, Path.GetFileName(file), file, GetAssetType((FileType)type), GetAssetTypeEditor((FileType)type), type);
                            assetManager.Add(asset);
                        }
                    }

                    var dirs = Directory.GetDirectories(fileLocation);
                    foreach (var dir in dirs)
                    {
                        RecursiveAllAssets(dir, type, ref assetManager, first);
                    }
                }
            }
        }

        private static void IncreaseFileTypeCount(string fileLocation)
        {
            if (fileFolderCount.ContainsKey(fileLocation))
                fileFolderCount[fileLocation]++;
            else
                fileFolderCount.Add(fileLocation, 1);
        }

        private static bool AreFilesTheSameCount(string folder, int count)
        {
            if(fileFolderCount.ContainsKey(folder))
                return fileFolderCount[folder] == count;

            return false;
        }

        private static AssetType GetAssetType(FileType fileType)
        {
            if (fileType == FileType.Models)
                return AssetType.Model;
            else if (fileType == FileType.Audio)
                return AssetType.Audio;
            else if (fileType == FileType.Textures)
                return AssetType.Texture;
            else
                return AssetType.Unknown;
        }

        private static AssetTypeEditor GetAssetTypeEditor(FileType fileType)
        {
            if (fileType == FileType.Models)
                return AssetTypeEditor.File;
            else if (fileType == FileType.Audio)
                return AssetTypeEditor.File;
            else if (fileType == FileType.Textures)
                return AssetTypeEditor.UI;
            else
                return AssetTypeEditor.Unknown;
        }

        public static void DeleteAsset(string assetPath)
        {
            if (assetPath == null || assetPath == "")
                return;

            if(File.Exists(assetPath))
            {
                try
                {
                    File.Delete(assetPath);

                    string? dir = Path.GetDirectoryName(assetPath);
                    if (dir != null && dir != "")
                        fileFolderCount[dir]--;
                }
                catch { }
            }
        }

        public static void DeleteFolder(string folderPath)
        {
            if (folderPath == null || folderPath == "")
                return;

            string folderFullPath = Environment.CurrentDirectory + "\\Assets\\" + folderPath;
            if (Directory.Exists(folderFullPath))
            {
                Directory.Delete(folderFullPath, true);
            }
        }

        public static void DisposeStreams()
        {
            foreach(Stream s in openedStreams)
            {
                if(!s.CanRead)
                    s.Dispose();
            }
            openedStreams.Clear();
        }
    }
}
