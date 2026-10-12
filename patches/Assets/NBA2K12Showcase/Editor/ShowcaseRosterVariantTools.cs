#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CrowdEyes.AI4Animation.Basketball;
using UnityEditor;
using UnityEngine;

namespace NBA2K12Showcase.Editor
{
    /// <summary>
    /// Maps imported, compatible Humanoid assets to Player Index 0-9 from P01...P10 filename prefixes.
    /// Never modifies the visual prefab unless at least one compatible asset was found; saves a backup first.
    /// </summary>
    public static class ShowcaseRosterVariantTools
    {
        private const string PrivateRoot = "Assets/NBA2K12Showcase/PrivateAssets";
        private const string VisualPrefabPath =
            "Assets/AI4AnimationRemake/Characters/CrowdEyesTwin/BasketballPlayerVisual.prefab";
        private static readonly Regex SlotPrefix = new Regex(
            @"^(?:P|PLAYER)[_-]?0*(?<number>10|[1-9])(?:[_ -]|$)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

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

        [MenuItem("Tools/NBA2K12 Showcase/15 - Assign P01-P10 Player Models by Filename")]
        public static void AssignVariantsByFilename()
        {
            string diskRoot = Path.Combine(Application.dataPath, "NBA2K12Showcase/PrivateAssets");
            if (!Directory.Exists(diskRoot))
            {
                EditorUtility.DisplayDialog(
                    "Showcase Roster Models",
                    "No private model folder exists yet. Use menu item 5 to import user-authorized player model exports first.",
                    "OK");
                return;
            }

            string[] files = Directory.GetFiles(diskRoot, "*.*", SearchOption.AllDirectories)
                .Where(path =>
                    path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".glb", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            var candidates = new System.Collections.Generic.Dictionary<int, GameObject>();
            var rejected = new System.Collections.Generic.List<string>();
            foreach (string fullPath in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(fullPath);
                Match match = SlotPrefix.Match(fileName);
                if (!match.Success) continue;

                int slotNumber = int.Parse(match.Groups["number"].Value);
                int playerIndex = slotNumber - 1;
                string assetPath = "Assets/NBA2K12Showcase/PrivateAssets/" +
                    fullPath.Substring(diskRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        .Replace(Path.DirectorySeparatorChar, '/');

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab == null)
                {
                    rejected.Add(fileName + ": Unity did not import this file as a GameObject.");
                    continue;
                }
                if (!Validate(prefab, out string reason))
                {
                    rejected.Add(fileName + ": " + reason);
                    continue;
                }

                if (candidates.ContainsKey(playerIndex))
                {
                    rejected.Add(fileName + ": duplicate file for P" + slotNumber.ToString("00") + "; kept the first valid asset.");
                    continue;
                }
                candidates[playerIndex] = prefab;
            }

            if (candidates.Count == 0)
            {
                string report = "No compatible model files with P01-P10 prefixes were found.\n\n" +
                    "Name files such as P01_Kobe_Bryant.fbx, P02_Player_Name.fbx ... P10_Player_Name.fbx. " +
                    "Each model must include a valid Humanoid Avatar, the required retarget bones and a weighted SkinnedMeshRenderer.\n\n" +
                    string.Join("\n", rejected.Take(8));
                EditorUtility.DisplayDialog("Showcase Roster Models", report, "OK");
                return;
            }

            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefabPath);
            if (prefabRoot == null)
            {
                EditorUtility.DisplayDialog(
                    "Showcase Roster Models",
                    "The local BasketballPlayerVisual prefab was not found. Run menu item 8 on a valid imported model first so the variant adapter is installed.",
                    "OK");
                return;
            }

            string sourceDiskPath = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "AI4AnimationRemake/Characters/CrowdEyesTwin/BasketballPlayerVisual.prefab"));
            if (!File.Exists(sourceDiskPath))
            {
                EditorUtility.DisplayDialog("Showcase Roster Models", "Could not locate the target prefab on disk. No changes were made.", "OK");
                return;
            }

            GameObject loaded = null;
            string backupPath = string.Empty;
            try
            {
                string backupDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "NBA2K12Showcase", "Backups");
                Directory.CreateDirectory(backupDir);
                backupPath = Path.Combine(backupDir,
                    "BasketballPlayerVisual_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".prefab");
                File.Copy(sourceDiskPath, backupPath, false);

                loaded = PrefabUtility.LoadPrefabContents(VisualPrefabPath);
                ShowcasePlayerVisualVariant variant = loaded.GetComponent<ShowcasePlayerVisualVariant>();
                if (variant == null)
                    throw new InvalidOperationException("The target visual prefab has no ShowcasePlayerVisualVariant. Run menu item 8 before assigning per-player models.");

                SerializedObject serialized = new SerializedObject(variant);
                SerializedProperty slots = serialized.FindProperty("playerSlots");
                if (slots == null || !slots.isArray)
                    throw new InvalidOperationException("The playerSlots serialized field was not found. The variant adapter may have changed.");

                slots.arraySize = 10;
                for (int index = 0; index < 10; index++)
                {
                    SerializedProperty slot = slots.GetArrayElementAtIndex(index);
                    slot.FindPropertyRelative("playerIndex").intValue = index;
                    SerializedProperty modelProperty = slot.FindPropertyRelative("modelPrefab");
                    if (candidates.TryGetValue(index, out GameObject model))
                        modelProperty.objectReferenceValue = model;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(variant);

                PrefabUtility.SaveAsPrefabAsset(loaded, VisualPrefabPath, out bool success);
                if (!success)
                    throw new InvalidOperationException("Unity did not confirm saving the updated player visual prefab.");

                AssetDatabase.SaveAssets();
                string rows = string.Join("\n", Enumerable.Range(0, 10).Select(index =>
                    "P" + (index + 1).ToString("00") + ": " +
                    (candidates.TryGetValue(index, out GameObject model) ? model.name : "(keeps fallback model)")));
                string report = "Assigned " + candidates.Count + "/10 compatible model slots.\n\n" + rows +
                    "\n\nBackup saved outside the Unity project:\n" + backupPath +
                    "\n\nRestart Play Mode to see the variants. Verify animation, materials, scale, face/jersey textures and ball contact on each player.";
                Debug.Log("[NBA2K12 Showcase] " + report);
                EditorUtility.DisplayDialog("Showcase Roster Models", report, "OK");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog(
                    "Showcase Roster Models",
                    "Variant assignment did not complete. Backup preserved at:\n" + backupPath + "\n\n" + ex.Message,
                    "OK");
            }
            finally
            {
                if (loaded != null)
                    PrefabUtility.UnloadPrefabContents(loaded);
            }
        }

        private static bool Validate(GameObject model, out string reason)
        {
            Animator animator = model.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
            {
                reason = "No valid Humanoid Avatar; configure the FBX Rig tab first.";
                return false;
            }

            foreach (HumanBodyBones bone in RequiredBones)
            {
                if (animator.GetBoneTransform(bone) == null)
                {
                    reason = "Missing required retarget bone " + bone + ".";
                    return false;
                }
            }

            bool weightedSkin = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any(renderer =>
                renderer != null && renderer.sharedMesh != null &&
                renderer.bones != null && renderer.bones.Length > 0 && renderer.rootBone != null);
            if (!weightedSkin)
            {
                reason = "No weighted SkinnedMeshRenderer with a mesh, bones and root bone.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }
}
#endif
