#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using CrowdEyes.AI4Animation.Basketball;
using UnityEditor;
using UnityEngine;

namespace NBA2K12Showcase.Editor
{
    /// <summary>
    /// Explicit, backed-up model integration. Never modifies the upstream prefab until the selected
    /// private FBX has a valid Humanoid Avatar.
    /// </summary>
    public static class NBA2K12RigIntegrationTools
    {
        private const string PrivateRoot = "Assets/NBA2K12Showcase/PrivateAssets";
        private const string VisualPrefabPath =
            "Assets/AI4AnimationRemake/Characters/CrowdEyesTwin/BasketballPlayerVisual.prefab";

        private static readonly HumanBodyBones[] RequiredBones =
        {
            HumanBodyBones.Hips,
            HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes,
            HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, HumanBodyBones.RightToes,
            HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.UpperChest,
            HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm,
            HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand,
            HumanBodyBones.Neck, HumanBodyBones.Head,
            HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm,
            HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand
        };

        [MenuItem("Tools/NBA2K12 Showcase/4 - Configure Selected Model as Humanoid")]
        public static void ConfigureSelectedModelAsHumanoid()
        {
            if (!TryGetSelectedPrivateModel(out GameObject model, out string path)) return;
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                EditorUtility.DisplayDialog("Showcase Rig Setup", "The selected asset is not an FBX/OBJ/DAE ModelImporter asset.", "OK");
                return;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.SaveAndReimport();

            GameObject refreshed = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Animator animator = refreshed != null ? refreshed.GetComponentInChildren<Animator>(true) : null;
            bool human = animator != null && animator.avatar != null && animator.avatar.isHuman;
            string message = human
                ? "Unity created a Humanoid Avatar for " + Path.GetFileName(path) + ". Run the rig audit before using it in gameplay."
                : "Unity could not create a valid Humanoid Avatar automatically. Open the asset's Rig tab, click Configure, and resolve all avatar mapping errors. The model has not been integrated.";
            Debug.Log("[NBA2K12 Showcase] " + message);
            EditorUtility.DisplayDialog("Showcase Rig Setup", message, "OK");
        }

        [MenuItem("Tools/NBA2K12 Showcase/7 - Audit Selected Player Rig")]
        public static void AuditSelectedRig()
        {
            if (!TryGetSelectedPrivateModel(out GameObject model, out string path)) return;
            Animator animator = model.GetComponentInChildren<Animator>(true);
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            SkinnedMeshRenderer[] skinned = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            int missing = 0;
            string missingNames = string.Empty;
            bool humanoid = animator != null && animator.avatar != null && animator.avatar.isHuman;
            if (humanoid)
            {
                foreach (HumanBodyBones bone in RequiredBones)
                {
                    if (animator.GetBoneTransform(bone) == null)
                    {
                        missing++;
                        missingNames += "  - " + bone + "\n";
                    }
                }
            }

            System.Text.StringBuilder report = new System.Text.StringBuilder();
            report.AppendLine("NBA2K12 Showcase — Player Rig Audit");
            report.AppendLine("Asset: " + path);
            report.AppendLine("Animator: " + (animator != null ? animator.name : "MISSING"));
            report.AppendLine("Humanoid Avatar: " + (humanoid ? "YES" : "NO"));
            report.AppendLine("Required retarget bones missing: " + (humanoid ? missing : RequiredBones.Length));
            if (missingNames.Length > 0) report.Append(missingNames);
            report.AppendLine("Renderers: " + renderers.Length);
            report.AppendLine("Skinned Mesh Renderers: " + skinned.Length);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;
                string meshName = renderer is SkinnedMeshRenderer smr && smr.sharedMesh != null
                    ? smr.sharedMesh.name
                    : "(non-skinned or no mesh)";
                report.AppendLine("Renderer " + i + ": " + renderer.name + " | " + renderer.GetType().Name + " | " + meshName);
                if (renderer is SkinnedMeshRenderer skin)
                    report.AppendLine("    bones=" + (skin.bones != null ? skin.bones.Length : 0) +
                                      ", rootBone=" + (skin.rootBone != null ? skin.rootBone.name : "NULL"));
            }

            string text = report.ToString();
            Debug.Log(text);
            EditorUtility.DisplayDialog("Showcase Player Rig Audit", text, "OK");
        }

        [MenuItem("Tools/NBA2K12 Showcase/8 - Replace In-Game Player Visual with Selected Model (LOCAL)")]
        public static void ReplaceInGameVisual()
        {
            if (!TryGetSelectedPrivateModel(out GameObject selected, out string selectedPath)) return;

            if (!ValidateModelForRetargeting(selected, out string validationFailure))
            {
                EditorUtility.DisplayDialog(
                    "Showcase Model Integration",
                    validationFailure + "\\n\\nUse menu items 4 and 7 to configure/audit the selected FBX. No prefab was changed.",
                    "OK");
                return;
            }

            Animator candidateAnimator = selected.GetComponentInChildren<Animator>(true);

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefabPath);
            if (prefabAsset == null)
            {
                EditorUtility.DisplayDialog("Showcase Model Integration", "Could not find the source visual prefab at " + VisualPrefabPath, "OK");
                return;
            }

            string sourceDiskPath = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "AI4AnimationRemake/Characters/CrowdEyesTwin/BasketballPlayerVisual.prefab"));
            if (!File.Exists(sourceDiskPath))
            {
                EditorUtility.DisplayDialog("Showcase Model Integration", "Could not locate the prefab file on disk. No changes were made.", "OK");
                return;
            }

            string backupDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NBA2K12Showcase", "Backups");
            Directory.CreateDirectory(backupDir);
            string backupPath = Path.Combine(
                backupDir, "BasketballPlayerVisual_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".prefab");
            File.Copy(sourceDiskPath, backupPath, false);

            GameObject prefabRoot = null;
            bool saved = false;
            string detail = string.Empty;
            try
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(VisualPrefabPath);
                BasketballHumanoidVisualRetargeter retargeter =
                    prefabRoot.GetComponent<BasketballHumanoidVisualRetargeter>();
                BasketballPlayerAppearance appearance =
                    prefabRoot.GetComponent<BasketballPlayerAppearance>();
                if (retargeter == null || appearance == null)
                    throw new InvalidOperationException("The visual prefab is missing its retargeter or appearance component.");

                Transform oldModelRoot = FindModelRoot(prefabRoot.transform, retargeter);
                if (oldModelRoot == null)
                    throw new InvalidOperationException("Could not safely identify the existing nested model root. No save was made.");

                GameObject replacement = PrefabUtility.InstantiatePrefab(selected, prefabRoot.scene) as GameObject;
                if (replacement == null)
                    throw new InvalidOperationException("Unity could not instantiate the selected model asset.");

                replacement.transform.SetParent(prefabRoot.transform, false);
                replacement.transform.localPosition = Vector3.zero;
                replacement.transform.localRotation = Quaternion.identity;
                replacement.transform.localScale = Vector3.one;

                Animator newAnimator = replacement.GetComponentInChildren<Animator>(true);
                if (newAnimator == null || newAnimator.avatar == null || !newAnimator.avatar.isHuman)
                {
                    UnityEngine.Object.DestroyImmediate(replacement, true);
                    throw new InvalidOperationException("The instantiated asset did not retain a valid Humanoid Avatar.");
                }

                // Remove original visual (including its number overlay meshes), then bind the new Humanoid.
                UnityEngine.Object.DestroyImmediate(oldModelRoot.gameObject, true);

                // The upstream visual stores its old jersey digits as separate direct child meshes.
                // Remove them as well; otherwise the fallback digits can float around the new model.
                for (int childIndex = prefabRoot.transform.childCount - 1; childIndex >= 0; childIndex--)
                {
                    Transform child = prefabRoot.transform.GetChild(childIndex);
                    if (child.name.StartsWith("Jersey Number", StringComparison.OrdinalIgnoreCase))
                        UnityEngine.Object.DestroyImmediate(child.gameObject, true);
                }

                BasketballJerseyNumberDisplay numberDisplay =
                    prefabRoot.GetComponent<BasketballJerseyNumberDisplay>();
                if (numberDisplay != null) UnityEngine.Object.DestroyImmediate(numberDisplay, true);

                Renderer[] modelRenderers = replacement.GetComponentsInChildren<Renderer>(true);
                Renderer body = FindRenderer(modelRenderers, "skin", "body", "head");
                Renderer jersey = FindRenderer(modelRenderers, "jersey", "shirt", "uniform", "top");
                Renderer shorts = FindRenderer(modelRenderers, "shorts", "trunks", "pants", "bottom");
                if (body == jersey) body = null;
                if (body == shorts) body = null;
                if (jersey == shorts) shorts = null;

                SerializedObject appearanceObject = new SerializedObject(appearance);
                SetReference(appearanceObject, "bodyRenderer", body);
                SetReference(appearanceObject, "jerseyRenderer", jersey);
                SetReference(appearanceObject, "shortsRenderer", shorts);
                SetReference(appearanceObject, "numberDisplay", null);
                ClearArray(appearanceObject, "skinMaterials");
                ClearArray(appearanceObject, "teamProfiles");
                appearanceObject.ApplyModifiedPropertiesWithoutUndo();

                retargeter.Configure(newAnimator);
                ShowcasePlayerVisualVariant variants =
                    prefabRoot.GetComponent<ShowcasePlayerVisualVariant>();
                if (variants == null) variants = prefabRoot.AddComponent<ShowcasePlayerVisualVariant>();
                variants.Configure(selected, replacement.transform);

                EditorUtility.SetDirty(appearance);
                EditorUtility.SetDirty(retargeter);
                EditorUtility.SetDirty(variants);

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, VisualPrefabPath, out bool succeeded);
                saved = succeeded;
                if (!saved) throw new InvalidOperationException("Unity did not confirm saving the updated visual prefab.");

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                detail =
                    "Updated local prefab: " + VisualPrefabPath + "\n" +
                    "Model: " + selectedPath + "\n" +
                    "Detected renderers — body: " + NameOrNone(body) +
                    ", jersey: " + NameOrNone(jersey) + ", shorts: " + NameOrNone(shorts) + "\n" +
                    "Backup: " + backupPath + "\n\n" +
                    "All players will now use this fallback model until player-specific model variants are assigned to ShowcasePlayerVisualVariant. " +
                    "The prefab's number overlay and automatic team-color tinting were disabled to avoid corrupting a textured uniform. " +
                    "This is local to the downloaded NeuralCourt submodule and is not pushed to GitHub.";
                Debug.Log("[NBA2K12 Showcase] " + detail);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                detail = "Integration failed. No successful save was confirmed. Backup preserved at:\n" +
                         backupPath + "\n\nError: " + ex.Message;
            }
            finally
            {
                if (prefabRoot != null) PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            EditorUtility.DisplayDialog(
                saved ? "Showcase Model Integrated" : "Showcase Model Integration Failed",
                detail,
                "OK");
        }

        private static bool ValidateModelForRetargeting(GameObject model, out string reason)
        {
            Animator animator = model != null ? model.GetComponentInChildren<Animator>(true) : null;
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
            {
                reason = "The selected model has no valid Humanoid Avatar.";
                return false;
            }

            for (int i = 0; i < RequiredBones.Length; i++)
            {
                if (animator.GetBoneTransform(RequiredBones[i]) == null)
                {
                    reason = "The model is missing a required retarget bone: " + RequiredBones[i] + ".";
                    return false;
                }
            }

            SkinnedMeshRenderer[] renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            bool hasSkinnedMesh = false;
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                if (renderer != null && renderer.sharedMesh != null &&
                    renderer.bones != null && renderer.bones.Length > 0 &&
                    renderer.rootBone != null)
                {
                    hasSkinnedMesh = true;
                    break;
                }
            }

            if (!hasSkinnedMesh)
            {
                reason = "No valid SkinnedMeshRenderer with a mesh, weighted bone array, and root bone was found. Static OBJ models are preview-only until they are rigged.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private static Transform FindModelRoot(
            Transform root,
            BasketballHumanoidVisualRetargeter retargeter)
        {
            Animator animator = retargeter != null ? retargeter.HumanoidAnimator : null;
            if (animator == null) animator = root.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                Transform current = animator.transform;
                while (current.parent != null && current.parent != root) current = current.parent;
                if (current.parent == root) return current;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.GetComponentInChildren<Animator>(true) != null ||
                    child.GetComponentInChildren<SkinnedMeshRenderer>(true) != null)
                    return child;
            }
            return null;
        }

        private static Renderer FindRenderer(Renderer[] renderers, params string[] tokens)
        {
            foreach (string token in tokens)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer != null &&
                        renderer.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                        return renderer;
                }
            }
            return null;
        }

        private static void SetReference(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void ClearArray(SerializedObject serializedObject, string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null && property.isArray) property.arraySize = 0;
        }

        private static string NameOrNone(UnityEngine.Object value) => value != null ? value.name : "(not auto-detected)";

        private static bool TryGetSelectedPrivateModel(out GameObject model, out string path)
        {
            model = Selection.activeObject as GameObject;
            path = model != null ? AssetDatabase.GetAssetPath(model) : string.Empty;
            if (model == null || string.IsNullOrWhiteSpace(path) ||
                !path.StartsWith(PrivateRoot + "/", StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog(
                    "Showcase Model Integration",
                    "Select an imported FBX/OBJ model asset inside Assets/NBA2K12Showcase/PrivateAssets first.",
                    "OK");
                return false;
            }
            return true;
        }
    }
}
#endif
