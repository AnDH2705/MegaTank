using UnityEngine;
namespace MegaTanks
{
    public sealed class BattleCameraPan : MonoBehaviour
    {
        BattleGame game;
        Camera view;
        public float MinX { get; private set; }
        public float MaxX { get; private set; }
        public float X { get; private set; }
        public float StartX { get; private set; }
        bool resetPending;
        float playerSpawnX,enemySpawnX;
        public bool CanPan => game!=null && game.Running && (game.UI==null || game.UI.Popup=="");
        public void Configure(BattleGame battle,float playerSpawn,float enemySpawn)
        { game=battle;view=GetComponent<Camera>();ResetToSpawn(playerSpawn,enemySpawn); }
        public void ResetToSpawn(float playerSpawn,float enemySpawn)
        { playerSpawnX=playerSpawn;enemySpawnX=enemySpawn;MinX=Mathf.Min(playerSpawn,enemySpawn);MaxX=Mathf.Max(playerSpawn,enemySpawn);resetPending=true;SetX(playerSpawn); }
        public void UpdateFraming(float viewWidth)
        {
            StartX=playerSpawnX+viewWidth*(.5f-game.TankStartViewportX);
            MinX=Mathf.Min(playerSpawnX,enemySpawnX,StartX);MaxX=Mathf.Max(playerSpawnX,enemySpawnX,StartX);
            if(resetPending){SetX(StartX);resetPending=false;}
            else SetX(X);
        }
        public void SetEnemySpawn(float enemySpawn)
        { enemySpawnX=enemySpawn;MinX=Mathf.Min(playerSpawnX,enemySpawnX,StartX);MaxX=Mathf.Max(playerSpawnX,enemySpawnX,StartX);SetX(X); }
        public void PanByPixels(float horizontalDelta)
        {
            if(!CanPan || view.pixelWidth<=0)return;
            float unitsPerPixel=2*view.orthographicSize*view.aspect/view.pixelWidth;
            // Grab-and-drag: the battlefield follows the finger, so the camera moves oppositely.
            SetX(X-horizontalDelta*unitsPerPixel);
        }
        void SetX(float value)
        {
            X=Mathf.Clamp(value,MinX,MaxX);
            transform.position=new Vector3(X,transform.position.y,transform.position.z);
        }
    }
}
