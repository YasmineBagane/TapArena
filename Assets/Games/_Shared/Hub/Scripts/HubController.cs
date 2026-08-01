using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace TapArena.Hub
{
    /// <summary>
    /// Central menu (SRS §7.1): one tappable tile per game. Unlocked tiles
    /// load that game's scene (Single mode, so the previous game's objects
    /// are fully torn down — this is what fixes "both games start at once").
    /// Locked tiles (future roadmap games) render dimmed and non-interactive.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class HubController : MonoBehaviour
    {
        [Serializable]
        public class GameTile
        {
            public string displayName = "Game Name";

            [Tooltip("Exact scene name as added in File > Build Settings.")]
            public string sceneName;

            [Tooltip("Locked tiles (roadmap games, SRS §7.1) show but aren't tappable yet.")]
            public bool unlocked = true;

            public string lockedSublabel = "Locked";
        }

        [SerializeField] private GameTile[] tiles;
        [SerializeField] private string tileRootName = "tile-root";

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            var tileRoot = root.Q<VisualElement>(tileRootName);

            if (tileRoot == null)
            {
                Debug.LogError($"HubController: could not find '{tileRootName}' in the UXML tree.");
                return;
            }

            tileRoot.Clear();

            foreach (var tile in tiles)
            {
                var button = new Button { text = tile.displayName };
                button.AddToClassList("hub-tile");

                if (tile.unlocked)
                {
                    string sceneToLoad = tile.sceneName;
                    button.clicked += () => LoadGame(sceneToLoad);
                }
                else
                {
                    button.AddToClassList("hub-tile--locked");
                    button.SetEnabled(false);

                    var sublabel = new Label(tile.lockedSublabel);
                    sublabel.AddToClassList("hub-tile__sublabel");
                    button.Add(sublabel);
                }

                tileRoot.Add(button);
            }
        }

        private void LoadGame(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("HubController: tile has no scene name assigned.");
                return;
            }

            // Single mode fully unloads the previous scene first — this is
            // what guarantees only one game's objects (and Start() calls)
            // are ever alive at a time.
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
    }
}
