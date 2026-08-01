using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace TapArena.Hub
{
    /// <summary>
    /// Drop this on any object in a game scene to get a quick way back to
    /// the hub while testing (Esc on desktop; Android's hardware/gesture
    /// back button also maps to KeyCode.Escape). This is a stand-in for
    /// testing — replace with a proper in-game "Menu" button before ship.
    /// </summary>
    public class ReturnToHubHotkey : MonoBehaviour
    {
        [SerializeField] private string hubSceneName = "Hub";

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
                SceneManager.LoadScene(hubSceneName, LoadSceneMode.Single);
        }
    }
}
