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
    /// <summary>Selectable era matchups. The simulation supports two teams per game; each menu entry selects a pair.</summary>
    public static class ShowcaseEraMatchupTools
    {
        private const string DemoScene = "Assets/Scenes/BasketballDemo.unity";
        private const string TeamRoot = "Assets/NBA2K12Showcase/Teams";

        private struct Player
        {
            public string Name;
            public string Position;
            public int Number;
            public Player(string name, string position, int number)
            { Name = name; Position = position; Number = number; }
        }

        private sealed class Team
        {
            public string Name;
            public Color Jersey;
            public Color Shorts;
            public Color Number;
            public Player[] Players;
        }

        private static readonly Team Heat2013 = new Team
        {
            Name = "MIAMI HEAT",
            Jersey = Hex("98002E"), Shorts = Hex("121212"), Number = Color.white,
            Players = new[] {
                new Player("Mario Chalmers", "PG", 15), new Player("Dwyane Wade", "SG", 3),
                new Player("LeBron James", "SF", 6), new Player("Chris Bosh", "PF", 1),
                new Player("Ray Allen", "SG", 34)
            }
        };

        private static readonly Team Celtics2008 = new Team
        {
            Name = "BOSTON CELTICS",
            Jersey = Hex("007A33"), Shorts = Hex("F5F5F0"), Number = Color.white,
            Players = new[] {
                new Player("Rajon Rondo", "PG", 9), new Player("Ray Allen", "SG", 20),
                new Player("Paul Pierce", "SF", 34), new Player("Kevin Garnett", "PF", 5),
                new Player("Kendrick Perkins", "C", 43)
            }
        };

        private static readonly Team Warriors2017 = new Team
        {
            Name = "GOLDEN STATE WARRIORS",
            Jersey = Hex("1D428A"), Shorts = Hex("FFC72C"), Number = Color.white,
            Players = new[] {
                new Player("Stephen Curry", "PG", 30), new Player("Klay Thompson", "SG", 11),
                new Player("Kevin Durant", "SF", 35), new Player("Draymond Green", "PF", 23),
                new Player("Zaza Pachulia", "C", 27)
            }
        };

        [MenuItem("Tools/NBA2K12 Showcase/17 - Matchup: 2013 Heat vs 2008 Celtics")]
        private static void HeatVsCeltics() => Apply(Heat2013, Celtics2008);

        [MenuItem("Tools/NBA2K12 Showcase/18 - Matchup: 2013 Heat vs 2017 Warriors")]
        private static void HeatVsWarriors() => Apply(Heat2013, Warriors2017);

        [MenuItem("Tools/NBA2K12 Showcase/19 - Matchup: 2008 Celtics vs 2017 Warriors")]
        private static void CelticsVsWarriors() => Apply(Celtics2008, Warriors2017);

        private static void Apply(Team home, Team away)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != DemoScene)
            {
                EditorUtility.DisplayDialog("Era Matchup", "Open Assets/Scenes/BasketballDemo.unity first. No changes were made.", "OK");
                return;
            }

            BasketballTeamMember[] members = UnityEngine.Object.FindObjectsByType<BasketballTeamMember>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(m => m != null && m.gameObject.scene == scene)
                .OrderBy(m => m.PlayerIndex).ToArray();

            if (members.Length != 10 ||
                Enumerable.Range(0, 10).Any(i => !members.Any(m => m.PlayerIndex == i && m.TeamId == (i < 5 ? 0 : 1))))
            {
                EditorUtility.DisplayDialog("Era Matchup", "Expected ten players with indices 0-9 and five members per team. Found " + members.Length + ". No changes were made.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Era Matchup",
                "Set " + home.Name + " vs " + away.Name + "?\n\nThis changes player names, jersey numbers, team colors and scoreboard labels. It does not install authentic NBA player meshes, uniforms or animations.",
                "Apply Matchup", "Cancel"))
                return;

            EnsureFolder("Assets/NBA2K12Showcase");
            EnsureFolder(TeamRoot);
            BasketballTeamAppearanceProfile homeProfile = Profile(home, 0);
            BasketballTeamAppearanceProfile awayProfile = Profile(away, 1);
            int changed = 0;

            for (int i = 0; i < 10; i++)
            {
                bool isHome = i < 5;
                Team team = isHome ? home : away;
                Player entry = team.Players[i % 5];
                BasketballTeamMember member = members.First(m => m.PlayerIndex == i);
                ShowcasePlayerIdentity identity = member.GetComponent<ShowcasePlayerIdentity>();
                if (identity == null) identity = Undo.AddComponent<ShowcasePlayerIdentity>(member.gameObject);
                Undo.RecordObject(identity, "Set Era Matchup Identity");
                identity.Configure(entry.Name, entry.Position, entry.Number, team.Name);
                EditorUtility.SetDirty(identity);

                SerializedObject memberData = new SerializedObject(member);
                SerializedProperty number = memberData.FindProperty("jerseyNumber");
                if (number != null) { number.intValue = entry.Number; memberData.ApplyModifiedProperties(); }

                foreach (BasketballPlayerAppearance appearance in member.GetComponentsInChildren<BasketballPlayerAppearance>(true))
                {
                    SerializedObject visual = new SerializedObject(appearance);
                    SerializedProperty profiles = visual.FindProperty("teamProfiles");
                    if (profiles != null && profiles.isArray)
                    {
                        profiles.arraySize = 2;
                        profiles.GetArrayElementAtIndex(0).objectReferenceValue = homeProfile;
                        profiles.GetArrayElementAtIndex(1).objectReferenceValue = awayProfile;
                        visual.ApplyModifiedProperties();
                        appearance.Apply();
                        PrefabUtility.RecordPrefabInstancePropertyModifications(appearance);
                    }
                }
                changed++;
            }

            ShowcaseBroadcastHud hud = UnityEngine.Object.FindFirstObjectByType<ShowcaseBroadcastHud>();
            if (hud != null)
            {
                SerializedObject hudData = new SerializedObject(hud);
                hudData.FindProperty("homeTeamName").stringValue = home.Name;
                hudData.FindProperty("awayTeamName").stringValue = away.Name;
                hudData.ApplyModifiedProperties();
                EditorUtility.SetDirty(hud);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            string report = "Applied " + home.Name + " vs " + away.Name + " to " + changed +
                " player identities and team appearance profiles.\n\n" +
                "Visual assets are still the current project's synthetic base models. This matchup tool does not create NBA-grade player likenesses, cloth simulation, or new animation clips.";
            Debug.Log("[NBA2K12 Showcase] " + report);
            EditorUtility.DisplayDialog("Era Matchup Applied", report, "OK");
        }

        private static BasketballTeamAppearanceProfile Profile(Team team, int id)
        {
            string safe = team.Name.Replace(" ", "_");
            string path = TeamRoot + "/Era_" + safe + ".asset";
            BasketballTeamAppearanceProfile profile = AssetDatabase.LoadAssetAtPath<BasketballTeamAppearanceProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<BasketballTeamAppearanceProfile>();
                profile.name = PathName(safe);
                AssetDatabase.CreateAsset(profile, path);
            }
            profile.Configure(id, team.Name, team.Jersey, team.Shorts, team.Number);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static string PathName(string name) => "Era_" + name;

        private static Color Hex(string value)
        {
            if (!ColorUtility.TryParseHtmlString("#" + value, out Color color))
                throw new ArgumentException("Invalid color " + value);
            return color;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
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
