//
// Bulk mesh handler
//
// Duplicate and combine given meshes into a single mesh. It duplicates the
// meshes until the number of vertices reaches to 64k or the number of copies
// reaches to 4k.
//
using UnityEngine;

namespace Kvant
{
    public partial class Spray
    {
        [System.Serializable]
        class BulkMesh
        {
            #region Properties

            // Single combined mesh.
            Mesh _mesh;
            public Mesh mesh { get { return _mesh; } }

            // Copy count.
            int _copyCount;
            public int copyCount { get { return _copyCount; } }

            #endregion

            #region Public Methods

            public BulkMesh(Mesh[] shapes)
            {
                CombineMeshes(shapes);
            }

            public void Rebuild(Mesh[] shapes)
            {
                Release();
                CombineMeshes(shapes);
            }

            public void Release()
            {
                if (_mesh)
                {
                    DestroyImmediate(_mesh);
                    _copyCount = 0;
                }
            }

            #endregion

            #region Private Methods

            // Cache structure which stores shape information
            struct ShapeCacheData
            {
                Vector3[] vertices;
                Vector3[] normals;
                Vector4[] tangents;
                Vector2[] uv;
                int[] indices;

                public ShapeCacheData(Mesh mesh)
                {
                    if (mesh)
                    {
                        vertices = mesh.vertices;
                        normals  = mesh.normals;
                        tangents = mesh.tangents;
                        uv       = mesh.uv;
                        indices  = mesh.GetIndices(0);
                    }
                    else
                    {
                        // An empty mesh was given; replaces with a two-sided quad.
                        vertices = new Vector3[] {
                            new Vector3 (-1, +1, 0), new Vector3 (+1, +1, 0),
                            new Vector3 (-1, -1, 0), new Vector3 (+1, -1, 0),
                            new Vector3 (+1, +1, 0), new Vector3 (-1, +1, 0),
                            new Vector3 (+1, -1, 0), new Vector3 (-1, -1, 0)
                        };
                        normals = new Vector3[] {
                             Vector3.forward,  Vector3.forward,
                             Vector3.forward,  Vector3.forward,
                            -Vector3.forward, -Vector3.forward,
                            -Vector3.forward, -Vector3.forward,
                        };
                        tangents = new Vector4[] {
                            new Vector4( 1, 0, 0, 1), new Vector4( 1, 0, 0, 1),
                            new Vector4( 1, 0, 0, 1), new Vector4( 1, 0, 0, 1),
                            new Vector4(-1, 0, 0, 1), new Vector4(-1, 0, 0, 1),
                            new Vector4(-1, 0, 0, 1), new Vector4(-1, 0, 0, 1)
                        };
                        uv = new Vector2[] {
                            new Vector2(0, 1), new Vector2(1, 1),
                            new Vector2(0, 0), new Vector2(1, 0),
                            new Vector2(1, 1), new Vector2(0, 1),
                            new Vector2(1, 0), new Vector2(0, 0)
                        };
                        indices = new int[] {0, 1, 2, 3, 2, 1, 4, 5, 6, 7, 6, 5};
                    }
                }

                public int VertexCount { get { return vertices.Length; } }
                public int IndexCount { get { return indices.Length; } }

                public void CopyVerticesTo(Vector3[] destination, int position)
                {
                    System.Array.Copy(vertices, 0, destination, position, vertices.Length);
                }

                public void CopyNormalsTo(Vector3[] destination, int position)
                {
                    System.Array.Copy(normals, 0, destination, position, normals.Length);
                }

                public void CopyTangentsTo(Vector4[] destination, int position)
                {
                    System.Array.Copy(tangents, 0, destination, position, tangents.Length);
                }

                public void CopyUVTo(Vector2[] destination, int position)
                {
                    System.Array.Copy(uv, 0, destination, position, uv.Length);
                }

                public void CopyIndicesTo(int[] destination, int position, int offset)
                {
                    for (var i = 0; i < indices.Length; i++)
                        destination[position + i] = offset + indices[i];
                }
            }

            // Mesh combiner functoin
            void CombineMeshes(Mesh[] shapes)
            {
                ShapeCacheData[] cache;

                if (shapes == null || shapes.Length == 0)
                {
                    // The shape array is empty; use the default shape.
                    cache = new ShapeCacheData[1];
                    cache[0] = new ShapeCacheData(null);
                }
                else
                {
                    // Store the meshes into the shape cache.
                    cache = new ShapeCacheData[shapes.Length];
                    for (var i = 0; i < shapes.Length; i++)
                        cache[i] = new ShapeCacheData(shapes[i]);
                }

                // Count the number of vertices and indices in the shape cache.
                var vc_shapes = 0;
                var ic_shapes = 0;
                foreach (var s in cache) {
                    vc_shapes += s.VertexCount;
                    ic_shapes += s.IndexCount;
                }

                // If there is nothing, break.
                if (vc_shapes == 0) return;

                // Determine the number of copies.
                // - The number of vertices must be less than 64k.
                // - The number of copies must be less than 4096.
                var vc = 0;
                var ic = 0;
                for (_copyCount = 0; _copyCount < 4096; _copyCount++)
                {
                    var s = cache[_copyCount % cache.Length];
                    if (vc + s.VertexCount > 65535) break;
                    vc += s.VertexCount;
                    ic += s.IndexCount;
                }

                // Create vertex arrays.
                var vertices = new Vector3[vc];
                var normals  = new Vector3[vc];
                var tangents = new Vector4[vc];
                var uv       = new Vector2[vc];
                var uv2      = new Vector2[vc];
                var indicies = new int[ic];

                for (int v_i = 0, i_i = 0, e_i = 0; v_i < vc; e_i++)
                {
                    var s = cache[e_i % cache.Length];

                    s.CopyVerticesTo(vertices, v_i);
                    s.CopyNormalsTo (normals,  v_i);
                    s.CopyTangentsTo(tangents, v_i);
                    s.CopyUVTo      (uv,       v_i);
                    s.CopyIndicesTo (indicies, i_i, v_i);

                    var coord = new Vector2((float)e_i / _copyCount, 0);
                    for (var i = 0; i < s.VertexCount; i++) uv2[v_i + i] = coord;

                    v_i += s.VertexCount;
                    i_i += s.IndexCount;
                }

                // Create a mesh object.
                _mesh = new Mesh();

                _mesh.vertices = vertices;
                _mesh.normals  = normals;
                _mesh.tangents = tangents;
                _mesh.uv       = uv;
                _mesh.uv2      = uv2;

                _mesh.SetIndices(indicies, MeshTopology.Triangles, 0);
                _mesh.Optimize();

                // This only for temporary use. Don't save.
                _mesh.hideFlags = HideFlags.DontSave;

                // Avoid being culled.
                _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000);
            }

            #endregion
        }
    }
}
