#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NBA2K12Showcase.Editor
{
    /// <summary>
    /// One-click entry point for the core local showcase setup.
    /// Uses existing setup tools; does not generate or overwrite the upstream demo scene.
    /// </summary>
    public static class ShowcaseQuickStartTools
    {
        private const string DemoScene = "Assets/Scenes/BasketballDemo.unity";

        [MenuItem("Tools/NBA2K12 Showcase/16 - One-Click Core Setup")]
        public static void ApplyCoreSetup()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != DemoScene)
            {
                EditorUtility.DisplayDialog(
                    "NBA2K12 Showcase Quick Start",
                    "Open Assets/Scenes/BasketballDemo.unity first. No changes were made.",
                    "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "NBA2K12 Showcase Quick Start",
                    "This runs the existing roster/HUD, gameplay-camera, and TV-presentation setup tools in sequence. " +
                    "It saves local scene/settings changes. It does not download assets or replace player models. Continue?",
                    "Apply Core Setup",
                    "Cancel"))
            {
                return;
            }

            Debug.Log("[NBA2K12 Showcase] Quick Start: attaching USB gamepad support.");
            var matchController = Object.FindFirstObjectByType<CrowdEyes.AI4Animation.Basketball.BasketballMatchController>();
            if (matchController != null && matchController.GetComponent<ShowcaseGamepadSupport>() == null)
                Undo.AddComponent<ShowcaseGamepadSupport>(matchController.gameObject);

            Debug.Log("[NBA2K12 Showcase] Quick Start: applying broadcast roster/HUD.");
            ShowcaseGameSetupTools.ApplyBroadcastAndRoster();

            Debug.Log("[NBA2K12 Showcase] Quick Start: applying gameplay camera framing.");
            ShowcasePresentationTools.ApplyGameplayCameraFraming();

            Debug.Log("[NBA2K12 Showcase] Quick Start: applying TV presentation settings.");
            ShowcasePresentationTools.ApplyTvPresentation();

            Debug.Log(
                "[NBA2K12 Showcase] Quick Start sequence completed. Dismiss any tool dialogs and inspect the scene in Play Mode. " +
                "This is a setup convenience, not a claim that the high-fidelity acceptance gate has passed.");
        }
    }
}
#endif
