using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class Scene
    {
        public List<Object> objects;
        public List<Object> _meshObjects;
        public List<Object> _instObjects;

        public Scene()
        {
            objects = new List<Object>();
            _meshObjects = new List<Object>();
            _instObjects = new List<Object>();
        }
    }
}
