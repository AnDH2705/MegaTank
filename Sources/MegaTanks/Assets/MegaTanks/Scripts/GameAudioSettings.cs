using UnityEngine;
namespace MegaTanks
{
    public enum GameAudioChannel { Sound, Music }
    // Add this to future audio sources to keep sound and music independently adjustable.
    [RequireComponent(typeof(AudioSource))]
    public sealed class GameAudioSettings : MonoBehaviour
    {
        public GameAudioChannel channel;
        [Range(0, 1)] public float sourceVolume = 1;
        public static float Sound => PlayerPrefs.GetFloat("MegaTanks.Sound", .8f);
        public static float Music => PlayerPrefs.GetFloat("MegaTanks.Music", .6f);
        public static bool Vibration => PlayerPrefs.GetInt("MegaTanks.Vibration", 1) == 1;
        public static void Set(GameAudioChannel channel, float value)
        {
            PlayerPrefs.SetFloat("MegaTanks." + channel, Mathf.Clamp01(value)); PlayerPrefs.Save();
            foreach (var source in FindObjectsOfType<GameAudioSettings>()) source.Apply();
        }
        public static void SetVibration(bool value) { PlayerPrefs.SetInt("MegaTanks.Vibration", value ? 1 : 0); PlayerPrefs.Save(); }
        void OnEnable() { Apply(); }
        void Apply() { GetComponent<AudioSource>().volume = sourceVolume * (channel == GameAudioChannel.Sound ? Sound : Music); }
    }
}
