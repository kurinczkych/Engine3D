using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class AssetManager
    {
        private List<Asset> toLoad = new List<Asset>();
        private List<Asset> toRemove = new List<Asset>();
        public List<string> loaded = new List<string>();
        private TextureManager textureManager;
        private EditorData editorData;

        public AssetManager(ref TextureManager textureManager, ref EditorData editorData)
        {
            this.textureManager = textureManager;
            this.editorData = editorData;
        }

        public void Add(Asset asset)
        {
            toLoad.Add(asset);
        }

        public void Add(List<Asset> assets)
        {
            toLoad.AddRange(assets);
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
                bool success = false;
                if(a.EditorType == AssetTypeEditor.UI)
                {
                    if (!textureManager.textures.ContainsKey("ui_" + a.Name))
                        textureManager.AddUITexture(a.Name, out success, flipY: false, type: a.EditorType);
                }
                else if(a.EditorType == AssetTypeEditor.Store)
                {
                    if (!textureManager.textures.ContainsKey("ui_" + a.Path))
                        textureManager.AddUITexture(a.Path, out success, flipY: false, type: a.EditorType);
                }
                else
                {
                    success = true;
                }
                //else if(a.EditorType == AssetTypeEditor.Unknown)

                if (success)
                {
                    loaded.Add(a.Path);
                    toLoad.Remove(a);
                    editorData.assets.Add(a);
                }
            }

            if(toRemove.Count > 0)
            {
                Asset a = toRemove[0];
                textureManager.DeleteTexture(a.Name);
                toRemove.Remove(a);
                loaded.Remove(a.Path);
                editorData.assets.Remove(a);
            }
        }
    }
}
