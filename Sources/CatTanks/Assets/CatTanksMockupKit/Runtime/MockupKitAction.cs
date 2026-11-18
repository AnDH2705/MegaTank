using UnityEngine;
using UnityEngine.UI;

namespace CatTanks.MockupKit
{
    [RequireComponent(typeof(Button))]
    public sealed class MockupKitAction : MonoBehaviour
    {
        public string action;
        void Start()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                var preview = GetComponentInParent<MockupKitPreview>();
                if (preview) preview.Navigate(action);
            });
        }
    }
}
