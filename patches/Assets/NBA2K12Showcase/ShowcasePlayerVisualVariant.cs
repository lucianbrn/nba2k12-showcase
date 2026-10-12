using System;
using System.Reflection;
using CrowdEyes.AI4Animation.Basketball;
using UnityEngine;

namespace NBA2K12Showcase
{
    /// <summary>
    /// Optional per-player visual variants. Assign separate, skinned Humanoid FBX prefab assets
    /// for the ten roster slots. The canonical neural rig remains authoritative for motion.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(600)]
    public sealed class ShowcasePlayerVisualVariant : MonoBehaviour
    {
        [Serializable]
        public sealed class PlayerSlot
        {
            [Range(0, 9)] public int playerIndex;
            public GameObject modelPrefab;
        }

        // This mirrors the exact private Animator field used by the pinned NeuralCourt retargeter.
        // Fail safely if an upstream update renames it; never silently swap the model without animation.
        private static readonly FieldInfo AnimatorField =
            typeof(BasketballHumanoidVisualRetargeter).GetField(
                "humanoidAnimator", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo InitializedField =
            typeof(BasketballHumanoidVisualRetargeter).GetField(
                "initialized", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo VisibilityCachedField =
            typeof(BasketballHumanoidVisualRetargeter).GetField(
                "visibilityCached", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly HumanBodyBones[] RequiredBones =
        {
            HumanBodyBones.Hips,
            HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg,
            HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes,
            HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg,
            HumanBodyBones.RightFoot, HumanBodyBones.RightToes,
            HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.UpperChest,
            HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm,
            HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand,
            HumanBodyBones.Neck, HumanBodyBones.Head,
            HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm,
            HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand
        };

        [Header("Fallback and slot models")]
        [SerializeField] private GameObject fallbackPrefab;
        [SerializeField] private Transform currentModelRoot;
        [SerializeField] private PlayerSlot[] playerSlots = Array.Empty<PlayerSlot>();

        private BasketballTeamMember teamMember;
        private BasketballHumanoidVisualRetargeter retargeter;
        private bool attempted;

        public GameObject FallbackPrefab => fallbackPrefab;
        public Transform CurrentModelRoot => currentModelRoot;

        private void Awake()
        {
            teamMember = GetComponentInParent<BasketballTeamMember>();
            retargeter = GetComponent<BasketballHumanoidVisualRetargeter>();
            if (currentModelRoot == null)
            {
                currentModelRoot = FindModelRoot();
            }
        }

        private void Start()
        {
            ApplyRosterVariant();
        }

        /// <summary>
        /// Uses PlayerIndex (P1-P10 = 0-9) to select a per-slot model. A missing slot keeps the
        /// fallback model. A selected model must contain an Animator with a valid Humanoid Avatar.
        /// </summary>
        public void ApplyRosterVariant()
        {
            if (attempted) return;
            if (teamMember == null) teamMember = GetComponentInParent<BasketballTeamMember>();
            if (teamMember == null) return;

            int index = teamMember.PlayerIndex;
            PlayerSlot slot = FindSlot(index);
            if (slot == null || slot.modelPrefab == null ||
                (fallbackPrefab != null && slot.modelPrefab == fallbackPrefab))
            {
                attempted = true;
                return;
            }

            if (!IsCompatibleModel(slot.modelPrefab, out string validationFailure))
            {
                Debug.LogError(
                    "[NBA2K12 Showcase] Player slot " + index + " was not switched. " +
                    validationFailure,
                    this);
                attempted = true;
                return;
            }

            Animator prefabAnimator = slot.modelPrefab.GetComponentInChildren<Animator>(true);

            if (AnimatorField == null || InitializedField == null || VisibilityCachedField == null)
            {
                Debug.LogError(
                    "[NBA2K12 Showcase] The pinned NeuralCourt retargeter API changed. " +
                    "Model variants were not applied; update the adapter rather than disabling retargeting.",
                    this);
                attempted = true;
                return;
            }

            if (retargeter == null) retargeter = GetComponent<BasketballHumanoidVisualRetargeter>();
            if (retargeter == null)
            {
                Debug.LogError("[NBA2K12 Showcase] Missing BasketballHumanoidVisualRetargeter.", this);
                attempted = true;
                return;
            }

            Transform parent = currentModelRoot != null ? currentModelRoot.parent : transform;
            Vector3 localPosition = currentModelRoot != null ? currentModelRoot.localPosition : Vector3.zero;
            Quaternion localRotation = currentModelRoot != null ? currentModelRoot.localRotation : Quaternion.identity;
            Vector3 localScale = currentModelRoot != null ? currentModelRoot.localScale : Vector3.one;

            GameObject oldRoot = currentModelRoot != null ? currentModelRoot.gameObject : null;
            if (oldRoot != null) oldRoot.SetActive(false);

            GameObject replacement = Instantiate(slot.modelPrefab, parent, false);
            replacement.name = slot.modelPrefab.name;
            replacement.transform.localPosition = localPosition;
            replacement.transform.localRotation = localRotation;
            replacement.transform.localScale = localScale;

            if (!IsCompatibleModel(replacement, out string instanceFailure))
            {
                Destroy(replacement);
                if (oldRoot != null) oldRoot.SetActive(true);
                Debug.LogError(
                    "[NBA2K12 Showcase] Instantiated variant is incompatible; fallback restored. " +
                    instanceFailure,
                    this);
                attempted = true;
                return;
            }

            Animator replacementAnimator = replacement.GetComponentInChildren<Animator>(true);

            retargeter.enabled = false;
            AnimatorField.SetValue(retargeter, replacementAnimator);
            InitializedField.SetValue(retargeter, false);
            VisibilityCachedField.SetValue(retargeter, false);
            currentModelRoot = replacement.transform;
            retargeter.enabled = true; // OnEnable revalidates and binds the new Human Avatar.

            if (oldRoot != null) Destroy(oldRoot);
            attempted = true;
            Debug.Log(
                "[NBA2K12 Showcase] Applied player visual variant " + index + ": " +
                slot.modelPrefab.name + ". Verify animation and uniform fit in Play Mode.",
                this);
        }

        private static bool IsCompatibleModel(GameObject model, out string reason)
        {
            Animator animator = model != null ? model.GetComponentInChildren<Animator>(true) : null;
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
            {
                reason = "The model needs a valid Unity Humanoid Avatar.";
                return false;
            }

            foreach (HumanBodyBones bone in RequiredBones)
            {
                if (animator.GetBoneTransform(bone) == null)
                {
                    reason = "The Avatar is missing required retarget bone " + bone + ".";
                    return false;
                }
            }

            bool validSkin = false;
            foreach (SkinnedMeshRenderer renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer != null && renderer.sharedMesh != null &&
                    renderer.bones != null && renderer.bones.Length > 0 &&
                    renderer.rootBone != null)
                {
                    validSkin = true;
                    break;
                }
            }

            if (!validSkin)
            {
                reason = "The model must include a SkinnedMeshRenderer with mesh, bones and root bone; static meshes are preview-only.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private PlayerSlot FindSlot(int playerIndex)
        {
            if (playerSlots == null) return null;
            for (int i = 0; i < playerSlots.Length; i++)
            {
                if (playerSlots[i] != null && playerSlots[i].playerIndex == playerIndex)
                    return playerSlots[i];
            }
            return null;
        }

        private Transform FindModelRoot()
        {
            Animator animator = GetComponentInChildren<Animator>(true);
            if (animator == null) return null;
            Transform current = animator.transform;
            while (current.parent != null && current.parent != transform)
                current = current.parent;
            return current.parent == transform ? current : animator.transform;
        }

#if UNITY_EDITOR
        public void Configure(GameObject fallbackModel, Transform modelRoot)
        {
            fallbackPrefab = fallbackModel;
            currentModelRoot = modelRoot;
            playerSlots = Array.Empty<PlayerSlot>();
            teamMember = GetComponentInParent<BasketballTeamMember>();
            retargeter = GetComponent<BasketballHumanoidVisualRetargeter>();
            attempted = false;
        }
#endif
    }
}
