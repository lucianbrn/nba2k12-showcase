using System;
using System.Linq;
using CrowdEyes.AI4Animation.Basketball;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace NBA2K12Showcase
{
    /// <summary>
    /// Optional pre-game flow for the Lakers/Celtics showcase. Displays team selection,
    /// confirms a five-player roster, runs a broadcast intro card, then loads the court scene.
    /// Add this component to a bootstrap/menu scene; do not add it to BasketballDemo itself.
    /// </summary>
    public sealed class ShowcasePreGameIntro : MonoBehaviour
    {
        [Serializable]
        public struct PlayerCard
        {
            public int number;
            public string name;
            public string position;
        }

        [Serializable]
        public struct TeamCard
        {
            public string teamName;
            public string abbreviation;
            public string arenaTagline;
            public Color primary;
            public Color secondary;
            public PlayerCard[] roster;
        }

        private enum FlowState { TeamSelect, Roster, Intro, Loading }

        [Header("Scene flow")]
        [SerializeField] private string courtSceneName = "BasketballDemo";
        [SerializeField] private float introDurationSeconds = 8f;
        [SerializeField] private bool requireConfirmationBeforeIntro = true;

        [Header("Teams")]
        [SerializeField] private TeamCard[] teams =
        {
            new TeamCard
            {
                teamName = "LOS ANGELES LAKERS",
                abbreviation = "LAL",
                arenaTagline = "LOS ANGELES, CALIFORNIA  •  PURPLE & GOLD",
                primary = new Color(0.22f, 0.07f, 0.38f),
                secondary = new Color(0.99f, 0.73f, 0.15f),
                roster = new[]
                {
                    new PlayerCard { number = 2, name = "Derek Fisher", position = "PG" },
                    new PlayerCard { number = 24, name = "Kobe Bryant", position = "SG" },
                    new PlayerCard { number = 15, name = "Metta World Peace", position = "SF" },
                    new PlayerCard { number = 16, name = "Pau Gasol", position = "PF" },
                    new PlayerCard { number = 17, name = "Andrew Bynum", position = "C" }
                }
            },
            new TeamCard
            {
                teamName = "BOSTON CELTICS",
                abbreviation = "BOS",
                arenaTagline = "BOSTON, MASSACHUSETTS  •  GREEN & WHITE",
                primary = new Color(0.0f, 0.32f, 0.17f),
                secondary = new Color(0.93f, 0.92f, 0.86f),
                roster = new[]
                {
                    new PlayerCard { number = 9, name = "Rajon Rondo", position = "PG" },
                    new PlayerCard { number = 20, name = "Ray Allen", position = "SG" },
                    new PlayerCard { number = 34, name = "Paul Pierce", position = "SF" },
                    new PlayerCard { number = 5, name = "Kevin Garnett", position = "PF" },
                    new PlayerCard { number = 7, name = "Jermaine O'Neal", position = "C" }
                }
            }
        };

        private FlowState state;
        private int selectedTeam;
        private int focusedTeam;
        private float introStarted;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle rosterStyle;
        private GUIStyle smallStyle;
        private GUIStyle buttonStyle;
        private Texture2D dimTexture;
        private Texture2D panelTexture;
        private Texture2D primaryTexture;
        private Texture2D secondaryTexture;
        private bool stylesReady;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad pad = Gamepad.current;

            switch (state)
            {
                case FlowState.TeamSelect:
                    if (keyboard != null)
                    {
                        if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
                            focusedTeam = (focusedTeam + teams.Length - 1) % teams.Length;
                        if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
                            focusedTeam = (focusedTeam + 1) % teams.Length;
                        if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                            ShowRoster(focusedTeam);
                    }
                    if (pad != null)
                    {
                        if (pad.dpad.left.wasPressedThisFrame || pad.leftStick.left.wasPressedThisFrame)
                            focusedTeam = (focusedTeam + teams.Length - 1) % teams.Length;
                        if (pad.dpad.right.wasPressedThisFrame || pad.leftStick.right.wasPressedThisFrame)
                            focusedTeam = (focusedTeam + 1) % teams.Length;
                        if (pad.buttonSouth.wasPressedThisFrame)
                            ShowRoster(focusedTeam);
                    }
                    break;

                case FlowState.Roster:
                    if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                        state = FlowState.TeamSelect;
                    if (pad != null && pad.buttonEast.wasPressedThisFrame)
                        state = FlowState.TeamSelect;
                    if (keyboard != null &&
                        (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                        StartIntro();
                    if (pad != null && pad.buttonSouth.wasPressedThisFrame)
                        StartIntro();
                    break;

                case FlowState.Intro:
                    bool skip = keyboard != null &&
                                (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame);
                    skip |= pad != null && pad.buttonSouth.wasPressedThisFrame;
                    if (skip || Time.unscaledTime - introStarted >= Mathf.Max(2f, introDurationSeconds))
                        LoadCourt();
                    break;
            }
        }

        private void ShowRoster(int teamIndex)
        {
            if (teams == null || teams.Length == 0)
                return;
            selectedTeam = Mathf.Clamp(teamIndex, 0, teams.Length - 1);
            state = FlowState.Roster;
        }

        private void StartIntro()
        {
            if (requireConfirmationBeforeIntro)
            {
                state = FlowState.Intro;
                introStarted = Time.unscaledTime;
            }
            else
            {
                LoadCourt();
            }
        }

        private void LoadCourt()
        {
            if (state == FlowState.Loading)
                return;
            state = FlowState.Loading;
            if (string.IsNullOrWhiteSpace(courtSceneName))
            {
                Debug.LogError("[NBA2K12 Showcase] Court scene name is empty.", this);
                state = FlowState.TeamSelect;
                return;
            }
            SceneManager.LoadScene(courtSceneName, LoadSceneMode.Single);
        }

        private void OnGUI()
        {
            if (state == FlowState.Loading || teams == null || teams.Length == 0)
                return;

            EnsureStyles();
            DrawBackdrop();

            switch (state)
            {
                case FlowState.TeamSelect:
                    DrawTeamSelection();
                    break;
                case FlowState.Roster:
                    DrawRoster();
                    break;
                case FlowState.Intro:
                    DrawIntro();
                    break;
            }
        }

        private void DrawTeamSelection()
        {
            GUI.Label(new Rect(80, 70, Screen.width - 160, 58), "NBA 2K SHOWCASE", titleStyle);
            GUI.Label(new Rect(80, 132, Screen.width - 160, 34), "SELECT YOUR TEAM", subtitleStyle);
            float width = Mathf.Min(430f, Screen.width * 0.40f);
            float gap = 34f;
            float total = width * teams.Length + gap * (teams.Length - 1);
            float x = (Screen.width - total) * 0.5f;
            for (int i = 0; i < teams.Length; i++)
            {
                TeamCard team = teams[i];
                Rect card = new Rect(x + i * (width + gap), Screen.height * 0.31f, width, Screen.height * 0.43f);
                DrawTeamCard(card, team, i == focusedTeam);
                if (GUI.Button(new Rect(card.x + 24, card.yMax - 68, card.width - 48, 48),
                    i == focusedTeam ? "SELECT TEAM  ›" : "VIEW TEAM", buttonStyle))
                    ShowRoster(i);
            }
            GUI.Label(new Rect(80, Screen.height - 78, Screen.width - 160, 36),
                "A / SPACE SELECT     •     LEFT/RIGHT CHANGE TEAM     •     CONNECTED CONTROLLERS SUPPORTED", smallStyle);
        }

        private void DrawRoster()
        {
            TeamCard team = teams[selectedTeam];
            SetTeamPalette(team);
            GUI.Label(new Rect(76, 46, Screen.width - 152, 50), team.teamName, titleStyle);
            GUI.Label(new Rect(76, 98, Screen.width - 152, 32), team.arenaTagline, subtitleStyle);

            float width = Mathf.Min(820f, Screen.width - 160f);
            float x = (Screen.width - width) * 0.5f;
            float top = Screen.height * 0.23f;
            GUI.Box(new Rect(x, top, width, 390f), GUIContent.none, boxStyle);
            GUI.Label(new Rect(x + 28, top + 18, width - 56, 34), "STARTING LINEUP", subtitleStyle);

            PlayerCard[] roster = team.roster ?? Array.Empty<PlayerCard>();
            for (int i = 0; i < roster.Length; i++)
            {
                PlayerCard player = roster[i];
                float y = top + 64 + i * 48;
                GUI.Label(new Rect(x + 30, y, 72, 38), "#" + player.number, rosterStyle);
                GUI.Label(new Rect(x + 112, y, width - 290, 38), player.name.ToUpperInvariant(), rosterStyle);
                GUI.Label(new Rect(x + width - 170, y, 136, 38), player.position, smallStyle);
            }

            if (GUI.Button(new Rect(x + 40, top + 320, 210, 48), "BACK", buttonStyle))
                state = FlowState.TeamSelect;
            if (GUI.Button(new Rect(x + width - 280, top + 320, 240, 48), "START INTRO  ›", buttonStyle))
                StartIntro();
            GUI.Label(new Rect(x + 255, top + 326, width - 550, 40),
                "ENTER / A: CONTINUE    •    ESC / B: BACK", smallStyle);
        }

        private void DrawIntro()
        {
            TeamCard team = teams[selectedTeam];
            SetTeamPalette(team);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), primaryTexture);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height * 0.28f), secondaryTexture);
            GUI.Label(new Rect(80, Screen.height * 0.23f, Screen.width - 160, 70),
                team.abbreviation, new GUIStyle(titleStyle)
                {
                    fontSize = Mathf.Clamp(Screen.height / 7, 62, 150),
                    normal = { textColor = team.secondary }
                });
            GUI.Label(new Rect(80, Screen.height * 0.44f, Screen.width - 160, 56),
                team.teamName, subtitleStyle);
            GUI.Label(new Rect(80, Screen.height * 0.53f, Screen.width - 160, 42),
                team.arenaTagline, smallStyle);
            GUI.Label(new Rect(80, Screen.height * 0.68f, Screen.width - 160, 44),
                "STARTING FIVE", subtitleStyle);
            PlayerCard[] roster = team.roster ?? Array.Empty<PlayerCard>();
            string lineup = string.Join("     •     ", roster.Select(p => "#" + p.number + " " + p.name));
            GUI.Label(new Rect(80, Screen.height * 0.74f, Screen.width - 160, 76),
                lineup, rosterStyle);
            GUI.Label(new Rect(80, Screen.height - 70, Screen.width - 160, 36),
                "PRE-GAME PRESENTATION     •     PRESS A / ENTER TO TIP OFF", smallStyle);
        }

        private GUIStyle boxStyle;

        private void DrawBackdrop()
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), dimTexture);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), panelTexture);
        }

        private void DrawTeamCard(Rect rect, TeamCard team, bool selected)
        {
            Color previous = GUI.color;
            GUI.color = team.primary;
            GUI.DrawTexture(rect, primaryTexture);
            GUI.color = previous;
            GUI.Box(rect, GUIContent.none, boxStyle);
            GUI.Label(new Rect(rect.x + 22, rect.y + 32, rect.width - 44, 65), team.abbreviation, titleStyle);
            GUI.Label(new Rect(rect.x + 22, rect.y + 108, rect.width - 44, 52), team.teamName, subtitleStyle);
            GUI.Label(new Rect(rect.x + 22, rect.y + 166, rect.width - 44, 76),
                team.arenaTagline, smallStyle);
            if (selected)
                GUI.Box(new Rect(rect.x - 4, rect.y - 4, rect.width + 8, rect.height + 8), GUIContent.none, selectedBoxStyle);
        }

        private GUIStyle selectedBoxStyle;

        private void EnsureStyles()
        {
            if (stylesReady)
                return;
            dimTexture = MakeTexture(new Color(0.01f, 0.015f, 0.03f, 1f));
            panelTexture = MakeTexture(new Color(0.025f, 0.035f, 0.06f, 0.97f));
            primaryTexture = MakeTexture(Color.white);
            secondaryTexture = MakeTexture(new Color(1f, 1f, 1f, 0.12f));
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(Screen.height / 24, 22, 42),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(Screen.height / 46, 16, 26),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.88f, 0.90f, 0.94f) }
            };
            rosterStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(Screen.height / 56, 14, 21),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            smallStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(Screen.height / 70, 11, 16),
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = new Color(0.74f, 0.80f, 0.86f) }
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.Clamp(Screen.height / 58, 14, 22),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white, background = MakeTexture(new Color(0.15f, 0.18f, 0.23f)) },
                hover = { textColor = Color.white, background = MakeTexture(new Color(0.28f, 0.32f, 0.4f)) },
                active = { textColor = Color.white, background = MakeTexture(new Color(0.08f, 0.1f, 0.15f)) }
            };
            boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = panelTexture },
                border = new RectOffset(8, 8, 8, 8)
            };
            selectedBoxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = secondaryTexture },
                border = new RectOffset(4, 4, 4, 4)
            };
            stylesReady = true;
        }

        private void SetTeamPalette(TeamCard team)
        {
            primaryTexture.SetPixel(0, 0, team.primary);
            primaryTexture.Apply();
            secondaryTexture.SetPixel(0, 0, new Color(team.secondary.r, team.secondary.g, team.secondary.b, 0.17f));
            secondaryTexture.Apply();
        }

        private static Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void OnDestroy()
        {
            DestroyTexture(dimTexture);
            DestroyTexture(panelTexture);
            DestroyTexture(primaryTexture);
            DestroyTexture(secondaryTexture);
        }

        private static void DestroyTexture(Texture2D texture)
        {
            if (texture != null)
                Destroy(texture);
        }
    }
}
