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
        BattleVfxEffect activeMuzzle;
        public Transform Muzzle => muzzle;
        public Vector2 FireDirection => transform.TransformVector(Quaternion.Euler(0, 0, artworkAngle) * Vector3.right).normalized;
        public bool EffectPlaying => (activeMuzzle!=null&&activeMuzzle.gameObject.activeInHierarchy)||(muzzleEffect != null && muzzleEffect.Playing);

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
            Vector3 local = transform.parent != null ? transform.parent.InverseTransformVector(direction) : (Vector3)direction;
            transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg - artworkAngle);
        }

        public void Fire(bool heavy=false)
        {
            Initialize();
            recoil = recoilDistance;
            StopMuzzle();
            muzzleEffect.Stop();
            activeMuzzle=BattleGame.Instance?.Vfx?.Muzzle(muzzle,FireDirection,heavy);
            if(activeMuzzle==null)muzzleEffect.Play();
        }

        public void ResetVisual()
        {
            Initialize();
            recoil = 0;
            recoilPart.localPosition = restPosition;
            transform.localRotation = Quaternion.identity;
            muzzleEffect.Stop();
            StopMuzzle();
        }

        void StopMuzzle()
        {
            if(activeMuzzle==null)return;
            activeMuzzle.gameObject.SetActive(false);Destroy(activeMuzzle.gameObject);activeMuzzle=null;
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
