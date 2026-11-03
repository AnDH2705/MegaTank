using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace MegaTanks
{
    public sealed class BattleGame : MonoBehaviour
    {
        public static BattleGame Instance { get; private set; }
        [Header("MG / AA")]
        [FormerlySerializedAs("mgInterval")]
        [SerializeField, Min(.02f), Tooltip("Reload begins after the final shot of the salvo.")] float mgReloadTime = 1.25f;
        [SerializeField, Min(1)] int mgSalvoSize = 3;
        [SerializeField, Min(.01f)] float mgShotDelay = .1f;
        [SerializeField, Min(1)] float mgDamage = 6;
        [SerializeField, Min(1)] float mgMaxRange = 50;
        [SerializeField, Min(0), Tooltip("Downward acceleration in units per second squared.")] float mgProjectileGravity = .35f;
        [Header("Main Gun")]
        [FormerlySerializedAs("mainGunInterval")]
        [SerializeField, Min(.02f), Tooltip("Reload begins after the final shot of the salvo.")] float mainGunReloadTime = 1.4f;
        [SerializeField, Min(1)] int mainGunSalvoSize = 1;
        [SerializeField, Min(.01f)] float mainGunShotDelay = .1f;
        [SerializeField, Min(1)] float mainGunDamage = 45;
        [SerializeField, Min(1)] float mainGunMaxRange = 80;
        [SerializeField, Min(0), Tooltip("Downward acceleration in units per second squared.")] float mainGunProjectileGravity = 3;
        [SerializeField] Sprite cannonProjectileSprite;
        [SerializeField, Min(.01f), Tooltip("Cannon shell sprite width in world units; aspect ratio is preserved.")] float cannonProjectileWidth = .65f;
        public Sprite CannonProjectileSprite => cannonProjectileSprite;
        public float CannonProjectileWidth => cannonProjectileWidth;
        [Header("Enemy approach")]
        [SerializeField, Min(1), Tooltip("Horizontal distance from the tank anchor to the front of each wave.")] float enemySpawnDistance = 90;
        [SerializeField, Min(.1f)] float enemyCameraSpeed = 3;
        [SerializeField, Min(.1f), Tooltip("Camera speed while catching up to a different enemy wave.")] float enemyCameraWaveSwitchSpeed = 10;
        public float CurrentEnemyCameraSpeed => switchingCameraWave ? enemyCameraWaveSwitchSpeed : enemyCameraSpeed;
        public Camera TankCamera => battleCamera;
        public Camera EnemyCamera => enemyCamera;
        public EnemyEntity TrackedEnemy { get; private set; }
        [Header("Tank artwork")]
        [SerializeField] TankArtwork tankArtworkPrefab;
        [SerializeField, Min(.1f)] float tankScale = 1.25f;
        public const float ReferenceWidth = 1080;
        public const float ReferenceHeight = 1920;
        public const float GroundHeight = -3.05f;
        const float HeaderHeight = 200;
        const float FooterHeight = 300;
        public TankArtwork Artwork { get; private set; }
        public AutoTurret MachineGun { get; private set; }
        public AutoTurret MainGun { get; private set; }
        public bool Running => deployed && !paused && Hull > 0;
        public float Hull { get; private set; } = 100;
        public int Score { get; private set; }
        public int Wave { get; private set; }
        public int EnemyCount => enemies.Count;
        public bool Paused => paused;
        readonly List<EnemyEntity> enemies = new List<EnemyEntity>();
        readonly List<FlashEffect> effects = new List<FlashEffect>();
        Transform scenery;
        Camera battleCamera;
        Camera enemyCamera;
        int cameraWaveNumber;
        bool switchingCameraWave;
        bool deployed, paused;
        float nextWave;
        int best;
        GUIStyle text, heading, caption, centered, button;
        readonly Color navy = new Color(.035f, .065f, .105f);
        readonly Color slate = new Color(.11f, .17f, .23f);
        readonly Color cyan = new Color(.32f, .88f, .95f);
        readonly Color amber = new Color(1, .72f, .3f);
        readonly Color muted = new Color(.55f, .66f, .74f);

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            best = PlayerPrefs.GetInt("MegaTanks.Best2D", 0);
            Physics2D.queriesHitTriggers = true;
            BuildScene();
        }

        void BuildScene()
        {
            battleCamera = new GameObject("2D Battle Camera").AddComponent<Camera>();
            battleCamera.tag = "MainCamera";
            battleCamera.orthographic = true;
            battleCamera.transform.position = new Vector3(0, 0, -10);
            battleCamera.clearFlags = CameraClearFlags.SolidColor;
            battleCamera.backgroundColor = navy;
            enemyCamera = new GameObject("Nearest Enemy Camera").AddComponent<Camera>();
            enemyCamera.orthographic = true;
            enemyCamera.clearFlags = CameraClearFlags.SolidColor;
            enemyCamera.backgroundColor = navy;
            enemyCamera.depth = battleCamera.depth + 1;
            enemyCamera.transform.position = new Vector3(84.5f, 1.4f, -10);
            scenery = new GameObject("2D Tank and Battlefield").transform;
            for (int x = -14; x <= 116; x++)
                ShapeFactory.Create("Grid vertical", scenery, new Vector2(x, 0), new Vector2(.012f, 40), new Color(.075f, .115f, .16f), false, -5);
            for (int y = -20; y <= 20; y++)
                ShapeFactory.Create("Grid horizontal", scenery, new Vector2(51, y), new Vector2(130, .012f), new Color(.075f, .115f, .16f), false, -5);
            ShapeFactory.Create("Ground lane", scenery, new Vector2(51, GroundHeight), new Vector2(130, .04f), new Color(.25f, .34f, .4f));
            if (tankArtworkPrefab == null) throw new System.InvalidOperationException("Tank artwork is missing. Run MegaTanks > Import Tank Artwork.");
            Artwork = Instantiate(tankArtworkPrefab, scenery);
            Artwork.transform.localScale = Vector3.one * tankScale;
            // Anchor the enlarged treads to the ground instead of scaling them through it.
            Artwork.transform.localPosition = new Vector3(-5.5f, GroundHeight + 102f / 180 * tankScale, 0);
            MachineGun = CreateTurret("MG / AA", Artwork.MachineGun, EnemyKind.Flyer, mgReloadTime, mgDamage, mgSalvoSize, mgShotDelay, mgMaxRange, mgProjectileGravity);
            MainGun = CreateTurret("MAIN GUN", Artwork.MainGun, EnemyKind.Ground, mainGunReloadTime, mainGunDamage, mainGunSalvoSize, mainGunShotDelay, mainGunMaxRange, mainGunProjectileGravity);
            FitCamera();
        }

        AutoTurret CreateTurret(string label, GunArtwork visual, EnemyKind kind, float reload, float damage, int salvoSize, float shotDelay, float range, float gravity)
        {
            var turret = visual.gameObject.AddComponent<AutoTurret>();
            turret.Configure(label, kind, reload, damage, salvoSize, shotDelay, range, gravity);
            turret.BindArtwork(visual);
            return turret;
        }

        void FitCamera()
        {
            // Reserve the top and bottom safe area for HUD/ultimates, keeping the reference composition intact.
            Rect safe = Screen.safeArea;
            float uiScale = Mathf.Min(safe.width / ReferenceWidth, safe.height / ReferenceHeight);
            float bottom = safe.yMin + FooterHeight * uiScale;
            float height = safe.height - (FooterHeight + HeaderHeight) * uiScale;
            battleCamera.rect = new Rect(safe.xMin / Screen.width, bottom / Screen.height, safe.width * .5f / Screen.width, height / Screen.height);
            enemyCamera.rect = new Rect((safe.xMin + safe.width * .5f) / Screen.width, bottom / Screen.height, safe.width * .5f / Screen.width, height / Screen.height);
            float aspect = safe.width * .5f / Mathf.Max(1, height);
            battleCamera.orthographicSize = Mathf.Max(4.8f, 4.6f / aspect);
            enemyCamera.orthographicSize = battleCamera.orthographicSize;
            battleCamera.transform.position = new Vector3(Artwork.transform.position.x + .6f, 1.4f, -10);
            // Keep the horizon aligned even while paused or between waves.
            enemyCamera.transform.position = new Vector3(enemyCamera.transform.position.x, battleCamera.transform.position.y, battleCamera.transform.position.z);
        }

        EnemyEntity FindNearestEnemy()
        {
            EnemyEntity nearest = null;
            float distance = float.PositiveInfinity;
            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.Alive) continue;
                float candidate = (enemy.transform.position - Artwork.transform.position).sqrMagnitude;
                if (candidate < distance) { nearest = enemy; distance = candidate; }
            }
            return nearest;
        }

        void LateUpdate()
        {
            if (!Running) return;
            TrackedEnemy = FindNearestEnemy();
            if (TrackedEnemy == null) return;
            // Remember the last wave through the empty interval between waves.
            if (TrackedEnemy.WaveNumber != cameraWaveNumber)
            {
                cameraWaveNumber = TrackedEnemy.WaveNumber;
                switchingCameraWave = true;
            }
            float x = Mathf.MoveTowards(enemyCamera.transform.position.x, TrackedEnemy.transform.position.x, CurrentEnemyCameraSpeed * Time.deltaTime);
            enemyCamera.transform.position = new Vector3(x, battleCamera.transform.position.y, battleCamera.transform.position.z);
            if (Mathf.Approximately(x, TrackedEnemy.transform.position.x)) switchingCameraWave = false;
        }

        public void StartBattle()
        {
            Time.timeScale = 1;
            foreach (EnemyEntity enemy in enemies) if (enemy != null) Destroy(enemy.gameObject);
            enemies.Clear();
            foreach (Projectile2D projectile in FindObjectsOfType<Projectile2D>()) Destroy(projectile.gameObject);
            foreach (FlashEffect effect in effects) if (effect != null) Destroy(effect.gameObject);
            effects.Clear();
            MachineGun.ResetWeapon();
            MainGun.ResetWeapon();
            Artwork.ResetVisual();
            Hull = 100;
            Score = Wave = 0;
            paused = false;
            deployed = true;
            SpawnWave();
            TrackedEnemy = FindNearestEnemy();
            switchingCameraWave = false;
            cameraWaveNumber = TrackedEnemy != null ? TrackedEnemy.WaveNumber : 0;
            if (TrackedEnemy != null) enemyCamera.transform.position = new Vector3(TrackedEnemy.transform.position.x, battleCamera.transform.position.y, battleCamera.transform.position.z);
        }

        public EnemyEntity SpawnEnemy(EnemyKind kind, Vector2 position)
        {
            var entity = new GameObject(kind + " Enemy").AddComponent<EnemyEntity>();
            entity.transform.position = position;
            entity.Initialize(kind, Wave);
            enemies.Add(entity);
            return entity;
        }

        void SpawnWave()
        {
            Wave++;
            int ground = Mathf.Min(3 + Wave, 12);
            int flyers = Mathf.Min(1 + Wave, 8);
            float spawnX = Artwork.transform.position.x + enemySpawnDistance;
            for (int i = 0; i < ground; i++)
                SpawnEnemy(EnemyKind.Ground, new Vector2(spawnX + i * 1.7f, -2.25f + i % 2 * .25f));
            for (int i = 0; i < flyers; i++)
                SpawnEnemy(EnemyKind.Flyer, new Vector2(spawnX + i * 2, 4.8f + i % 2 * .65f));
        }

        public EnemyEntity FindTarget(Vector2 position, EnemyKind priority, float range)
        {
            EnemyEntity preferred = null, fallback = null;
            float preferredDistance = range * range, fallbackDistance = range * range;
            foreach (EnemyEntity enemy in enemies)
            {
                if (enemy == null || !enemy.Alive) continue;
                float distance = ((Vector2)enemy.transform.position - position).sqrMagnitude;
                if (enemy.Kind == priority && distance <= preferredDistance) { preferred = enemy; preferredDistance = distance; }
                if (distance <= fallbackDistance) { fallback = enemy; fallbackDistance = distance; }
            }
            return preferred != null ? preferred : fallback;
        }

        public void RemoveEnemy(EnemyEntity enemy, bool killed)
        {
            if (!enemies.Remove(enemy)) return;
            if (killed) Score += enemy.Kind == EnemyKind.Flyer ? 75 : 100;
            if (enemies.Count == 0) nextWave = 3;
        }

        public void DamageHull(float damage)
        {
            if (!Running) return;
            Hull = Mathf.Max(0, Hull - damage);
            if (Hull > 0) return;
            best = Mathf.Max(best, Score);
            PlayerPrefs.SetInt("MegaTanks.Best2D", best);
            PlayerPrefs.Save();
        }

        public void SetPaused(bool value)
        {
            if (!deployed || Hull <= 0) return;
            paused = value;
            Time.timeScale = value ? 0 : 1;
        }

        void Update()
        {
            FitCamera();
            if (Input.GetKeyDown(KeyCode.Escape)) SetPaused(!paused);
            if (!Running) return;
            if (enemies.Count == 0)
            {
                nextWave -= Time.deltaTime;
                if (nextWave <= 0) SpawnWave();
            }
        }

        public void Flash(Vector2 position, Color color, float size)
        {
            var effect = ShapeFactory.Create("Impact", scenery, position, Vector2.one * size, color, true, 85).gameObject.AddComponent<FlashEffect>();
            effects.Add(effect);
        }

        public void ForgetEffect(FlashEffect effect) { effects.Remove(effect); }
        void OnApplicationPause(bool value) { if (value && Running) SetPaused(true); }
        void OnDestroy() { Time.timeScale = 1; if (Instance == this) Instance = null; ShapeFactory.Release(); }

        void PrepareStyles()
        {
            if (text != null) return;
            text = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold };
            text.normal.textColor = new Color(.9f, .95f, .97f);
            heading = new GUIStyle(text) { fontSize = 48 };
            caption = new GUIStyle(text) { fontSize = 22, fontStyle = FontStyle.Normal };
            caption.normal.textColor = muted;
            centered = new GUIStyle(text) { alignment = TextAnchor.MiddleCenter, fontSize = 22 };
            button = new GUIStyle(GUI.skin.button) { fontSize = 32, fontStyle = FontStyle.Bold };
        }

        void Fill(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        void WorldLabel(Vector2 world, string content, Color color, bool badge = false, Camera view = null)
        {
            if (view == null) view = battleCamera;
            Vector3 viewport = view.WorldToViewportPoint(world);
            if (viewport.x < 0 || viewport.x > 1 || viewport.y < 0 || viewport.y > 1) return;
            Vector3 p = view.WorldToScreenPoint(world);
            Rect safe = Screen.safeArea;
            float scale = Mathf.Min(safe.width / ReferenceWidth, safe.height / ReferenceHeight);
            float x = (p.x - safe.xMin) / scale;
            x = Mathf.Clamp(x, (view.pixelRect.xMin - safe.xMin) / scale + 108, (view.pixelRect.xMax - safe.xMin) / scale - 108);
            float y = (Screen.height - p.y - (Screen.height - safe.yMax)) / scale;
            if (badge) Fill(new Rect(x - 92, y - 30, 184, 60), new Color(.035f, .065f, .105f, .92f));
            GUI.color = color;
            GUI.Label(new Rect(x - 108, y - 36, 216, 72), content, centered);
            GUI.color = Color.white;
        }

        void OnGUI()
        {
            PrepareStyles();
            Rect safe = Screen.safeArea;
            float scale = Mathf.Max(.1f, Mathf.Min(safe.width / ReferenceWidth, safe.height / ReferenceHeight));
            GUI.matrix = Matrix4x4.TRS(new Vector3(safe.xMin, Screen.height - safe.yMax, 0), Quaternion.identity, Vector3.one * scale);
            float width = safe.width / scale, height = safe.height / scale;
            Fill(new Rect(0, 0, width, HeaderHeight), navy);
            Fill(new Rect(24, 28, 5, 70), cyan);
            GUI.Label(new Rect(44, 18, width - 210, 70), "MEGATANKS", heading);
            GUI.Label(new Rect(46, 91, width - 210, 36), "2D / MOBILE DEFENSE", caption);
            GUI.Label(new Rect(40, 147, 250, 44), "WAVE " + Wave.ToString("00"), text);
            GUI.Label(new Rect(300, 151, 390, 38), EnemyCount + " HOSTILES REMAINING", caption);
            GUI.Label(new Rect(width - 280, 147, 260, 44), Score.ToString("000000") + " PTS", text);
            if (GUI.Button(new Rect(width - 144, 24, 112, 96), paused ? ">" : "II", button)) SetPaused(!paused);

            WorldLabel(Artwork.transform.TransformPoint(new Vector3(-1.35f, 4.73f, 0)), "SUBROOM 1\n[ EMPTY ]", muted, true);
            WorldLabel(Artwork.transform.TransformPoint(new Vector3(-1.35f, 2.66f, 0)), "SUBROOM 2\n[ EMPTY ]", muted, true);
            WorldLabel((Vector2)MachineGun.transform.position + Vector2.up * .9f, "MG / AA", cyan);
            WorldLabel((Vector2)MainGun.transform.position + new Vector2(1, 1.1f), "MAIN GUN", amber);
            WorldLabel(new Vector2(-5.9f, -3.55f), "HULL  " + Mathf.CeilToInt(Hull) + "%", cyan);
            WorldLabel(new Vector2(enemyCamera.transform.position.x, 6.4f), "AIR APPROACH", amber, false, enemyCamera);
            WorldLabel(new Vector2(enemyCamera.transform.position.x, -3.55f), "GROUND APPROACH", muted, false, enemyCamera);
            Fill(new Rect(width / 2 - 2, HeaderHeight, 4, height - HeaderHeight - FooterHeight), new Color(.32f, .47f, .56f));
            GUI.Label(new Rect(28, HeaderHeight + 18, width / 2 - 48, 40), "TANK VIEW", caption);
            GUI.Label(new Rect(width / 2 + 28, HeaderHeight + 18, width / 2 - 48, 40), "NEAREST ENEMY", caption);
            if (TrackedEnemy != null)
                GUI.Label(new Rect(width / 2 + 28, HeaderHeight + 58, width / 2 - 48, 40), Vector2.Distance(Artwork.transform.position, TrackedEnemy.transform.position).ToString("0.0") + " UNITS FROM TANK", caption);

            float bottom = height - FooterHeight;
            Fill(new Rect(0, bottom, width, FooterHeight), navy);
            Fill(new Rect(24, bottom, width - 48, 1), new Color(.22f, .31f, .38f));
            GUI.Label(new Rect(28, bottom + 12, width - 56, 36), "ULTIMATES", caption);
            float slot = (width - 48 - 12 * 4) / 5;
            float groupWidth = slot * 5 + 12 * 4;
            float left = (width - groupWidth) * .5f;
            for (int i = 0; i < 5; i++)
            {
                float x = left + i * (slot + 12);
                Fill(new Rect(x, bottom + 60, slot, 136), slate);
                Fill(new Rect(x, bottom + 60, slot, 3), new Color(.32f, .4f, .47f));
                GUI.Label(new Rect(x, bottom + 78, slot, 44), "ULTIMATE " + (i + 1), centered);
                GUI.Label(new Rect(x, bottom + 135, slot, 34), "PLACEHOLDER", new GUIStyle(caption) { alignment = TextAnchor.MiddleCenter, fontSize = 19 });
            }
            GUI.Label(new Rect(24, bottom + 220, width - 48, 68), "MG-AA: " + mgSalvoSize + " x " + mgDamage + " DMG / " + mgReloadTime.ToString("0.00") + "s RELOAD\nMAIN: " + mainGunSalvoSize + " x " + mainGunDamage + " DMG / " + mainGunReloadTime.ToString("0.0") + "s RELOAD", new GUIStyle(caption) { alignment = TextAnchor.MiddleCenter });

            if (Running && EnemyCount == 0)
                GUI.Label(new Rect(width / 2 - 200, HeaderHeight + 30, 400, 60), "NEXT WAVE IN " + Mathf.CeilToInt(nextWave), centered);
            if (deployed && !paused && Hull > 0) return;
            float panelWidth = Mathf.Min(width - 64, 900);
            Fill(new Rect((width - panelWidth) / 2, height / 2 - 180, panelWidth, 360), new Color(.035f, .065f, .105f, .97f));
            Fill(new Rect((width - panelWidth) / 2, height / 2 - 180, panelWidth, 4), Hull <= 0 ? amber : cyan);
            GUI.Label(new Rect((width - panelWidth) / 2 + 20, height / 2 - 145, panelWidth - 40, 70), !deployed ? "BATTLE STATION READY" : paused ? "BATTLE PAUSED" : "HULL DESTROYED", new GUIStyle(heading) { alignment = TextAnchor.MiddleCenter, fontSize = 36 });
            GUI.Label(new Rect((width - panelWidth) / 2 + 20, height / 2 - 60, panelWidth - 40, 60), Hull <= 0 ? "SCORE " + Score + "  /  BEST " + best : "Both turrets aim and fire automatically.", new GUIStyle(caption) { alignment = TextAnchor.MiddleCenter, fontSize = 26 });
            GUI.backgroundColor = cyan;
            if (GUI.Button(new Rect(width / 2 - 280, height / 2 + 40, 560, 96), !deployed ? "START BATTLE" : paused ? "RESUME" : "TRY AGAIN", button))
            { if (paused) SetPaused(false); else StartBattle(); }
            GUI.backgroundColor = Color.white;
        }
    }

    public sealed class FlashEffect : MonoBehaviour
    {
        float lifetime = .16f;
        void Update()
        {
            if (BattleGame.Instance == null || !BattleGame.Instance.Running) return;
            lifetime -= Time.deltaTime;
            transform.localScale *= 1 + Time.deltaTime * 5;
            var sprite = GetComponent<SpriteRenderer>();
            Color color = sprite.color;
            color.a = Mathf.Clamp01(lifetime / .16f);
            sprite.color = color;
            if (lifetime <= 0)
            {
                BattleGame.Instance.ForgetEffect(this);
                Destroy(gameObject);
            }
        }
    }
}
