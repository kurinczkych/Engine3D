using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public partial class Engine
    {
        private void MouseMoving()
        {
            if (firstMove)
            {
                lastPos = new Vector2(MouseState.X, MouseState.Y);
                firstMove = false;
            }
            else
            {
                deltaX = MouseState.X - lastPos.X;
                deltaY = MouseState.Y - lastPos.Y;
                if (deltaX != 0 || deltaY != 0)
                {
                    lastPos = new Vector2(MouseState.X, MouseState.Y);
                }
            }
        }
    }
}
