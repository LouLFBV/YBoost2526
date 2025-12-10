using UnityEngine;

[RequireComponent(typeof(Transform))]
public class Descrutable : MonoBehaviour
{
    public enum DebrisShape { Cube, Sphere, Triangle, Custom }

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

    // Validate inspector inputs so max is never smaller than min and ranges stay valid.
    private void OnValidate()
    {
        debrisCountMin = Mathf.Max(1, debrisCountMin);
        if (debrisCountMax < debrisCountMin)
            debrisCountMax = debrisCountMin;

        debrisScaleRange.x = Mathf.Clamp01(debrisScaleRange.x);
        debrisScaleRange.y = Mathf.Clamp01(Mathf.Max(debrisScaleRange.y, debrisScaleRange.x));

        spawnBiasUp = Mathf.Clamp01(spawnBiasUp);
        debrisLifetime = Mathf.Max(0f, debrisLifetime);
        explosionForce = Mathf.Max(0f, explosionForce);
        explosionRadius = Mathf.Max(0f, explosionRadius);
    }

    // Public API: destroy the object fully (no hit point)
    public void DestroyObject()
    {
        if (fracturedPrefab != null)
        {
            Instantiate(fracturedPrefab, transform.position, transform.rotation, transform.parent);
            if (destroyOriginal)
                Destroy(gameObject);
            return;
        }

        GenerateProceduralDebris(null, 0f);
    }

    // Public API: destroy around a hit point (partial destruction)
    public void DestroyObject(Vector3 hitPoint, float radius)
    {
        if (fracturedPrefab != null)
        {
            Instantiate(fracturedPrefab, transform.position, transform.rotation, transform.parent);
            if (destroyOriginal)
                Destroy(gameObject);
            return;
        }

        GenerateProceduralDebris(hitPoint, radius);
    }

    // Core procedural generator. If hitPoint is provided and radius > 0, debris will be biased to spawn near that point (partial destruction).
    private void GenerateProceduralDebris(Vector3? hitPoint, float radius)
    {
        // Choose piece count from configured range (inclusive)
        int pieceCount = Mathf.Max(1, debrisCountMin);
        if (debrisCountMax <= debrisCountMin)
            pieceCount = debrisCountMin;
        else
            pieceCount = Random.Range(debrisCountMin, debrisCountMax + 1);

        // Try to copy material from the original renderer if requested
        var mf = GetComponent<MeshFilter>();
        var mr = GetComponent<MeshRenderer>();
        // Determine which material to apply to generated debris: explicit override, else original if requested
        Material debrisMat = null;
        if (debrisMaterial != null)
            debrisMat = debrisMaterial;
        else if (mr != null && useOriginalMaterial)
            debrisMat = mr.sharedMaterial;

        // Use mesh bounds if available to place debris roughly inside the object volume
        Bounds bounds = (mf != null && mf.sharedMesh != null) ? mf.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one);
        // Compute world-space bounds size (taking into account lossyScale)
        Vector3 worldSize = Vector3.Scale(bounds.size, transform.lossyScale);
        // Ensure non-zero volume
        float objectVolume = Mathf.Max(1e-6f, worldSize.x * worldSize.y * worldSize.z);

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
            wx = Mathf.Max(wx, 1e-4f);
            wy = Mathf.Max(wy, 1e-4f);
            wz = Mathf.Max(wz, 1e-4f);
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
        Vector3 worldCenter = transform.TransformPoint(bounds.center);
        
        // Create a container GameObject to group all debris pieces for easier inspection during testing
        GameObject debrisRoot = new GameObject($"Debris_{gameObject.name}_{System.DateTime.Now.Ticks % 1000000}");
        // Parent the root next to the original object so hierarchy stays tidy
        debrisRoot.transform.SetParent(transform.parent, true);
        debrisRoot.transform.position = transform.position;

        for (int i = 0; i < pieceCount; i++)
        {
            Vector3 desiredWorldSize = worldSizes[i]; // (width, height, depth) in world units

            // We allow multiple attempts to find a spawn location near the hitPoint when doing partial destruction.
            int attempts = (hitPoint.HasValue && radius > 0f) ? partialPlacementAttempts : 1;
            bool placed = false;
            Vector3 worldPos = Vector3.zero;
            Vector3 desiredLocalSize = Vector3.zero;

            for (int attempt = 0; attempt < attempts; attempt++)
            {
                // Compute desired local size relative to the object's local space so we can pick a valid inside-local position
                desiredLocalSize = new Vector3(
                    desiredWorldSize.x / Mathf.Max(transform.lossyScale.x, 1e-6f),
                    desiredWorldSize.y / Mathf.Max(transform.lossyScale.y, 1e-6f),
                    desiredWorldSize.z / Mathf.Max(transform.lossyScale.z, 1e-6f)
                );

                // Compute a local position that keeps the piece inside the original bounds (accounting for the piece half-size)
                Vector3 halfLocal = desiredLocalSize * 0.5f;
                Vector3 minLocal = -bounds.extents + halfLocal;
                Vector3 maxLocal = bounds.extents - halfLocal;
                // If the piece is larger than the bounds on an axis, clamp min>max; we'll allow center placement then
                if (minLocal.x > maxLocal.x) { minLocal.x = maxLocal.x = 0f; }
                if (minLocal.y > maxLocal.y) { minLocal.y = maxLocal.y = 0f; }
                if (minLocal.z > maxLocal.z) { minLocal.z = maxLocal.z = 0f; }

                Vector3 localRandomPos = new Vector3(
                    Random.Range(minLocal.x, maxLocal.x),
                    Random.Range(minLocal.y, maxLocal.y),
                    Random.Range(minLocal.z, maxLocal.z)
                );

                worldPos = transform.TransformPoint(localRandomPos);

                // Apply a small upward bias along the object's local up to reduce chance of spawning debris below ground
                worldPos += transform.up * (spawnBiasUp * worldSize.y * 0.5f);

                // If the original object has a collider, ensure debris won't be placed below the collider's bottom in world space
                var origCollider = GetComponent<Collider>();
                if (origCollider != null)
                {
                    float pieceHalfHeight = desiredWorldSize.y * 0.5f;
                    float minAllowedY = origCollider.bounds.min.y + pieceHalfHeight + 0.01f;
                    if (worldPos.y < minAllowedY)
                    {
                        float delta = minAllowedY - worldPos.y;
                        worldPos += Vector3.up * delta;
                    }
                }

                // If partial (hitPoint provided), prefer positions close to it
                if (hitPoint.HasValue && radius > 0f)
                {
                    float dist = Vector3.Distance(worldPos, hitPoint.Value);
                    if (dist <= radius)
                    {
                        placed = true;
                        break;
                    }
                    // allow some chance based on distance to still place a piece (soft falloff)
                    float p = Mathf.Clamp01(1f - (dist / radius));
                    if (Random.value < p * 0.25f)
                    {
                        placed = true;
                        break;
                    }
                }
                else
                {
                    placed = true;
                    break;
                }
            }

            if (!placed)
            {
                // If we couldn't find a good local spot near hitPoint after attempts,
                // fall back to the last sampled position instead of skipping the piece so
                // the configured piece count is honored.
                // `worldPos` already holds the last sampled position from the attempts loop.
            }

            GameObject piece = null;
            bool instantiatedFromPrefab = false;

            switch (debrisShape)
            {
                case DebrisShape.Cube:
                    piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    break;
                case DebrisShape.Sphere:
                    piece = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    break;
                case DebrisShape.Triangle:
                    piece = CreateTetrahedron();
                    break;
                case DebrisShape.Custom:
                    if (customDebrisPrefab != null)
                    {
                        piece = Instantiate(customDebrisPrefab, worldPos, Random.rotation, transform.parent);
                        instantiatedFromPrefab = true;
                    }
                    else
                    {
                        piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    }
                    break;
                default:
                    piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    break;
            }

            if (!instantiatedFromPrefab)
            {
                piece.transform.SetParent(debrisRoot.transform, true);
                piece.transform.position = worldPos;
                piece.transform.rotation = Random.rotation;
            }

            // Compute localScale so that piece's world scale equals desiredWorldSize (accounting for parent's lossyScale)
            Vector3 parentLossy = (piece.transform.parent != null) ? piece.transform.parent.lossyScale : Vector3.one;
            parentLossy = new Vector3(Mathf.Max(parentLossy.x, 1e-6f), Mathf.Max(parentLossy.y, 1e-6f), Mathf.Max(parentLossy.z, 1e-6f));
            Vector3 localScale = new Vector3(
                desiredWorldSize.x / parentLossy.x,
                desiredWorldSize.y / parentLossy.y,
                desiredWorldSize.z / parentLossy.z
            );

            piece.transform.localScale = localScale;

            // Apply material to all renderers on the piece (useful for prefabs with multiple renderers)
            if (debrisMat != null)
            {
                var rends = piece.GetComponentsInChildren<Renderer>();
                foreach (var r in rends)
                    r.sharedMaterial = debrisMat;
            }

            // Ensure there is a collider suitable for physics (tetra uses mesh collider added in CreateTetrahedron)
            if (debrisShape == DebrisShape.Triangle)
            {
                var meshCol = piece.GetComponent<MeshCollider>();
                if (meshCol == null)
                {
                    meshCol = piece.AddComponent<MeshCollider>();
                    meshCol.convex = true;
                }
            }

            // Add or reuse Rigidbody and apply explosion
            var rb = piece.GetComponent<Rigidbody>();
            if (rb == null) rb = piece.AddComponent<Rigidbody>();
            // Compute volume in cubic meters (world units) and convert to mass using density
            float pieceVolume = Mathf.Max(1e-6f, desiredWorldSize.x * desiredWorldSize.y * desiredWorldSize.z);
            float computedMass = pieceVolume * debrisDensity;
            rb.mass = Mathf.Clamp(computedMass, debrisMassMin, debrisMassMax);
            rb.AddExplosionForce(explosionForce * Random.Range(0.8f, 1.2f), worldCenter, explosionRadius);
            rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);

            Destroy(piece, debrisLifetime + Random.Range(0f, 2f));
        }

        // Finally remove the original object
        if (destroyOriginal)
            Destroy(gameObject);

        // Destroy the debris root after all pieces have been removed to keep hierarchy tidy.
        // Pieces are destroyed at `debrisLifetime + jitter` where jitter in [0,2], so add a small buffer.
        Destroy(debrisRoot, debrisLifetime + 4f);
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
