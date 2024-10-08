using ImGuiNET;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class EditorData
    {
        #region Data
        public bool recalculateObjects = true;

        public ImGuiIOPtr io;
        public bool uiHasMouse = false;

        public Object? selectedItem;
        public int anyObjectHovered = -1;

        public bool[] mouseTypes = new bool[3];

        public AssetStoreManager assetStoreManager;

        public int currentAssetTexture = 0;
        public List<Asset> AssetTextures;

        public GameWindowProperty gameWindow;

        public Vector2 gizmoWindowPos = Vector2.Zero;
        public Vector2 gizmoWindowSize = Vector2.Zero;

        public Physx physx;

        public bool runParticles = false;
        #endregion

        #region Properties 
        public bool windowResized = false;
        public MouseCursor mouseType = MouseCursor.Default;

        public bool manualCursor = false;
        public bool isGameFullscreen = false;
        public bool justSetGameState = false;
        public bool isPaused = false;
        public GameState prevGameState;
        private GameState _gameRunning;
        public GameState gameRunning
        {
            get
            {
                return _gameRunning;
            }
            set
            {
                prevGameState = _gameRunning;
                _gameRunning = value;
            }
        }
        #endregion

        public int animType = 0;
        public int animEndType = 0;
        public int matrixType = 5;

        public EditorData()
        {
            _gameRunning = GameState.Stopped;
        }
    }
}
