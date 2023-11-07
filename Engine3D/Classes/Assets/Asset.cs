using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public enum AssetType
    {
        Texture,
        Model,
        Audio,
        Unknown
    }
    public enum AssetTypeEditor
    {
        Store,
        UI,
        Full,
        File,
        Unknown
    }

    public class Asset
    {
        public static int CurrentId;

        public int Id;
        public string Name;
        public string Path;
        public string WebPathFull;
        public AssetType Type;
        public AssetTypeEditor EditorType;

        public Asset(int id, string name, string path, AssetType assetType, AssetTypeEditor editorType)
        {
            Id = id;
            if (Id > CurrentId)
                CurrentId = Id;

            Name = name;
            Path = path;
            Type = assetType;
            EditorType = editorType;
        }
    }
}
