using Microsoft.Xna.Framework;
using System.Linq;

namespace Liru3D.Models.Data
{
    /// <summary> Represents the data of a single skinned mesh residing within RAM (has not been uploaded to the graphics device). </summary>
    /// <remarks> Creates a new data with the given name and collections. </remarks>
    /// <param name="name"> The name of the mesh. </param>
    /// <param name="vertices"> The collection of vertices. </param>
    /// <param name="indices"> The collection of indices. </param>
    public readonly struct SkinnedMeshData(string name, SkinnedVertex[] vertices, int[] indices)
    {
        #region Properties
        /// <summary> The name of the mesh. </summary>
        public readonly string Name = name;

        /// <summary> The collection of vertices. Each vertex within this collection holds multiple pieces of data, see <see cref="SkinnedVertex"/> for more. </summary>
        public readonly SkinnedVertex[] Vertices = vertices;

        /// <summary> The number of vertices in this data. </summary>
        public int VertexCount => Vertices == null ? 0 : Vertices.Length;

        /// <summary> The collection of indices. </summary>
        public readonly int[] Indices = indices;

        /// <summary> The number of indices in this data. </summary>
        public int IndexCount => Indices == null ? 0 : Indices.Length;

        #endregion
        #region Constructors
        #endregion

        #region Bounding Functions
        /// <summary> Calculates a bounding sphere for the data's vertices. </summary>
        /// <returns> The calculated bounding sphere. </returns>
        public BoundingSphere CalculateBoundingSphere()
            => VertexCount == 0 ? new BoundingSphere() : BoundingSphere.CreateFromPoints(Vertices.Select(v => v.Position));
        #endregion
    }
}
