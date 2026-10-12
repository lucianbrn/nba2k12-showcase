#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using CrowdEyes.AI4Animation.Basketball;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace NBA2K12Showcase.Editor
{
    /// <summary>Presentation-only tools. These do not modify neural simulation state.</summary>
    public static class ShowcasePresentationTools
    {
        private const string DemoScene = "Assets/Scenes/BasketballDemo.unity";
        private const string Root = "Assets/NBA2K12Showcase";
        private const string Teams = Root + "/Teams";
        private const string Settings = Root + "/Settings";
        private const string GradePath = Settings + "/Showcase_TV_Grade.asset";
        private const string VolumeName = "NBA2K12 Showcase - TV Color Grade";

        [MenuItem("Tools/NBA2K12 Showcase/1 - Apply TV Presentation Polish")]
        public static void ApplyTvPresentation()
        {
            Camera cam = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (cam == null)
            {
                EditorUtility.DisplayDialog("Showcase", "No camera in the active scene. Open BasketballDemo.unity first.", "OK");
                return;
            }

            Undo.RecordObject(cam, "Apply Showcase Camera Quality");
            cam.allowHDR = true;
            cam.allowMSAA = true;
            cam.farClipPlane = Mathf.Max(cam.farClipPlane, 180f);

            UniversalAdditionalCameraData data = cam.GetComponent<UniversalAdditionalCameraData>();
            if (data == null) data = Undo.AddComponent<UniversalAdditionalCameraData>(cam.gameObject);
            Undo.RecordObject(data, "Enable Showcase Post Processing");
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntialiasing;
            data.antialiasingQuality = AntialiasingQuality.High;

            QualitySettings.antiAliasing = 4;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowCascades = 4;
            QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance, 85f);
            QualitySettings.vSyncCount = 1;

            int directionalCount = 0;
            foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (light == null || light.type != LightType.Directional || !light.gameObject.scene.IsValid()) continue;
                Undo.RecordObject(light, "Enable Showcase Soft Shadows");
                light.shadows = LightShadows.Soft;
                light.shadowStrength = Mathf.Clamp(light.shadowStrength, 0.72f, 0.9f);
                directionalCount++;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.30f, 0.35f, 0.43f);
            RenderSettings.ambientEquatorColor = new Color(0.15f, 0.17f, 0.21f);
            RenderSettings.ambientGroundColor = new Color(0.055f, 0.06f, 0.075f);

            EnsureFolder(Root);
            EnsureFolder(Settings);
            VolumeProfile profile = GetOrCreateGrade();
            GameObject go = GameObject.Find(VolumeName);
            if (go == null)
            {
                go = new GameObject(VolumeName);
                Undo.RegisterCreatedObjectUndo(go, "Create Showcase Color Grade");
            }
            Volume volume = go.GetComponent<Volume>();
            if (volume == null) volume = Undo.AddComponent<Volume>(go);
            Undo.RecordObject(volume, "Apply Showcase Global Volume");
            volume.isGlobal = true;
            volume.priority = 20f;
            volume.weight = 1f;
            volume.sharedProfile = profile;

            EditorUtility.SetDirty(cam);
            EditorUtility.SetDirty(data);
            EditorUtility.SetDirty(volume);
            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();

            string report = "TV presentation settings applied.\n\nCamera: " + cam.name +
                "\nDirectional lights configured: " + directionalCount +
                "\nQuality: 4x MSAA request, high shadows, 4 cascades" +
                "\nURP volume: ACES tonemapping, subtle bloom, contrast grade and vignette." +
                "\n\nCheck the Game view and reduce shadow distance/MSAA if frame rate suffers.";
            Debug.Log("[NBA2K12 Showcase] " + report);
            EditorUtility.DisplayDialog("NBA2K12 Showcase", report, "OK");
        }

        [MenuItem("Tools/NBA2K12 Showcase/2 - Apply Lakers vs Celtics Palette")]
        public static void ApplyTeamPalette()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !string.Equals(scene.path, DemoScene, StringComparison.Ordinal))
            {
                EditorUtility.DisplayDialog("Showcase", "Open Assets/Scenes/BasketballDemo.unity first. No scene was changed.", "OK");
                return;
            }

            EnsureFolder(Root);
            EnsureFolder(Teams);
            BasketballTeamAppearanceProfile home = CreateProfile(
                Teams + "/Showcase_Lakers.asset", 0, "Los Angeles Lakers",
                Hex("552583"), Hex("34204F"), Hex("FDB927"));
            BasketballTeamAppearanceProfile away = CreateProfile(
                Teams + "/Showcase_Celtics.asset", 1, "Boston Celtics",
                Hex("F5F3EA"), Hex("E4E1D6"), Hex("007A33"));

            int updated = 0;
            foreach (BasketballPlayerAppearance appearance in
                     UnityEngine.Object.FindObjectsByType<BasketballPlayerAppearance>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (appearance == null || appearance.gameObject.scene != scene) continue;

                SerializedObject so = new SerializedObject(appearance);
                SerializedProperty array = so.FindProperty("teamProfiles");
                if (array == null || !array.isArray) continue;
                Undo.RecordObject(appearance, "Apply Showcase Team Palette");
                array.arraySize = 2;
                array.GetArrayElementAtIndex(0).objectReferenceValue = home;
                array.GetArrayElementAtIndex(1).objectReferenceValue = away;
                so.ApplyModifiedProperties();
                appearance.Apply();
                PrefabUtility.RecordPrefabInstancePropertyModifications(appearance);
                EditorUtility.SetDirty(appearance);
                updated++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            string report = "Assigned team color profiles to " + updated + " player appearance component(s).\n\n" +
                "Team ID 0: Lakers-inspired purple/gold.\nTeam ID 1: Celtics-inspired white/green.\n\n" +
                "This uses the existing jersey, shorts and number-color system; it does not add official logos, " +
                "authentic licensed uniform textures, or NBA player likenesses.";
            Debug.Log("[NBA2K12 Showcase] " + report);
            EditorUtility.DisplayDialog("NBA2K12 Showcase", report, "OK");
        }

        [MenuItem("Tools/NBA2K12 Showcase/3 - Audit Active Scene")]
        public static void Audit()
        {
            Scene scene = SceneManager.GetActiveScene();
            int players = 0, skinned = 0, renderers = 0, cameras = 0, appearances = 0;
            foreach (BasketballTeamMember v in UnityEngine.Object.FindObjectsByType<BasketballTeamMember>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (v != null && v.gameObject.scene == scene) players++;
            foreach (SkinnedMeshRenderer v in UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (v != null && v.gameObject.scene == scene) skinned++;
            foreach (Renderer v in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (v != null && v.gameObject.scene == scene) renderers++;
            foreach (Camera v in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (v != null && v.gameObject.scene == scene) cameras++;
            foreach (BasketballPlayerAppearance v in UnityEngine.Object.FindObjectsByType<BasketballPlayerAppearance>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (v != null && v.gameObject.scene == scene) appearances++;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("NBA2K12 Showcase - Active Scene Audit");
            sb.AppendLine("Scene: " + (string.IsNullOrEmpty(scene.path) ? scene.name : scene.path));
            sb.AppendLine("Basketball team members: " + players);
            sb.AppendLine("Skinned mesh renderers: " + skinned);
            sb.AppendLine("Renderers total: " + renderers);
            sb.AppendLine("Cameras: " + cameras);
            sb.AppendLine("Player appearance components: " + appearances);
            sb.AppendLine("Render pipeline: " + (GraphicsSettings.currentRenderPipeline != null
                ? GraphicsSettings.currentRenderPipeline.GetType().Name : "not active"));
            Debug.Log(sb.ToString());
            EditorUtility.DisplayDialog("NBA2K12 Showcase - Audit", sb.ToString(), "OK");
        }

        private static VolumeProfile GetOrCreateGrade()
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GradePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "Showcase TV Grade";
                AssetDatabase.CreateAsset(profile, GradePath);
            }

            if (!profile.TryGet(out Bloom bloom)) bloom = profile.Add<Bloom>(true);
            bloom.active = true;
            bloom.intensity.Override(0.18f);
            bloom.threshold.Override(1.15f);
            bloom.scatter.Override(0.55f);

            if (!profile.TryGet(out ColorAdjustments grade)) grade = profile.Add<ColorAdjustments>(true);
            grade.active = true;
            grade.postExposure.Override(0.05f);
            grade.contrast.Override(8f);
            grade.saturation.Override(-5f);
            grade.colorFilter.Override(new Color(0.97f, 0.985f, 1f, 1f));

            if (!profile.TryGet(out Vignette vignette)) vignette = profile.Add<Vignette>(true);
            vignette.active = true;
            vignette.intensity.Override(0.12f);
            vignette.smoothness.Override(0.38f);
            vignette.center.Override(new Vector2(0.5f, 0.52f));

            if (!profile.TryGet(out Tonemapping tone)) tone = profile.Add<Tonemapping>(true);
            tone.active = true;
            tone.mode.Override(TonemappingMode.ACES);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static BasketballTeamAppearanceProfile CreateProfile(
            string path, int teamId, string name, Color jersey, Color shorts, Color number)
        {
            BasketballTeamAppearanceProfile profile =
                AssetDatabase.LoadAssetAtPath<BasketballTeamAppearanceProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<BasketballTeamAppearanceProfile>();
                profile.name = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(profile, path);
            }
            profile.Configure(teamId, name, jersey, shorts, number);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static Color Hex(string text)
        {
            if (!ColorUtility.TryParseHtmlString("#" + text, out Color color))
                throw new ArgumentException("Invalid HTML color: " + text, nameof(text));
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
