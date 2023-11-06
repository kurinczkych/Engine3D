using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public partial class Engine
    {
        private void FrustumCalculating()
        {
            if (editorData.gameRunning == GameState.Running || firstRun)
            {
                foreach (Object obj in objects)
                {
                    obj.GetMesh().CalculateFrustumVisibility();
                }
                firstRun = false;
            }
        }
    }
}
