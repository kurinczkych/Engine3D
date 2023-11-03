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

    public static class FileManager
    {
        private static List<Stream> openedStreams = new List<Stream>();

        public static Stream? GetFileStream(string file, FileType type)
        {
            Stream? s = Stream.Null;

            string fileLocation = Environment.CurrentDirectory + "\\" + type.ToString();

            if (!Directory.Exists(fileLocation))
            {
                s = GetFileStreamFromResource(file, type);
            }
            else
            {
                string[] files = Directory.GetFiles(fileLocation);
                if (!files.Any(x => Path.GetFileName(x) == file))
                {
                    s = GetFileStreamFromResource(file, type);
                }
                else
                {
                    string foundFile = Directory.GetFiles(fileLocation).Where(x => Path.GetFileName(x) == file).First();
                    s = File.OpenRead(foundFile);
                }
            }

            return s;
        }

        public static string GetFilePath(string file, FileType type)
        {
            string path = "";

            string fileLocation = Environment.CurrentDirectory + "\\" + type.ToString();

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

        private static Stream? GetFileStreamFromResource(string file, FileType type)
        {
            string resourceName = GetResourceNameByNameEnd(file);
            if (resourceName == "")
                throw new Exception("'" + file + "' doesn't exist!");


            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream? s = assembly.GetManifestResourceStream(resourceName);
            return s;
        }

        public static List<Asset> GetAllAssets()
        {
            List<Asset> assets = new List<Asset>();
            int assetCount = 0;

            foreach(var type in Enum.GetValues(typeof(FileType)))
            {
                string fileLocation = Environment.CurrentDirectory + "\\" + type.ToString();
                if(Directory.Exists(fileLocation))
                {
                    var files = Directory.GetFiles(fileLocation);
                    foreach(var file in files)
                    {
                        Asset asset = new Asset(assetCount, Path.GetFileName(file), file, GetAssetType((FileType)type));
                        assetCount++;
                        assets.Add(asset);
                    }
                }
            }

            return assets;
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
