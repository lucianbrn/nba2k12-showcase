#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NBA2K12Showcase.Editor
{
    /// <summary>Creates a playable bootstrap menu and puts it first in the local build scene list.</summary>
    public static class ShowcaseRuntimeMenuTools
    {
        private const string MenuScenePath = "Assets/Scenes/NBA2K12Menu.unity";
        private const string CourtScenePath = "Assets/Scenes/BasketballDemo.unity";

        [MenuItem("Tools/NBA2K12 Showcase/20 - Create Playable Matchup Menu")]
        public static void CreatePlayableMenu()
        {
            if (!System.IO.File.Exists(CourtScenePath))
            {
                EditorUtility.DisplayDialog("Playable Matchup Menu",
                    "Court scene not found at " + CourtScenePath + ". Open the NeuralCourt project and run the showcase installer first.", "OK");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath) != null &&
                !EditorUtility.DisplayDialog("Playable Matchup Menu",
                    "NBA2K12Menu.unity already exists. Replace it with a fresh menu scene?", "Replace", "Cancel"))
                return;

            Scene menuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject menuRoot = new GameObject("NBA 2K Showcase - Matchup Menu");
            Undo.RegisterCreatedObjectUndo(menuRoot, "Create Matchup Menu");
            menuRoot.AddComponent<ShowcaseRuntimeMatchupMenu>();
            if (!EditorSceneManager.SaveScene(menuScene, MenuScenePath))
            {
                EditorUtility.DisplayDialog("Playable Matchup Menu", "Could not save " + MenuScenePath + ".", "OK");
                return;
            }

            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            var ordered = new System.Collections.Generic.List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(MenuScenePath, true),
                new EditorBuildSettingsScene(CourtScenePath, true)
            };
            foreach (EditorBuildSettingsScene scene in existing)
            {
                if (scene == null || !scene.enabled) continue;
                if (scene.path == MenuScenePath || scene.path == CourtScenePath) continue;
                ordered.Add(scene);
            }
            EditorBuildSettings.scenes = ordered.ToArray();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            EditorUtility.DisplayDialog("Playable Matchup Menu Ready",
                "Created " + MenuScenePath + " and set it as the first enabled build scene. Press Play to choose one of the three supported matchups. The menu loads BasketballDemo and applies roster identities, team appearance profiles, jersey numbers, and scoreboard labels at runtime. Authentic NBA-grade models, stitched uniforms, and new animation clips still require suitable licensed/local assets.",
                "OK");
        }
    }
}
#endif
