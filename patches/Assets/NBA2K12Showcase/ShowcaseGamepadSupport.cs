using System.Linq;
using System.Reflection;
using CrowdEyes.AI4Animation.Basketball;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NBA2K12Showcase
{
    /// <summary>
    /// Adds gamepad-first match controls using Unity's Input System. Standard wired USB
    /// Xbox and supported PlayStation controllers are normally exposed through Gamepad.
    /// Keyboard/mouse remains a fallback. This component does not replace the simulation.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class ShowcaseGamepadSupport : MonoBehaviour
    {
        private BasketballMatchController match;
        private BasketballTeamMember[] players = System.Array.Empty<BasketballTeamMember>();
        private bool gamepadMode;
        private bool inputModeInitialized;
        private float nextPlayerRefreshTime;

        private void Awake()
        {
            match = FindFirstObjectByType<BasketballMatchController>();
            RefreshPlayers();
            nextPlayerRefreshTime = Time.unscaledTime + 1f;
            SetGamepadMode(Gamepad.current != null);
        }

        private void Update()
        {
            if (match == null)
                match = FindFirstObjectByType<BasketballMatchController>();
            if (match == null)
                return;

            if (players.Length == 0 || Time.unscaledTime >= nextPlayerRefreshTime)
            {
                RefreshPlayers();
                nextPlayerRefreshTime = Time.unscaledTime + 1f;
                ApplyInputMode(gamepadMode);
            }

            Gamepad pad = Gamepad.current;
            Keyboard keyboard = Keyboard.current;

            bool gamepadActive = pad != null &&
                (pad.leftStick.ReadValue().sqrMagnitude > 0.04f ||
                 pad.rightStick.ReadValue().sqrMagnitude > 0.04f ||
                 pad.dpad.ReadValue().sqrMagnitude > 0f ||
                 pad.buttonSouth.wasPressedThisFrame ||
                 pad.buttonEast.wasPressedThisFrame ||
                 pad.buttonWest.wasPressedThisFrame ||
                 pad.buttonNorth.wasPressedThisFrame ||
                 pad.leftShoulder.wasPressedThisFrame ||
                 pad.rightShoulder.wasPressedThisFrame ||
                 pad.startButton.wasPressedThisFrame ||
                 pad.leftStickButton.wasPressedThisFrame ||
                 pad.leftTrigger.wasPressedThisFrame ||
                 pad.rightTrigger.wasPressedThisFrame);

            bool keyboardActive = keyboard != null &&
                (keyboard.wKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame ||
                 keyboard.sKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame ||
                 keyboard.spaceKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame ||
                 keyboard.leftShiftKey.wasPressedThisFrame || keyboard.leftCtrlKey.wasPressedThisFrame ||
                 keyboard.escapeKey.wasPressedThisFrame);

            if (pad == null)
                SetGamepadMode(false);
            else if (gamepadActive)
                SetGamepadMode(true);
            else if (keyboardActive)
                SetGamepadMode(false);

            if (!gamepadMode || pad == null)
                return;

            if (pad.dpad.left.wasPressedThisFrame)
                SelectNext(-1);
            else if (pad.dpad.right.wasPressedThisFrame)
                SelectNext(1);

            if (pad.leftTrigger.ReadValue() > 0.55f && pad.buttonSouth.wasPressedThisFrame)
                TryPass(pad.rightStick.ReadValue());

            if (pad.startButton.wasPressedThisFrame)
                TogglePause();
        }

        private void RefreshPlayers()
        {
            players = FindObjectsByType<BasketballTeamMember>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(p => p != null && p.gameObject.scene == gameObject.scene)
                .OrderBy(p => p.PlayerIndex)
                .ToArray();
        }

        private void SetGamepadMode(bool enabled)
        {
            if (inputModeInitialized && gamepadMode == enabled)
                return;

            gamepadMode = enabled;
            inputModeInitialized = true;
            ApplyInputMode(enabled);
            Debug.Log("[NBA2K12 Showcase] Active input: " +
                      (enabled ? "gamepad (USB supported where exposed by Input System)" : "keyboard/mouse"), this);
        }

        private void ApplyInputMode(bool enabled)
        {
            foreach (BasketballTeamMember player in players)
            {
                if (player?.InputProvider == null)
                    continue;
                var mode = enabled
                    ? BasketballKeyboardMouseInputProvider.InputMode.Gamepad
                    : BasketballKeyboardMouseInputProvider.InputMode.Keyboard;
                if (player.InputProvider.Mode != mode)
                    player.InputProvider.SetMode(mode);
            }
        }

        private void SelectNext(int direction)
        {
            if (match == null || players.Length == 0)
                return;
            int teamId = match.ActivePlayer != null ? match.ActivePlayer.TeamId : 0;
            BasketballTeamMember[] eligible = players
                .Where(p => p != null && p.IsOnCourt && p.TeamId == teamId)
                .OrderBy(p => p.PlayerIndex)
                .ToArray();
            if (eligible.Length < 2)
                return;
            int current = System.Array.FindIndex(eligible,
                p => p.PlayerIndex == match.ActivePlayerIndex);
            int next = (current + direction + eligible.Length) % eligible.Length;
            match.SelectPlayer(eligible[next].PlayerIndex);
        }

        private void TryPass(Vector2 aim)
        {
            BasketballTeamMember active = match != null ? match.ActivePlayer : null;
            if (active == null || match.Owner != active || match.PossessionManager == null)
                return;

            Vector3 direction = aim.sqrMagnitude > 0.09f
                ? new Vector3(aim.x, 0f, aim.y).normalized
                : active.transform.forward;
            BasketballTeamMember target = players
                .Where(p => p != null && p.IsOnCourt && p.TeamId == active.TeamId && p != active)
                .Select(p => new { Player = p, Offset = p.transform.position - active.transform.position })
                .OrderByDescending(item =>
                {
                    Vector3 offset = item.Offset;
                    offset.y = 0f;
                    return Vector3.Dot(direction, offset.sqrMagnitude > 0.001f ? offset.normalized : direction);
                })
                .ThenBy(item => item.Offset.sqrMagnitude)
                .Select(item => item.Player)
                .FirstOrDefault();

            if (target == null || !match.PossessionManager.RequestPass(active, target))
                return;

            // Reuse the match controller's existing pass lifecycle so its neural pass
            // preparation, release window and timeout handling stay authoritative.
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            SetPrivateField("passTarget", target, flags);
            SetPrivateField("passCommitted", true, flags);
            SetPrivateField("passCommittedSeconds", 0f, flags);
            SetPrivateField("shotAttempt", false, flags);
            SetPrivateField("shotCommitted", false, flags);
            SetPrivateField("lockedTarget", null, flags);
            Debug.Log("[NBA2K12 Showcase] Gamepad pass committed to player " + target.PlayerIndex, this);
        }

        private void TogglePause()
        {
            ShowcaseBroadcastHud hud = FindFirstObjectByType<ShowcaseBroadcastHud>();
            if (hud == null)
                return;
            FieldInfo pausedField = typeof(ShowcaseBroadcastHud).GetField(
                "paused", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo setPaused = typeof(ShowcaseBroadcastHud).GetMethod(
                "SetPaused", BindingFlags.Instance | BindingFlags.NonPublic);
            if (pausedField == null || setPaused == null)
                return;
            bool paused = (bool)pausedField.GetValue(hud);
            setPaused.Invoke(hud, new object[] { !paused });
        }

        private void SetPrivateField(string fieldName, object value, BindingFlags flags)
        {
            FieldInfo field = typeof(BasketballMatchController).GetField(fieldName, flags);
            if (field == null)
            {
                Debug.LogWarning("[NBA2K12 Showcase] Controller pass integration could not find field " + fieldName, this);
                return;
            }
            field.SetValue(match, value);
        }
    }
}