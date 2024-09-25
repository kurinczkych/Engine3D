using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public interface ISelectable
    {
        public bool isSelected { get; set; }
        public Transformation transformation { get; set; }
    }
}
