using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Dread.Systems.Core
{
    /// <summary>
    /// Client-local hallucination mob: baked mesh snapshot from a live enemy, or tinted fallback.
    /// Never instantiates enemy prefabs (no Photon / Enemy.Awake).
    /// </summary>
    internal static class PsychoticBreakHallucinationPresenter
    {
        private const string SkinnedMeshRendererTypeName = "SkinnedMeshRenderer";
        private const int MinTotalVertices = 80;
        internal const float MaxTemplatePickDistanceMeters = 28f;
        internal const float MaxTemplatePickDistanceRelaxedMeters = 65f;
        private const int DefaultRankedCandidateCount = 8;

        public struct BuildResult
        {
            public GameObject? Root;
            public string Mode;
            public string TemplateObjectName;
            public string TemplateRootName;
            public float TemplateDistanceMeters;
            public int PartCount;
            public int TotalVertices;
            public int SkinnedPartCount;
            public int StaticPartCount;
            public int BakeMeshFailures;
            public int BindPoseFallbackParts;
        }

        public static BuildResult Build(EnemyHealth? template, Vector3 position, Quaternion rotation, Vector3 playerPosition)
        {
            var result = new BuildResult
            {
                Mode = "none",
                TemplateObjectName = template != null ? template.gameObject.name : "(none)",
                TemplateRootName = template != null ? template.transform.root.name : "(none)",
            };

            if (template != null && EnemyHealthCompat.IsValid(template))
            {
                try
                {
                    result.TemplateDistanceMeters = Vector3.Distance(position, template.transform.position);
                }
                catch
                {
                    result.TemplateDistanceMeters = -1f;
                }
            }

            try
            {
                var root = new GameObject("DreadHallucination");
                root.transform.position = position;
                var rootRotation = rotation;
                if (template != null && EnemyHealthCompat.IsValid(template))
                {
                    try
                    {
                        rootRotation = GetTemplateVisualRoot(template).rotation;
                    }
                    catch { }
                }

                root.transform.rotation = rootRotation;
                root.AddComponent<DreadHallucinationMob>();
                result.Root = root;

                if (template != null && EnemyHealthCompat.IsValid(template))
                {
                    AddBakedSnapshotParts(root, template, ref result);
                }

                if (result.PartCount == 0 || result.TotalVertices < MinTotalVertices)
                {
                    ClearSnapshotChildren(root);
                    ApplyFallbackSilhouette(root, template);
                    result.Mode = "fallback";
                    result.PartCount = 0;
                    result.TotalVertices = 0;
                }
                else
                {
                    result.Mode = "snapshot";
                    AlignSnapshotFeetToFloor(root, position.y);
                    LoggingService.LogInfo(
                        "[PsychoticBreak] Hallucination world pos="
                        + root.transform.position
                        + " (spawn in front of player)");
                }

                return result;
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning($"[PsychoticBreak] Hallucination build failed: {ex.Message}");
                result.Mode = "error";
                return result;
            }
        }

        public static void LogBuildResult(in BuildResult result)
        {
            var sb = new StringBuilder();
            sb.Append("[PsychoticBreak] Hallucination spawn: mode=").Append(result.Mode);
            sb.Append(" template=").Append(result.TemplateObjectName);
            sb.Append(" root=").Append(result.TemplateRootName);
            sb.Append(" dist=").Append(result.TemplateDistanceMeters.ToString("F1")).Append("m");
            sb.Append(" parts=").Append(result.PartCount);
            sb.Append(" verts=").Append(result.TotalVertices);
            sb.Append(" skinned=").Append(result.SkinnedPartCount);
            sb.Append(" static=").Append(result.StaticPartCount);
            if (result.BakeMeshFailures > 0)
                sb.Append(" bakeFails=").Append(result.BakeMeshFailures);
            if (result.BindPoseFallbackParts > 0)
                sb.Append(" bindPose=").Append(result.BindPoseFallbackParts);

            if (result.Mode == "fallback")
            {
                LoggingService.LogWarning(sb.ToString()
                    + " (mesh bake failed or too few vertices; used primitive silhouette)");
            }
            else
            {
                LoggingService.LogInfo(sb.ToString());
            }
        }

        public static void DestroyBuilt(GameObject? root)
        {
            if (root == null)
                return;

            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            if (filters != null)
            {
                for (int i = 0; i < filters.Length; i++)
                {
                    var mf = filters[i];
                    if (mf == null)
                        continue;
                    var mesh = mf.mesh;
                    mf.mesh = null;
                    if (mesh != null && mesh.name != "UnityPrimitive")
                    {
                        try
                        {
                            UnityEngine.Object.Destroy(mesh);
                        }
                        catch { }
                    }
                }
            }

            try
            {
                UnityEngine.Object.Destroy(root);
            }
            catch { }
        }

        public static void PlayAttackSoundNear(EnemyHealth? template, Vector3 position)
        {
            AudioClip? clip = null;
            if (template != null && EnemyHealthCompat.IsValid(template))
                clip = FindAttackClipOnTemplate(template);

            if (clip == null)
                return;

            try
            {
                AudioSource.PlayClipAtPoint(clip, position, 0.85f);
            }
            catch { }
        }

        public static bool IsSuccessfulSnapshot(in BuildResult build) =>
            build.Root != null && build.Mode == "snapshot" && build.TotalVertices >= MinTotalVertices;

        /// <summary>Try ranked candidates until a mesh snapshot succeeds; else tinted fallback.</summary>
        public static (EnemyHealth? template, BuildResult build) BuildBest(
            IList<EnemyHealth> candidates,
            Vector3 position,
            Quaternion rotation,
            Vector3 playerPosition)
        {
            int tried = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                var t = candidates[i];
                tried++;
                var build = Build(t, position, rotation, playerPosition);
                if (IsSuccessfulSnapshot(build))
                {
                    if (i > 0)
                    {
                        LoggingService.LogInfo(
                            "[PsychoticBreak] Hallucination template: used candidate #"
                            + (i + 1)
                            + " "
                            + build.TemplateObjectName
                            + " after "
                            + tried
                            + " tries");
                    }

                    return (t, build);
                }

                if (build.Root != null)
                    DestroyBuilt(build.Root);
            }

            var fallbackTemplate = candidates.Count > 0 ? candidates[0] : null;
            var fallback = Build(fallbackTemplate, position, rotation, playerPosition);
            LoggingService.LogWarning(
                "[PsychoticBreak] Hallucination: no mesh snapshot after "
                + tried
                + " candidate(s); mode="
                + fallback.Mode);
            return (fallbackTemplate, fallback);
        }

        public static List<EnemyHealth> RankTemplates(
            EnemyHealth[]? enemies,
            Vector3 origin,
            int maxCount,
            float maxDistanceMeters = MaxTemplatePickDistanceMeters)
        {
            var ranked = new List<(EnemyHealth enemy, float score)>();
            if (enemies == null || enemies.Length == 0)
                return new List<EnemyHealth>();

            for (int i = 0; i < enemies.Length; i++)
            {
                var e = enemies[i];
                if (!EnemyHealthCompat.IsValid(e))
                    continue;
                if (DreadHallucinationMob.IsHallucination(e))
                    continue;
                if (IsRejectedTemplate(e))
                    continue;

                float score = ScoreTemplate(e, origin, maxDistanceMeters);
                if (score < 0f)
                    continue;

                ranked.Add((e, score));
            }

            ranked.Sort((a, b) => b.score.CompareTo(a.score));

            int take = maxCount > 0 ? maxCount : DefaultRankedCandidateCount;
            if (take > ranked.Count)
                take = ranked.Count;

            var result = new List<EnemyHealth>(take);
            for (int i = 0; i < take; i++)
                result.Add(ranked[i].enemy);

            return result;
        }

        public static void LogCandidateDiagnostics(EnemyHealth[]? enemies, Vector3 origin, float maxDistanceMeters)
        {
            if (enemies == null || enemies.Length == 0)
            {
                LoggingService.LogWarning("[PsychoticBreak] Hallucination: no EnemyHealth in scan");
                return;
            }

            var sb = new StringBuilder();
            sb.Append("[PsychoticBreak] Hallucination candidates (max ").Append(maxDistanceMeters.ToString("F0")).Append("m): ");
            for (int i = 0; i < enemies.Length; i++)
            {
                var e = enemies[i];
                if (!EnemyHealthCompat.IsValid(e))
                {
                    sb.Append("[invalid] ");
                    continue;
                }

                string name;
                float dist;
                try
                {
                    name = e.gameObject.name;
                    dist = Vector3.Distance(origin, ProximityScan.GetFocusPosition(e));
                }
                catch
                {
                    sb.Append("[err] ");
                    continue;
                }

                string reason = "ok";
                string visualName = "?";
                if (DreadHallucinationMob.IsHallucination(e))
                    reason = "hallucination";
                else if (IsRejectedTemplate(e))
                    reason = "rejected-name";
                else if (dist > maxDistanceMeters)
                    reason = "too-far";
                else
                {
                    try
                    {
                        visualName = GetTemplateVisualRoot(e).name;
                    }
                    catch
                    {
                        visualName = "?";
                    }

                    if (CountSnapshotSources(e) == 0)
                        reason = "no-mesh";
                    else if (EstimateVertexCount(e) < MinTotalVertices)
                        reason = "low-verts";
                }

                sb.Append(name).Append('@').Append(dist.ToString("F1")).Append("m vis=").Append(visualName)
                    .Append('(').Append(reason).Append(") ");
            }

            LoggingService.LogWarning(sb.ToString());
        }

        /// <summary>Pick enemy with the richest skinned mesh (best hallucination source).</summary>
        public static EnemyHealth? PickBestTemplate(EnemyHealth[]? enemies, Vector3 origin)
        {
            var ranked = RankTemplates(enemies, origin, 1);
            return ranked.Count > 0 ? ranked[0] : null;
        }

        public static void SetMobVisible(GameObject? root, bool visible)
        {
            if (root == null)
                return;

            try
            {
                var renderers = root.GetComponentsInChildren<Renderer>(true);
                if (renderers == null)
                    return;

                for (int i = 0; i < renderers.Length; i++)
                {
                    var r = renderers[i];
                    if (r != null)
                        r.enabled = visible;
                }
            }
            catch { }
        }

        private static bool IsSkinnedMeshRendererComponent(Component comp)
        {
            if (comp == null)
                return false;

            var name = comp.GetType().Name;
            return name == SkinnedMeshRendererTypeName || name.Contains("SkinnedMesh");
        }

        /// <summary>EnemyHealth often sits on a logic node; mesh lives on parents/siblings.</summary>
        private static Transform GetTemplateVisualRoot(EnemyHealth enemy)
        {
            if (!EnemyHealthCompat.IsValid(enemy))
            {
                try
                {
                    return enemy.transform;
                }
                catch
                {
                    return null!;
                }
            }

            try
            {
                var walk = enemy.transform;
                while (walk != null)
                {
                    if (CountSnapshotSourcesOnTransform(walk) > 0)
                        return walk;
                    walk = walk.parent;
                }

                return enemy.transform.root;
            }
            catch
            {
                return enemy.transform;
            }
        }

        private static int CountSnapshotSourcesOnTransform(Transform searchRoot)
        {
            int count = 0;
            try
            {
                foreach (var comp in searchRoot.gameObject.GetComponentsInChildren<Component>(true))
                {
                    if (comp == null)
                        continue;

                    if (IsSkinnedMeshRendererComponent(comp))
                    {
                        var meshProp = comp.GetType().GetProperty("sharedMesh");
                        var shared = meshProp?.GetValue(comp, null) as Mesh;
                        if (shared != null && shared.vertexCount >= 4)
                            count++;
                        continue;
                    }

                    if (comp is MeshFilter mf && mf.sharedMesh != null && mf.sharedMesh.vertexCount >= 4)
                        count++;
                }
            }
            catch { }

            return count;
        }

        private static bool IsRejectedTemplate(EnemyHealth enemy)
        {
            try
            {
                var name = enemy.gameObject.name;
                if (string.IsNullOrEmpty(name))
                    return true;

                var lower = name.ToLowerInvariant();
                if (lower.Contains("playercontroller") || lower == "player" || lower == "localplayer" || lower == "camera")
                    return true;
            }
            catch
            {
                return true;
            }

            return false;
        }

        private static int CountSnapshotSources(EnemyHealth enemy)
        {
            try
            {
                return CountSnapshotSourcesOnTransform(GetTemplateVisualRoot(enemy));
            }
            catch
            {
                return 0;
            }
        }

        private static float ScoreTemplate(EnemyHealth enemy, Vector3 origin, float maxDistanceMeters)
        {
            float dist = Vector3.Distance(origin, ProximityScan.GetFocusPosition(enemy));
            if (dist > maxDistanceMeters)
                return -1f;

            if (CountSnapshotSources(enemy) == 0)
                return -1f;

            int verts = EstimateVertexCount(enemy);
            if (verts < MinTotalVertices)
                return -1f;

            float distPenalty = dist < 15f ? dist * 0.5f : dist * 2f;
            return verts - distPenalty;
        }

        private static int EstimateVertexCount(EnemyHealth enemy)
        {
            int total = 0;
            try
            {
                var visualRoot = GetTemplateVisualRoot(enemy);
                foreach (var comp in visualRoot.gameObject.GetComponentsInChildren<Component>(true))
                {
                    if (comp == null)
                        continue;
                    if (!IsSkinnedMeshRendererComponent(comp))
                        continue;

                    var meshProp = comp.GetType().GetProperty("sharedMesh");
                    var shared = meshProp?.GetValue(comp, null) as Mesh;
                    if (shared != null)
                        total += shared.vertexCount;
                }

                foreach (var mf in visualRoot.gameObject.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mf != null && mf.sharedMesh != null)
                        total += mf.sharedMesh.vertexCount;
                }
            }
            catch { }

            return total;
        }

        private static void AddBakedSnapshotParts(GameObject root, EnemyHealth template, ref BuildResult result)
        {
            var bakedMeshes = new List<Mesh>();
            var templateRoot = GetTemplateVisualRoot(template);

            try
            {
                foreach (var comp in templateRoot.gameObject.GetComponentsInChildren<Component>(true))
                {
                    if (comp == null)
                        continue;

                    if (IsSkinnedMeshRendererComponent(comp))
                    {
                        if (TryBakeSkinnedPart(root, templateRoot, comp, bakedMeshes, ref result))
                            result.SkinnedPartCount++;
                        continue;
                    }

                    if (comp is MeshFilter mf && mf.sharedMesh != null)
                    {
                        if (TryAddStaticMeshPart(root, templateRoot, mf, ref result))
                            result.StaticPartCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogVerbose($"[PsychoticBreak] Snapshot partial: {ex.Message}");
            }

            if (result.PartCount == 0)
            {
                for (int i = 0; i < bakedMeshes.Count; i++)
                {
                    try
                    {
                        UnityEngine.Object.Destroy(bakedMeshes[i]);
                    }
                    catch { }
                }
            }
        }

        private static bool TryBakeSkinnedPart(
            GameObject root,
            Transform templateRoot,
            Component smrComponent,
            List<Mesh> bakedMeshes,
            ref BuildResult result)
        {
            var smrTransform = smrComponent.transform;
            if (smrTransform == null)
                return false;

            Mesh? mesh = new Mesh();
            mesh.name = "DreadHallucinationBake";
            bool destroyMeshOnFailure = true;
            try
            {
                if (!InvokeBakeMesh(smrComponent, mesh))
                {
                    result.BakeMeshFailures++;
                    UnityEngine.Object.Destroy(mesh);
                    mesh = TryDuplicateBindPoseMesh(smrComponent);
                    destroyMeshOnFailure = false;
                    if (mesh == null)
                        return false;
                    result.BindPoseFallbackParts++;
                }
            }
            catch
            {
                result.BakeMeshFailures++;
                if (destroyMeshOnFailure && mesh != null)
                    UnityEngine.Object.Destroy(mesh);
                mesh = TryDuplicateBindPoseMesh(smrComponent);
                if (mesh == null)
                    return false;
                result.BindPoseFallbackParts++;
            }

            if (mesh.vertexCount < 4)
            {
                if (destroyMeshOnFailure)
                    UnityEngine.Object.Destroy(mesh);
                return false;
            }

            bakedMeshes.Add(mesh);
            if (!AttachMeshPartLocal(root, templateRoot, smrTransform, mesh, GetSharedMaterials(smrComponent), ref result))
            {
                return false;
            }

            result.PartCount++;
            result.TotalVertices += mesh.vertexCount;
            return true;
        }

        private static Mesh? TryDuplicateBindPoseMesh(Component smrComponent)
        {
            try
            {
                var meshProp = smrComponent.GetType().GetProperty("sharedMesh");
                var shared = meshProp?.GetValue(smrComponent, null) as Mesh;
                if (shared == null || shared.vertexCount < 4)
                    return null;

                var dup = UnityEngine.Object.Instantiate(shared) as Mesh;
                if (dup != null)
                    dup.name = "DreadHallucinationBindPose";
                return dup;
            }
            catch
            {
                return null;
            }
        }

        private static bool InvokeBakeMesh(Component smrComponent, Mesh mesh)
        {
            var type = smrComponent.GetType();
            var two = type.GetMethod("BakeMesh", new[] { typeof(Mesh), typeof(bool) });
            if (two != null)
            {
                two.Invoke(smrComponent, new object[] { mesh, true });
                return true;
            }

            var one = type.GetMethod("BakeMesh", new[] { typeof(Mesh) });
            if (one != null)
            {
                one.Invoke(smrComponent, new object[] { mesh });
                return true;
            }

            return false;
        }

        private static bool TryAddStaticMeshPart(
            GameObject root,
            Transform templateRoot,
            MeshFilter source,
            ref BuildResult result)
        {
            if (source.sharedMesh == null)
                return false;

            var verts = source.sharedMesh.vertexCount;
            if (verts < 4)
                return false;

            var mr = source.GetComponent<MeshRenderer>();
            if (!AttachMeshPartLocal(
                    root,
                    templateRoot,
                    source.transform,
                    source.sharedMesh,
                    mr != null ? GetSharedMaterials(mr) : null,
                    ref result))
            {
                return false;
            }

            result.PartCount++;
            result.TotalVertices += verts;
            return true;
        }

        private static bool AttachMeshPartLocal(
            GameObject root,
            Transform templateRoot,
            Transform sourceTransform,
            Mesh mesh,
            Material[]? materials,
            ref BuildResult result)
        {
            var part = new GameObject(sourceTransform.name + "_Snap");
            var worldOffset = sourceTransform.position - templateRoot.position;
            part.transform.position = root.transform.position + worldOffset;
            part.transform.rotation = sourceTransform.rotation;
            part.transform.SetParent(root.transform, true);
            part.transform.localScale = sourceTransform.lossyScale;

            var mf = part.AddComponent<MeshFilter>();
            mf.mesh = mesh;
            var mr = part.AddComponent<MeshRenderer>();
            if (materials != null && materials.Length > 0)
                mr.sharedMaterials = materials;

            return true;
        }

        private static void AlignSnapshotFeetToFloor(GameObject root, float floorY)
        {
            try
            {
                var filters = root.GetComponentsInChildren<MeshFilter>(true);
                if (filters == null || filters.Length == 0)
                    return;

                float minY = float.MaxValue;
                for (int i = 0; i < filters.Length; i++)
                {
                    var mf = filters[i];
                    if (mf == null || mf.mesh == null || mf.transform == null)
                        continue;

                    var meshBounds = mf.mesh.bounds;
                    var corners = new[]
                    {
                        meshBounds.min,
                        meshBounds.max,
                        new Vector3(meshBounds.min.x, meshBounds.min.y, meshBounds.max.z),
                        new Vector3(meshBounds.min.x, meshBounds.max.y, meshBounds.min.z),
                        new Vector3(meshBounds.max.x, meshBounds.min.y, meshBounds.min.z),
                        new Vector3(meshBounds.max.x, meshBounds.max.y, meshBounds.min.z),
                        new Vector3(meshBounds.max.x, meshBounds.min.y, meshBounds.max.z),
                        new Vector3(meshBounds.min.x, meshBounds.max.y, meshBounds.max.z),
                    };

                    for (int c = 0; c < corners.Length; c++)
                    {
                        float y = mf.transform.TransformPoint(corners[c]).y;
                        if (y < minY)
                            minY = y;
                    }
                }

                if (minY < float.MaxValue - 1f)
                {
                    float dy = floorY - minY;
                    if (dy > 0.01f || dy < -0.01f)
                        root.transform.position += new Vector3(0f, dy, 0f);
                }
            }
            catch { }
        }

        private static void ClearSnapshotChildren(GameObject root)
        {
            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                var child = root.transform.GetChild(i);
                if (child != null)
                {
                    try
                    {
                        UnityEngine.Object.Destroy(child.gameObject);
                    }
                    catch { }
                }
            }
        }

        private static void ApplyFallbackSilhouette(GameObject root, EnemyHealth? template)
        {
            Material[]? mats = GetTemplateMaterials(template);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "FallbackBody";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.9f, 1.1f, 0.9f);
            DisableCollider(body);
            ApplyMaterials(body, mats);

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "FallbackHead";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.85f, 0f);
            head.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
            DisableCollider(head);
            ApplyMaterials(head, mats);
        }

        private static Material[]? GetTemplateMaterials(EnemyHealth? template)
        {
            if (template == null || !EnemyHealthCompat.IsValid(template))
                return null;

            var visualRoot = GetTemplateVisualRoot(template);
            foreach (var comp in visualRoot.gameObject.GetComponentsInChildren<Component>(true))
            {
                if (comp == null)
                    continue;
                var typeName = comp.GetType().Name;
                if (!IsSkinnedMeshRendererComponent(comp) && typeName != "MeshRenderer")
                    continue;

                var mats = GetSharedMaterials(comp);
                if (mats != null && mats.Length > 0)
                    return mats;
            }

            return null;
        }

        private static void ApplyMaterials(GameObject go, Material[]? materials)
        {
            if (materials == null || materials.Length == 0)
                return;

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
                mr.sharedMaterials = materials;
        }

        private static void DisableCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null)
                col.enabled = false;
        }

        private static Material[]? GetSharedMaterials(Component rendererComponent)
        {
            try
            {
                var prop = rendererComponent.GetType().GetProperty("sharedMaterials");
                return prop?.GetValue(rendererComponent, null) as Material[];
            }
            catch
            {
                return null;
            }
        }

        private static AudioClip? FindAttackClipOnTemplate(EnemyHealth template)
        {
            var sources = template.gameObject.GetComponentsInChildren<AudioSource>(true);
            if (sources == null)
                return null;

            AudioClip? best = null;
            float bestLen = 0f;
            for (int i = 0; i < sources.Length; i++)
            {
                var src = sources[i];
                if (src == null || src.clip == null)
                    continue;
                if (src.clip.length < 0.15f || src.clip.length > 3f)
                    continue;
                if (src.clip.length > bestLen)
                {
                    bestLen = src.clip.length;
                    best = src.clip;
                }
            }

            return best;
        }
    }
}
