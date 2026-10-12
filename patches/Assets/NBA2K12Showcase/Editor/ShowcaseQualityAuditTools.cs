#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using CrowdEyes.AI4Animation.Basketball;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace NBA2K12Showcase.Editor
{
    /// <summary>
    /// Reports observable prerequisites for the quality target. This audit intentionally
    /// never claims AAA quality from file presence or a successful compilation.
    /// </summary>
    public static class ShowcaseQualityAuditTools
    {
        private const string ReportRoot = "Assets/NBA2K12Showcase/Reports";

        [MenuItem("Tools/NBA2K12 Showcase/14 - Run Full Game Readiness Audit")]
        public static void RunAudit()
        {
            Scene scene = SceneManager.GetActiveScene();
            BasketballTeamMember[] players =
                UnityEngine.Object.FindObjectsByType<BasketballTeamMember>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(p => p != null && p.gameObject.scene == scene)
                .OrderBy(p => p.PlayerIndex)
                .ToArray();

            BasketballBallController ball = UnityEngine.Object.FindObjectsByType<BasketballBallController>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(v => v != null && v.gameObject.scene == scene);
            BasketballCourt court = UnityEngine.Object.FindObjectsByType<BasketballCourt>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(v => v != null && v.gameObject.scene == scene);
            BasketballMatchController match = UnityEngine.Object.FindObjectsByType<BasketballMatchController>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(v => v != null && v.gameObject.scene == scene);
            BasketballScoreTracker scores = UnityEngine.Object.FindObjectsByType<BasketballScoreTracker>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(v => v != null && v.gameObject.scene == scene);
            ShowcaseBroadcastHud hud = UnityEngine.Object.FindObjectsByType<ShowcaseBroadcastHud>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(v => v != null && v.gameObject.scene == scene);

            int homeCount = players.Count(p => p.TeamId == 0);
            int awayCount = players.Count(p => p.TeamId == 1);
            int identities = 0;
            int appearances = 0;
            int retargeters = 0;
            int validHumanoids = 0;
            int weightedSkinnedMeshes = 0;
            int renderers = 0;
            int missingMaterials = 0;
            int missingTextures = 0;

            foreach (BasketballTeamMember player in players)
            {
                if (player.GetComponent<ShowcasePlayerIdentity>() != null)
                    identities++;

                BasketballPlayerAppearance[] playerAppearances =
                    player.GetComponentsInChildren<BasketballPlayerAppearance>(true);
                appearances += playerAppearances.Length;

                BasketballHumanoidVisualRetargeter[] playerRetargeters =
                    player.GetComponentsInChildren<BasketballHumanoidVisualRetargeter>(true);
                retargeters += playerRetargeters.Length;
                foreach (BasketballHumanoidVisualRetargeter retargeter in playerRetargeters)
                {
                    Animator animator = retargeter.HumanoidAnimator;
                    if (animator != null && animator.avatar != null && animator.avatar.isHuman)
                        validHumanoids++;
                }

                SkinnedMeshRenderer[] skinned = player.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (SkinnedMeshRenderer mesh in skinned)
                {
                    if (mesh != null && mesh.sharedMesh != null &&
                        mesh.bones != null && mesh.bones.Length > 0 && mesh.rootBone != null)
                        weightedSkinnedMeshes++;
                }

                Renderer[] playerRenderers = player.GetComponentsInChildren<Renderer>(true);
                renderers += playerRenderers.Length;
                foreach (Renderer renderer in playerRenderers)
                {
                    if (renderer == null) continue;
                    Material[] materials = renderer.sharedMaterials;
                    if (materials == null || materials.Length == 0)
                    {
                        missingMaterials++;
                        continue;
                    }
                    foreach (Material material in materials)
                    {
                        if (material == null)
                        {
                            missingMaterials++;
                            continue;
                        }
                        if (material.mainTexture == null)
                            missingTextures++;
                    }
                }
            }

            Camera[] cameras = UnityEngine.Object.FindObjectsByType<Camera>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(c => c != null && c.gameObject.scene == scene && c.enabled)
                .ToArray();
            Camera mainCamera = cameras.FirstOrDefault(c => c.CompareTag("MainCamera")) ?? cameras.FirstOrDefault();
            UniversalAdditionalCameraData cameraData = mainCamera != null
                ? mainCamera.GetComponent<UniversalAdditionalCameraData>()
                : null;
            Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(l => l != null && l.gameObject.scene == scene && l.enabled)
                .ToArray();
            int directionalLights = lights.Count(l => l.type == LightType.Directional);
            int skinnedCount = UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Count(r => r != null && r.gameObject.scene == scene);

            var blockers = new System.Collections.Generic.List<string>();
            if (players.Length != 10 || homeCount != 5 || awayCount != 5)
                blockers.Add("FAIL: Expected 10 players (5 per team); found " + players.Length + " total, " + homeCount + " home, " + awayCount + " away.");
            if (identities != 10)
                blockers.Add("WARN: Only " + identities + "/10 players have ShowcasePlayerIdentity metadata.");
            if (retargeters < 10)
                blockers.Add("FAIL: Found " + retargeters + " visual retargeters for 10 players.");
            if (validHumanoids < 10)
                blockers.Add("FAIL: Only " + validHumanoids + " retargeters reference a valid Humanoid Avatar.");
            if (weightedSkinnedMeshes < 10)
                blockers.Add("FAIL: Only " + weightedSkinnedMeshes + " weighted SkinnedMeshRenderers found under player roots. A modern high-detail model may have multiple renderers per player.");
            if (ball == null || ball.GetComponent<Rigidbody>() == null || ball.GetComponent<SphereCollider>() == null)
                blockers.Add("FAIL: Ball physics root/controller or required Rigidbody/SphereCollider is missing.");
            if (court == null || court.PositiveZHoop == null || court.NegativeZHoop == null)
                blockers.Add("FAIL: Court or one of its hoops is missing.");
            if (match == null || scores == null)
                blockers.Add("FAIL: Match controller or score tracker is missing.");
            if (hud == null)
                blockers.Add("WARN: Live broadcast HUD is not installed on the active scene.");
            if (mainCamera == null)
                blockers.Add("FAIL: No enabled gameplay camera found.");
            else if (cameraData == null || !cameraData.renderPostProcessing)
                blockers.Add("WARN: Main camera is missing URP additional camera data or post-processing is off.");
            if (directionalLights == 0)
                blockers.Add("WARN: No enabled directional light found in the scene.");
            if (missingMaterials > 0)
                blockers.Add("WARN: " + missingMaterials + " renderer material slot(s) are missing/null.");
            if (missingTextures > 0)
                blockers.Add("INFO: " + missingTextures + " materials have no main texture; some may intentionally use flat/PBR colors.");
            blockers.Add("BLOCKER: Visual fidelity cannot be graded from code alone. Inspect faces, uniforms, mesh fit, animation and court presentation in Play Mode.");
            blockers.Add("BLOCKER: No authentic NBA 2K12 player/face/uniform assets are bundled in the public repository; the PS3 SDAT-to-skinned-model export path remains unverified.");

            StringBuilder report = new StringBuilder();
            report.AppendLine("NBA2K12 Showcase — Full Game Readiness Audit");
            report.AppendLine("Date UTC: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            report.AppendLine("Active scene: " + (string.IsNullOrEmpty(scene.path) ? scene.name : scene.path));
            report.AppendLine("Unity version: " + Application.unityVersion);
            report.AppendLine("Play Mode: " + EditorApplication.isPlaying);
            report.AppendLine();
            report.AppendLine("STRUCTURE");
            report.AppendLine("Players: " + players.Length + " (Team 0: " + homeCount + ", Team 1: " + awayCount + ")");
            report.AppendLine("Roster identities: " + identities + "/10");
            report.AppendLine("Player appearances: " + appearances);
            report.AppendLine("Retargeters: " + retargeters + "; valid Humanoid avatars: " + validHumanoids);
            report.AppendLine("Weighted skinned meshes under players: " + weightedSkinnedMeshes);
            report.AppendLine("Player renderers: " + renderers + "; missing material slots: " + missingMaterials + "; textureless materials: " + missingTextures);
            report.AppendLine("Global skinned meshes in scene: " + skinnedCount);
            report.AppendLine("Ball controller: " + (ball != null ? ball.name : "MISSING"));
            report.AppendLine("Court + two hoops: " + (court != null && court.PositiveZHoop != null && court.NegativeZHoop != null));
            report.AppendLine("Match + score tracker: " + (match != null && scores != null));
            report.AppendLine("Broadcast HUD: " + (hud != null));
            report.AppendLine("Enabled cameras: " + cameras.Length + "; directional lights: " + directionalLights);
            report.AppendLine();
            report.AppendLine("BLOCKERS / WARNINGS");
            foreach (string line in blockers)
                report.AppendLine("- " + line);
            report.AppendLine();
            report.AppendLine("STATUS: IN DEVELOPMENT — this audit reports structure, not AAA visual quality or a verified Play Mode test.");

            string text = report.ToString();
            Debug.Log(text);

            if (!AssetDatabase.IsValidFolder("Assets/NBA2K12Showcase"))
                AssetDatabase.CreateFolder("Assets", "NBA2K12Showcase");
            if (!AssetDatabase.IsValidFolder(ReportRoot))
                AssetDatabase.CreateFolder("Assets/NBA2K12Showcase", "Reports");
            string outputPath = ReportRoot + "/ReadinessAudit_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
            File.WriteAllText(outputPath, text, new UTF8Encoding(false));
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "NBA2K12 Showcase — Readiness Audit",
                "Wrote report to:\n" + outputPath + "\n\n" +
                "Players: " + players.Length + "/10. Humanoid avatars: " + validHumanoids +
                ". Weighted skinned renderers: " + weightedSkinnedMeshes +
                ".\n\nThis is a structural check only; Play Mode visual checks are still required.",
                "OK");
        }
    }
}
#endif
