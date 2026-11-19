using UnityEngine;

namespace CatTanks
{
    // A scene marker, rather than a static flag, also works when domain reload is disabled.
    [DisallowMultipleComponent]
    public sealed class PreviewIsolation : MonoBehaviour
    {
        public static bool IsActive => FindObjectsOfType<PreviewIsolation>(true).Length > 0;
        public static void RequireLiveGame()
        {
            if (IsActive)
                throw new System.InvalidOperationException("Profile access is disabled in an isolated preview scene.");
        }
    }
}
