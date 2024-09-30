using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public interface IComponent
    {
    }

    public class ComponentType
    {
        public string name;
        public string baseClass;
        public Type type;

        public ComponentType(string name, string baseClass, Type type)
        {
            this.name = name;
            this.baseClass = baseClass;
            this.type = type;
        }
    }
}
