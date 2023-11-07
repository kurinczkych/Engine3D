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
        Models
    }

    public class FileTypeCount
    {
        public bool First = true;
        public int Textures = 0;
        public int Fonts = 0;
        public int Audio = 0;
        public int Models = 0;
    }

    public static class FileManager
    {
        private static List<Stream> openedStreams = new List<Stream>();

        public static Stream? GetFileStream(string file, string folder)
        {
            Stream? s = Stream.Null;

            string fileLocation = Environment.CurrentDirectory + "\\" + folder;

            if (!Directory.Exists(fileLocation))
            {
                s = GetFileStreamFromResource(file);
            }
            else
            {
                string[] files = Directory.GetFiles(fileLocation);
                if (!files.Any(x => Path.GetFileName(x) == file))
                {
                    s = GetFileStreamFromResource(file);
                }
                else
                {
                    string foundFile = Directory.GetFiles(fileLocation).Where(x => Path.GetFileName(x) == file).First();
                    s = File.OpenRead(foundFile);
                }
            }

            return s;
        }

        public static string GetFilePath(string file, string folder)
        {
            string path = "";

            string fileLocation = Environment.CurrentDirectory + "\\" + folder;

            if (!Directory.Exists(fileLocation))
            {
                path = GetResourceNameByNameEnd(file);
            }
            else
            {
                if (!Directory.GetFiles(fileLocation).Any(x => Path.GetFileName(x) == file))
                {
                    path = GetResourceNameByNameEnd(file);
                }
                else
                {
                    path = Directory.GetFiles(fileLocation).Where(x => Path.GetFileName(x) == file).First();
                }
            }

            return path;
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

        public static void GetAllAssets(FileTypeCount fileTypeCount, ref AssetManager assetManager)
        {
            if (fileTypeCount.First)
            {
                foreach (var type in Enum.GetValues(typeof(FileType)))
                {
                    string fileLocation = Environment.CurrentDirectory + "\\" + type.ToString();
                    if (Directory.Exists(fileLocation))
                    {
                        var files = Directory.GetFiles(fileLocation);
                        foreach (var file in files)
                        {
                            IncreaseFileTypeCount(ref fileTypeCount, (FileType)type);
                            Asset asset = new Asset(Asset.CurrentId + 1, Path.GetFileName(file), file, GetAssetType((FileType)type), GetAssetTypeEditor((FileType)type));
                            assetManager.Add(asset);
                        }
                    }
                }
                fileTypeCount.First = false;
            }
            else
            {
                foreach (var type in Enum.GetValues(typeof(FileType)))
                {
                    string fileLocation = Environment.CurrentDirectory + "\\" + type.ToString();
                    if (Directory.Exists(fileLocation))
                    {
                        var files = Directory.GetFiles(fileLocation);

                        if (AreFilesTheSameCount(ref fileTypeCount, (FileType)type, files.Count()))
                            continue;

                        foreach (var file in files)
                        {
                            if (assetManager.loaded.Contains(file))
                                continue;

                            Asset asset = new Asset(Asset.CurrentId + 1, Path.GetFileName(file), file, GetAssetType((FileType)type), GetAssetTypeEditor((FileType)type));
                            assetManager.Add(asset);
                        }
                    }
                }
            }
        }

        private static void IncreaseFileTypeCount(ref FileTypeCount fileTypeCount, FileType fileType)
        {
            if (fileType == FileType.Models)
                fileTypeCount.Models++;
            else if (fileType == FileType.Audio)
                fileTypeCount.Audio++;
            else if (fileType == FileType.Textures)
                fileTypeCount.Textures++;
            else if (fileType == FileType.Fonts)
                fileTypeCount.Fonts++;
        }

        private static bool AreFilesTheSameCount(ref FileTypeCount fileTypeCount, FileType fileType, int count)
        {
            if (fileType == FileType.Models)
                return fileTypeCount.Models == count;
            else if (fileType == FileType.Audio)
                return fileTypeCount.Audio == count;
            else if (fileType == FileType.Textures)
                return fileTypeCount.Textures == count;
            else if (fileType == FileType.Fonts)
                return fileTypeCount.Fonts == count;
            else
                return true;
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
