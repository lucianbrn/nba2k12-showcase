#if UNITY_EDITOR
using System;
using System.Linq;
using CrowdEyes.AI4Animation.Basketball;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NBA2K12Showcase.Editor
{
    /// <summary>Creates the broadcast HUD and the requested 2013 Heat vs 2008 Celtics default roster.</summary>
    public static class ShowcaseGameSetupTools
    {
        private const string DemoScene = "Assets/Scenes/BasketballDemo.unity";
        private const string AssetRoot = "Assets/NBA2K12Showcase";
        private const string TeamRoot = AssetRoot + "/Teams";

        private struct RosterEntry
        {
            public int PlayerIndex;
            public int TeamId;
            public int Number;
            public string Name;
            public string Position;
            public string Team;
        }

        private static readonly RosterEntry[] DemoRoster =
        {
            new() { PlayerIndex = 0, TeamId = 0, Number = 15, Name = "Mario Chalmers", Position = "PG", Team = "Miami Heat" },
            new() { PlayerIndex = 1, TeamId = 0, Number = 3, Name = "Dwyane Wade", Position = "SG", Team = "Miami Heat" },
            new() { PlayerIndex = 2, TeamId = 0, Number = 6, Name = "LeBron James", Position = "SF", Team = "Miami Heat" },
            new() { PlayerIndex = 3, TeamId = 0, Number = 1, Name = "Chris Bosh", Position = "PF", Team = "Miami Heat" },
            new() { PlayerIndex = 4, TeamId = 0, Number = 34, Name = "Ray Allen", Position = "SG", Team = "Miami Heat" },

            new() { PlayerIndex = 5, TeamId = 1, Number = 9, Name = "Rajon Rondo", Position = "PG", Team = "Boston Celtics" },
            new() { PlayerIndex = 6, TeamId = 1, Number = 20, Name = "Ray Allen", Position = "SG", Team = "Boston Celtics" },
            new() { PlayerIndex = 7, TeamId = 1, Number = 34, Name = "Paul Pierce", Position = "SF", Team = "Boston Celtics" },
            new() { PlayerIndex = 8, TeamId = 1, Number = 5, Name = "Kevin Garnett", Position = "PF", Team = "Boston Celtics" },
            new() { PlayerIndex = 9, TeamId = 1, Number = 43, Name = "Kendrick Perkins", Position = "C", Team = "Boston Celtics" },
        };

        [MenuItem("Tools/NBA2K12 Showcase/9 - Apply Broadcast HUD and 2013 Heat vs 2008 Celtics Roster")]
        public static void ApplyBroadcastAndRoster()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !string.Equals(scene.path, DemoScene, StringComparison.Ordinal))
            {
                EditorUtility.DisplayDialog(
                    "NBA2K12 Showcase Setup",
                    "Open Assets/Scenes/BasketballDemo.unity before applying the demo roster. No changes were made.",
                    "OK");
                return;
            }

            BasketballTeamMember[] members = UnityEngine.Object.FindObjectsByType<BasketballTeamMember>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(m => m != null && m.gameObject.scene == scene)
                .OrderBy(m => m.PlayerIndex)
                .ToArray();

            if (members.Length != 10 || DemoRoster.Any(entry =>
                    !members.Any(member => member.PlayerIndex == entry.PlayerIndex && member.TeamId == entry.TeamId)))
            {
                EditorUtility.DisplayDialog(
                    "NBA2K12 Showcase Setup",
                    "Expected ten 5-on-5 players with PlayerIndex 0-9 (five each for TeamId 0 and TeamId 1). Found " +
                    members.Length + ". Run the upstream 5v5 scene builder only if you are prepared to regenerate its synthetic scene.",
                    "OK");
                return;
            }

            EnsureFolder(AssetRoot);
            EnsureFolder(TeamRoot);
            BasketballTeamAppearanceProfile heat = GetOrCreateProfile(
                TeamRoot + "/Showcase_MIAMI_HEAT.asset", 0, "Miami Heat",
                Hex("98002E"), Hex("121212"), Hex("FFFFFF"));
            BasketballTeamAppearanceProfile celtics = GetOrCreateProfile(
                TeamRoot + "/Showcase_BOSTON_CELTICS.asset", 1, "Boston Celtics",
                Hex("007A33"), Hex("F5F5F0"), Hex("FFFFFF"));

            int updated = 0;
            foreach (RosterEntry entry in DemoRoster)
            {
                BasketballTeamMember member = members.First(m =>
                    m.PlayerIndex == entry.PlayerIndex && m.TeamId == entry.TeamId);
                ShowcasePlayerIdentity identity = member.GetComponent<ShowcasePlayerIdentity>();
                if (identity == null) identity = Undo.AddComponent<ShowcasePlayerIdentity>(member.gameObject);

                Undo.RecordObject(identity, "Set Showcase Roster Identity");
                identity.Configure(entry.Name, entry.Position, entry.Number, entry.Team);
                EditorUtility.SetDirty(identity);

                SerializedObject memberData = new SerializedObject(member);
                SerializedProperty number = memberData.FindProperty("jerseyNumber");
                if (number != null)
                {
                    number.intValue = entry.Number;
                    memberData.ApplyModifiedProperties();
                }

                foreach (BasketballPlayerAppearance appearance in
                         member.GetComponentsInChildren<BasketballPlayerAppearance>(true))
                {
                    if (appearance == null) continue;
                    SerializedObject visual = new SerializedObject(appearance);
                    SerializedProperty profiles = visual.FindProperty("teamProfiles");
                    if (profiles != null && profiles.isArray)
                    {
                        profiles.arraySize = 2;
                        profiles.GetArrayElementAtIndex(0).objectReferenceValue = heat;
                        profiles.GetArrayElementAtIndex(1).objectReferenceValue = celtics;
                        visual.ApplyModifiedProperties();
                        appearance.Apply();
                        PrefabUtility.RecordPrefabInstancePropertyModifications(appearance);
                    }
                }

                updated++;
            }

            BasketballMatchController match = UnityEngine.Object.FindFirstObjectByType<BasketballMatchController>();
            BasketballScoreTracker scores = UnityEngine.Object.FindFirstObjectByType<BasketballScoreTracker>();
            BasketballPossessionManager possession = UnityEngine.Object.FindFirstObjectByType<BasketballPossessionManager>();
            GameObject root = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "NBA2K12 Showcase Runtime");
            if (root == null)
            {
                root = new GameObject("NBA2K12 Showcase Runtime");
                Undo.RegisterCreatedObjectUndo(root, "Create Showcase Runtime Systems");
                SceneManager.MoveGameObjectToScene(root, scene);
            }

            ShowcaseBroadcastHud hud = root.GetComponent<ShowcaseBroadcastHud>();
            if (hud == null) hud = Undo.AddComponent<ShowcaseBroadcastHud>(root);
            hud.Configure(match, scores, possession);
            EditorUtility.SetDirty(hud);

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);

            string report = "Applied requested 2013 Heat / 2008 Celtics identities to " + updated +
                " players and attached the live broadcast HUD.\n\n" +
                "The scoreboard reads the live score tracker; shot-clock violations use the possession manager. " +
                "Clock rules are an implementation layer and should be play-tested before release.\n\n" +
                "Note: this is a roster identity/configuration preset, not authentic NBA 2K player geometry or an extracted save roster. " +
                "It changes the local NeuralCourt submodule/scene only.";
            Debug.Log("[NBA2K12 Showcase] " + report);
            EditorUtility.DisplayDialog("NBA2K12 Showcase Setup", report, "OK");
        }

        private static BasketballTeamAppearanceProfile GetOrCreateProfile(
            string path, int id, string name, Color jersey, Color shorts, Color number)
        {
            BasketballTeamAppearanceProfile profile =
                AssetDatabase.LoadAssetAtPath<BasketballTeamAppearanceProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<BasketballTeamAppearanceProfile>();
                profile.name = System.IO.Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(profile, path);
            }
            profile.Configure(id, name, jersey, shorts, number);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static Color Hex(string color)
        {
            if (!ColorUtility.TryParseHtmlString("#" + color, out Color parsed))
                throw new ArgumentException("Invalid color: " + color);
            return parsed;
        }

        private static void EnsureFolder(string assetPath)
        {
            string[] parts = assetPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
