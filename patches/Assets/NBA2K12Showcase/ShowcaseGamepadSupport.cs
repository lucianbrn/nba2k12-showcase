using System;
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
            match = FindSceneComponent<BasketballMatchController>();
            RefreshPlayers();
            nextPlayerRefreshTime = Time.unscaledTime + 1f;
            SetGamepadMode(Gamepad.current != null);
        }

        private void Update()
        {
            if (match == null)
                match = FindSceneComponent<BasketballMatchController>();
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
            Mouse mouse = Mouse.current;

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
                 pad.rightTrigger.wasPressedThisFrame ||
                 pad.rightTrigger.ReadValue() > 0.1f);

            bool keyboardActive = keyboard != null &&
                (keyboard.wKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame ||
                 keyboard.sKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame ||
                 keyboard.qKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame ||
                 keyboard.spaceKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame ||
                 keyboard.digit1Key.wasPressedThisFrame || keyboard.digit2Key.wasPressedThisFrame ||
                 keyboard.digit3Key.wasPressedThisFrame || keyboard.digit4Key.wasPressedThisFrame ||
                 keyboard.digit5Key.wasPressedThisFrame ||
                 keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame ||
                 keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame ||
                 keyboard.leftCtrlKey.wasPressedThisFrame || keyboard.rightCtrlKey.wasPressedThisFrame ||
                 keyboard.escapeKey.wasPressedThisFrame);
            bool mouseActive = mouse != null &&
                (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame ||
                 mouse.middleButton.wasPressedThisFrame);

            if (pad == null)
                SetGamepadMode(false);
            else if (gamepadActive)
                SetGamepadMode(true);
            else if (keyboardActive || mouseActive)
                SetGamepadMode(false);

            if (!gamepadMode || pad == null)
                return;

            if (pad.dpad.left.wasPressedThisFrame)
                SelectNext(-1);
            else if (pad.dpad.right.wasPressedThisFrame)
                SelectNext(1);

            if (pad.leftTrigger.ReadValue() > 0.55f && pad.buttonSouth.wasPressedThisFrame)
                TryPass(pad.rightStick.ReadValue());

            // Pause is owned by ShowcaseBroadcastHud.Update. Handling Start here too
            // toggles pause twice in the same frame and can immediately resume the game.
        }

        private T FindSceneComponent<T>() where T : Component
        {
            return FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(candidate =>
                    candidate != null && candidate.gameObject.scene == gameObject.scene);
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
            // If the current selection is missing or temporarily off-court, choose the
            // first eligible teammate rather than wrapping from an invalid index.
            int next = current < 0
                ? 0
                : (current + direction + eligible.Length) % eligible.Length;
            match.SelectPlayer(eligible[next].PlayerIndex);
        }

        private void TryPass(Vector2 aim)
        {
            BasketballTeamMember active = match != null ? match.ActivePlayer : null;
            if (active == null || match.Owner != active || match.PossessionManager == null)
                return;

            // Interpret the right stick in camera space, as players expect from a
            // console basketball game. A raw (x, y) -> world (x, z) mapping changes
            // pass direction whenever the broadcast camera rotates around the court.
            Vector3 cameraForward = Camera.main != null
                ? Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up)
                : Vector3.ProjectOnPlane(active.transform.forward, Vector3.up);
            if (cameraForward.sqrMagnitude < 0.001f)
                cameraForward = Vector3.forward;
            cameraForward.Normalize();
            Vector3 cameraRight = Vector3.Cross(Vector3.up, cameraForward).normalized;
            Vector3 direction = aim.sqrMagnitude > 0.09f
                ? (cameraRight * aim.x + cameraForward * aim.y).normalized
                : Vector3.ProjectOnPlane(active.transform.forward, Vector3.up).normalized;
            if (direction.sqrMagnitude < 0.001f)
                direction = cameraForward;

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

            if (target == null)
                return;

            // Validate the complete upstream adapter contract before changing possession.
            // If NeuralCourt renames a private field, abort before RequestPass can leave a
            // pass in preparation without the controller's release/timeout lifecycle.
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            string[] fieldNames =
            {
                "passTarget", "passCommitted", "passCommittedSeconds",
                "shotAttempt", "shotCommitted", "lockedTarget"
            };
            FieldInfo[] fields = fieldNames
                .Select(name => typeof(BasketballMatchController).GetField(name, flags))
                .ToArray();
            if (fields.Any(field => field == null))
            {
                string missing = string.Join(", ", fieldNames.Where((name, index) => fields[index] == null));
                Debug.LogError(
                    "[NBA2K12 Showcase] Pass cancelled before possession changes; NeuralCourt pass lifecycle fields are missing: " + missing,
                    this);
                return;
            }

            if (fields.Any(field => field.IsStatic || field.IsInitOnly || field.IsLiteral))
            {
                Debug.LogError(
                    "[NBA2K12 Showcase] Pass cancelled before possession changes; a NeuralCourt pass lifecycle field is no longer writable.",
                    this);
                return;
            }

            object[] values = { target, true, 0f, false, false, null };
            for (int i = 0; i < fields.Length; i++)
            {
                if (values[i] != null && !fields[i].FieldType.IsInstanceOfType(values[i]))
                {
                    Debug.LogError(
                        "[NBA2K12 Showcase] Pass cancelled; NeuralCourt field " + fieldNames[i] +
                        " has unexpected type " + fields[i].FieldType.FullName + ".",
                        this);
                    return;
                }

                if (values[i] == null && fields[i].FieldType.IsValueType &&
                    Nullable.GetUnderlyingType(fields[i].FieldType) == null)
                {
                    Debug.LogError(
                        "[NBA2K12 Showcase] Pass cancelled; NeuralCourt field " + fieldNames[i] +
                        " no longer accepts null.",
                        this);
                    return;
                }
            }

            object[] previousValues = fields.Select(field => field.GetValue(match)).ToArray();
            if (!match.PossessionManager.RequestPass(active, target))
                return;

            // Reuse the match controller's existing pass lifecycle so its neural pass
            // preparation, release window and timeout handling stay authoritative.
            try
            {
                for (int i = 0; i < fields.Length; i++)
                    fields[i].SetValue(match, values[i]);
            }
            catch (Exception exception)
            {
                // Roll back partial reflection writes and restore the original ball owner.
                for (int i = 0; i < fields.Length; i++)
                {
                    try { fields[i].SetValue(match, previousValues[i]); }
                    catch (Exception rollbackException)
                    {
                        Debug.LogWarning(
                            "[NBA2K12 Showcase] Could not roll back pass field " + fieldNames[i] +
                            ": " + rollbackException.Message,
                            this);
                    }
                }

                Vector3 recoveryBallPosition = active.AimPoint + active.transform.forward * 0.16f;
                match.PossessionManager.ResetPossession(
                    active,
                    recoveryBallPosition,
                    Quaternion.LookRotation(active.transform.forward, Vector3.up));
                Debug.LogError(
                    "[NBA2K12 Showcase] Pass lifecycle update failed; previous possession was restored. " +
                    exception.Message,
                    this);
                return;
            }

            Debug.Log("[NBA2K12 Showcase] Gamepad pass committed to player " + target.PlayerIndex, this);
        }
    }
}