#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace NBA2K12Showcase.Editor
{
    /// <summary>
    /// Downloads only assets whose current upstream manifest explicitly identifies them as CC0 1.0.
    /// The site models are static geometry: this tool does not invent collisions, gameplay, or animation.
    /// </summary>
    public static class ShowcaseCC0AssetLibraryTools
    {
        private const string Root = "Assets/NBA2K12Showcase/ExternalCC0";
        private const string BasketballManifestUrl =
            "https://3dassets.dev/api/v1/packs/basketball-club-and-street-courts";
        private const string SportsHallManifestUrl =
            "https://3dassets.dev/api/v1/packs/community-sports-hall-and-changing-rooms";
        private const string CdnPrefix = "https://cdn.3dassets.dev/assets/";
        private const string LicenseUrl = "https://creativecommons.org/publicdomain/zero/1.0/";

        [Serializable] private sealed class ManifestRoot { public ManifestData data; }
        [Serializable] private sealed class ManifestData
        {
            public string title;
            public License license;
            public AssetRecord starterScene;
            public AssetRecord[] assets;
        }
        [Serializable] private sealed class AssetRecord
        {
            public string id;
            public string title;
            public string slug;
            public string cdnUrl;
            public string status;
            public License license;
        }
        [Serializable] private sealed class License
        {
            public string slug;
            public string name;
            public string url;
            public bool attributionRequired;
        }

        [MenuItem("Tools/NBA2K12 Showcase/10 - Download CC0 Basketball Props Library")]
        public static async void DownloadBasketballPack()
        {
            await DownloadPackAsync(
                BasketballManifestUrl,
                "BasketballClubAndStreetCourts",
                includeAllAssets: true);
        }

        [MenuItem("Tools/NBA2K12 Showcase/11 - Download CC0 Indoor Sports Hall")]
        public static async void DownloadSportsHall()
        {
            await DownloadPackAsync(
                SportsHallManifestUrl,
                "CommunitySportsHall",
                includeAllAssets: false);
        }

        private static async Task DownloadPackAsync(
            string manifestUrl,
            string folderName,
            bool includeAllAssets)
        {
            string targetAssetPath = Root + "/" + folderName;
            string targetDiskPath = Path.Combine(Application.dataPath, "NBA2K12Showcase/ExternalCC0/" + folderName);
            try
            {
                EditorUtility.DisplayProgressBar("NBA2K12 Showcase", "Reading the asset manifest...", 0.03f);
                string json = await DownloadTextAsync(manifestUrl);
                ManifestRoot parsed = JsonUtility.FromJson<ManifestRoot>(json);
                if (parsed == null || parsed.data == null)
                    throw new InvalidOperationException("The asset provider returned an unrecognized manifest.");

                EnsureCC0(parsed.data.license, "pack");
                Directory.CreateDirectory(targetDiskPath);
                File.WriteAllText(Path.Combine(targetDiskPath, "source-manifest.json"), json, new UTF8Encoding(false));
                File.WriteAllText(
                    Path.Combine(targetDiskPath, "LICENSE_CC0.txt"),
                    "CC0 1.0 Universal\r\n" +
                    "Source pack: " + parsed.data.title + "\r\n" +
                    "Source manifest: " + manifestUrl + "\r\n" +
                    "License: " + LicenseUrl + "\r\n\r\n" +
                    "These are static third-party geometry assets. This pack does not contain the user's NBA 2K12 files, NBA player likenesses, or official NBA branding.\r\n",
                    new UTF8Encoding(false));

                if (includeAllAssets)
                {
                    AssetRecord[] records = parsed.data.assets ?? Array.Empty<AssetRecord>();
                    if (records.Length == 0)
                        throw new InvalidOperationException("The CC0 pack manifest contained no model records.");

                    for (int index = 0; index < records.Length; index++)
                    {
                        AssetRecord asset = records[index];
                        EnsureCC0(asset.license, asset.title);
                        ValidateCdnUrl(asset.cdnUrl);
                        string fileName = Sanitize(asset.id + "_" + asset.title) + ".glb";
                        string output = Path.Combine(targetDiskPath, fileName);
                        float progress = 0.08f + 0.84f * (float)index / records.Length;
                        EditorUtility.DisplayProgressBar(
                            "NBA2K12 Showcase — downloading CC0 geometry",
                            (index + 1) + "/" + records.Length + "  " + asset.title,
                            progress);
                        if (!File.Exists(output))
                        {
                            byte[] bytes = await DownloadBytesAsync(asset.cdnUrl);
                            File.WriteAllBytes(output, bytes);
                        }
                    }
                }
                else
                {
                    AssetRecord starter = parsed.data.starterScene;
                    if (starter == null)
                        throw new InvalidOperationException("No starterScene record was provided for this pack.");
                    EnsureCC0(starter.license ?? parsed.data.license, starter.title);
                    ValidateCdnUrl(starter.cdnUrl);
                    string output = Path.Combine(targetDiskPath, Sanitize(starter.id + "_" + starter.title) + ".glb");
                    if (!File.Exists(output))
                    {
                        EditorUtility.DisplayProgressBar(
                            "NBA2K12 Showcase — downloading indoor hall",
                            "Downloading the CC0 community sports-centre starter scene...",
                            0.7f);
                        byte[] bytes = await DownloadBytesAsync(starter.cdnUrl);
                        File.WriteAllBytes(output, bytes);
                    }
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.SaveAssets();
                string done = includeAllAssets
                    ? "Downloaded/imported " + (parsed.data.assets != null ? parsed.data.assets.Length : 0) +
                      " CC0 basketball models into:\n" + targetAssetPath
                    : "Downloaded/imported the CC0 community indoor sports-hall starter model into:\n" + targetAssetPath;
                done += "\n\nUnity must finish importing the GLB models. These are static setting/prop assets, not animated NBA players or official NBA 2K12 uniforms.";
                Debug.Log("[NBA2K12 Showcase] " + done);
                EditorUtility.DisplayDialog("NBA2K12 Showcase — CC0 Assets", done, "OK");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog(
                    "NBA2K12 Showcase — CC0 Assets",
                    "Asset download did not complete. Nothing has been reported as successful.\n\n" + ex.Message,
                    "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Tools/NBA2K12 Showcase/12 - Install CC0 Basketball Mesh on Live Ball")]
        public static void InstallBasketballMesh()
        {
            string root = Root + "/BasketballClubAndStreetCourts";
            string[] files = Directory.Exists(Path.Combine(Application.dataPath, "NBA2K12Showcase/ExternalCC0/BasketballClubAndStreetCourts"))
                ? Directory.GetFiles(Path.Combine(Application.dataPath, "NBA2K12Showcase/ExternalCC0/BasketballClubAndStreetCourts"), "28548_*.glb", SearchOption.TopDirectoryOnly)
                : Array.Empty<string>();

            if (files.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "NBA2K12 Showcase — Ball Mesh",
                    "The CC0 basketball model was not found. Run menu item 10 first and let Unity finish importing its GLB files.",
                    "OK");
                return;
            }

            string assetPath = root + "/" + Path.GetFileName(files[0]);
            GameObject visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (visualPrefab == null)
            {
                EditorUtility.DisplayDialog(
                    "NBA2K12 Showcase — Ball Mesh",
                    "Unity has not exposed the imported GLB as a GameObject prefab yet. Confirm com.unity.cloud.gltfast is installed and check the asset's Inspector/import errors.",
                    "OK");
                return;
            }

            BasketballBallController ball = UnityEngine.Object.FindFirstObjectByType<CrowdEyes.AI4Animation.Basketball.BasketballBallController>();
            if (ball == null)
            {
                EditorUtility.DisplayDialog("NBA2K12 Showcase — Ball Mesh", "No BasketballBallController was found in the active scene.", "OK");
                return;
            }

            const string childName = "Showcase_CC0_BasketballVisual";
            Transform existingImported = ball.transform.Find(childName);
            if (existingImported != null)
                UnityEngine.Object.DestroyImmediate(existingImported.gameObject);

            Renderer[] oldRenderers = ball.GetComponentsInChildren<Renderer>(true);
            GameObject instance = PrefabUtility.InstantiatePrefab(visualPrefab, ball.gameObject.scene) as GameObject;
            if (instance == null)
                throw new InvalidOperationException("Unity could not instantiate the imported CC0 basketball model.");

            Undo.RegisterCreatedObjectUndo(instance, "Install CC0 Basketball Visual");
            instance.name = childName;
            instance.transform.SetParent(ball.transform, false);
            instance.transform.localPosition = new Vector3(0f, -ball.Radius, 0f);
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            foreach (Renderer renderer in oldRenderers)
            {
                if (renderer == null || renderer.transform == instance.transform ||
                    renderer.transform.IsChildOf(instance.transform))
                    continue;
                Undo.RecordObject(renderer, "Hide Previous Ball Visual");
                renderer.enabled = false;
                EditorUtility.SetDirty(renderer);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            string message = "Installed the CC0 240 mm basketball model as a child of the existing physics ball. The original Rigidbody, SphereCollider, and ball authority remain on the game object.\n\nCheck scale, rotation and contact in Play Mode.";
            Debug.Log("[NBA2K12 Showcase] " + message);
            EditorUtility.DisplayDialog("NBA2K12 Showcase — Ball Mesh", message, "OK");
        }

        [MenuItem("Tools/NBA2K12 Showcase/13 - Create Separate CC0 Sports Hall Preview")]
        public static void CreateSportsHallPreview()
        {
            string diskRoot = Path.Combine(Application.dataPath, "NBA2K12Showcase/ExternalCC0/CommunitySportsHall");
            string[] files = Directory.Exists(diskRoot)
                ? Directory.GetFiles(diskRoot, "*.glb", SearchOption.TopDirectoryOnly)
                : Array.Empty<string>();
            if (files.Length == 0)
            {
                EditorUtility.DisplayDialog("Showcase Sports Hall Preview", "Download the indoor hall using menu item 11 first.", "OK");
                return;
            }

            string assetRoot = Root + "/CommunitySportsHall";
            string assetPath = assetRoot + "/" + Path.GetFileName(files[0]);
            GameObject hallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (hallPrefab == null)
            {
                EditorUtility.DisplayDialog("Showcase Sports Hall Preview", "Unity has not imported the GLB as a GameObject yet. Check glTFast package resolution.", "OK");
                return;
            }

            EnsureAssetFolder(Root + "/Previews");
            Scene preview = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);
            preview.name = "CC0 Sports Hall Asset Preview";
            GameObject hall = PrefabUtility.InstantiatePrefab(hallPrefab, preview) as GameObject;
            if (hall == null)
                throw new InvalidOperationException("Could not instantiate the CC0 sports hall preview.");
            hall.name = "CC0 Community Sports Centre — Visual Reference";
            hall.transform.position = Vector3.zero;

            Camera camera = preview.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<Camera>(true))
                .FirstOrDefault(c => c != null);
            if (camera != null)
            {
                camera.transform.position = new Vector3(0f, 8f, -23f);
                camera.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 1.9f, 0f) - camera.transform.position, Vector3.up);
                camera.fieldOfView = 45f;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 150f;
            }

            SceneManager.SetActiveScene(preview);
            string savePath = AssetDatabase.GenerateUniqueAssetPath(Root + "/Previews/CC0_SportsHall_Reference.unity");
            EditorSceneManager.SaveScene(preview, savePath);
            EditorSceneManager.OpenScene(savePath, OpenSceneMode.Single);
            string message = "Created a separate preview scene: " + savePath + "\n\nThis is a CC0 municipal indoor sports centre. It is useful as a practice-gym setting reference, but it is not a pro NBA arena and has no basketball gameplay, colliders, or animated NBA players.";
            Debug.Log("[NBA2K12 Showcase] " + message);
            EditorUtility.DisplayDialog("Showcase Sports Hall Preview", message, "OK");
        }

        private static async Task<string> DownloadTextAsync(string url)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 45;
                await request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                    throw new InvalidOperationException("Request failed for " + url + ": " + request.error);
                return request.downloadHandler.text;
            }
        }

        private static async Task<byte[]> DownloadBytesAsync(string url)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 90;
                await request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                    throw new InvalidOperationException("Download failed for " + url + ": " + request.error);
                byte[] data = request.downloadHandler.data;
                if (data == null || data.Length < 12)
                    throw new InvalidOperationException("The file downloaded from " + url + " was empty or too small to be a model.");
                return data;
            }
        }

        private static void EnsureCC0(License license, string name)
        {
            if (license == null || !string.Equals(license.slug, "cc0-1.0", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The provider did not identify '" + name + "' as CC0 1.0. It was not downloaded.");
        }

        private static void ValidateCdnUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url) ||
                !url.StartsWith(CdnPrefix, StringComparison.OrdinalIgnoreCase) ||
                !url.EndsWith(".glb", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Rejected asset URL outside the expected CDN: " + url);
        }

        private static string Sanitize(string value)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');
            value = value.Replace('/', '_').Replace('\\', '_').Replace(':', '_');
            while (value.Contains("__")) value = value.Replace("__", "_");
            return value.Trim(' ', '.', '_');
        }

        private static void EnsureAssetFolder(string assetPath)
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
