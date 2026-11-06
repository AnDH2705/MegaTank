using UnityEngine;

namespace MegaTanks
{
    public sealed class MortarArtwork : MonoBehaviour
    {
        [SerializeField] Transform barrel, muzzle;
        Vector3 rest;
        bool initialized;
        float recoil;
        public Transform Muzzle => muzzle;
        public void Configure(Transform tube, Transform socket) { barrel = tube; muzzle = socket; }
        void Initialize() { if (initialized) return; rest = barrel.localPosition; initialized = true; }
        public void ResetVisual() { Initialize(); recoil = 0; barrel.localPosition = rest; }
        public void Fire() { Initialize(); recoil = .08f; }
        void LateUpdate()
        {
            if (BattleGame.Instance == null || !BattleGame.Instance.Running) return;
            Initialize();
            recoil = Mathf.MoveTowards(recoil, 0, .4f * Time.deltaTime);
            barrel.localPosition = rest + Vector3.down * recoil;
        }
    }
}
