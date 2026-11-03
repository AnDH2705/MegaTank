using UnityEngine;

namespace MegaTanks
{
    public sealed class GunArtwork : MonoBehaviour
    {
        [SerializeField] Transform recoilPart;
        [SerializeField] Transform muzzle;
        [SerializeField] SpriteClipPlayer muzzleEffect;
        [SerializeField] float artworkAngle;
        [SerializeField] float recoilDistance = .06f;
        Vector3 restPosition;
        bool initialized;
        float recoil;
        public Transform Muzzle => muzzle;
        public Vector2 FireDirection => Quaternion.Euler(0, 0, artworkAngle) * transform.right;
        public bool EffectPlaying => muzzleEffect != null && muzzleEffect.Playing;

        public void Configure(Transform barrel, Transform socket, SpriteClipPlayer effect, float angle, float distance)
        {
            recoilPart = barrel;
            muzzle = socket;
            muzzleEffect = effect;
            artworkAngle = angle;
            recoilDistance = distance;
        }

        void Initialize()
        {
            if (initialized) return;
            restPosition = recoilPart.localPosition;
            initialized = true;
        }

        public void Aim(Vector2 direction)
        {
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - artworkAngle);
        }

        public void Fire()
        {
            Initialize();
            recoil = recoilDistance;
            muzzleEffect.Play();
        }

        public void ResetVisual()
        {
            Initialize();
            recoil = 0;
            recoilPart.localPosition = restPosition;
            transform.localRotation = Quaternion.identity;
            muzzleEffect.Stop();
        }

        void LateUpdate()
        {
            if (BattleGame.Instance == null || !BattleGame.Instance.Running) return;
            Initialize();
            recoil = Mathf.MoveTowards(recoil, 0, recoilDistance / .16f * Time.deltaTime);
            Vector3 axis = Quaternion.Euler(0, 0, artworkAngle) * Vector3.right;
            recoilPart.localPosition = restPosition - axis * recoil;
        }
    }
}
