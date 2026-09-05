using System.Collections.Generic;
using KimLIb.ModuleSystems;
using UnityEngine;
using UnityEngine.Rendering;

namespace _00_Members.KYM.Scripts.Humans.Effects
{
    public sealed class BloodPoolModule : MonoBehaviour, IModule
    {
        private static readonly int[] PoolAtlasTiles = { 0, 6, 8, 11 };

        private sealed class GrowingStain
        {
            public Transform Transform;
            public Vector3 StartScale;
            public Vector3 TargetScale;
            public float StartTime;
            public float Duration;
        }

        [SerializeField] private Material bloodMaterial;
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField, Min(0f)] private float spawnDelay = 1.1f;
        [SerializeField, Min(0.1f)] private float raycastHeight = 1.4f;
        [SerializeField, Min(0.1f)] private float raycastDistance = 3.2f;
        [SerializeField] private Vector2 finalDiameter = new Vector2(1.15f, 1.75f);
        [SerializeField] private Vector2 growthDuration = new Vector2(6f, 10f);
        [SerializeField, Range(0, 4)] private int satelliteStains = 2;
        [SerializeField, Min(1f)] private float lifetime = 120f;

        private readonly List<GameObject> _spawnedPools = new List<GameObject>();
        private readonly List<Mesh> _runtimeMeshes = new List<Mesh>();
        private readonly List<GrowingStain> _growingStains = new List<GrowingStain>();
        private AbstractHuman _human;
        private bool _pendingSpawn;
        private float _spawnTime;

        public void Initialize(ModuleOwner owner)
        {
            _human = owner as AbstractHuman;
            if (_human == null)
            {
                enabled = false;
                return;
            }

            _human.Died += OnDied;
            _human.Revived += OnRevived;
        }

        private void OnDied()
        {
            _pendingSpawn = true;
            _spawnTime = Time.time + spawnDelay;
        }

        private void Update()
        {
            if (_pendingSpawn && Time.time >= _spawnTime)
            {
                _pendingSpawn = false;
                TrySpawnPool();
            }

            UpdateGrowth();
        }

        private void TrySpawnPool()
        {
            if (bloodMaterial == null ||
                !TryFindGround(GetLowestBodyPoint(), out RaycastHit groundHit))
            {
                return;
            }

            float diameter = Random.Range(
                Mathf.Min(finalDiameter.x, finalDiameter.y),
                Mathf.Max(finalDiameter.x, finalDiameter.y));
            float duration = Random.Range(
                Mathf.Min(growthDuration.x, growthDuration.y),
                Mathf.Max(growthDuration.x, growthDuration.y));

            CreateStain(
                "MainBloodPool",
                groundHit.point,
                groundHit.normal,
                diameter,
                duration,
                GetRandomPoolTile());
            CreateStain(
                "BloodPoolDepthLayer",
                groundHit.point + groundHit.normal * 0.004f,
                groundHit.normal,
                diameter * Random.Range(0.58f, 0.76f),
                duration * Random.Range(0.65f, 0.82f),
                GetRandomPoolTile());

            for (int i = 0; i < satelliteStains; i++)
            {
                Vector2 offset2D = Random.insideUnitCircle.normalized * Random.Range(0.22f, 0.58f);
                Vector3 offset = new Vector3(offset2D.x, 0f, offset2D.y);
                Vector3 satelliteOrigin = groundHit.point + offset + Vector3.up * raycastHeight;
                if (!TryFindGround(satelliteOrigin, out RaycastHit satelliteHit, true))
                {
                    continue;
                }

                CreateStain(
                    "BloodSatellite",
                    satelliteHit.point,
                    satelliteHit.normal,
                    diameter * Random.Range(0.16f, 0.32f),
                    duration * Random.Range(0.45f, 0.75f),
                    GetRandomPoolTile());
            }
        }

        private static int GetRandomPoolTile()
        {
            return PoolAtlasTiles[Random.Range(0, PoolAtlasTiles.Length)];
        }

        private void CreateStain(
            string objectName,
            Vector3 position,
            Vector3 normal,
            float diameter,
            float duration,
            int atlasTile)
        {
            GameObject stain = new GameObject(objectName);
            stain.layer = _human.gameObject.layer;
            Quaternion groundRotation = Quaternion.FromToRotation(Vector3.up, normal);
            stain.transform.SetPositionAndRotation(
                position + normal * 0.008f,
                Quaternion.AngleAxis(Random.Range(0f, 360f), normal) * groundRotation);

            Mesh mesh = CreateIrregularPoolMesh(atlasTile);
            _runtimeMeshes.Add(mesh);
            MeshFilter filter = stain.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = stain.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = bloodMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.sortingOrder = 2;

            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            Color color = Color.Lerp(
                new Color(0.13f, 0.001f, 0.002f, 0.88f),
                new Color(0.28f, 0.003f, 0.005f, 0.94f),
                Random.Range(0f, 0.45f));
            properties.SetColor("_BaseColor", color);
            properties.SetColor("_Color", color);
            renderer.SetPropertyBlock(properties);

            Vector3 startScale = new Vector3(diameter * 0.08f, 1f, diameter * 0.08f);
            Vector3 targetScale = new Vector3(
                diameter * Random.Range(0.85f, 1.15f),
                1f,
                diameter * Random.Range(0.72f, 1.08f));
            stain.transform.localScale = startScale;
            _growingStains.Add(new GrowingStain
            {
                Transform = stain.transform,
                StartScale = startScale,
                TargetScale = targetScale,
                StartTime = Time.time,
                Duration = Mathf.Max(0.1f, duration)
            });

            _spawnedPools.Add(stain);
            Destroy(stain, lifetime);
        }

        private void UpdateGrowth()
        {
            for (int i = _growingStains.Count - 1; i >= 0; i--)
            {
                GrowingStain stain = _growingStains[i];
                if (stain.Transform == null)
                {
                    _growingStains.RemoveAt(i);
                    continue;
                }

                float t = Mathf.Clamp01((Time.time - stain.StartTime) / stain.Duration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                stain.Transform.localScale = Vector3.LerpUnclamped(
                    stain.StartScale,
                    stain.TargetScale,
                    eased);
                if (t >= 1f)
                {
                    _growingStains.RemoveAt(i);
                }
            }
        }

        private Vector3 GetLowestBodyPoint()
        {
            Collider[] colliders = _human.GetComponentsInChildren<Collider>(true);
            bool found = false;
            Vector3 lowest = _human.DamageSamplePoint;
            foreach (Collider bodyCollider in colliders)
            {
                if (!bodyCollider.enabled || bodyCollider.isTrigger)
                {
                    continue;
                }

                Vector3 candidate = bodyCollider.bounds.center;
                candidate.y = bodyCollider.bounds.min.y;
                if (!found || candidate.y < lowest.y)
                {
                    found = true;
                    lowest = candidate;
                }
            }

            return lowest;
        }

        private bool TryFindGround(Vector3 point, out RaycastHit groundHit, bool pointIsRayOrigin = false)
        {
            Vector3 origin = pointIsRayOrigin ? point : point + Vector3.up * raycastHeight;
            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                Vector3.down,
                pointIsRayOrigin ? raycastDistance + raycastHeight : raycastDistance,
                groundLayers,
                QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.transform == null || hit.transform.IsChildOf(_human.transform))
                {
                    continue;
                }

                if (Vector3.Dot(hit.normal, Vector3.up) < 0.45f)
                {
                    continue;
                }

                groundHit = hit;
                return true;
            }

            groundHit = default;
            return false;
        }

        private static Mesh CreateIrregularPoolMesh(int atlasTile)
        {
            const int segments = 20;
            Vector3[] vertices = new Vector3[segments + 1];
            Vector3[] normals = new Vector3[segments + 1];
            Vector2[] uvs = new Vector2[segments + 1];
            int[] triangles = new int[segments * 3];

            int tileX = Mathf.Clamp(atlasTile, 0, 15) % 4;
            int tileY = Mathf.Clamp(atlasTile, 0, 15) / 4;
            Vector2 tileMin = new Vector2(tileX * 0.25f, tileY * 0.25f);
            Vector2 tileCenter = tileMin + Vector2.one * 0.125f;

            vertices[0] = Vector3.zero;
            normals[0] = Vector3.up;
            uvs[0] = tileCenter;
            for (int i = 0; i < segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                float radius = Random.Range(0.78f, 1.08f) * 0.5f;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                vertices[i + 1] = new Vector3(x, 0f, z);
                normals[i + 1] = Vector3.up;
                uvs[i + 1] = tileCenter + new Vector2(x, z) * 0.235f;

                int triangle = i * 3;
                triangles[triangle] = 0;
                triangles[triangle + 1] = i + 1;
                triangles[triangle + 2] = (i + 1) % segments + 1;
            }

            Mesh mesh = new Mesh { name = "RuntimeBloodPool" };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnRevived()
        {
            _pendingSpawn = false;
            CleanupPools();
        }

        private void CleanupPools()
        {
            foreach (GameObject pool in _spawnedPools)
            {
                if (pool != null)
                {
                    Destroy(pool);
                }
            }

            foreach (Mesh mesh in _runtimeMeshes)
            {
                if (mesh != null)
                {
                    Destroy(mesh);
                }
            }

            _spawnedPools.Clear();
            _runtimeMeshes.Clear();
            _growingStains.Clear();
        }

        private void OnDestroy()
        {
            if (_human != null)
            {
                _human.Died -= OnDied;
                _human.Revived -= OnRevived;
            }

            CleanupPools();
        }
    }
}
