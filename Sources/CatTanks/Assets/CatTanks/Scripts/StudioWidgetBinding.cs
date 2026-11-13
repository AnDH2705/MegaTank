using UnityEngine;

namespace CatTanks
{
    public sealed class StudioWidgetBinding : MonoBehaviour
    {
        [HideInInspector] public string id;
        [HideInInspector] public string kind;
        [Tooltip("Keep this prefab's RectTransform and typography when gameplay data updates.")]
        public bool authored = true;
    }
}
