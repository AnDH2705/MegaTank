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
            view = new BattleView(cam); Preview();
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
        void Preview()
        {
            battle = new Battle(db, profile, profile.selected); battle.Step(.001f); view.Render(battle);
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
            inBattle = false; paused = false; modal = ""; view.Dispose(); view = new BattleView(Camera.main); Preview();
        }
        void OnApplicationPause(bool pause) {if (PreviewIsolation.IsActive) return; if (pause && inBattle) paused = true; if (pause) profile?.Save();}
        void OnApplicationQuit() {if (!PreviewIsolation.IsActive) profile?.Save();}
        void OnDestroy() {view?.Dispose(); if (shot) Destroy(shot); if (boom) Destroy(boom);}
        void Notify(string value) {toast = value; toastUntil = Time.unscaledTime + 3.5f;}
        bool Pay(params (string key, int amount)[] cost) {if (profile.Spend(cost)) return true; Notify("Thiếu tài nguyên"); return false;}
        void Changed(string message = "Đã lưu") {profile.Save(); Notify(message); if (!inBattle) {view.Dispose(); view = new BattleView(Camera.main); Preview();}}
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
            Panel(202, 103, 1196, 694); var s = db.stages[profile.selected - 1];
            Text(236, 133, 715, "Ải " + s.index + " · " + s.name, title, 63);
            Text(236, 218, 688, $"Đề nghị {N(s.recommendedPower)} lực chiến  ·  Pháo đài {N(profile.MaxHp)} HP  ·  {profile.FloorCount} tầng", subtitle, 60);
            Text(236, 286, 688, "Đội hình dùng trang bị hiện tại trong Doanh trại. Pháo và bộ binh tự động tấn công.", label, 55);
            Text(236, 349, 688, "3 đợt tiếp viện xuất hiện tại giây 0 / 25 / 55. Chọn mục tiêu bằng chuột để tập trung hỏa lực.", label, 60);
            Surface(944, 150, 420, 260, Color.white,catsFrame:"Roster");
            Art(956, 162, 396, 236, StagePreview(s));
            Surface(236, 438, 1128, 164, Color.white,catsFrame:"Roster");
            for (int i = 0; i < profile.FloorCount; i++)
            {
                float x=252+i*221;
                Surface(x, 452, 205, 136, Color.white,catsFrame:"Tile");
                Art(x+37, 460, 131, 82, profile.floors[i].turret);
                Text(x+18, 550, 169, "Tầng " + (i + 1), small, 28);
            }
            int moonCages = Mathf.Min(5, profile.Get("moonCage")), beastCages = Mathf.Min(5 - moonCages, profile.Get("beastCage"));
            Text(236, 619, 1120, $"Mang {beastCages} lồng thú · {moonCages} lồng trăng (tối đa 5).\nQuái thường: {Battle.CaptureHealthThreshold(false):0%} HP trở xuống. Trùm: dưới {Battle.CaptureHealthThreshold(true):0%} HP, dùng Lồng trăng (Shift+C).", small, 52);
            if (Button(236, 709, 216, 60, "Trở về")) modal = "";
            if (IllustratedButton(464, 709, 376, 60, "Chỉnh đội hình", "cat-0",normalSpriteKey:"blue")) {modal = ""; page = "barracks"; tab = "squad";}
            if (IllustratedButton(852, 709, 512, 60, "XUẤT KÍCH", "Battle", primary: true)) StartBattle();
        }
        void Result()
        {
            bool won = battle.state == BattleState.Won;
            Panel(260, 201, 1080, 498);
            Text(294, 244, 660, won ? "CHIẾN THẮNG DƯỚI TRĂNG!" : "PHÁO ĐÀI CẦN TIẾP VIỆN", title, 61, dynamicLayout: true);
            Text(294, 330, 660, $"{battle.kills} quái bị hạ  ·  {N(battle.time)} giây  ·  {battle.caught.Count} quái bắt được", subtitle, 60, dynamicLayout: true);
            Surface(294, 401, 665, 137, Color.white,catsFrame:"Roster");
            Text(314, 420, 625, Rewards(receipt ?? new Dictionary<string, int>()), label, 104, dynamicLayout: true);
            Art(978, 231, 328, 311, won ? "Screens/victory" : "Screens/defeat");
            if (Button(294, 594, 492, 69, "VỀ DOANH TRẠI",normalSpriteKey:"blue")) {Hub(); page = "barracks"; tab = "cats";}
            if (Button(800, 594, 506, 69, won && !battle.classic && !battle.arena && profile.selected < 20 ? "ẢI TIẾP THEO" : "THỬ LẠI", true, true))
            {
                bool classic = battle.classic, arena = battle.arena;
                if (battle.state == BattleState.Won && !classic && !arena && profile.selected < 20) profile.selected++;
                StartBattle(classic, arena);
            }
        }
        void ToggleSound() {muted = !muted; PlayerPrefs.SetInt("CatTanks.Muted", muted ? 1 : 0); PlayerPrefs.Save();}
        void Settings()
        {
            Panel(328, 151, 944, 598);
            Text(364, 186, 872, "Cài đặt chỉ huy", title, 57);
            Surface(364, 254, 872, 75, Color.white,catsFrame:"Roster");
            Text(384, 271, 194, "Tên chỉ huy", label); NameInput(574, 265, 642, 50);
            if (Button(364, 348, 430, 60, muted ? "Bật âm thanh" : "Tắt âm thanh",normalSpriteKey:"blue")) ToggleSound();
            if (Button(806, 348, 430, 60, "Toàn màn hình",normalSpriteKey:"blue")) Screen.fullScreen = !Screen.fullScreen;
            if (Button(364, 423, 430, 60, "Xuất hồ sơ JSON")) {profile.Save(); string path = Path.Combine(Application.persistentDataPath, "cattanks-export-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json"); File.WriteAllText(path, JsonConvert.SerializeObject(profile, Formatting.Indented)); Notify("Đã xuất: " + path);}
            if (Button(806, 423, 430, 60, "Thử thách trăng · 6 đợt")) StartBattle(true);
            if (Button(364, 499, 872, 57, reduceMotion ? "Giảm chuyển động: BẬT" : "Giảm chuyển động: TẮT",normalSpriteKey:"blue")) { reduceMotion = !reduceMotion; PlayerPrefs.SetInt("CatTanks.ReducedMotion", reduceMotion ? 1 : 0); PlayerPrefs.Save(); }
            Text(364, 582, 872, "Tiến trình lưu trên máy • Esc để đóng • Tab / Enter để điều hướng", small, 40);
            if (Button(364, 647, 872, 64, "LƯU & ĐÓNG", true, true)) {profile.Save(); modal = "";}
        }
        void ToggleBook()
        {
            if (modal == "book") modal = "";
            else if (modal == "" && (!inBattle || (!paused && !resolved))) modal = "book";
        }

        void Book()
        {
            Panel(98, 70, 1404, 760); Text(134, 97, 1100, "Bách khoa trăng xanh", title, 57);
            if (Button(1312, 95, 152, 52, "Đóng [B]")) ToggleBook();
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
