using UnityEngine;
using UnityEngine.Rendering;

namespace MegaTanks
{
    [RequireComponent(typeof(EnemyVehicleArtwork))]
    public sealed class TrojanScrapCat : MonoBehaviour
    {
        public const string EnemyId = "TrojanScrapCat";
        const float DogGroundOffset = .55f;
        const int OccupantSortingOrder = 16;
        // Positions and aperture come from the art manifest's 600 PPU assembly.
        static readonly Vector2[] DogSockets = { CanvasPoint(520, 840), CanvasPoint(650, 840), CanvasPoint(780, 840) };
        static Vector2 CanvasPoint(float x, float y) => new Vector2(800 - x, 952 - y) / 600;

        [SerializeField, Min(.15f), Tooltip("Distance between the carrier and player hull edges, in world units.")]
        float releaseDistance = 3;
        [SerializeField] Transform shell;
        [SerializeField] EnemyDefinition dog;

        readonly CommandoArtwork[] occupants = new CommandoArtwork[DogSockets.Length];
        SpriteMask feetMask;
        bool released;
        Vector3 shellStart;
        float throwTime;
        int runId;

        internal void InitializeOccupants()
        {
            gameObject.AddComponent<SortingGroup>().sortingOrder = 15;
            var opening = new GameObject("Feet aperture").transform;
            opening.SetParent(transform, false);
            opening.localPosition = CanvasPoint(720, 810);
            opening.localScale = new Vector3(620f / 600, 70f / 600, 1);
            feetMask = opening.gameObject.AddComponent<SpriteMask>();
            feetMask.sprite = ShapeFactory.SquareSprite;

            float size = 1 / Mathf.Abs(transform.lossyScale.y);
            for (int i = 0; i < occupants.Length; i++)
            {
                var occupant = Instantiate(dog.commandoPrefab, transform, false);
                occupants[i] = occupant;
                occupant.transform.localScale *= size;
                occupant.transform.localPosition = DogSockets[i] + Vector2.up * (DogGroundOffset * size);
                occupant.Animation.GetComponent<MeshRenderer>().sortingOrder = OccupantSortingOrder;
                occupant.Animation.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                occupant.ResetVisual();
            }
        }

        internal void AnimateOccupants(bool moving, float deltaTime)
        {
            for (int i = 0; i < occupants.Length; i++)
            {
                var occupant = occupants[i];
                bool startWalking = moving && occupant.CurrentAnimation != "walk";
                occupant.Locomotion(moving);
                occupant.Advance(deltaTime + (startWalking ? i * .19f : 0));
            }
        }

        internal bool InReleaseRange(Collider2D carrier, Collider2D hull)
            => CommandoMelee.DistanceSquared(carrier, hull) <= releaseDistance * releaseDistance;

        internal void Release(EnemyEntity carrier, BattleGame game)
        {
            if (released) return;
            released = true;
            Physics2D.SyncTransforms();
            var bounds = carrier.GetComponent<Collider2D>().bounds;
            float direction = carrier.ApproachDirection;
            float front = bounds.center.x + direction * bounds.extents.x;
            var hull = game.HullHitbox.GetComponent<Collider2D>().bounds;
            // Keep the lead dog outside the tank even if it drove into the carrier.
            front = direction > 0 ? Mathf.Min(front, hull.min.x - .4f) : Mathf.Max(front, hull.max.x + .4f);
            var stats = dog.AtDifficulty(carrier.Difficulty);
            feetMask.enabled = false;
            int deployedSortingOrder = dog.commandoPrefab.Animation.GetComponent<MeshRenderer>().sortingOrder;
            for (int i = 0; i < occupants.Length; i++)
            {
                var occupant = occupants[i];
                occupant.Animation.maskInteraction = SpriteMaskInteraction.None;
                occupant.Animation.GetComponent<MeshRenderer>().sortingOrder = deployedSortingOrder;
                var position = new Vector2(front - direction * (.85f * i), BattleGame.GroundHeight + DogGroundOffset);
                var enemy = game.SpawnEnemy(stats, position, carrier.Difficulty, occupant);
                if (!enemy.Alive) occupant.gameObject.SetActive(false);
                occupants[i] = null;
            }

            if (shell == null) return;
            runId = game.RunId;
            transform.SetParent(null, true);
            GetComponent<EnemyVehicleArtwork>().SetHitFlash(false);
            shellStart = shell.localPosition;
        }

        void Update()
        {
            if (!released || shell == null) return;
            var game = BattleGame.Instance;
            if (game == null || !game.Deployed || game.RunId != runId)
            {
                Destroy(gameObject);
                return;
            }

            throwTime += Time.deltaTime;
            float progress = Mathf.Clamp01(throwTime / .6f);
            shell.localPosition = shellStart + new Vector3(-.8f * progress, 3.6f * progress * (1 - progress), 0);
            shell.localRotation = Quaternion.Euler(0, 0, 120 * progress);
            if (progress >= 1) shell.gameObject.SetActive(false);
            if (throwTime >= 1.6f) Destroy(gameObject);
        }
    }
}
