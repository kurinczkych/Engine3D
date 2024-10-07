using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class EngineData
    {
        public AssetManager assetManager;
        public TextureManager textureManager;
        public GizmoManager gizmoManager;

        public List<Object> objects = new List<Object>();

        public EngineData()
        {
            
        }
    }
}
