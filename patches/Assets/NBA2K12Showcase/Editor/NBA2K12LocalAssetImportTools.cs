#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NBA2K12Showcase.Editor
{
    /// <summary>
    /// Imports locally exported models and textures without downloading or committing game data.
    /// A preview scene is not the same as binding a model to the live simulation rig.
    /// </summary>
    public static class NBA2K12LocalAssetImportTools
    {
        private const string Root = "Assets/NBA2K12Showcase";
        private const string PrivateRoot = Root + "/PrivateAssets";
        private const string PreviewRoot = Root + "/Previews";

        private static readonly HashSet<string> AllowedExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".fbx", ".obj", ".dae", ".glb", ".gltf", ".png", ".tga", ".jpg", ".jpeg",
                ".bmp", ".tif", ".tiff", ".psd", ".dds", ".mtl", ".mat", ".json", ".txt"
            };

        [MenuItem("Tools/NBA2K12 Showcase/5 - Import Local Model and Texture Exports")]
        public static void ImportLocalExports()
        {
            string source = EditorUtility.OpenFolderPanel(
                "Select a folder of exported models/textures",
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                string.Empty);
            if (string.IsNullOrEmpty(source)) return;

            string[] files;
            try
            {
                files = Directory.GetFiles(source, "*", SearchOption.AllDirectories)
                    .Where(path => AllowedExtensions.Contains(Path.GetExtension(path)))
                    .ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Showcase Asset Import", "Could not read the chosen folder. See Console.", "OK");
                return;
            }

            if (files.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Showcase Asset Import",
                    "No supported FBX/OBJ/DAE/GLB/GLTF model or texture files were found. Unity cannot import SDAT or IFF archives directly; export the contained model/texture files first.",
                    "OK");
                return;
            }

            EnsureFolder(Root);
            EnsureFolder(PrivateRoot);
            EnsurePrivateIgnoreFile();

            string name = Sanitize(Path.GetFileName(source));
            if (string.IsNullOrWhiteSpace(name)) name = "ImportedAssets";
            string destinationAsset = PrivateRoot + "/" + name;
            EnsureFolder(destinationAsset);
            string sourceRoot = source.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string destinationRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "NBA2K12Showcase/PrivateAssets/" + name));
            int copied = 0, models = 0, textures = 0;

            foreach (string file in files)
            {
                string relative = file.Substring(sourceRoot.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string destination = Path.Combine(destinationRoot, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(file, destination, true);
                copied++;
                string ext = Path.GetExtension(file);
                if (IsModel(ext)) models++;
                if (IsTexture(ext)) textures++;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.SaveAssets();

            int importedModelAssets = 0;
            var unsupportedModelFiles = new List<string>();
            foreach (string file in files.Where(path => IsModel(Path.GetExtension(path))))
            {
                string relative = file.Substring(sourceRoot.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Replace('\\', '/');
                string assetPath = PrivateRoot + "/" + name + "/" + relative;
                if (AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) != null)
                {
                    importedModelAssets++;
                }
                else if (Path.GetExtension(file).Equals(".glb", StringComparison.OrdinalIgnoreCase) ||
                         Path.GetExtension(file).Equals(".gltf", StringComparison.OrdinalIgnoreCase))
                {
                    unsupportedModelFiles.Add(Path.GetFileName(file));
                }
            }

            string importerWarning = unsupportedModelFiles.Count > 0
                ? "\n\nGLB/GLTF files copied but not imported as Unity models: " +
                  string.Join(", ", unsupportedModelFiles.Take(8)) +
                  (unsupportedModelFiles.Count > 8 ? " (and " + (unsupportedModelFiles.Count - 8) + " more)" : "") +
                  ". Install a Unity glTF importer such as glTFast, or convert these files to FBX, then reimport."
                : string.Empty;
            string message = "Copied " + copied + " files to local-only folder:\n" + destinationAsset +
                "\n\nModel files found: " + models + "\nUnity model assets loaded: " + importedModelAssets +
                "\nTextures: " + textures + importerWarning +
                "\n\nFiles under PrivateAssets are excluded from Git. Importing files does not yet replace the active match player's rig.";
            Debug.Log("[NBA2K12 Showcase] " + message);
            EditorUtility.DisplayDialog("Showcase Asset Import", message, "OK");
        }

        [MenuItem("Tools/NBA2K12 Showcase/6 - Preview Selected Imported Model in Separate Scene")]
        public static void PreviewSelectedModel()
        {
            GameObject selected = Selection.activeObject as GameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("Showcase Asset Preview", "Select an imported model asset in the Project window first.", "OK");
                return;
            }

            string path = AssetDatabase.GetAssetPath(selected);
            if (string.IsNullOrWhiteSpace(path) ||
                !path.StartsWith(PrivateRoot + "/", StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog(
                    "Showcase Asset Preview",
                    "The selected model must be inside Assets/NBA2K12Showcase/PrivateAssets.",
                    "OK");
                return;
            }

            EnsureFolder(PreviewRoot);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            scene.name = "Showcase Model Preview";

            Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera != null)
            {
                camera.transform.position = new Vector3(0f, 1.35f, -4.8f);
                camera.transform.rotation = Quaternion.LookRotation(
                    new Vector3(0f, 0.95f, 0f) - camera.transform.position, Vector3.up);
                camera.fieldOfView = 42f;
                camera.nearClipPlane = 0.02f;
                camera.farClipPlane = 100f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.035f, 0.05f, 0.07f, 1f);
            }

            Light light = UnityEngine.Object.FindFirstObjectByType<Light>();
            if (light != null)
            {
                light.type = LightType.Directional;
                light.intensity = 1.15f;
                light.shadows = LightShadows.Soft;
                light.transform.rotation = Quaternion.Euler(32f, -28f, 0f);
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(selected, scene) as GameObject;
            if (instance == null)
            {
                instance = UnityEngine.Object.Instantiate(selected);
                SceneManager.MoveGameObjectToScene(instance, scene);
            }
            instance.name = "Preview - " + selected.name;
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                if (bounds.size.y > 0.001f)
                {
                    float scale = 1.9f / bounds.size.y;
                    instance.transform.localScale *= scale;
                    instance.transform.position = new Vector3(0f, -bounds.min.y * scale, 0f);
                }
            }

            Selection.activeGameObject = instance;
            string savePath = AssetDatabase.GenerateUniqueAssetPath(
                PreviewRoot + "/" + Sanitize(selected.name) + "_Preview.unity");
            EditorSceneManager.SaveScene(scene, savePath);
            Debug.Log("[NBA2K12 Showcase] Saved model preview: " + savePath);
            EditorUtility.DisplayDialog(
                "Showcase Asset Preview",
                "Saved an isolated model preview to:\n" + savePath +
                "\n\nThis is a visual inspection scene only; the model has not yet been wired into 5-on-5 animation.",
                "OK");
        }

        private static bool IsModel(string ext) =>
            ext.Equals(".fbx", StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".obj", StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".dae", StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".glb", StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".gltf", StringComparison.OrdinalIgnoreCase);

        private static bool IsTexture(string ext) =>
            ext.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".tga", StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".bmp", StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".tif", StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".tiff", StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".psd", StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".dds", StringComparison.OrdinalIgnoreCase);

        private static string Sanitize(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
            return value.Trim().Replace(' ', '_');
        }

        private static void EnsurePrivateIgnoreFile()
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "NBA2K12Showcase/PrivateAssets/.gitignore"));
            const string contents =
                "# Local proprietary/third-party imports: never commit these files.\n*\n!.gitignore\n";
            if (!File.Exists(path) || File.ReadAllText(path) != contents)
                File.WriteAllText(path, contents);
        }

        private static void EnsureFolder(string assetPath)
        {
            string[] parts = assetPath.Split('/');
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
