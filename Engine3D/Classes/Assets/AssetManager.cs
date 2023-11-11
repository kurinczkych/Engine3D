using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class AssetFolder
    {
        public string name;
        public Dictionary<string, AssetFolder> folders;
        public List<Asset> assets;
        public AssetFolder? parentFolder;
        public string path;

        public AssetFolder(string name) 
        {
            this.name = name;
            folders = new Dictionary<string, AssetFolder>();
            assets = new List<Asset>();
            parentFolder = null;
        }

        public void Insert(Asset asset)
        {
            List<string> dirs = GetDirectories(asset);

            InsertRec(dirs, asset);
        }

        private void InsertRec(List<string> dirs, Asset asset)
        {
            if (dirs.Count == 1)
            {
                AssetFolder a = GetAssetFolder(dirs[0]);

                a.assets.Add(asset);
                return;
            }

            AssetFolder assetFolder = GetAssetFolder(dirs[0]);
            assetFolder.InsertRec(dirs.GetRange(1, dirs.Count() - 1), asset);
        }

        public string Remove(Asset asset)
        {
            List<string> dirs = GetDirectories(asset);
            
            RemoveRec(dirs, asset);

            string removeFolder = FindEmptyFolderRec(dirs);

            return removeFolder;
        }

        private void RemoveRec(List<string> dirs, Asset asset)
        {
            if (dirs.Count == 1)
            {
                AssetFolder a = GetAssetFolder(dirs[0]);

                a.assets.Remove(asset);

                return;
            }

            AssetFolder assetFolder = GetAssetFolder(dirs[0]);
            assetFolder.RemoveRec(dirs.GetRange(1, dirs.Count() - 1), asset);
        }

        private string FindEmptyFolderRec(List<string> dirs)
        {
            if(dirs.Count == 1)
            {
                AssetFolder a = GetAssetFolder(dirs[0]);
                if (a.assets.Count == 0)
                {
                    return a.path;
                }

                return "";
            }

            AssetFolder assetFolder = GetAssetFolder(dirs[0]);
            string removeFolderPath = assetFolder.FindEmptyFolderRec(dirs.GetRange(1, dirs.Count() - 1));
            if(removeFolderPath != "")
            {
                string removeFolderName = removeFolderPath.Split('\\').Last();

                if (assetFolder.folders.ContainsKey(removeFolderName))
                {
                    assetFolder.folders.Remove(removeFolderName);
                }
            }
            return removeFolderPath;
        }

        private List<string> GetDirectories(Asset asset)
        {
            string? fullDirPath = Path.GetDirectoryName(asset.Path);
            if (fullDirPath == null)
                return new List<string>();

            List<string> dirs = new List<string>();
            var split = fullDirPath.Split('\\');

            bool add = false;
            for (int i = 0; i < split.Length; i++)
            {
                if (add)
                {
                    dirs.Add(split[i]);
                }
                else if (split[i] == asset.fileType.ToString())
                {
                    add = true;
                    dirs.Add(split[i]);
                    continue;
                }
            }

            return dirs;
        }

        private AssetFolder GetAssetFolder(string name)
        {
            if (folders.ContainsKey(name))
                return folders[name];

            AssetFolder a = new AssetFolder(name);
            a.path = path + "\\" + a.name;
            a.parentFolder = this;
            folders.Add(name, a);
            return folders[name];
        }
    }

    public class AssetManager
    {
        private List<Asset> toLoad = new List<Asset>();
        public List<string> toLoadString = new List<string>();
        private List<Asset> toRemove = new List<Asset>();
        public List<string> loaded = new List<string>();
        private List<Asset> loadedAssets = new List<Asset>();
        public AssetFolder assets = new AssetFolder("Assets");
        private TextureManager textureManager;

        public AssetManager(ref TextureManager textureManager)
        {
            this.textureManager = textureManager;

            foreach (var type in Enum.GetValues(typeof(FileType)))
            {
                string name = ((FileType)type).ToString();
                assets.folders.Add(name, new AssetFolder(name) { path = name });
            }
        }

        public void Add(Asset asset)
        {
            toLoad.Add(asset);
            toLoadString.Add(asset.Path);
        }

        public void Add(List<Asset> assets)
        {
            toLoad.AddRange(assets);
            foreach (var asset in assets)
                toLoadString.Add(asset.Path);
        }

        public void Remove(Asset asset)
        {
            toRemove.Add(asset);
        }

        public void Remove(List<Asset> assets)
        {
            toRemove.AddRange(assets);
        }

        public void UpdateIfNeeded()
        {
            if(toLoad.Count > 0)
            {
                Asset a = toLoad[0];
                if (a.Path.Contains("Space"))
                    ;
                bool success = false;
                if(a.EditorType == AssetTypeEditor.UI)
                {
                    if (!textureManager.textures.ContainsKey("ui_" + Path.GetFileName(a.Path)))
                        textureManager.AddUITexture(a.Path, out success, flipY: false, type: a.EditorType);
                }
                else if(a.EditorType == AssetTypeEditor.Store)
                {
                    if (!textureManager.textures.ContainsKey("ui_" + Path.GetFileName(a.Path)))
                        textureManager.AddUITexture(a.Path, out success, flipY: false, type: a.EditorType);
                }
                else
                {
                    success = true;
                }

                if (success)
                {
                    loaded.Add(a.Path);
                    toLoad.Remove(a);
                    toLoadString.Remove(a.Path);
                    loadedAssets.Add(a);
                    if(a.EditorType != AssetTypeEditor.Store)
                        assets.Insert(a);
                }
            }

            if(toRemove.Count > 0)
            {
                Asset a = toRemove[0];
                textureManager.DeleteTexture(a.Name);
                toRemove.Remove(a);
                loaded.Remove(a.Path);
                loadedAssets.Remove(a);
                string assetFolderToDelete = assets.Remove(a);
                FileManager.DeleteAsset(a.Path);
                if (assetFolderToDelete != "")
                    FileManager.DeleteFolder(assetFolderToDelete);
            }
        }

        public static string GetRelativeModelsFolder(string fullpath)
        {
            List<string> dirs = new List<string>();
            var split = fullpath.Split('\\');

            bool add = false;
            for (int i = 0; i < split.Length; i++)
            {
                if (add)
                {
                    dirs.Add(split[i]);
                }
                else if (split[i] == "Models")
                {
                    add = true;
                    continue;
                }
            }

            return string.Join('\\', dirs);
        }
    }
}
