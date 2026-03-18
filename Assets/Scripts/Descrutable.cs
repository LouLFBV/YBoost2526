using UnityEngine;
using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Transform))]
public class Descrutable : MonoBehaviour
{
    public enum DebrisShape { Cube, Sphere, Triangle, Custom }

    private const float MinDimension = 1e-4f;
    private const float MinValue = 1e-6f;

    [Header("Debris shape")]
    [Tooltip("Shape of the debris pieces generated when this object is destroyed.\nChoose 'Custom' to use a specific prefab for each piece.\nSphere is weird with non-uniform scaling, not really usable.")]
    [SerializeField] private DebrisShape debrisShape = DebrisShape.Cube;
    [Tooltip("When DebrisShape is Custom, this prefab will be instantiated for each debris piece.")]
    [SerializeField] private GameObject customDebrisPrefab;
    [Header("Optional fractured prefab")]
    [Tooltip("If assigned, this prefab will be instantiated instead of generating procedural debris.")]
    [SerializeField] private GameObject fracturedPrefab;

    [Header("Procedural debris (used when no prefab is provided)")]
    [Tooltip("Range (min..max) number of debris pieces to generate. The max will be clamped to be >= min.")]
    [SerializeField, Range(3, 30)] private int debrisCountMin = 3;
    [SerializeField, Range(3, 30)] private int debrisCountMax = 12;
    [SerializeField, Min(0f)] private float explosionForce = 100f;
    [SerializeField, Min(0.1f)] private float explosionRadius = 4f;
    [SerializeField, Min(0.01f)] private Vector2 debrisScaleRange = new Vector2(0.05f, 0.35f); // interpreted as fraction of object's max dimension
    [Tooltip("Optional material to use for generated debris. If set, this overrides copying the original object's material.")]
    [SerializeField] private Material debrisMaterial;
    [Tooltip("Bias spawn positions upward relative to the object's local up vector (0..1). Helps avoid debris appearing under the map when object sits on the ground.")]
    [SerializeField, Range(0f, 1f)] private float spawnBiasUp = 0.5f;
    [Header("Partial destruction")]
    [Tooltip("How many placement attempts to try per piece when doing partial (hit-point) destruction.")]
    [SerializeField, Range(1, 12)] private int partialPlacementAttempts = 6;
    [SerializeField] private bool useOriginalMaterial = true;
    [SerializeField, Min(0f)] private float debrisLifetime = 8f;
    [SerializeField] private bool destroyOriginal = true;
    [Header("Physics")]
    [Tooltip("Approximate density used to compute debris mass (kg per cubic meter). Increase to make pieces heavier.")]
    [SerializeField, Min(0.01f)] private float debrisDensity = 800f;
    [Tooltip("Clamp mass of individual debris pieces between these values (kg).")]
    [SerializeField, Min(0.01f)] private float debrisMassMin = 0.1f;
    [SerializeField, Min(0.1f)] private float debrisMassMax = 200f;
    [Tooltip("Extra initial burst speed applied to each debris piece to guarantee visible motion at spawn.")]
    [SerializeField, Min(0f)] private float initialBurstSpeed = 6f;
    [Tooltip("Random variation applied to initial burst speed (0..1).")]
    [SerializeField, Range(0f, 1f)] private float initialBurstRandomness = 0.35f;
    [Header("Pooling")]
    [Tooltip("Reuse debris instances instead of destroying/re-instantiating them to reduce spikes and GC allocations.")]
    [SerializeField] private bool useDebrisPooling = true;
    [Tooltip("Maximum inactive debris kept in pool for each debris type.")]
    [SerializeField, Range(8, 256)] private int maxPooledDebrisPerKey = 64;
    [Header("Destruction permissions")]
    [Tooltip("If disabled, player attacks cannot destroy this object. External systems can still call DestroyObject().")]
    [SerializeField] private bool canBeDestroyedByPlayers = true;

    private Transform cachedTransform;
    private Collider[] cachedColliders;
    private Renderer[] cachedRenderers;
    private Renderer primaryRenderer;

    private void Awake()
    {
        CacheChildComponents();
    }

    // Validate inspector inputs so max is never smaller than min and ranges stay valid.
    private void OnValidate()
    {
        if (cachedTransform == null)
            cachedTransform = transform;

        debrisCountMin = Mathf.Max(1, debrisCountMin);
        if (debrisCountMax < debrisCountMin)
            debrisCountMax = debrisCountMin;

        debrisScaleRange.x = Mathf.Clamp01(debrisScaleRange.x);
        debrisScaleRange.y = Mathf.Clamp01(Mathf.Max(debrisScaleRange.y, debrisScaleRange.x));

        spawnBiasUp = Mathf.Clamp01(spawnBiasUp);
        debrisLifetime = Mathf.Max(0f, debrisLifetime);
        explosionForce = Mathf.Max(0f, explosionForce);
        explosionRadius = Mathf.Max(0f, explosionRadius);
        initialBurstSpeed = Mathf.Max(0f, initialBurstSpeed);
        initialBurstRandomness = Mathf.Clamp01(initialBurstRandomness);
        maxPooledDebrisPerKey = Mathf.Clamp(maxPooledDebrisPerKey, 8, 256);

        CacheChildComponents();
    }

    // Public API: destroy the object fully (no hit point)
    public void DestroyObject()
    {
        if (TrySpawnFracturedPrefab())
            return;

        GenerateProceduralDebris(null, 0f);
    }

    // Public API: attempt destruction from player interactions.
    // Returns false when this object is configured to ignore player destruction.
    public bool TryDestroyFromPlayer()
    {
        if (!canBeDestroyedByPlayers)
            return false;

        DestroyObject();
        return true;
    }

    // Public API: destroy around a hit point (partial destruction)
    public void DestroyObject(Vector3 hitPoint, float radius)
    {
        if (TrySpawnFracturedPrefab())
            return;

        GenerateProceduralDebris(hitPoint, radius);
    }

    // Public API: attempt partial destruction from player interactions.
    // Returns false when this object is configured to ignore player destruction.
    public bool TryDestroyFromPlayer(Vector3 hitPoint, float radius)
    {
        if (!canBeDestroyedByPlayers)
            return false;

        DestroyObject(hitPoint, radius);
        return true;
    }

    // Core procedural generator. If hitPoint is provided and radius > 0, debris will be biased to spawn near that point (partial destruction).
    private void GenerateProceduralDebris(Vector3? hitPoint, float radius)
    {
        if (cachedTransform == null)
            cachedTransform = transform;

        if (cachedRenderers == null)
            CacheChildComponents();

        // Choose piece count from configured range (inclusive)
        int pieceCount = Mathf.Max(1, debrisCountMin);
        if (debrisCountMax <= debrisCountMin)
            pieceCount = debrisCountMin;
        else
            pieceCount = Random.Range(debrisCountMin, debrisCountMax + 1);

        // Determine which material to apply to generated debris: explicit override, else original if requested
        Material debrisMat = null;
        if (debrisMaterial != null)
            debrisMat = debrisMaterial;
        else if (primaryRenderer != null && useOriginalMaterial)
            debrisMat = primaryRenderer.sharedMaterial;

        // Use robust world-space bounds so assets with off-center pivots (like some map props)
        // still spawn debris around the visible object and not around a parent/world origin.
        Bounds worldBounds;
        if (!TryGetWorldSpawnBounds(out worldBounds))
        {
            worldBounds = new Bounds(cachedTransform.position, Vector3.one);
        }

        Vector3 worldSize = worldBounds.size;
        // Ensure non-zero volume
        float objectVolume = Mathf.Max(MinValue, worldSize.x * worldSize.y * worldSize.z);

        // Interpret debrisScaleRange as fraction of the object's corresponding axis size
        float minFrac = Mathf.Clamp01(debrisScaleRange.x);
        float maxFrac = Mathf.Clamp(debrisScaleRange.y, minFrac, 1f);

        // Precompute per-piece sizes (width, height, depth) in WORLD space
        Vector3[] worldSizes = new Vector3[pieceCount];
        float totalVolume = 0f;
        for (int i = 0; i < pieceCount; i++)
        {
            float wx = Random.Range(minFrac * worldSize.x, maxFrac * worldSize.x);
            float wy = Random.Range(minFrac * worldSize.y, maxFrac * worldSize.y);
            float wz = Random.Range(minFrac * worldSize.z, maxFrac * worldSize.z);
            // Prevent degenerate zero dimensions
            wx = Mathf.Max(wx, MinDimension);
            wy = Mathf.Max(wy, MinDimension);
            wz = Mathf.Max(wz, MinDimension);
            worldSizes[i] = new Vector3(wx, wy, wz);
            totalVolume += wx * wy * wz;
        }

        // If total debris volume exceeds original object's volume, scale all pieces down proportionally
        if (totalVolume > objectVolume)
        {
            float scaleFactor = Mathf.Pow(objectVolume / totalVolume, 1f / 3f);
            for (int i = 0; i < pieceCount; i++)
            {
                worldSizes[i] *= scaleFactor;
            }
            totalVolume = objectVolume; // approximate
        }

        // World center to apply explosion from
        Vector3 worldCenter = worldBounds.center;
        
        // Create a container GameObject to group all debris pieces for easier inspection during testing
        GameObject debrisRoot = new GameObject($"Debris_{name}_{GetInstanceID()}_{Time.frameCount}");
        // Parent the root next to the original object so hierarchy stays tidy
        debrisRoot.transform.SetParent(cachedTransform.parent, true);
        debrisRoot.transform.position = worldCenter;

        bool isPartialDestruction = hitPoint.HasValue && radius > 0f;
        int attemptsPerPiece = isPartialDestruction ? partialPlacementAttempts : 1;
        Vector3 upBiasOffset = cachedTransform.up * (spawnBiasUp * worldSize.y * 0.5f);

        for (int i = 0; i < pieceCount; i++)
        {
            Vector3 desiredWorldSize = worldSizes[i]; // (width, height, depth) in world units

            // We allow multiple attempts to find a spawn location near the hitPoint when doing partial destruction.
            Vector3 worldPos = Vector3.zero;
            for (int attempt = 0; attempt < attemptsPerPiece; attempt++)
            {
                // Compute a world position that keeps the piece inside the world bounds.
                Vector3 halfWorld = desiredWorldSize * 0.5f;
                Vector3 minWorld = worldBounds.min + halfWorld;
                Vector3 maxWorld = worldBounds.max - halfWorld;
                // If the piece is larger than bounds on one axis, collapse to center on that axis.
                if (minWorld.x > maxWorld.x) { minWorld.x = maxWorld.x = worldCenter.x; }
                if (minWorld.y > maxWorld.y) { minWorld.y = maxWorld.y = worldCenter.y; }
                if (minWorld.z > maxWorld.z) { minWorld.z = maxWorld.z = worldCenter.z; }

                worldPos = new Vector3(
                    Random.Range(minWorld.x, maxWorld.x),
                    Random.Range(minWorld.y, maxWorld.y),
                    Random.Range(minWorld.z, maxWorld.z)
                );

                // Apply a small upward bias along the object's local up to reduce chance of spawning debris below ground
                worldPos += upBiasOffset;

                // Keep debris from spawning below the object's bottom bound.
                float pieceHalfHeight = desiredWorldSize.y * 0.5f;
                float minAllowedY = worldBounds.min.y + pieceHalfHeight + 0.01f;
                if (worldPos.y < minAllowedY)
                {
                    float delta = minAllowedY - worldPos.y;
                    worldPos += Vector3.up * delta;
                }

                // If partial (hitPoint provided), prefer positions close to it
                if (isPartialDestruction)
                {
                    float dist = Vector3.Distance(worldPos, hitPoint.Value);
                    if (dist <= radius)
                        break;

                    // allow some chance based on distance to still place a piece (soft falloff)
                    float p = Mathf.Clamp01(1f - (dist / radius));
                    if (Random.value < p * 0.25f)
                        break;
                }
                else
                {
                    break;
                }
            }

            string poolKey;
            GameObject piece = AcquireDebrisPiece(out poolKey);
            piece.transform.SetParent(debrisRoot.transform, true);
            piece.transform.position = worldPos;
            piece.transform.rotation = Random.rotation;

            // Compute localScale so that piece's world scale equals desiredWorldSize (accounting for parent's lossyScale)
            Vector3 parentLossy = (piece.transform.parent != null) ? piece.transform.parent.lossyScale : Vector3.one;
            parentLossy = new Vector3(Mathf.Max(parentLossy.x, MinValue), Mathf.Max(parentLossy.y, MinValue), Mathf.Max(parentLossy.z, MinValue));
            Vector3 localScale = new Vector3(
                desiredWorldSize.x / parentLossy.x,
                desiredWorldSize.y / parentLossy.y,
                desiredWorldSize.z / parentLossy.z
            );

            piece.transform.localScale = localScale;

            var pieceCache = piece.GetComponent<DebrisPieceCache>();
            if (pieceCache == null)
                pieceCache = piece.AddComponent<DebrisPieceCache>();
            pieceCache.Refresh();

            // Apply material to all renderers on the piece (useful for prefabs with multiple renderers)
            if (debrisMat != null)
            {
                var rends = pieceCache.Renderers;
                foreach (var r in rends)
                    r.sharedMaterial = debrisMat;
            }

            // Ensure there is a collider suitable for physics (tetra uses mesh collider added in CreateTetrahedron)
            if (debrisShape == DebrisShape.Triangle)
            {
                var meshCol = pieceCache.MeshCollider;
                if (meshCol == null)
                {
                    meshCol = piece.AddComponent<MeshCollider>();
                    meshCol.convex = true;
                    pieceCache.MeshCollider = meshCol;
                }
            }

            // Add or reuse Rigidbody and apply explosion
            var rb = pieceCache.Rigidbody;
            if (rb == null)
            {
                rb = piece.AddComponent<Rigidbody>();
                pieceCache.Rigidbody = rb;
            }

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;
            // Compute volume in cubic meters (world units) and convert to mass using density
            float pieceVolume = Mathf.Max(MinValue, desiredWorldSize.x * desiredWorldSize.y * desiredWorldSize.z);
            float computedMass = pieceVolume * debrisDensity;
            rb.mass = Mathf.Clamp(computedMass, debrisMassMin, debrisMassMax);
            rb.AddExplosionForce(explosionForce * Random.Range(0.8f, 1.2f), worldCenter, explosionRadius);

            // Add a guaranteed outward kick so debris does not appear static with heavy pieces or constrained prefab settings.
            Vector3 burstDir = (worldPos - worldCenter);
            if (burstDir.sqrMagnitude < MinValue)
                burstDir = Random.onUnitSphere;
            burstDir.Normalize();

            float burstMultiplier = 1f + Random.Range(-initialBurstRandomness, initialBurstRandomness);
            float burstSpeed = Mathf.Max(0f, initialBurstSpeed * burstMultiplier);
            rb.linearVelocity += burstDir * burstSpeed;
            rb.AddForce(burstDir * burstSpeed * rb.mass * 0.5f, ForceMode.Impulse);

            rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);
            rb.WakeUp();

            float pieceLife = debrisLifetime + Random.Range(0f, 2f);
            var autoReturn = piece.GetComponent<DebrisAutoReturn>();
            if (autoReturn == null)
                autoReturn = piece.AddComponent<DebrisAutoReturn>();
            autoReturn.Schedule(poolKey, useDebrisPooling, maxPooledDebrisPerKey, pieceLife);
        }

        // Finally remove the original object
        if (destroyOriginal)
            Destroy(gameObject);

        // Destroy the debris root after all pieces have been removed to keep hierarchy tidy.
        // Pieces are destroyed at `debrisLifetime + jitter` where jitter in [0,2], so add a small buffer.
        Destroy(debrisRoot, debrisLifetime + 4f);
    }

    private bool TryGetWorldSpawnBounds(out Bounds bounds)
    {
        bool hasBounds = false;
        bounds = default;

        // Prefer colliders because they are usually a better gameplay volume reference.
        var colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            var c = colliders[i];
            if (c == null || !c.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = c.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(c.bounds);
            }
        }

        // Fallback to renderer bounds when no collider is available.
        if (!hasBounds)
        {
            var renderers = GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null || !r.enabled)
                    continue;

                if (!hasBounds)
                {
                    bounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }
        }

        return hasBounds;
    }

    private bool TrySpawnFracturedPrefab()
    {
        if (fracturedPrefab == null)
            return false;

        if (cachedTransform == null)
            cachedTransform = transform;

        Instantiate(fracturedPrefab, cachedTransform.position, cachedTransform.rotation, cachedTransform.parent);
        if (destroyOriginal)
            Destroy(gameObject);

        return true;
    }

    private GameObject AcquireDebrisPiece(out string poolKey)
    {
        DebrisShape effectiveShape = debrisShape;
        if (effectiveShape == DebrisShape.Custom && customDebrisPrefab == null)
            effectiveShape = DebrisShape.Cube;

        int customId = (effectiveShape == DebrisShape.Custom) ? customDebrisPrefab.GetInstanceID() : 0;
        poolKey = $"{effectiveShape}_{customId}";

        if (useDebrisPooling)
            return DebrisPool.Acquire(poolKey, () => CreateDebrisPiece(effectiveShape));

        return CreateDebrisPiece(effectiveShape);
    }

    private GameObject CreateDebrisPiece(DebrisShape shape)
    {
        switch (shape)
        {
            case DebrisShape.Cube:
                return GameObject.CreatePrimitive(PrimitiveType.Cube);
            case DebrisShape.Sphere:
                return GameObject.CreatePrimitive(PrimitiveType.Sphere);
            case DebrisShape.Triangle:
                return CreateTetrahedron();
            case DebrisShape.Custom:
                return Instantiate(customDebrisPrefab);
            default:
                return GameObject.CreatePrimitive(PrimitiveType.Cube);
        }
    }

    private void CacheChildComponents()
    {
        if (cachedTransform == null)
            cachedTransform = transform;

        cachedColliders = GetComponentsInChildren<Collider>();
        cachedRenderers = GetComponentsInChildren<Renderer>();

        primaryRenderer = null;
        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] == null)
                continue;

            primaryRenderer = cachedRenderers[i];
            break;
        }
    }

    // Create a small tetrahedron mesh (unit-sized). The returned GameObject has MeshFilter + MeshRenderer + MeshCollider.
    private GameObject CreateTetrahedron()
    {
        var go = new GameObject("Debris_Tetra");
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();

        // Original unique vertices (conceptual corners)
        Vector3 v0 = new Vector3( 0.577f,  0.577f,  0.577f);
        Vector3 v1 = new Vector3(-0.577f, -0.577f,  0.577f);
        Vector3 v2 = new Vector3(-0.577f,  0.577f, -0.577f);
        Vector3 v3 = new Vector3( 0.577f, -0.577f, -0.577f);

        // We duplicate vertices per face so each face can have its own UVs (prevents seams/artifacts)
        Vector3[] verts = new Vector3[12];
        int[] tris = new int[12];
        Vector2[] uvs = new Vector2[12];

        // Face definitions (winding outward): groups of three indices into {v0,v1,v2,v3}
        int[][] faces = new int[][] {
            new int[] {0,2,1},
            new int[] {0,1,3},
            new int[] {0,3,2},
            new int[] {1,2,3}
        };

        // UV triangle mapping for each face (maps whole texture per face)
        Vector2 uvA = new Vector2(0.5f, 1f);
        Vector2 uvB = new Vector2(0f, 0f);
        Vector2 uvC = new Vector2(1f, 0f);

        for (int f = 0; f < 4; f++)
        {
            int baseV = f * 3;
            int a = faces[f][0];
            int b = faces[f][1];
            int c = faces[f][2];

            verts[baseV + 0] = (a == 0) ? v0 : (a == 1) ? v1 : (a == 2) ? v2 : v3;
            verts[baseV + 1] = (b == 0) ? v0 : (b == 1) ? v1 : (b == 2) ? v2 : v3;
            verts[baseV + 2] = (c == 0) ? v0 : (c == 1) ? v1 : (c == 2) ? v2 : v3;

            tris[baseV + 0] = baseV + 0;
            tris[baseV + 1] = baseV + 1;
            tris[baseV + 2] = baseV + 2;

            uvs[baseV + 0] = uvA;
            uvs[baseV + 1] = uvB;
            uvs[baseV + 2] = uvC;
        }

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mf.sharedMesh = mesh;

        var meshCol = go.AddComponent<MeshCollider>();
        meshCol.sharedMesh = mesh;
        meshCol.convex = true;

        return go;
    }
}

internal static class DebrisPool
{
    private static readonly Dictionary<string, Stack<GameObject>> PoolByKey = new Dictionary<string, Stack<GameObject>>();
    private static Transform poolRoot;

    private static Transform PoolRoot
    {
        get
        {
            if (poolRoot == null)
            {
                var rootObject = new GameObject("DebrisPoolRoot");
                UnityEngine.Object.DontDestroyOnLoad(rootObject);
                poolRoot = rootObject.transform;
            }

            return poolRoot;
        }
    }

    public static GameObject Acquire(string key, Func<GameObject> factory)
    {
        if (PoolByKey.TryGetValue(key, out var stack))
        {
            while (stack.Count > 0)
            {
                var go = stack.Pop();
                if (go == null)
                    continue;

                go.SetActive(true);
                return go;
            }
        }

        var created = factory();
        var cache = created.GetComponent<DebrisPieceCache>();
        if (cache == null)
            cache = created.AddComponent<DebrisPieceCache>();
        cache.Refresh();
        return created;
    }

    public static void Release(string key, GameObject piece, int maxPerKey)
    {
        if (piece == null)
            return;

        if (!PoolByKey.TryGetValue(key, out var stack))
        {
            stack = new Stack<GameObject>();
            PoolByKey[key] = stack;
        }

        if (stack.Count >= maxPerKey)
        {
            UnityEngine.Object.Destroy(piece);
            return;
        }

        var rb = piece.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }

        piece.transform.SetParent(PoolRoot, false);
        piece.SetActive(false);
        stack.Push(piece);
    }
}

internal sealed class DebrisPieceCache : MonoBehaviour
{
    public Renderer[] Renderers { get; private set; } = Array.Empty<Renderer>();
    public Rigidbody Rigidbody { get; set; }
    public MeshCollider MeshCollider { get; set; }

    public void Refresh()
    {
        Renderers = GetComponentsInChildren<Renderer>(true);
        Rigidbody = GetComponent<Rigidbody>();
        MeshCollider = GetComponent<MeshCollider>();
    }
}

internal sealed class DebrisAutoReturn : MonoBehaviour
{
    private string poolKey;
    private bool usePooling;
    private int maxPerKey;

    public void Schedule(string key, bool enablePooling, int maxCount, float delay)
    {
        poolKey = key;
        usePooling = enablePooling;
        maxPerKey = maxCount;

        CancelInvoke(nameof(ReturnNow));
        if (delay <= 0f)
            ReturnNow();
        else
            Invoke(nameof(ReturnNow), delay);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(ReturnNow));
    }

    private void ReturnNow()
    {
        if (usePooling)
            DebrisPool.Release(poolKey, gameObject, maxPerKey);
        else
            Destroy(gameObject);
    }
}
