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
    [SerializeField, Min(50f)] private float explosionForce = 300f;
    [SerializeField, Min(1f)] private float explosionRadius = 5f;
    [SerializeField, Min(0.1f)] private Vector2 debrisScaleRange = new Vector2(0.1f, 0.5f); // interpreted as fraction of object's max dimension
    [SerializeField] private bool useOriginalMaterial = true;
    [SerializeField, Min(0f)] private float debrisLifetime = 8f;
    [SerializeField] private bool destroyOriginal = true;

    // Validate inspector inputs so max is never smaller than min and ranges stay valid.
    private void OnValidate()
    {
        debrisCountMin = Mathf.Max(1, debrisCountMin);
        if (debrisCountMax < debrisCountMin)
            debrisCountMax = debrisCountMin;

        // Clamp scale range between 0 and 1, ensure max >= min
        debrisScaleRange.x = Mathf.Clamp(debrisScaleRange.x, 0f, 1f);
        debrisScaleRange.y = Mathf.Clamp(debrisScaleRange.y, debrisScaleRange.x, 1f);

        // Keep other numeric fields sensible
        explosionForce = Mathf.Max(0f, explosionForce);
        explosionRadius = Mathf.Max(0.0001f, explosionRadius);
        debrisLifetime = Mathf.Max(0f, debrisLifetime);
    }

    /// <summary>
    /// Called to 'destroy' this object: either instantiate a fractured prefab if provided,
    /// or generate a number of procedural debris pieces (cubes), copy material, add rigidbodies
    /// and apply an explosion force.
    /// This approach works without any prefab assigned.
    /// </summary>
    public void DestroyObject()
    {
        Debug.Log($"Descrutable: DestroyObject invoked on '{name}'");

        if (fracturedPrefab != null)
        {
            InstantiateFracturedPrefab();
            return;
        }

        GenerateProceduralDebris();
    }

    private void InstantiateFracturedPrefab()
    {
        var go = Instantiate(fracturedPrefab, transform.position, transform.rotation, transform.parent);

        // Apply explosion to any rigidbodies inside the prefab
        var rbs = go.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in rbs)
        {
            rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
        }

        Destroy(go, debrisLifetime);

        if (destroyOriginal)
            Destroy(gameObject);
    }

    private void GenerateProceduralDebris()
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
        Material originalMat = (mr != null && useOriginalMaterial) ? mr.sharedMaterial : null;

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

        for (int i = 0; i < pieceCount; i++)
        {
            Vector3 desiredWorldSize = worldSizes[i]; // (width, height, depth) in world units

            // Compute desired local size relative to the object's local space so we can pick a valid inside-local position
            Vector3 desiredLocalSize = new Vector3(
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

            Vector3 worldPos = transform.TransformPoint(localRandomPos);

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
                piece.transform.SetParent(transform.parent, true);
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
            if (originalMat != null)
            {
                var rends = piece.GetComponentsInChildren<Renderer>();
                foreach (var r in rends)
                    r.sharedMaterial = originalMat;
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
            rb.mass = Mathf.Clamp((desiredWorldSize.x * desiredWorldSize.y * desiredWorldSize.z), 0.1f, 10f);
            rb.AddExplosionForce(explosionForce * Random.Range(0.8f, 1.2f), worldCenter, explosionRadius);
            rb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);

            Destroy(piece, debrisLifetime + Random.Range(0f, 2f));
        }

        // Finally remove the original object
        if (destroyOriginal)
            Destroy(gameObject);
    }

    // Create a small tetrahedron mesh (unit-sized). The returned GameObject has MeshFilter + MeshRenderer + MeshCollider.
    private GameObject CreateTetrahedron()
    {
        var go = new GameObject("Debris_Tetra");
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();

        // Simple tetrahedron vertices (roughly unit-sized, centered near origin)
        Vector3[] verts = new Vector3[] {
            new Vector3( 0.577f,  0.577f,  0.577f),
            new Vector3(-0.577f, -0.577f,  0.577f),
            new Vector3(-0.577f,  0.577f, -0.577f),
            new Vector3( 0.577f, -0.577f, -0.577f)
        };


        // Ensure winding produces outward-facing normals by using consistent triangle order
        int[] tris = new int[] {
            0, 2, 1,
            0, 1, 3,
            0, 3, 2,
            1, 2, 3
        };

        mesh.vertices = verts;
        mesh.triangles = tris;

        // Simple UVs so textures map reasonably on each face
        Vector2[] uvs = new Vector2[verts.Length];
        uvs[0] = new Vector2(0.5f, 1f);
        uvs[1] = new Vector2(0f, 0f);
        uvs[2] = new Vector2(1f, 0f);
        uvs[3] = new Vector2(0.5f, 0.33f);
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
