using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class GameWindowProperty
    {
        public float topPanelSize = 50;
        public float bottomPanelSize = 25;

        public float bottomPanelPercent = 0.25f;
        public float leftPanelPercent = 0.15f;
        public float rightPanelPercent = 0.20f;

        public float origBottomPanelPercent;
        public float origLeftPanelPercent;
        public float origRightPanelPercent;

        public Vector2 gameWindowPos;
        public Vector2 gameWindowSize;

        public GameWindowProperty()
        {
            origBottomPanelPercent = bottomPanelPercent;
            origLeftPanelPercent = leftPanelPercent;
            origRightPanelPercent = rightPanelPercent;
        }
    }
}
