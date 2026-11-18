using UnityEngine;

namespace MegaTankVFX
{
    /// <summary>Isolated VFX gallery; never changes the game's battle scene.</summary>
    public sealed class VfxDemoController : MonoBehaviour
    {
        public GameObject[] Effects;
        public bool AutoPlay = true;
        private float timer;
        private int next;
        private readonly string[] labels = { "Small explosion", "Big explosion", "Cluster burst", "Hit spark", "Muzzle flash", "Smoke puff", "Dust puff", "Fire bolt" };
        private void Update()
        {
            if (!AutoPlay || Effects == null || Effects.Length == 0) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            Show(next++ % Effects.Length);
            timer = 2f;
        }
        public void Show(int index)
        {
            if (Effects == null || index < 0 || index >= Effects.Length) return;
            bool fireBolt = index == 7;
            var position = fireBolt ? new Vector3(-3.5f, -1.6f, 0f) : Vector3.zero;
            var rotation = fireBolt ? Quaternion.Euler(0f, 0f, 30f) : Quaternion.identity;
            VfxAutoRelease.Spawn(Effects[index], position, rotation);
        }
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(14, 12, Screen.width - 28, 92));
            GUILayout.Label("MegaTank VFX - redrawn from both supplied references");
            AutoPlay = GUILayout.Toggle(AutoPlay, "Auto preview");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < labels.Length; i++) if (GUILayout.Button(labels[i], GUILayout.Height(30))) Show(i);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
