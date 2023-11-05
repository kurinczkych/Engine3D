using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D.Classes.GPU
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DrawArraysIndirectCommand
    {
        public uint count;
        public uint instanceCount;
        public uint first;
        public uint baseInstance;
    }
}
