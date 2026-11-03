using UnityEngine;

namespace MegaTanks
{
    public sealed class TankArtwork : MonoBehaviour
    {
        [SerializeField] GunArtwork machineGun;
        [SerializeField] GunArtwork mainGun;
        [SerializeField] Transform body;
        [SerializeField] SpriteClipPlayer[] exhaustPuffs;
        Vector3 bodyRest;
        bool initialized;
        float smokeTimer;
        int nextPuff;
        public GunArtwork MachineGun => machineGun;
        public GunArtwork MainGun => mainGun;

        public void Configure(GunArtwork mg, GunArtwork cannon, Transform hull, SpriteClipPlayer[] smoke)
        {
            machineGun = mg;
            mainGun = cannon;
            body = hull;
            exhaustPuffs = smoke;
        }

        public void ResetVisual()
        {
            if (!initialized) { bodyRest = body.localPosition; initialized = true; }
            body.localPosition = bodyRest;
            machineGun.ResetVisual();
            mainGun.ResetVisual();
            foreach (var puff in exhaustPuffs) puff.Stop();
            smokeTimer = 0;
            nextPuff = 0;
        }

        void Update()
        {
            if (BattleGame.Instance == null || !BattleGame.Instance.Running) return;
            if (!initialized) ResetVisual();
            body.localPosition = bodyRest + Vector3.up * (Mathf.Sin(Time.time * 17) * .007f);
            smokeTimer -= Time.deltaTime;
            if (smokeTimer <= 0 && exhaustPuffs.Length > 0)
            {
                exhaustPuffs[nextPuff].Play();
                nextPuff = (nextPuff + 1) % exhaustPuffs.Length;
                smokeTimer = .3f;
            }
        }
    }
}
