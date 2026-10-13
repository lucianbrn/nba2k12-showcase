using System;
using System.Linq;
using CrowdEyes.AI4Animation.Basketball;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NBA2K12Showcase
{
    /// <summary>
    /// Broadcast-style overlay driven by the live simulation's score/possession data.
    /// This is a presentation layer; it does not simulate or fake game results.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShowcaseBroadcastHud : MonoBehaviour
    {
        [Header("Live simulation references")]
        [SerializeField] private BasketballMatchController matchController;
        [SerializeField] private BasketballScoreTracker scoreTracker;
        [SerializeField] private BasketballPossessionManager possessionManager;
        [SerializeField] private string homeTeamName = "MIAMI HEAT";
        [SerializeField] private string awayTeamName = "BOSTON CELTICS";

        [Header("Clock rules")]
        [SerializeField, Min(60f)] private float quarterLengthSeconds = 720f;
        [SerializeField, Min(5f)] private float shotClockLengthSeconds = 24f;
        [SerializeField, Min(1f)] private float offensiveReboundResetSeconds = 14f;
        [SerializeField, Min(60f)] private float overtimeLengthSeconds = 300f;
        [SerializeField, Min(1)] private int regulationPeriods = 4;

        private BasketballTeamMember[] players = Array.Empty<BasketballTeamMember>();
        private BasketballScoreTracker subscribedScoreTracker;
        private readonly int[] playerPoints = new int[10];
        private readonly int[] playerFieldGoalsMade = new int[10];
        private readonly int[] playerThreePointersMade = new int[10];
        // Slots 0-3 are regulation quarters; slot 4 accumulates overtime scoring.
        private readonly int[,] periodScores = new int[5, 2];
        private float gameClock;
        private float shotClock;
        private int period = 1;
        private bool overtime;
        private bool paused;
        private bool final;
        private bool periodEnded;
        private float restoreTimeScale = 1f;
        private int lastOwnerTeam = -1;
        private bool shotFlightSeen;
        private string playCall = "TIP-OFF";
        private float playCallUntil;
        private float deadBallStopUntil;
        private GUIStyle panelStyle;
        private GUIStyle teamStyle;
        private GUIStyle scoreStyle;
        private GUIStyle clockLabelStyle;
        private GUIStyle clockValueStyle;
        private GUIStyle shotClockStyle;
        private GUIStyle dangerClockStyle;
        private GUIStyle contextStyle;
        private GUIStyle overlayTitleStyle;
        private GUIStyle overlayBodyStyle;
        private GUIStyle buttonStyle;
        private Texture2D panelTexture;
        private Texture2D homeTexture;
        private Texture2D awayTexture;
        private Texture2D limeTexture;
        private bool stylesReady;

        private bool IsClockRunning => !paused && !final && !periodEnded && Application.isPlaying;

        private void Awake()
        {
            ResolveReferences();
            gameClock = quarterLengthSeconds;
            shotClock = shotClockLengthSeconds;
            restoreTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            players = UnityEngine.Object.FindObjectsByType<BasketballTeamMember>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(p => p != null && p.gameObject.scene == gameObject.scene)
                .OrderBy(p => p.PlayerIndex)
                .ToArray();

            if (possessionManager != null && possessionManager.Owner != null)
                lastOwnerTeam = possessionManager.Owner.TeamId;
        }

        private void OnEnable()
        {
            // A scene can enable this HUD before other gameplay components finish
            // initializing. Resolve again here, then make subscription idempotent.
            ResolveReferences();
            BindScoreEvent();
        }

        private void Start()
        {
            // The upstream UIToolkit starts in a neural-debug view. Switch its view to
            // gameplay mode so debug telemetry does not cover the broadcast HUD.
            BasketballUIToolkitController upstreamHud =
                UnityEngine.Object.FindFirstObjectByType<BasketballUIToolkitController>();
            if (upstreamHud != null)
            {
                upstreamHud.TogglePrimaryHud();
                upstreamHud.SetMatchStatus("NBA 2K SHOWCASE");
            }

            foreach (BasketballDebugVisualizer visualizer in
                     UnityEngine.Object.FindObjectsByType<BasketballDebugVisualizer>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (visualizer != null && visualizer.gameObject.scene == gameObject.scene)
                    visualizer.SetVisible(false);
            }
        }

        private void OnDisable()
        {
            if (subscribedScoreTracker != null)
                subscribedScoreTracker.Scored -= HandleScore;
            subscribedScoreTracker = null;
            RestoreTimeScale();
            ReleaseTextures();
        }

        private void OnDestroy()
        {
            RestoreTimeScale();
            ReleaseTextures();
        }

        private void ResolveReferences()
        {
            if (matchController == null)
                matchController = FindSceneComponent<BasketballMatchController>();
            if (scoreTracker == null)
                scoreTracker = FindSceneComponent<BasketballScoreTracker>();
            if (possessionManager == null)
                possessionManager = FindSceneComponent<BasketballPossessionManager>();
            BindScoreEvent();
        }

        private T FindSceneComponent<T>() where T : Component
        {
            return UnityEngine.Object.FindObjectsByType<T>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(candidate =>
                    candidate != null && candidate.gameObject.scene == gameObject.scene);
        }

        private void BindScoreEvent()
        {
            // Unity does not guarantee Awake/OnEnable ordering across different objects.
            // Rebind only when the live tracker changes, so a tracker created after this
            // HUD becomes active still drives the scoreboard without duplicate callbacks.
            if (subscribedScoreTracker == scoreTracker)
                return;

            if (subscribedScoreTracker != null)
                subscribedScoreTracker.Scored -= HandleScore;

            subscribedScoreTracker = scoreTracker;
            if (subscribedScoreTracker != null && isActiveAndEnabled)
            {
                subscribedScoreTracker.Scored -= HandleScore;
                subscribedScoreTracker.Scored += HandleScore;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;

            if (matchController == null || scoreTracker == null || possessionManager == null)
                ResolveReferences();

            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && !final && !periodEnded)
                SetPaused(!paused);

            if (gamepad != null && gamepad.startButton.wasPressedThisFrame && !final && !periodEnded)
                SetPaused(!paused);

            if (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame && (paused || periodEnded || final))
            {
                if (final) RestartGame();
                else if (periodEnded) NextPeriod();
                else SetPaused(false);
            }

            // Keyboard users need a way to continue after a quarter/overtime and restart
            // after the final horn; Escape is intentionally reserved for pause toggling.
            if (keyboard != null &&
                (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) &&
                (paused || periodEnded || final))
            {
                if (final) RestartGame();
                else if (periodEnded) NextPeriod();
                else SetPaused(false);
            }

            if (keyboard != null && IsClockRunning)
                ProcessPlayerSwitchInput(keyboard);

            if (!IsClockRunning)
                return;

            float delta = Time.unscaledDeltaTime;
            if (delta <= 0f)
                return;

            bool deadBall = possessionManager != null &&
                            possessionManager.BallState == BasketballPossessionState.DeadBall;
            bool inScoreRestartWindow = Time.unscaledTime < deadBallStopUntil;
            if (!deadBall && !inScoreRestartWindow)
            {
                gameClock = Mathf.Max(0f, gameClock - delta);
                UpdatePossessionAndShotClock(delta);
            }

            if (gameClock <= 0f)
            {
                // A shot released before zero is allowed to finish its flight (buzzer-beater rule).
                // Once the ball leaves ShotFlight, the period ends; no rebound follow-up is allowed.
                if (possessionManager == null ||
                    possessionManager.BallState != BasketballPossessionState.ShotFlight)
                    EndPeriod();
            }
        }

        private void ProcessPlayerSwitchInput(Keyboard keyboard)
        {
            if (matchController == null)
                return;

            int slot = -1;
            if (keyboard.digit1Key.wasPressedThisFrame) slot = 0;
            else if (keyboard.digit2Key.wasPressedThisFrame) slot = 1;
            else if (keyboard.digit3Key.wasPressedThisFrame) slot = 2;
            else if (keyboard.digit4Key.wasPressedThisFrame) slot = 3;
            else if (keyboard.digit5Key.wasPressedThisFrame) slot = 4;
            if (slot < 0)
                return;

            BasketballTeamMember active = matchController.ActivePlayer;
            int teamId = active != null
                ? active.TeamId
                : possessionManager != null && possessionManager.Owner != null
                    ? possessionManager.Owner.TeamId
                    : 0;

            int playerIndex = teamId * 5 + slot;
            BasketballTeamMember chosen = players.FirstOrDefault(p =>
                p != null && p.PlayerIndex == playerIndex && p.TeamId == teamId);
            if (chosen == null)
                return;

            matchController.SelectPlayer(playerIndex);
            ShowcasePlayerIdentity identity = chosen.GetComponent<ShowcasePlayerIdentity>();
            ShowPlayCall(identity != null
                ? "CONTROL: " + identity.PlayerName.ToUpperInvariant()
                : "PLAYER " + (slot + 1));
        }

        private void UpdatePossessionAndShotClock(float delta)
        {
            if (possessionManager == null)
                return;

            BasketballTeamMember owner = possessionManager.Owner;
            BasketballPossessionState state = possessionManager.BallState;

            if (state == BasketballPossessionState.ShotFlight)
            {
                shotFlightSeen = true;
                // A release before zero is allowed to finish even if the clock hits zero in flight.
                shotClock = Mathf.Max(0f, shotClock - delta);
                return;
            }

            if (owner != null)
            {
                if (lastOwnerTeam < 0)
                {
                    lastOwnerTeam = owner.TeamId;
                    shotClock = shotClockLengthSeconds;
                }
                else if (owner.TeamId != lastOwnerTeam)
                {
                    lastOwnerTeam = owner.TeamId;
                    shotClock = shotClockLengthSeconds;
                    shotFlightSeen = false;
                }
                else if (shotFlightSeen)
                {
                    // Same-team recovery after a shot is treated as an offensive rebound.
                    shotClock = offensiveReboundResetSeconds;
                    shotFlightSeen = false;
                }
            }

            // The shot clock must continue through a live pass preparation/flight, even while
            // the possession manager temporarily has no ball owner. The passer identifies offense.
            BasketballTeamMember offense = owner;
            bool liveControlledPossession = owner != null &&
                (state == BasketballPossessionState.Possessed ||
                 state == BasketballPossessionState.PassPreparing ||
                 state == BasketballPossessionState.CatchBlend);
            if (owner == null && state == BasketballPossessionState.PassFlight)
            {
                offense = possessionManager.Passer;
                liveControlledPossession = offense != null;
            }

            if (!liveControlledPossession)
                return;

            shotClock = Mathf.Max(0f, shotClock - delta);
            if (shotClock <= 0f && offense != null)
                ApplyShotClockViolation(offense);
        }

        private void ApplyShotClockViolation(BasketballTeamMember offense)
        {
            if (offense == null || possessionManager == null)
                return;

            BasketballTeamMember defense = players
                .Where(p => p != null && p.IsOnCourt && p.TeamId != offense.TeamId)
                .OrderBy(p => (p.transform.position - offense.transform.position).sqrMagnitude)
                .FirstOrDefault();

            if (defense == null)
                return;

            Vector3 ballPosition = defense.AimPoint + defense.transform.forward * 0.16f;
            bool reset = possessionManager.ResetPossession(
                defense,
                ballPosition,
                Quaternion.LookRotation(defense.transform.forward, Vector3.up));

            if (!reset)
                return;

            shotClock = shotClockLengthSeconds;
            lastOwnerTeam = defense.TeamId;
            shotFlightSeen = false;
            matchController?.SelectPlayer(defense.PlayerIndex);
            ShowPlayCall("SHOT CLOCK VIOLATION");
        }

        private void HandleScore(BasketballScoreEvent score)
        {
            deadBallStopUntil = Time.unscaledTime + 1.25f;
            shotClock = shotClockLengthSeconds;
            shotFlightSeen = false;

            BasketballTeamMember scorer = players.FirstOrDefault(p =>
                p != null && p.TeamId == score.TeamId && p.PlayerIndex == score.PlayerIndex);
            if (score.PlayerIndex >= 0 && score.PlayerIndex < playerPoints.Length)
            {
                playerPoints[score.PlayerIndex] += score.Points;
                playerFieldGoalsMade[score.PlayerIndex]++;
                if (score.Points == 3) playerThreePointersMade[score.PlayerIndex]++;
            }
            if (score.TeamId >= 0 && score.TeamId < 2)
            {
                int periodSlot = overtime ? 4 : Mathf.Clamp(period - 1, 0, 3);
                periodScores[periodSlot, score.TeamId] += score.Points;
            }

            ShowcasePlayerIdentity identity = scorer != null
                ? scorer.GetComponent<ShowcasePlayerIdentity>()
                : null;
            string scorerName = identity != null
                ? identity.PlayerName.ToUpperInvariant()
                : "PLAYER " + (score.PlayerIndex + 1);
            string finish = score.Points == 3 ? "THREE" : "BUCKET";
            ShowPlayCall(scorerName + "  /  " + finish + "  +" + score.Points);
        }

        private void EndPeriod()
        {
            periodEnded = true;
            paused = true;
            Time.timeScale = 0f;
            if (period >= regulationPeriods && !overtime)
            {
                int home = scoreTracker != null ? scoreTracker.TeamZeroScore : 0;
                int away = scoreTracker != null ? scoreTracker.TeamOneScore : 0;
                if (home != away)
                {
                    final = true;
                    ShowPlayCall("FINAL");
                }
                else
                {
                    ShowPlayCall("OVERTIME");
                }
            }
            else
            {
                ShowPlayCall("END OF " + PeriodLabel());
            }
        }

        private string PeriodLabel()
        {
            if (overtime)
                return "OVERTIME";
            return period switch
            {
                1 => "1ST QUARTER",
                2 => "2ND QUARTER",
                3 => "3RD QUARTER",
                4 => "4TH QUARTER",
                _ => period + "TH PERIOD"
            };
        }

        private void NextPeriod()
        {
            if (final)
                return;

            if (!overtime && period >= regulationPeriods)
            {
                int home = scoreTracker != null ? scoreTracker.TeamZeroScore : 0;
                int away = scoreTracker != null ? scoreTracker.TeamOneScore : 0;
                if (home != away)
                {
                    final = true;
                    periodEnded = false;
                    paused = true;
                    ShowPlayCall("FINAL");
                    return;
                }

                overtime = true;
                period = regulationPeriods + 1;
            }
            else if (overtime)
            {
                int home = scoreTracker != null ? scoreTracker.TeamZeroScore : 0;
                int away = scoreTracker != null ? scoreTracker.TeamOneScore : 0;
                if (home != away)
                {
                    final = true;
                    periodEnded = false;
                    paused = true;
                    ShowPlayCall("FINAL");
                    return;
                }
            }
            else
            {
                period++;
            }

            gameClock = overtime ? overtimeLengthSeconds : quarterLengthSeconds;
            shotClock = shotClockLengthSeconds;
            shotFlightSeen = false;
            deadBallStopUntil = 0f;
            periodEnded = false;
            paused = false;
            Time.timeScale = restoreTimeScale > 0f ? restoreTimeScale : 1f;
            ShowPlayCall(overtime ? "OVERTIME" : "START " + PeriodLabel());
        }

        private void RestartGame()
        {
            if (scoreTracker != null)
                scoreTracker.ResetScore();
            Array.Clear(playerPoints, 0, playerPoints.Length);
            Array.Clear(playerFieldGoalsMade, 0, playerFieldGoalsMade.Length);
            Array.Clear(playerThreePointersMade, 0, playerThreePointersMade.Length);
            Array.Clear(periodScores, 0, periodScores.Length);

            period = 1;
            overtime = false;
            final = false;
            periodEnded = false;
            gameClock = quarterLengthSeconds;
            shotClock = shotClockLengthSeconds;
            shotFlightSeen = false;
            deadBallStopUntil = 0f;
            lastOwnerTeam = -1;
            if (players.Length > 0 && possessionManager != null)
            {
                BasketballTeamMember starter = players.FirstOrDefault(p => p != null && p.TeamId == 0);
                if (starter != null)
                {
                    possessionManager.ResetPossession(
                        starter,
                        starter.AimPoint + starter.transform.forward * 0.16f,
                        Quaternion.LookRotation(starter.transform.forward, Vector3.up));
                    matchController?.SelectPlayer(starter.PlayerIndex);
                    lastOwnerTeam = starter.TeamId;
                }
            }

            paused = false;
            Time.timeScale = restoreTimeScale > 0f ? restoreTimeScale : 1f;
            ShowPlayCall("TIP-OFF");
        }

        private void SetPaused(bool value)
        {
            if (final || periodEnded && value)
                return;

            paused = value;
            if (paused)
            {
                restoreTimeScale = Time.timeScale > 0f ? Time.timeScale : restoreTimeScale;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = restoreTimeScale > 0f ? restoreTimeScale : 1f;
            }
        }

        private void RestoreTimeScale()
        {
            if (Time.timeScale == 0f)
                Time.timeScale = restoreTimeScale > 0f ? restoreTimeScale : 1f;
        }

        private void ShowPlayCall(string text)
        {
            playCall = text ?? string.Empty;
            playCallUntil = Time.unscaledTime + 2.5f;
        }

        private string GetPeriodTime()
        {
            int total = Mathf.CeilToInt(gameClock);
            return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }

        private string GetShotTime() => Mathf.Max(0f, shotClock).ToString("00.0");

        private ShowcasePlayerIdentity GetActiveIdentity()
        {
            BasketballTeamMember active = matchController != null ? matchController.ActivePlayer : null;
            return active != null ? active.GetComponent<ShowcasePlayerIdentity>() : null;
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
                return;

            EnsureStyles();
            float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            if (scale <= 0f)
                scale = 1f;

            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            DrawScoreboard();
            DrawLiveInfo();
            if (paused || periodEnded || final)
                DrawPauseCard();
            if (!string.IsNullOrEmpty(playCall) && Time.unscaledTime < playCallUntil && !paused)
                GUI.Label(new Rect(710, 150, 500, 44), playCall, overlayTitleStyle);
            GUI.matrix = previousMatrix;
        }

        private void DrawScoreboard()
        {
            const float width = 900f;
            const float x = (1920f - width) * 0.5f;
            GUI.Box(new Rect(x, 24, width, 112), GUIContent.none, panelStyle);

            GUI.DrawTexture(new Rect(x + 6, 30, 286, 100), homeTexture);
            GUI.DrawTexture(new Rect(x + 608, 30, 286, 100), awayTexture);

            BasketballTeamMember ballOwner = possessionManager != null
                ? possessionManager.Owner
                : null;
            if (ballOwner == null && possessionManager != null &&
                (possessionManager.BallState == BasketballPossessionState.PassFlight ||
                 possessionManager.BallState == BasketballPossessionState.ShotFlight))
                ballOwner = possessionManager.Passer;
            int attackingTeam = ballOwner != null ? ballOwner.TeamId : -1;

            string homeLabel = (attackingTeam == 0 ? "▶  " : "") + homeTeamName;
            string awayLabel = (attackingTeam == 1 ? "▶  " : "") + awayTeamName;
            GUI.Label(new Rect(x + 18, 40, 260, 27), homeLabel, teamStyle);
            GUI.Label(new Rect(x + 18, 71, 260, 50),
                (scoreTracker != null ? scoreTracker.TeamZeroScore : 0).ToString("00"), scoreStyle);

            GUI.Label(new Rect(x + 608, 40, 260, 27), awayLabel, teamStyle);
            GUI.Label(new Rect(x + 608, 71, 260, 50),
                (scoreTracker != null ? scoreTracker.TeamOneScore : 0).ToString("00"), scoreStyle);

            GUI.Label(new Rect(x + 302, 35, 296, 24), final ? "FINAL" : PeriodLabel(), clockLabelStyle);
            GUI.Label(new Rect(x + 302, 59, 296, 37), GetPeriodTime(), clockValueStyle);
            GUI.Label(new Rect(x + 302, 96, 296, 24), "SHOT", clockLabelStyle);
            GUI.Label(new Rect(x + 360, 96, 180, 24), GetShotTime(),
                shotClock <= 5f && !final ? dangerClockStyle : shotClockStyle);
        }

        private void DrawLiveInfo()
        {
            const float y = 936f;
            GUI.Box(new Rect(34, y, 610, 108), GUIContent.none, panelStyle);
            ShowcasePlayerIdentity identity = GetActiveIdentity();
            BasketballTeamMember active = matchController != null ? matchController.ActivePlayer : null;
            int index = active != null ? active.PlayerIndex : -1;
            int points = index >= 0 && index < playerPoints.Length ? playerPoints[index] : 0;
            int makes = index >= 0 && index < playerFieldGoalsMade.Length ? playerFieldGoalsMade[index] : 0;
            int threes = index >= 0 && index < playerThreePointersMade.Length ? playerThreePointersMade[index] : 0;
            string playerName = identity != null ? identity.PlayerName : "PLAYER " +
                (matchController != null ? matchController.ActivePlayerIndex + 1 : 1);
            string jersey = identity != null ? "#" + identity.JerseyNumber + "  " + identity.Position : "LIVE GAMEPLAY";
            GUI.Label(new Rect(56, y + 8, 560, 29), playerName, teamStyle);
            GUI.Label(new Rect(56, y + 38, 560, 23),
                jersey + "   •   " + points + " PTS   " + makes + " FGM   " + threes + " 3PM", contextStyle);
            GUI.Label(new Rect(56, y + 68, 560, 23), Gamepad.current != null ? "D-PAD SWITCH   •   LEFT STICK MOVE   •   L3 SPRINT   •   Y / TRIANGLE SHOOT" : "1–5 SWITCH   •   WASD MOVE   •   SHIFT RUN   •   SPACE SHOOT", contextStyle);

            GUI.Box(new Rect(1400, y, 486, 108), GUIContent.none, panelStyle);
            GUI.Label(new Rect(1420, y + 8, 440, 29), "NBA 2K SHOWCASE  /  LIVE", teamStyle);
            GUI.Label(new Rect(1420, y + 38, 440, 23), Gamepad.current != null ? "LT + A / CROSS PASS   •   B / CIRCLE HOLD   •   START PAUSE" : "CTRL + CLICK  PASS   •   ESC  PAUSE", contextStyle);
            GUI.Label(new Rect(1420, y + 68, 440, 23), Gamepad.current != null ? "LB / RB BALL SPIN   •   D-PAD UP / DOWN BALL HEIGHT   •   RIGHT STICK BALL CONTROL" : "MOUSE LOOK   •   R RECENTER CAMERA", contextStyle);
        }

        private void DrawPauseCard()
        {
            GUI.DrawTexture(new Rect(0, 0, 1920, 1080), panelTexture);
            const float x = 610, y = 300, w = 700, h = 430;
            GUI.Box(new Rect(x, y, w, h), GUIContent.none, panelStyle);
            string title = final ? "FINAL" : periodEnded ? PeriodLabel() + " COMPLETE" : "PAUSED";
            GUI.Label(new Rect(x + 30, y + 34, w - 60, 70), title, overlayTitleStyle);

            int home = scoreTracker != null ? scoreTracker.TeamZeroScore : 0;
            int away = scoreTracker != null ? scoreTracker.TeamOneScore : 0;
            string result = homeTeamName + "   " + home + "     —     " + away + "   " + awayTeamName;
            GUI.Label(new Rect(x + 30, y + 112, w - 60, 42), result, overlayBodyStyle);
            GUI.Label(new Rect(x + 30, y + 154, w - 60, 40), BuildPeriodBreakdown(), contextStyle);

            if (final)
            {
                if (GUI.Button(new Rect(x + 170, y + 214, 360, 58), "NEW GAME", buttonStyle))
                    RestartGame();
            }
            else if (periodEnded)
            {
                string next = !overtime && period >= regulationPeriods
                    ? (home == away ? "START OVERTIME" : "VIEW FINAL")
                    : overtime ? "NEXT OVERTIME" : "NEXT QUARTER";
                if (GUI.Button(new Rect(x + 170, y + 214, 360, 58), next, buttonStyle))
                    NextPeriod();
            }
            else
            {
                if (GUI.Button(new Rect(x + 170, y + 214, 360, 58), "RESUME GAME", buttonStyle))
                    SetPaused(false);
            }

            if (!final && !periodEnded && GUI.Button(new Rect(x + 170, y + 292, 360, 48), "RESTART GAME", buttonStyle))
                RestartGame();
        }

        private string BuildPeriodBreakdown()
        {
            string[] labels = { "Q1", "Q2", "Q3", "Q4", "OT" };
            int lastSlot = overtime ? 4 : Mathf.Clamp(period - 1, 0, 3);
            var segments = new System.Collections.Generic.List<string>();
            for (int slot = 0; slot <= lastSlot; slot++)
            {
                segments.Add(
                    labels[slot] + " " + periodScores[slot, 0] +
                    "–" + periodScores[slot, 1]);
            }

            return "PERIOD SCORES   " + string.Join("    ", segments);
        }

        private void EnsureStyles()
        {
            if (stylesReady)
                return;
            panelTexture = MakeTexture(new Color(0.025f, 0.04f, 0.065f, 0.94f));
            homeTexture = MakeTexture(GetTeamColor(homeTeamName, new Color(0.60f, 0.00f, 0.18f, 0.99f)));
            awayTexture = MakeTexture(GetTeamColor(awayTeamName, new Color(0.00f, 0.48f, 0.20f, 0.99f)));
            limeTexture = MakeTexture(new Color(0.79f, 0.95f, 0.43f, 1f));

            panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = panelTexture },
                border = new RectOffset(12, 12, 12, 12)
            };
            teamStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 19,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            scoreStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 44,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            clockLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.77f, 0.82f, 0.87f) }
            };
            clockValueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 34,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            shotClockStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            dangerClockStyle = new GUIStyle(shotClockStyle)
            {
                normal = { textColor = new Color(1f, 0.28f, 0.24f) }
            };
            contextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.75f, 0.81f, 0.85f) }
            };
            overlayTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            overlayBodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 19,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = new Color(0.82f, 0.86f, 0.89f) }
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = limeTexture, textColor = new Color(0.04f, 0.09f, 0.06f) },
                hover = { background = limeTexture, textColor = new Color(0.04f, 0.09f, 0.06f) },
                active = { background = limeTexture, textColor = new Color(0.04f, 0.09f, 0.06f) }
            };
            stylesReady = true;
        }

        private static Color GetTeamColor(string teamName, Color fallback)
        {
            string normalized = (teamName ?? string.Empty).ToUpperInvariant();
            if (normalized.Contains("MIAMI HEAT")) return new Color(0.60f, 0.00f, 0.18f, 0.99f);
            if (normalized.Contains("BOSTON CELTICS")) return new Color(0.00f, 0.48f, 0.20f, 0.99f);
            if (normalized.Contains("GOLDEN STATE WARRIORS")) return new Color(0.00f, 0.25f, 0.62f, 0.99f);
            return fallback;
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

        private void ReleaseTextures()
        {
            DestroyTexture(ref panelTexture);
            DestroyTexture(ref homeTexture);
            DestroyTexture(ref awayTexture);
            DestroyTexture(ref limeTexture);
            stylesReady = false;
        }

        private void DestroyTexture(ref Texture2D texture)
        {
            if (texture == null)
                return;
            if (Application.isPlaying)
                Destroy(texture);
            else
                DestroyImmediate(texture);
            texture = null;
        }

#if UNITY_EDITOR
        public void Configure(
            BasketballMatchController controller,
            BasketballScoreTracker scores,
            BasketballPossessionManager possession)
        {
            matchController = controller;
            scoreTracker = scores;
            possessionManager = possession;
            homeTeamName = "MIAMI HEAT";
            awayTeamName = "BOSTON CELTICS";
        }
#endif
    }
}
