using UnityEngine;

namespace MegaTanks
{
    public sealed class TankArtwork : MonoBehaviour
    {
        [SerializeField] GunArtwork machineGun;
        [SerializeField] GunArtwork mainGun;
        [SerializeField, HideInInspector] GunArtwork replacementMortarGun;
        [SerializeField, HideInInspector] GunArtwork mortarVisualPrefab;
        [SerializeField, HideInInspector] GunArtwork replacementMachineGun;
        [SerializeField, HideInInspector] GunArtwork machineGunVisualPrefab;
        [SerializeField] MortarArtwork mortar;
        [SerializeField] TankTrackAnimator tracks;
        [SerializeField] Transform body;
        [SerializeField] SpriteClipPlayer[] exhaustPuffs;
        Vector3 bodyRest;
        SpriteRenderer[] wheels;
        Quaternion[] wheelRest;
        bool initialized;
        float smokeTimer;
        int nextPuff;
        public GunArtwork MachineGun => replacementMachineGun != null ? replacementMachineGun : machineGun;
        public EnemyEntity EnemyOwner { get; set; }
        public GunArtwork MainGun => mainGun;
        public GunArtwork MissilePack => replacementMortarGun;
        public MortarArtwork Mortar => mortar;
        public void BindMortar(MortarArtwork visual) { mortar = visual; }
        public TankTrackAnimator Tracks => tracks;
        public void BindTracks(TankTrackAnimator animation) { tracks = animation; }

        public void EquipMachineGunVisual(GunDefinition definition)
        {
            var prefab = definition != null ? definition.artworkPrefab : null;
            if (prefab == machineGunVisualPrefab) return;
            machineGun.ResetVisual();
            // Keep the mount and its AutoTurret alive while hiding the original artwork.
            foreach (Transform part in machineGun.transform) part.gameObject.SetActive(prefab == null);
            if (replacementMachineGun != null)
            {
                replacementMachineGun.gameObject.SetActive(false);
                Destroy(replacementMachineGun.gameObject);
            }
            replacementMachineGun = null;
            machineGunVisualPrefab = prefab;
            if (prefab == null) return;
            replacementMachineGun = Instantiate(prefab, machineGun.transform.parent, false);
            replacementMachineGun.transform.localPosition = machineGun.transform.localPosition;
            foreach (var part in replacementMachineGun.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = machineGun.gameObject.layer;
            replacementMachineGun.ResetVisual();
        }

        public void EquipMortarVisual(GunDefinition definition)
        {
            var prefab = definition != null ? definition.artworkPrefab : null;
            if (prefab == mortarVisualPrefab) return;
            mortar.ResetVisual();
            foreach (Transform part in mortar.transform) part.gameObject.SetActive(prefab == null);
            if (replacementMortarGun != null)
            {
                replacementMortarGun.gameObject.SetActive(false);
                Destroy(replacementMortarGun.gameObject);
            }
            replacementMortarGun = null;
            mortarVisualPrefab = prefab;
            if (prefab == null) return;
            replacementMortarGun = Instantiate(prefab, mortar.transform.parent, false);
            replacementMortarGun.transform.localPosition = mortar.transform.localPosition;
            foreach (var part in replacementMortarGun.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = mortar.gameObject.layer;
            replacementMortarGun.ResetVisual();
        }

        public void Configure(GunArtwork mg, GunArtwork cannon, Transform hull, SpriteClipPlayer[] smoke)
        {
            machineGun = mg;
            mainGun = cannon;
            body = hull;
            exhaustPuffs = smoke;
        }

        public void ResetVisual()
        {
            if (!initialized)
            {
                bodyRest = body.localPosition;
                wheels = System.Array.FindAll(GetComponentsInChildren<SpriteRenderer>(), renderer => renderer.name.Contains("_wheel_"));
                wheelRest = new Quaternion[wheels.Length];
                for (int i = 0; i < wheels.Length; i++) wheelRest[i] = wheels[i].transform.localRotation;
                initialized = true;
            }
            for (int i = 0; i < wheels.Length; i++) wheels[i].transform.localRotation = wheelRest[i];
            body.localPosition = bodyRest;
            MachineGun.ResetVisual();
            mainGun.ResetVisual();
            if(replacementMortarGun!=null)replacementMortarGun.ResetVisual();
            if (mortar != null) mortar.ResetVisual();
            if (tracks != null) tracks.ResetVisual();
            foreach (var puff in exhaustPuffs) puff.Stop();
            smokeTimer = 0;
            nextPuff = 0;
        }

        void Update()
        {
            var game = BattleGame.Instance;
            if (game == null) return;
            float travel = EnemyOwner != null ? -EnemyOwner.Velocity.x * Time.deltaTime : game.TankTravelThisFrame;
            if (!game.Running && (EnemyOwner != null || travel == 0)) return;
            if (!initialized) ResetVisual();
            body.localPosition = bodyRest + Vector3.up * (Mathf.Sin(Time.time * 17) * .007f);
            foreach (var wheel in wheels)
            {
                float radius = wheel.sprite.bounds.size.y * Mathf.Abs(wheel.transform.lossyScale.y) * .5f;
                wheel.transform.Rotate(0, 0, -travel / Mathf.Max(.01f, radius) * Mathf.Rad2Deg);
            }
            smokeTimer -= Time.deltaTime;
            if (smokeTimer <= 0 && exhaustPuffs.Length > 0)
            {
                exhaustPuffs[nextPuff].Play(continueOutsideCombat: EnemyOwner == null);
                nextPuff = (nextPuff + 1) % exhaustPuffs.Length;
                smokeTimer = .3f;
            }
        }
    }
}
