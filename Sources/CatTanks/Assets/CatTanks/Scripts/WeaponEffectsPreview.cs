using System.Collections.Generic;
using UnityEngine;

namespace CatTanks
{
    [RequireComponent(typeof(PreviewIsolation))]
    public sealed class WeaponEffectsPreview : MonoBehaviour
    {
        static readonly string[] WeaponIds = { "turret-3", "turret-0", "turret-2" };
        const int AllWeapons = 3;
        Catalog catalog;
        Battle battle;
        BattleView view;
        int selected = AllWeapons;
        bool paused, slow;

        void Start()
        {
            catalog = Catalog.Load();
            var camera = new GameObject("Weapon Effects Camera", typeof(Camera)).GetComponent<Camera>();
            camera.transform.SetParent(transform, false);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Catalog.Color("#041828");
            camera.farClipPlane = 3000;
            view = new BattleView(camera, useNativeWeaponVfx: true);
            view.root.transform.SetParent(transform, false);
            Restart(selected);
        }

        void Restart(int selection)
        {
            selected = selection;
            var profile = Profile.New(catalog);
            profile.accountLevel = 40;
            for (int i = 0; i < WeaponIds.Length; i++) profile.floors[i].turret = WeaponIds[i];

            battle = new Battle(catalog, profile);
            battle.units.RemoveAll(unit => unit.role != "tower");
            foreach (var unit in battle.units)
            {
                unit.weapon = catalog.Weapon(WeaponIds[unit.index]).Copy();
                unit.timer = .25f;
                unit.stun = selection == AllWeapons || unit.index == selection ? 0 : float.PositiveInfinity;
            }
            battle.waves = new List<Wave> { new Wave { count = 1, bursts = new List<Burst>() } };
            foreach (float x in new[] { 930f, 1025f, 1140f })
            {
                var target = battle.Spawn("bug-0", x);
                target.stun = float.PositiveInfinity;
                target.hp = target.maxHp = 100000;
            }
            view.Render(battle);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) Restart(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Restart(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Restart(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) Restart(AllWeapons);
            if (Input.GetKeyDown(KeyCode.R)) Restart(selected);
            if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Space)) paused = !paused;
            if (Input.GetKeyDown(KeyCode.S)) slow = !slow;

            if (!paused) battle.Step(Time.unscaledDeltaTime * (slow ? .25f : 1));
            else if (Input.GetKeyDown(KeyCode.N)) battle.Step(1f / 60f);
            view.Render(battle);
        }

        void OnGUI()
        {
            if (catalog == null) return;
            GUILayout.BeginArea(new Rect(12, 12, 520, 118), GUI.skin.box);
            GUILayout.Label(selected == AllWeapons ? "Bắn đồng thời" : catalog.Weapon(WeaponIds[selected]).name);
            GUILayout.Label("1 Gatling   2 Đại bác   3 Tên lửa   4 Bắn đồng thời");
            GUILayout.Label("P / Space Tạm dừng   N Từng khung   S Tốc độ   R Bắt đầu lại");
            GUILayout.Label($"{(view.UsesNativeWeaponVfx ? "Unity Native" : "Sprite fallback")}  •  {(paused ? "Tạm dừng" : "Đang chạy")}  •  {(slow ? "0,25×" : "1×")}  •  {battle.time:F2} s");
            GUILayout.EndArea();
        }

        void OnDestroy() => view?.Dispose();
    }
}
