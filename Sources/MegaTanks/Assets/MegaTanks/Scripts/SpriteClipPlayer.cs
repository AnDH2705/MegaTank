using UnityEngine;

namespace MegaTanks
{
    public sealed class SpriteClipPlayer : MonoBehaviour
    {
        [SerializeField] Sprite[] frames;
        [SerializeField] float secondsPerFrame = .04f;
        SpriteRenderer sprite;
        float age;
        bool playing;
        public bool Playing => playing;
        public int FrameCount => frames == null ? 0 : frames.Length;

        public void Configure(Sprite[] images, float interval, int order)
        {
            frames = images;
            secondsPerFrame = interval;
            sprite = GetComponent<SpriteRenderer>();
            if (sprite == null) sprite = gameObject.AddComponent<SpriteRenderer>();
            sprite.sortingOrder = order;
            sprite.enabled = false;
        }

        public void Play()
        {
            if (sprite == null) sprite = GetComponent<SpriteRenderer>();
            if (frames == null || frames.Length == 0) return;
            age = 0;
            playing = true;
            sprite.sprite = frames[0];
            sprite.enabled = true;
        }

        public void Stop()
        {
            playing = false;
            if (sprite == null) sprite = GetComponent<SpriteRenderer>();
            if (sprite != null) sprite.enabled = false;
        }

        void Update()
        {
            if (!playing || BattleGame.Instance == null || !BattleGame.Instance.Running) return;
            age += Time.deltaTime;
            int frame = Mathf.FloorToInt(age / secondsPerFrame);
            if (frame >= frames.Length) Stop();
            else sprite.sprite = frames[frame];
        }
    }
}
