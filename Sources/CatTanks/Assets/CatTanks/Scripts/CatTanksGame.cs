using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace CatTanks
{
    public sealed partial class CatTanksGame : MonoBehaviour
    {
        public Catalog db;
        public Profile profile;
        public Battle battle;
        public BattleView view;
        public bool inBattle, paused, muted;
        [SerializeField] bool useNativeWeaponVfx = true;
        [SerializeField] bool useWebWeaponVfx = true;
        public string page = "march", tab = "cats", modal = "", toast = "";
        public int selectedCat, selectedTurret, selectedPet, bookTab, selectedOpponent;
        float toastUntil, soundAt;
        bool resolved;
        Dictionary<string, int> receipt;
        AudioSource audioSource;
        AudioClip shot, boom;
        Vector2 scroll;
        Font font;
        StudioTextStyle label, title, subtitle, small;
        readonly Color ink = Catalog.Color("#d9f2ff"), cyan = Catalog.Color("#61e3f4"), gold = Catalog.Color("#ffcf75");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureGame() {if (!PreviewIsolation.IsActive && !FindObjectOfType<CatTanksGame>()) new GameObject("CatTanks").AddComponent<CatTanksGame>();}
        void Awake()
        {
#if UNITY_EDITOR
            EditorTestProfile.Configure();
#endif
            if (PreviewIsolation.IsActive) { enabled = false; return; }
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 144;
            db = Catalog.Load(); profile = Profile.Load(db); muted = PlayerPrefs.GetInt("CatTanks.Muted", 0) != 0;
            var cam = Camera.main;
            if (!cam) {var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); go.tag = "MainCamera"; cam = go.GetComponent<Camera>();}
            cam.backgroundColor = Catalog.Color("#041828"); cam.clearFlags = CameraClearFlags.SolidColor; cam.farClipPlane = 3000;
            if (!FindObjectOfType<Light>()) {var light = new GameObject("Moonlight", typeof(Light)).GetComponent<Light>(); light.type = LightType.Directional; light.intensity = .7f; light.transform.eulerAngles = new Vector3(35, -30, 0);}
            view = new BattleView(cam, profile.selected); Preview();
            audioSource = gameObject.AddComponent<AudioSource>(); shot = Tone("Cannon", 110, .085f); boom = Tone("Rocket", 60, .2f);
            var fps=new GameObject("Frame Rate Overlay");fps.transform.SetParent(transform,false);fps.AddComponent<FrameRateOverlay>();
        }
        AudioClip Tone(string name, float hz, float length)
        {
            int count = Mathf.CeilToInt(22050 * length); float[] samples = new float[count]; var random = new System.Random(23);
            for (int i = 0; i < count; i++) {float t = i / 22050f, decay = Mathf.Pow(1 - i / (float)count, 2); samples[i] = (Mathf.Sin(t * hz * Mathf.PI * 2 * (1 - t * 2)) * .7f + (float)(random.NextDouble() - .5) * .3f) * decay * .3f;}
            var clip = AudioClip.Create(name, count, 1, 22050, false); clip.SetData(samples, 0); return clip;
        }
        void PlayShot(Weapon w)
        {
            if (muted || Time.unscaledTime < soundAt) return;
            soundAt = Time.unscaledTime + .08f; audioSource.PlayOneShot(w.splash > 0 ? boom : shot, .25f);
        }
        // Ở màn chính pháo đài đứng gần giữa (giới hạn phải của playerX) để không bị các nút hai bên che.
        const float HubFortressX = 550;
        void RebuildHubScene() {view?.Dispose(); view = new BattleView(Camera.main, profile.selected); Preview();}
        void Preview()
        {
            battle = new Battle(db, profile, profile.selected); battle.playerX = HubFortressX; battle.Step(.001f); view.Render(battle);
        }
        public void StartBattle(bool classic = false, bool arena = false)
        {
            modal = ""; paused = false; resolved = false; receipt = null; inBattle = true;
            battle = new Battle(db, profile, arena ? Mathf.Min(20, Mathf.Max(1, profile.highestCleared)) : profile.selected, classic, arena, selectedOpponent); battle.onFire = PlayShot;
            view.Dispose(); view = new BattleView(Camera.main, classic ? 1 : battle.stage.index, useNativeWeaponVfx, useWebWeaponVfx);
            profile.Save(); view.Render(battle);
        }
        void Update()
        {
            using(new GamePerformance.Scope(GamePerformance.Area.Update))TickGame();
        }
        void TickGame()
        {
            if (UnityEngine.EventSystems.EventSystem.current && UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject && UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.GetComponent<UnityEngine.UI.InputField>() is UnityEngine.UI.InputField input && input.isFocused) return;
            if (Input.GetKeyDown(KeyCode.F)) Screen.fullScreen = !Screen.fullScreen;
            if (Input.GetKeyDown(KeyCode.Escape)) {if (!string.IsNullOrEmpty(modal)) modal = ""; else if (inBattle && !resolved) paused = !paused; else if (!inBattle) { page = "march"; tab = "cats"; }}
            else if (Input.GetKeyDown(KeyCode.B)) ToggleBook();
            if (!inBattle) {view.Resize(); return;}
            bool spaceForPause = Input.GetKeyDown(KeyCode.Space) && !(UnityEngine.EventSystems.EventSystem.current && UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject);
            if ((Input.GetKeyDown(KeyCode.P) || spaceForPause) && !resolved && modal == "") paused = !paused;
            if (!paused && string.IsNullOrEmpty(modal) && battle.state == BattleState.Playing)
            {
                battle.move = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
                for (int i = 0; i < battle.skills.Count && i < 6; i++) if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i))) battle.UseSkill(battle.skills[i].id);
                if (battle.CaptureEnabled && Input.GetKeyDown(KeyCode.C)) battle.Catch(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
                if (Input.GetMouseButtonDown(0))
                {
                    Vector2 mouse = Input.mousePosition; Vector2 ui = ToUI(new Vector2(mouse.x, Screen.height - mouse.y));
                    if (ui.y > 170 && ui.y < 680 && !(UnityEngine.EventSystems.EventSystem.current && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()))
                    {
                        Vector2 world = Camera.main.ScreenToWorldPoint(mouse);
                        var e = battle.enemies.Where(v => !v.dead && Vector2.Distance(v.position, world) < v.def.size * .55f).OrderBy(v => Vector2.Distance(v.position, world)).FirstOrDefault();
                        if (e != null) battle.targetId = e.id;
                    }
                }
                battle.Step(Time.unscaledDeltaTime);
                if (battle.message != null) {Notify(battle.message); battle.message = null;}
            }
            view.Render(battle);
            if (!resolved && battle.state != BattleState.Playing) Resolve();
        }
        void Resolve() {receipt = battle.Finish(); resolved = true;}
        void Hub()
        {
            if (!resolved && inBattle) {battle.state = BattleState.Retreated; Resolve();}
            inBattle = false; paused = false; modal = ""; RebuildHubScene();
        }
        void OnApplicationPause(bool pause) {if (PreviewIsolation.IsActive) return; if (pause && inBattle) paused = true; if (pause) profile?.Save();}
        void OnApplicationQuit() {if (!PreviewIsolation.IsActive) profile?.Save();}
        void OnDestroy() {view?.Dispose(); if (shot) Destroy(shot); if (boom) Destroy(boom);}
        void Notify(string value) {toast = value; toastUntil = Time.unscaledTime + 3.5f;}
        bool Pay(params (string key, int amount)[] cost) {if (profile.Spend(cost)) return true; Notify("Thiếu tài nguyên"); return false;}
        void Changed(string message = "Đã lưu") {profile.Save(); Notify(message); if (!inBattle) RebuildHubScene();}
        public static Vector2 ToUI(Vector2 screen)
        {
            Rect safe = Screen.safeArea; float scale = Mathf.Min(safe.width / 1600f, safe.height / 900f);
            return (screen - new Vector2(safe.x + (safe.width - 1600 * scale) / 2, Screen.height - safe.yMax + (safe.height - 900 * scale) / 2)) / scale;
        }
        static string N(float n) => n.ToString("N0");
        string Cost(params (string key, int amount)[] costs) => string.Join(" · ", costs.Select(c => N(c.amount) + " " + Currency(c.key)));
        static string Currency(string id)
        {
            switch (id) {case "coins": return "xu"; case "gem": return "ngọc"; case "spiritDust": return "bụi"; case "catXp": return "XP mèo"; case "forgeStone": return "đá rèn"; case "soulEssence": return "tinh chất"; case "summonScroll": return "vé"; case "skillCore": return "lõi"; case "beastCage": return "lồng thú"; case "moonCage": return "lồng trăng"; case "monsterChow": return "thức ăn"; case "arenaToken": return "huy hiệu"; case "accountXp": return "XP chỉ huy"; default: return id;}
        }
        string Rewards(Dictionary<string, int> values) => string.Join("    ", values.Where(v => v.Value > 0).Select(v => "+" + N(v.Value) + " " + Currency(v.Key)));
        void Prebattle()
        {
            var s = db.stages[profile.selected - 1];
            PopupFrame(200, 110, 1200, 690, $"Ải {s.index:00} · {s.name}", true);
            Surface(232, 160, 548, 330, Color.white, catsFrame: "Roster");
            Art(246, 174, 520, 302, StagePreview(s));
            if (s.boss) Text(258, 436, 300, "TRẬN TRÙM", subtitle, 40);
            int moonCages = Mathf.Min(5, profile.Get("moonCage")), beastCages = Mathf.Min(5 - moonCages, profile.Get("beastCage"));
            PrebattleStat(160, "Interface/Cats/PowerIcon", "LỰC CHIẾN ĐỀ NGHỊ", $"{N(s.recommendedPower)}   ·   đội bạn {N(profile.Power(db))}");
            PrebattleStat(272, "Interface/Cats/Heart", "PHÁO ĐÀI", $"{N(profile.MaxHp)} HP   ·   {profile.FloorCount} tầng");
            PrebattleStat(384, "item-beastCage", "LỒNG MANG THEO (TỐI ĐA 5)", $"{beastCages} lồng thú   ·   {moonCages} lồng trăng");
            Text(232, 500, 1136, $"Bắt quái: quái thường từ {Battle.CaptureHealthThreshold(false):0%} HP trở xuống · trùm dưới {Battle.CaptureHealthThreshold(true):0%} HP, dùng Lồng trăng (Shift+C)", small, 28, align: TextAnchor.UpperCenter);
            Surface(232, 538, 1136, 136, Color.white, catsFrame: "Roster");
            for (int i = 0; i < profile.FloorCount; i++)
            {
                float x = 232 + (1136 - (profile.FloorCount * 221 - 16)) / 2f + i * 221;
                Surface(x, 550, 205, 112, Color.white, catsFrame: "Tile");
                Art(x + 37, 556, 131, 70, profile.floors[i].turret);
                Text(x + 10, 626, 185, "Tầng " + (i + 1), small, 28, align: TextAnchor.UpperCenter);
            }
            if (IllustratedButton(232, 698, 420, 82, "Chỉnh đội hình", "cat-0", normalSpriteKey: "blue")) {modal = ""; page = "barracks"; tab = "squad";}
            if (IllustratedButton(680, 690, 688, 96, "XUẤT KÍCH", "Battle", primary: true)) StartBattle();
            Enlarge(30);
        }
        void PrebattleStat(float y, string icon, string caption, string value)
        {
            Surface(806, y, 562, 100, Color.white, catsFrame: "Ink");
            Art(824, y + 20, 60, 60, icon);
            Text(900, y + 14, 450, caption, small, 26);
            Text(900, y + 42, 450, value, subtitle, 40);
        }
        void Result()
        {
            bool won = battle.state == BattleState.Won;
            PopupFrame(280, 160, 1040, 580, won ? "Chiến thắng!" : "Thất bại", false);
            Art(306, 214, 310, 310, won ? "Screens/victory" : "Screens/defeat");
            Text(640, 214, 650, won ? "Pháo đài trụ vững dưới trăng!" : "Pháo đài cần tiếp viện…", subtitle, 40);
            ResultStat(0, "Interface/Cats/Sword", N(battle.kills), "quái bị hạ");
            ResultStat(1, "Interface/Cats/Speed", N(battle.time) + " giây", "thời gian");
            ResultStat(2, "item-beastCage", N(battle.caught.Count), "quái bắt được");
            Surface(640, 362, 650, 180, Color.white, catsFrame: "Roster");
            Text(662, 374, 300, "PHẦN THƯỞNG", small, 26);
            var rewards = (receipt ?? new Dictionary<string, int>()).Where(v => v.Value > 0).Take(6).ToList();
            if (rewards.Count == 0) Text(662, 420, 600, "Không có phần thưởng lần này.", label, 36);
            for (int i = 0; i < rewards.Count; i++)
            {
                float x = 662 + i % 3 * 208, y = 410 + i / 3 * 62;
                string icon = RewardIcon(rewards[i].Key);
                if (icon != null) Art(x, y, 46, 46, icon);
                Text(x + 54, y + 2, 150, "+" + N(rewards[i].Value), subtitle, 30);
                Text(x + 54, y + 30, 150, Currency(rewards[i].Key), small, 24);
            }
            if (Button(310, 594, 430, 86, "VỀ MÀN CHÍNH", normalSpriteKey: "blue")) {Hub(); page = "march";}
            Enlarge(24);
            if (Button(770, 588, 520, 98, won && !battle.classic && !battle.arena && profile.selected < 20 ? "ẢI TIẾP THEO" : "THỬ LẠI", true, true))
            {
                bool classic = battle.classic, arena = battle.arena;
                if (battle.state == BattleState.Won && !classic && !arena && profile.selected < 20) profile.selected++;
                StartBattle(classic, arena);
            }
            Enlarge(30);
        }
        void ResultStat(int i, string icon, string value, string caption)
        {
            float x = 640 + i * 222;
            Surface(x, 256, 206, 92, Color.white, catsFrame: "Ink");
            Art(x + 14, 276, 48, 48, icon);
            Text(x + 72, 262, 126, value, subtitle, 36);
            Text(x + 72, 300, 126, caption, small, 26);
        }
        void ToggleSound() {muted = !muted; PlayerPrefs.SetInt("CatTanks.Muted", muted ? 1 : 0); PlayerPrefs.Save();}
        void Settings()
        {
            PopupFrame(430, 120, 740, 620, "Cài đặt", true);
            SettingRow(170, "Tên chỉ huy", "cat-0"); NameInput(740, 182, 374, 50);
            SettingRow(256, "Âm thanh", "UI/music"); if (TogglePill(256, !muted)) ToggleSound();
            SettingRow(342, "Toàn màn hình", "Interface/Cats/InfoIcon"); if (TogglePill(342, Screen.fullScreen)) Screen.fullScreen = !Screen.fullScreen;
            SettingRow(428, "Giảm chuyển động", "Interface/Cats/Speed"); if (TogglePill(428, reduceMotion)) { reduceMotion = !reduceMotion; PlayerPrefs.SetInt("CatTanks.ReducedMotion", reduceMotion ? 1 : 0); PlayerPrefs.Save(); }
            if (!inBattle && Button(470, 526, 320, 64, "Thử thách trăng · 6 đợt", normalSpriteKey: "blue")) StartBattle(true);
            if (Button(inBattle ? 470 : 810, 526, inBattle ? 660 : 320, 64, "Xuất hồ sơ JSON")) {profile.Save(); string path = Path.Combine(Application.persistentDataPath, "cattanks-export-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json"); File.WriteAllText(path, JsonConvert.SerializeObject(profile, Formatting.Indented)); Notify("Đã xuất: " + path);}
            Text(470, 598, 660, "Tiến trình tự lưu trên máy  ·  Esc hoặc chạm ra ngoài để đóng", small, 28, align: TextAnchor.UpperCenter);
            if (Button(470, 638, 660, 80, "LƯU & ĐÓNG", true, true)) {profile.Save(); modal = "";}
            Enlarge(26);
        }
        void ToggleBook()
        {
            if (modal == "book") modal = "";
            else if (modal == "" && (!inBattle || (!paused && !resolved))) modal = "book";
        }

        void Book()
        {
            PopupFrame(98, 90, 1404, 740, "Bách khoa trăng xanh", true);
            string[] names = {"THÁP PHÁO", "VŨ KHÍ MÈO", "MODULE", "QUÁI VẬT", "MÈO"};
            string[] icons = {"turret-0", "gun-0", "pack-0", "bug-0", "cat-0"};
            for (int i = 0; i < 5; i++) if (TabButton(134 + i * 268, 165, 258, 54, names[i], bookTab == i, icons[i])) {bookTab = i; scroll = Vector2.zero;}
            int count = bookTab == 3 ? db.monsters.Count : bookTab == 4 ? db.cats.Count : db.weapons.Count(w => bookTab == 0 ? w.id.StartsWith("turret") : bookTab == 1 ? w.id.StartsWith("gun") : w.id.StartsWith("pack"));
            Surface(124, 232, 1352, 575, Color.white,catsFrame:"Roster");
            scroll = BeginScroll(new Rect(138, 246, 1324, 547), scroll, new Rect(0, 0, 1300, Mathf.Ceil(count / 4f) * 260));
            for (int i = 0; i < count; i++)
            {
                float x = i % 4 * 326, y = i / 4 * 260; Panel(x, y, 314, 248);
                string asset, name, desc;
                if (bookTab == 3) {var d = db.monsters[i]; asset = d.asset; name = d.name; desc = $"HP {N(d.hp)} · ATK {N(d.damage)}\n" + d.description;}
                else if (bookTab == 4) {var d = db.cats[i]; asset = d.id; name = d.name; desc = d.rarity + " · " + d.role + "\n" + (d.specialty == "operator" ? "Chuyên vận hành pháo" : "Chuyên bộ binh");}
                else {var d = db.weapons.Where(w => bookTab == 0 ? w.id.StartsWith("turret") : bookTab == 1 ? w.id.StartsWith("gun") : w.id.StartsWith("pack")).ElementAt(i); asset = d.asset; name = d.name; desc = $"ATK {N(d.damage)} · {d.rate:0.##}s\n" + d.description;}
                Art(x + 27, y + 10, 260, 100, asset); Text(x + 15, y + 117, 284, name, subtitle, 47); Text(x + 15, y + 166, 284, desc, small, 74);
            }
            EndScroll();
        }
    }
}
