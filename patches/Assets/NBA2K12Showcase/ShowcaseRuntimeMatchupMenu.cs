using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CrowdEyes.AI4Animation.Basketball;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace NBA2K12Showcase
{
    /// <summary>
    /// Runtime matchup selector for the three supported historical rosters.
    /// It persists across the court-scene load, applies the selected identities and
    /// appearance profiles, then removes itself. It does not create licensed likenesses.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShowcaseRuntimeMatchupMenu : MonoBehaviour
    {
        [Serializable]
        private sealed class Player
        {
            public string name;
            public string position;
            public int number;
            public Player(string playerName, string playerPosition, int jersey)
            { name = playerName; position = playerPosition; number = jersey; }
        }

        [Serializable]
        private sealed class Team
        {
            public string name;
            public string abbreviation;
            public string location;
            public Color primary;
            public Color secondary;
            public Player[] players;
            public Team(string teamName, string code, string tagline, Color jersey, Color shorts, Player[] roster)
            { name = teamName; abbreviation = code; location = tagline; primary = jersey; secondary = shorts; players = roster; }
        }

        private sealed class Matchup
        {
            public Team home;
            public Team away;
            public string label => home.abbreviation + "  VS  " + away.abbreviation;
            public Matchup(Team homeTeam, Team awayTeam) { home = homeTeam; away = awayTeam; }
        }

        private enum MenuState { SelectMatchup, Preview, Loading }

        [SerializeField] private string courtSceneName = "BasketballDemo";
        private readonly Team heat = new Team("MIAMI HEAT", "MIA", "2013  •  MIAMI, FLORIDA",
            Hex("98002E"), Hex("121212"), new[] {
                new Player("Mario Chalmers", "PG", 15), new Player("Dwyane Wade", "SG", 3),
                new Player("LeBron James", "SF", 6), new Player("Chris Bosh", "PF", 1),
                new Player("Ray Allen", "SG", 34)
            });
        private readonly Team celtics = new Team("BOSTON CELTICS", "BOS", "2008  •  BOSTON, MASSACHUSETTS",
            Hex("007A33"), Hex("F5F5F0"), new[] {
                new Player("Rajon Rondo", "PG", 9), new Player("Ray Allen", "SG", 20),
                new Player("Paul Pierce", "SF", 34), new Player("Kevin Garnett", "PF", 5),
                new Player("Kendrick Perkins", "C", 43)
            });
        private readonly Team warriors = new Team("GOLDEN STATE WARRIORS", "GSW", "2017  •  SAN FRANCISCO BAY AREA",
            Hex("1D428A"), Hex("FFC72C"), new[] {
                new Player("Stephen Curry", "PG", 30), new Player("Klay Thompson", "SG", 11),
                new Player("Kevin Durant", "SF", 35), new Player("Draymond Green", "PF", 23),
                new Player("Zaza Pachulia", "C", 27)
            });

        private Matchup[] matchups;
        private MenuState state;
        private int selected;
        private float guiScale = 1f;
        private GUIStyle titleStyle, subtitleStyle, bodyStyle, mutedStyle, buttonStyle, panelStyle;
        private Texture2D backdrop, panel, swatch, buttonTexture, hoverTexture;
        private bool stylesReady;

        private void Awake()
        {
            if (transform.parent != null)
            {
                Debug.LogError("[NBA2K12 Showcase] Matchup menu must be on a root GameObject.", this);
                enabled = false;
                return;
            }

            matchups = new[] {
                new Matchup(heat, celtics),
                new Matchup(heat, warriors),
                new Matchup(celtics, warriors)
            };
            state = MenuState.SelectMatchup;
        }

        private void Update()
        {
            if (matchups == null || matchups.Length == 0) return;
            Keyboard keyboard = Keyboard.current;
            Gamepad pad = Gamepad.current;

            if (state == MenuState.SelectMatchup)
            {
                bool left = keyboard != null && (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame);
                bool right = keyboard != null && (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame);
                bool confirm = keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame);
                left |= pad != null && (pad.dpad.left.wasPressedThisFrame || pad.leftStick.left.wasPressedThisFrame);
                right |= pad != null && (pad.dpad.right.wasPressedThisFrame || pad.leftStick.right.wasPressedThisFrame);
                confirm |= pad != null && pad.buttonSouth.wasPressedThisFrame;
                if (left) selected = (selected + matchups.Length - 1) % matchups.Length;
                if (right) selected = (selected + 1) % matchups.Length;
                if (confirm) state = MenuState.Preview;
            }
            else if (state == MenuState.Preview)
            {
                bool back = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
                bool start = keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame);
                back |= pad != null && pad.buttonEast.wasPressedThisFrame;
                start |= pad != null && pad.buttonSouth.wasPressedThisFrame;
                if (back) state = MenuState.SelectMatchup;
                else if (start) LoadCourt();
            }
        }

        private void OnGUI()
        {
            if (state == MenuState.Loading || matchups == null) return;
            EnsureStyles();
            guiScale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            if (guiScale <= 0f) guiScale = 1f;
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(guiScale, guiScale, 1f));
            GUI.DrawTexture(new Rect(0, 0, 1920, 1080), backdrop);
            GUI.DrawTexture(new Rect(80, 64, 1760, 950), panel);
            if (state == MenuState.SelectMatchup) DrawMatchups();
            else DrawPreview();
            GUI.matrix = previous;
        }

        private void DrawMatchups()
        {
            GUI.Label(new Rect(120, 100, 1680, 70), "NBA 2K SHOWCASE", titleStyle);
            GUI.Label(new Rect(120, 170, 1680, 40), "CHOOSE A HISTORICAL MATCHUP", subtitleStyle);
            float width = 520f, gap = 40f, startX = (1920f - (width * 3f + gap * 2f)) * 0.5f;
            for (int i = 0; i < matchups.Length; i++)
            {
                Matchup matchup = matchups[i];
                float x = startX + i * (width + gap);
                Rect card = new Rect(x, 280, width, 540);
                GUI.color = matchup.home.primary;
                GUI.DrawTexture(card, swatch);
                GUI.color = Color.white;
                GUI.Box(card, GUIContent.none, panelStyle);
                GUI.Label(new Rect(x + 24, 320, width - 48, 56), matchup.home.abbreviation, titleStyle);
                GUI.Label(new Rect(x + 24, 380, width - 48, 34), matchup.home.name, subtitleStyle);
                GUI.Label(new Rect(x + 24, 430, width - 48, 30), matchup.home.location, mutedStyle);
                GUI.Label(new Rect(x + 24, 475, width - 48, 48), "VS", titleStyle);
                GUI.color = matchup.away.primary;
                GUI.DrawTexture(new Rect(x + 18, 540, width - 36, 3), swatch);
                GUI.color = Color.white;
                GUI.Label(new Rect(x + 24, 566, width - 48, 56), matchup.away.abbreviation, titleStyle);
                GUI.Label(new Rect(x + 24, 626, width - 48, 34), matchup.away.name, subtitleStyle);
                GUI.Label(new Rect(x + 24, 676, width - 48, 30), matchup.away.location, mutedStyle);
                if (i == selected)
                    GUI.Box(new Rect(x - 7, 273, width + 14, 554), GUIContent.none, buttonStyle);
                if (GUI.Button(new Rect(x + 24, 746, width - 48, 54),
                    i == selected ? "SELECT MATCHUP  ›" : "VIEW MATCHUP", buttonStyle))
                {
                    selected = i;
                    state = MenuState.Preview;
                }
            }
            GUI.Label(new Rect(100, 860, 1720, 45),
                "LEFT / RIGHT  CHANGE MATCHUP     •     ENTER / A  VIEW LINEUPS     •     ESC / B  BACK", mutedStyle);
            GUI.Label(new Rect(100, 915, 1720, 35),
                "PLAYER MODELS AND UNIFORMS USE YOUR CURRENT LOCAL ASSETS; THIS MENU DOES NOT ADD LICENSED NBA LIKENESSES.", mutedStyle);
        }

        private void DrawPreview()
        {
            Matchup matchup = matchups[selected];
            GUI.Label(new Rect(100, 100, 1720, 70), matchup.home.name + "  VS  " + matchup.away.name, titleStyle);
            GUI.Label(new Rect(120, 185, 800, 42), "HOME STARTING FIVE", subtitleStyle);
            GUI.Label(new Rect(1000, 185, 800, 42), "AWAY STARTING FIVE", subtitleStyle);
            DrawRosterColumn(matchup.home, 140, 250);
            DrawRosterColumn(matchup.away, 1020, 250);
            if (GUI.Button(new Rect(300, 790, 360, 68), "BACK TO MATCHUPS", buttonStyle))
                state = MenuState.SelectMatchup;
            if (GUI.Button(new Rect(1260, 790, 360, 68), "TIP OFF  ›", buttonStyle))
                LoadCourt();
            GUI.Label(new Rect(100, 900, 1720, 40), "ESC / B  BACK     •     ENTER / A  TIP OFF", mutedStyle);
        }

        private void DrawRosterColumn(Team team, float x, float y)
        {
            GUI.color = team.primary;
            GUI.DrawTexture(new Rect(x, y, 800, 510), swatch);
            GUI.color = Color.white;
            GUI.Box(new Rect(x, y, 800, 510), GUIContent.none, panelStyle);
            Player[] roster = team.players ?? Array.Empty<Player>();
            for (int i = 0; i < roster.Length; i++)
            {
                Player player = roster[i];
                float rowY = y + 34 + i * 82;
                GUI.Label(new Rect(x + 28, rowY, 88, 48), "#" + player.number, subtitleStyle);
                GUI.Label(new Rect(x + 128, rowY, 500, 48), player.name, bodyStyle);
                GUI.Label(new Rect(x + 650, rowY, 110, 48), player.position, mutedStyle);
            }
        }

        private void LoadCourt()
        {
            if (state == MenuState.Loading) return;
            if (string.IsNullOrWhiteSpace(courtSceneName))
            {
                Debug.LogError("[NBA2K12 Showcase] Court scene name is empty.", this);
                return;
            }
            state = MenuState.Loading;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnCourtLoaded;
            try
            {
                SceneManager.LoadScene(courtSceneName, LoadSceneMode.Single);
            }
            catch (Exception exception)
            {
                SceneManager.sceneLoaded -= OnCourtLoaded;
                Debug.LogError("[NBA2K12 Showcase] Could not load court scene '" + courtSceneName + "': " + exception.Message, this);
                state = MenuState.Preview;
            }
        }

        private void OnCourtLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != courtSceneName) return;
            SceneManager.sceneLoaded -= OnCourtLoaded;
            try { ApplyMatchupToScene(scene, matchups[selected]); }
            catch (Exception exception)
            {
                Debug.LogError("[NBA2K12 Showcase] Matchup loaded but runtime roster setup failed: " + exception, this);
            }
            Destroy(gameObject);
        }

        private static FieldInfo FindFieldInHierarchy(Type type, string fieldName)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
                if (field != null) return field;
            }
            return null;
        }

        private static void ApplyMatchupToScene(Scene scene, Matchup matchup)
        {
            BasketballTeamMember[] members = UnityEngine.Object.FindObjectsByType<BasketballTeamMember>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(member => member != null && member.gameObject.scene == scene)
                .OrderBy(member => member.PlayerIndex)
                .ToArray();
            if (members.Length != 10 ||
                Enumerable.Range(0, 10).Any(index =>
                    !members.Any(member => member.PlayerIndex == index && member.TeamId == (index < 5 ? 0 : 1))))
            {
                Debug.LogError("[NBA2K12 Showcase] Expected ten active players (indices 0-9, five per team); found " + members.Length + ". Roster identities were not changed.");
                return;
            }

            var profiles = new HashSet<BasketballTeamAppearanceProfile>();
            for (int index = 0; index < members.Length; index++)
            {
                bool home = index < 5;
                Team team = home ? matchup.home : matchup.away;
                Player player = team.players[index % 5];
                BasketballTeamMember member = members[index];
                ShowcasePlayerIdentity identity = member.GetComponent<ShowcasePlayerIdentity>();
                if (identity == null) identity = member.gameObject.AddComponent<ShowcasePlayerIdentity>();
                identity.Configure(player.name, player.position, player.number, team.name);

                FieldInfo jerseyField = FindFieldInHierarchy(typeof(BasketballTeamMember), "jerseyNumber");
                if (jerseyField != null && jerseyField.FieldType == typeof(int) && !jerseyField.IsInitOnly)
                    jerseyField.SetValue(member, player.number);

                foreach (BasketballPlayerAppearance appearance in member.GetComponentsInChildren<BasketballPlayerAppearance>(true))
                {
                    FieldInfo profileField = FindFieldInHierarchy(typeof(BasketballPlayerAppearance), "teamProfiles");
                    Array assigned = profileField != null ? profileField.GetValue(appearance) as Array : null;
                    if (assigned != null)
                    {
                        for (int slot = 0; slot < assigned.Length; slot++)
                        {
                            BasketballTeamAppearanceProfile profile = assigned.GetValue(slot) as BasketballTeamAppearanceProfile;
                            if (profile == null || !profiles.Add(profile)) continue;
                            Team source = slot == 0 ? matchup.home : matchup.away;
                            profile.Configure(slot, source.name, source.primary, source.secondary, Color.white);
                        }
                    }
                    appearance.Apply();
                }
            }

            ShowcaseBroadcastHud hud = UnityEngine.Object.FindObjectsByType<ShowcaseBroadcastHud>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate != null && candidate.gameObject.scene == scene);
            if (hud != null) hud.ConfigureTeams(matchup.home.name, matchup.away.name);
            Debug.Log("[NBA2K12 Showcase] Runtime matchup applied: " + matchup.home.name + " vs " + matchup.away.name + ". Player geometry remains dependent on installed local assets.");
        }

        private void EnsureStyles()
        {
            if (stylesReady) return;
            backdrop = MakeTexture(new Color(0.015f, 0.02f, 0.035f, 1f));
            panel = MakeTexture(new Color(0.025f, 0.035f, 0.055f, 0.97f));
            swatch = MakeTexture(Color.white);
            buttonTexture = MakeTexture(new Color(0.10f, 0.13f, 0.19f, 0.96f));
            hoverTexture = MakeTexture(new Color(0.24f, 0.29f, 0.38f, 1f));
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 38, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
            subtitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 23, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            mutedStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, alignment = TextAnchor.MiddleCenter, wordWrap = true, normal = { textColor = new Color(0.74f, 0.80f, 0.87f) } };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 19, fontStyle = FontStyle.Bold, normal = { textColor = Color.white, background = buttonTexture }, hover = { textColor = Color.white, background = hoverTexture }, active = { textColor = Color.white, background = buttonTexture } };
            panelStyle = new GUIStyle(GUI.skin.box) { normal = { background = panel }, border = new RectOffset(8, 8, 8, 8) };
            stylesReady = true;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnCourtLoaded;
            DestroyTexture(backdrop);
            DestroyTexture(panel);
            DestroyTexture(swatch);
            DestroyTexture(buttonTexture);
            DestroyTexture(hoverTexture);
        }

        private static Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static void DestroyTexture(Texture2D texture)
        {
            if (texture != null) Destroy(texture);
        }

        private static Color Hex(string value)
        {
            if (!ColorUtility.TryParseHtmlString("#" + value, out Color color))
                throw new ArgumentException("Invalid team color: " + value);
            return color;
        }
    }
}
