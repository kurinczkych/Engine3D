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

    public class Asset
    {
        public int Id;
        public string Name;
        public string Path;
        public AssetType Type;

        public Asset(int id, string name, string path, AssetType assetType)
        {
            Id = id;
            Name = name;
            Path = path;
            Type = assetType;
        }
    }
}
