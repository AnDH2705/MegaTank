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
        public string page = "march", tab = "cats", modal = "", toast = "";
        public int selectedCat, selectedTurret, selectedPet, bookTab, selectedOpponent;
        float toastUntil, soundAt;
        bool resolved;
        Dictionary<string, int> receipt;
        AudioSource audioSource;
        AudioClip shot, boom;
        Vector2 scroll;
        Font font;
        GUIStyle label, title, subtitle, small, button, primaryButton, card, field;
        readonly Color ink = Catalog.Color("#d9f2ff"), cyan = Catalog.Color("#61e3f4"), gold = Catalog.Color("#ffcf75");
        Texture2D panelTexture, buttonTexture, hoverTexture, activeTexture;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureGame() {if (!FindObjectOfType<CatTanksGame>()) new GameObject("CatTanks").AddComponent<CatTanksGame>();}
        void Awake()
        {
            Application.targetFrameRate = 60;
            db = Catalog.Load(); profile = Profile.Load(db); muted = PlayerPrefs.GetInt("CatTanks.Muted", 0) != 0;
            var cam = Camera.main;
            if (!cam) {var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); go.tag = "MainCamera"; cam = go.GetComponent<Camera>();}
            cam.backgroundColor = Catalog.Color("#041828"); cam.clearFlags = CameraClearFlags.SolidColor; cam.farClipPlane = 3000;
            if (!FindObjectOfType<Light>()) {var light = new GameObject("Moonlight", typeof(Light)).GetComponent<Light>(); light.type = LightType.Directional; light.intensity = .7f; light.transform.eulerAngles = new Vector3(35, -30, 0);}
            view = new BattleView(cam); Preview();
            audioSource = gameObject.AddComponent<AudioSource>(); shot = Tone("Cannon", 110, .085f); boom = Tone("Rocket", 60, .2f);
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
            view.Dispose(); view = new BattleView(Camera.main);
            battle = new Battle(db, profile, arena ? Mathf.Min(20, Mathf.Max(1, profile.highestCleared)) : profile.selected, classic, arena, selectedOpponent); battle.onFire = PlayShot;
            profile.Save(); view.Render(battle);
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F)) Screen.fullScreen = !Screen.fullScreen;
            if (Input.GetKeyDown(KeyCode.Escape)) {if (!string.IsNullOrEmpty(modal)) modal = ""; else if (inBattle) paused = !paused;}
            if (!inBattle) {view.Resize(); return;}
            if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Space)) paused = !paused;
            if (Input.GetKeyDown(KeyCode.B)) modal = modal == "book" ? "" : "book";
            if (!paused && string.IsNullOrEmpty(modal) && battle.state == BattleState.Playing)
            {
                battle.move = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
                for (int i = 0; i < battle.skills.Count && i < 6; i++) if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i))) battle.UseSkill(battle.skills[i].id);
                if (Input.GetKeyDown(KeyCode.C)) battle.Catch(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
                if (Input.GetMouseButtonDown(0))
                {
                    Vector2 mouse = Input.mousePosition; Vector2 ui = ToUI(new Vector2(mouse.x, Screen.height - mouse.y));
                    if (ui.y > 170 && ui.y < 680)
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
        void OnApplicationPause(bool pause) {if (pause && inBattle) paused = true; if (pause) profile?.Save();}
        void OnApplicationQuit() {profile?.Save();}
        void OnDestroy() {view?.Dispose(); if (shot) Destroy(shot); if (boom) Destroy(boom);}
        void Notify(string value) {toast = value; toastUntil = Time.unscaledTime + 3.5f;}
        bool Pay(params (string key, int amount)[] cost) {if (profile.Spend(cost)) return true; Notify("Thiếu tài nguyên"); return false;}
        void Changed(string message = "Đã lưu") {profile.Save(); Notify(message); if (!inBattle) {view.Dispose(); view = new BattleView(Camera.main); Preview();}}
        public static Vector2 ToUI(Vector2 screen)
        {
            float scale = Mathf.Min(Screen.width / 1600f, Screen.height / 900f); return (screen - new Vector2((Screen.width - 1600 * scale) / 2, (Screen.height - 900 * scale) / 2)) / scale;
        }
        Texture2D Flat(string hex)
        {
            var tx = new Texture2D(1, 1); tx.SetPixel(0, 0, Catalog.Color(hex)); tx.Apply(); return tx;
        }
        void Styles()
        {
            if (label != null) return;
            font = Font.CreateDynamicFontFromOSFont(new[] {"Arial", "Helvetica", "DejaVu Sans"}, 24);
            panelTexture = Flat("#0b2c46"); buttonTexture = Flat("#175675"); hoverTexture = Flat("#247d9d"); activeTexture = Flat("#d69739");
            label = new GUIStyle(GUI.skin.label) {font = font, fontSize = 18, wordWrap = true}; label.normal.textColor = ink;
            small = new GUIStyle(label) {fontSize = 14}; small.normal.textColor = Catalog.Color("#9fcbdc");
            title = new GUIStyle(label) {fontSize = 40, fontStyle = FontStyle.Bold}; title.normal.textColor = gold;
            subtitle = new GUIStyle(label) {fontSize = 24, fontStyle = FontStyle.Bold};
            button = new GUIStyle(GUI.skin.button) {font = font, fontSize = 17, fontStyle = FontStyle.Bold, wordWrap = true, padding = new RectOffset(12, 12, 8, 8), border = new RectOffset()};
            button.normal.background = buttonTexture; button.normal.textColor = ink; button.hover.background = hoverTexture; button.hover.textColor = Color.white; button.active.background = activeTexture; button.active.textColor = Color.white;
            primaryButton = new GUIStyle(button);
            primaryButton.normal.background = Flat("#f2b953"); primaryButton.normal.textColor = Catalog.Color("#3f301b");
            primaryButton.hover.background = Flat("#ffdc8c"); primaryButton.hover.textColor = Catalog.Color("#3f301b");
            primaryButton.active.background = activeTexture;
            card = new GUIStyle {normal = {background = panelTexture}, padding = new RectOffset(20, 20, 15, 15)};
            field = new GUIStyle(GUI.skin.textField) {font = font, fontSize = 20, padding = new RectOffset(10, 10, 8, 8)};
        }
        void Text(float x, float y, float w, string text, GUIStyle style = null, float h = 35) {var chosen = style ?? label; GUI.Label(new Rect(x, y, w, Mathf.Max(h, chosen.fontSize * 1.45f)), text, chosen);}
        bool Button(float x, float y, float w, float h, string text, bool enabled = true, bool primary = false)
        {
            GUI.enabled = enabled;
            bool result = GUI.Button(new Rect(x, y, w, h), text, primary ? primaryButton : button); GUI.enabled = true; return result;
        }
        void Panel(float x, float y, float w, float h, float alpha = 1)
        {
            Color old = GUI.color; GUI.color = new Color(1, 1, 1, alpha); GUI.Box(new Rect(x, y, w, h), GUIContent.none, card); GUI.color = old;
        }
        void Art(float x, float y, float w, float h, string name)
        {
            var sprite = view.Art(name); if (sprite) GUI.DrawTexture(new Rect(x, y, w, h), sprite.texture, ScaleMode.ScaleToFit, true);
        }
        void Bar(float x, float y, float w, float h, float ratio, Color color)
        {
            Color old = GUI.color; GUI.color = Catalog.Color("#08233c"); GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture); GUI.color = color; GUI.DrawTexture(new Rect(x + 2, y + 2, (w - 4) * Mathf.Clamp01(ratio), h - 4), Texture2D.whiteTexture); GUI.color = old;
        }
        static string N(float n) => n.ToString("N0");
        string Cost(params (string key, int amount)[] costs) => string.Join(" · ", costs.Select(c => N(c.amount) + " " + Currency(c.key)));
        static string Currency(string id)
        {
            switch (id) {case "coins": return "xu"; case "gem": return "ngọc"; case "spiritDust": return "bụi"; case "catXp": return "XP mèo"; case "forgeStone": return "đá rèn"; case "soulEssence": return "tinh chất"; case "summonScroll": return "vé"; case "skillCore": return "lõi"; case "beastCage": return "lồng thú"; case "moonCage": return "lồng trăng"; case "monsterChow": return "thức ăn"; case "arenaToken": return "huy hiệu"; case "accountXp": return "XP chỉ huy"; default: return id;}
        }
        string Rewards(Dictionary<string, int> values) => string.Join("    ", values.Where(v => v.Value > 0).Select(v => "+" + N(v.Value) + " " + Currency(v.Key)));
        void OnGUI()
        {
            if (db == null) return; Styles();
            float scale = Mathf.Min(Screen.width / 1600f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1600 * scale) / 2, (Screen.height - 900 * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            if (inBattle) BattleHUD(); else HubUI();
            if (modal == "book") Book(); else if (modal == "settings") Settings(); else if (modal == "prebattle") Prebattle();
            if (Time.unscaledTime < toastUntil) {Panel(380, 650, 840, 54, .96f); Text(400, 662, 800, toast);}
        }
        void HubUI()
        {
            Panel(0, 0, 1600, 105, .98f);
            Text(35, 16, 400, "MEOW BASTION", title); Text(37, 65, 400, "PHÁO ĐÀI TRĂNG XANH  /  UNITY", small);
            Text(540, 23, 970, $"{N(profile.Get("coins"))} XU       {N(profile.Get("gem"))} NGỌC       {N(profile.Get("spiritDust"))} BỤI       {profile.Get("summonScroll")} VÉ", subtitle);
            Text(540, 63, 800, $"{profile.name}  ·  Chỉ huy cấp {profile.accountLevel}  ·  Lực chiến {N(profile.Power(db))}  ·  Ải {profile.highestCleared}/20", small);
            if (Button(1450, 25, 112, 52, "Cài đặt")) modal = "settings";
            Panel(24, 125, 210, 740, .96f);
            string[] nav = {"march", "barracks", "town", "chest", "quests", "book"};
            string[] names = {"HÀNH QUÂN", "DOANH TRẠI", "THÀNH MÈO", "RƯƠNG AFK", "NHIỆM VỤ", "BÁCH KHOA"};
            for (int i = 0; i < nav.Length; i++) if (Button(40, 152 + i * 79, 178, 60, names[i], true, page == nav[i])) {if (nav[i] == "book") modal = "book"; else {page = nav[i]; scroll = Vector2.zero;}}
            Art(65, 660, 130, 120, "cat-0"); Text(46, 795, 170, "Giữ vững pháo đài!", small);
            Panel(254, 125, 1322, 740, .94f);
            if (page == "march") March();
            else if (page == "barracks") Barracks();
            else if (page == "town") Town();
            else if (page == "chest") Chest();
            else if (page == "quests") Quests();
            Text(30, 873, 1530, "A/D di chuyển  ·  Chuột chọn mục tiêu  ·  1–4 kỹ năng  ·  C bắt quái  ·  Shift+C lồng trăng  ·  P tạm dừng  ·  B bách khoa  ·  F toàn màn hình", small);
        }
        void March()
        {
            Text(286, 151, 1000, "Hành quân dưới trăng", title);
            Text(288, 207, 1100, "20 vùng đất · Mỗi 5 ải có trùm · Thắng để mở khóa đội hình và hệ thống mới", small);
            for (int i = 0; i < 20; i++)
            {
                int col = i % 5, row = i / 5; var s = db.stages[i]; float x = 288 + col * 246, y = 267 + row * 118; bool unlocked = i <= profile.highestCleared;
                if (Button(x, y, 228, 99, $"{(i < profile.highestCleared ? "✓ " : "")}{s.index:00}  {s.name}\n{(unlocked ? s.boss ? "TRÙM HẮC NGUYỆT" : "Lực chiến " + N(s.recommendedPower) : "CHƯA MỞ")}", unlocked, profile.selected == i + 1)) {profile.selected = i + 1; profile.Save();}
            }
            var selected = db.stages[profile.selected - 1];
            Text(288, 760, 830, "Ải " + selected.index + ": " + selected.name, subtitle);
            Text(288, 802, 830, "Thưởng lần đầu: " + Rewards(selected.firstClearRewards), small);
            if (Button(1178, 760, 332, 66, "CHUẨN BỊ XUẤT KÍCH", true, true)) modal = "prebattle";
        }
        void Prebattle()
        {
            Panel(220, 140, 1160, 620); var s = db.stages[profile.selected - 1];
            Text(260, 175, 1000, "Ải " + s.index + " · " + s.name, title);
            Text(260, 240, 1000, $"Đề nghị {N(s.recommendedPower)} lực chiến  ·  Pháo đài {N(profile.MaxHp)} HP  ·  {profile.FloorCount} tầng", subtitle);
            Text(260, 300, 1000, "Đội hình dùng trang bị hiện tại trong Doanh trại. Pháo và bộ binh tự động tấn công.", label);
            Text(260, 355, 1000, "3 đợt tiếp viện xuất hiện tại giây 0 / 25 / 55. Chọn mục tiêu bằng chuột để tập trung hỏa lực.", label, 65);
            for (int i = 0; i < profile.FloorCount; i++) {Art(275 + i * 180, 440, 130, 80, profile.floors[i].turret); Text(270 + i * 180, 530, 160, "Tầng " + (i + 1), small);}
            Text(260, 584, 1000, $"Mang tối đa 5 lượt ném: {Mathf.Min(5, profile.Get("beastCage"))} lồng thú · {Mathf.Min(5, profile.Get("moonCage"))} lồng trăng. Quái dưới 25% HP mới bắt được.", small, 50);
            if (Button(260, 670, 230, 54, "Trở về")) modal = "";
            if (Button(1050, 670, 280, 54, "XUẤT KÍCH", true, true)) StartBattle();
        }
        void BattleHUD()
        {
            Panel(24, 22, 400, 72, .9f); if (Button(35, 33, 55, 48, paused ? "▶" : "II")) paused = !paused;
            Text(110, 39, 300, $"{N(battle.coins)} xu    {battle.kills} hạ gục", subtitle);
            Panel(555, 18, 490, 86, .92f);
            Text(580, 28, 440, battle.classic ? $"THỬ THÁCH · ĐỢT {battle.wave + 1} / 6" : battle.arena ? "ĐẤU TRƯỜNG · MÔ PHỎNG LOCAL" : $"ẢI {battle.stage.index} · {battle.stage.name}", subtitle);
            Text(580, 67, 440, $"{Mathf.FloorToInt(battle.time / 60):00}:{Mathf.FloorToInt(battle.time % 60):00}   ·   {battle.spawned}/{battle.waves[battle.wave].count} đã xuất hiện", small);
            if (Button(1180, 30, 180, 52, "BÁCH KHOA")) modal = "book";
            if (Button(1375, 30, 86, 52, muted ? "Âm: Tắt" : "Âm: Bật")) ToggleSound();
            if (Button(1476, 30, 86, 52, "Thoát")) paused = true;
            Text(65, 115, 460, $"PHÁO ĐÀI MÈO    {N(battle.hp)} / {N(battle.maxHp)}", small);
            Bar(65, 150, 460, 20, battle.hp / battle.maxHp, Catalog.Color("#81e65a"));
            float enemyHp = battle.enemies.Where(e => !e.dead).Sum(e => e.hp), enemyMax = battle.enemies.Where(e => !e.dead).Sum(e => e.maxHp);
            Text(1080, 115, 460, "QUÁI ĐANG TIẾN ĐẾN    " + battle.enemies.Count(e => !e.dead) + " MỤC TIÊU", small);
            Bar(1080, 150, 460, 20, enemyHp / Mathf.Max(1, enemyMax), Catalog.Color("#ff6973"));
            Panel(30, 748, 290, 112, .96f); Text(48, 764, 245, "NĂNG LƯỢNG   " + N(battle.energy) + "/600", label);
            Bar(48, 803, 248, 15, battle.energy / 600, gold); Text(48, 828, 245, "Tự hồi · Hạ quái nhận thêm", small);
            for (int i = 0; i < battle.skills.Count; i++)
            {
                var s = battle.skills[i]; float x = 348 + i * 142, cd = battle.Cooldown(s.id);
                if (Button(x, 748, 130, 112, $"[{i + 1}] {s.name}\n{s.cost} năng lượng\n{(cd > 0 ? Mathf.CeilToInt(cd) + "s" : "SẴN SÀNG")}", cd <= 0 && battle.energy >= s.cost && !paused && modal == "")) battle.UseSkill(s.id);
            }
            if (!battle.classic && !battle.arena)
            {
                if (Button(1250, 755, 145, 90, $"[C] BẮT QUÁI\nLồng thú: {battle.cages}", !paused)) battle.Catch(false);
                if (Button(1410, 755, 160, 90, $"[⇧C] LỒNG TRĂNG\nCòn: {battle.moonCages}", !paused)) battle.Catch(true);
            }
            var target = battle.enemies.Find(e => e.id == battle.targetId && !e.dead);
            if (target != null)
            {
                Text(580, 185, 570, $"MỤC TIÊU: {target.def.name}  ·  {target.hp / target.maxHp:P0} HP", subtitle);
                if (target.hp / target.maxHp <= .25f) Text(580, 226, 570, "Đủ yếu để ném lồng!", small);
            }
            foreach (var fx in battle.effects.Where(e => e.kind == "text"))
            {
                var p = Camera.main.WorldToScreenPoint(fx.position + Vector2.up * (1 - fx.life / fx.maxLife) * 50); Vector2 ui = ToUI(new Vector2(p.x, Screen.height - p.y));
                Color old = GUI.color; GUI.color = fx.color; Text(ui.x - 25, ui.y, 100, fx.text, small); GUI.color = old;
            }
            Text(35, 875, 1530, "A / D  di chuyển     Chuột  khóa mục tiêu     1–4  kỹ năng     C  bắt quái     P / Space  tạm dừng     B  bách khoa", small);
            if (resolved) Result();
            else if (paused && modal == "")
            {
                Panel(490, 270, 620, 330); Text(570, 305, 490, "ĐÃ TẠM DỪNG", title);
                if (Button(550, 395, 500, 60, "Tiếp tục")) paused = false;
                if (Button(550, 480, 500, 60, "Rút lui về doanh trại")) Hub();
            }
        }
        void Result()
        {
            Panel(310, 235, 980, 445); Text(360, 275, 880, battle.state == BattleState.Won ? "CHIẾN THẮNG DƯỚI TRĂNG!" : "PHÁO ĐÀI CẦN TIẾP VIỆN", title);
            Text(360, 350, 880, $"{battle.kills} quái bị hạ  ·  {N(battle.time)} giây  ·  {battle.caught.Count} quái bắt được", subtitle);
            Text(360, 410, 880, Rewards(receipt ?? new Dictionary<string, int>()), label, 90);
            if (Button(360, 555, 380, 70, "VỀ DOANH TRẠI")) Hub();
            if (Button(800, 555, 430, 70, battle.state == BattleState.Won && !battle.classic && !battle.arena && profile.selected < 20 ? "ẢI TIẾP THEO" : "THỬ LẠI", true, true))
            {
                bool classic = battle.classic, arena = battle.arena;
                if (battle.state == BattleState.Won && !classic && !arena && profile.selected < 20) profile.selected++;
                StartBattle(classic, arena);
            }
        }
        void ToggleSound() {muted = !muted; PlayerPrefs.SetInt("CatTanks.Muted", muted ? 1 : 0); PlayerPrefs.Save();}
        void Settings()
        {
            Panel(330, 170, 940, 570); Text(380, 205, 800, "Cài đặt chỉ huy", title);
            Text(380, 280, 200, "Tên chỉ huy", label); profile.name = GUI.TextField(new Rect(590, 276, 610, 45), profile.name, 24, field);
            if (Button(380, 350, 380, 60, muted ? "Bật âm thanh" : "Tắt âm thanh")) ToggleSound();
            if (Button(800, 350, 400, 60, "Toàn màn hình")) Screen.fullScreen = !Screen.fullScreen;
            if (Button(380, 440, 380, 60, "Xuất hồ sơ JSON")) {profile.Save(); string path = Path.Combine(Application.persistentDataPath, "cattanks-export-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json"); File.WriteAllText(path, JsonConvert.SerializeObject(profile, Formatting.Indented)); Notify("Đã xuất: " + path);}
            if (Button(800, 440, 400, 60, "Thử thách trăng · 6 đợt")) StartBattle(true);
            Text(380, 535, 820, "Tiến trình tự lưu trên máy. Hồ sơ Unity độc lập với localStorage của bản HTML. Đấu trường là mô phỏng local.", small, 70);
            if (Button(380, 645, 820, 55, "LƯU & ĐÓNG", true, true)) {profile.Save(); modal = "";}
        }
        void Book()
        {
            Panel(110, 75, 1380, 760); Text(150, 105, 900, "Bách khoa trăng xanh", title);
            if (Button(1310, 103, 135, 50, "Đóng [B]")) modal = "";
            string[] names = {"THÁP PHÁO", "VŨ KHÍ MÈO", "MODULE", "QUÁI VẬT", "MÈO"};
            for (int i = 0; i < 5; i++) if (Button(150 + i * 245, 175, 230, 48, names[i], true, bookTab == i)) {bookTab = i; scroll = Vector2.zero;}
            int count = bookTab == 3 ? db.monsters.Count : bookTab == 4 ? db.cats.Count : db.weapons.Count(w => bookTab == 0 ? w.id.StartsWith("turret") : bookTab == 1 ? w.id.StartsWith("gun") : w.id.StartsWith("pack"));
            scroll = GUI.BeginScrollView(new Rect(145, 250, 1310, 550), scroll, new Rect(0, 0, 1275, Mathf.Ceil(count / 4f) * 250));
            for (int i = 0; i < count; i++)
            {
                float x = i % 4 * 317, y = i / 4 * 250; Panel(x, y, 300, 232);
                string asset, name, desc;
                if (bookTab == 3) {var d = db.monsters[i]; asset = d.asset; name = d.name; desc = $"HP {N(d.hp)} · ATK {N(d.damage)}\n" + d.description;}
                else if (bookTab == 4) {var d = db.cats[i]; asset = d.id; name = d.name; desc = d.rarity + " · " + d.role + "\n" + (d.specialty == "operator" ? "Chuyên vận hành pháo" : "Chuyên bộ binh");}
                else {var d = db.weapons.Where(w => bookTab == 0 ? w.id.StartsWith("turret") : bookTab == 1 ? w.id.StartsWith("gun") : w.id.StartsWith("pack")).ElementAt(i); asset = d.asset; name = d.name; desc = $"ATK {N(d.damage)} · {d.rate:0.##}s\n" + d.description;}
                Art(x + 25, y + 10, 250, 100, asset); Text(x + 15, y + 120, 270, name, subtitle); Text(x + 15, y + 157, 270, desc, small, 70);
            }
            GUI.EndScrollView();
        }
    }
}
