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
            if (gameState == GameState.Running || firstRun)
            {
                foreach (Object obj in scene.objects)
                {
                    BaseMesh? mesh = (BaseMesh?)obj.GetComponent<BaseMesh>();
                    if(mesh != null)
                        mesh.CalculateFrustumVisibility();
                }
                firstRun = false;
            }
        }
    }
}
