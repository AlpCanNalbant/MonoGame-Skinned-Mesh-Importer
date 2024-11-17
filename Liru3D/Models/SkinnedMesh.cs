using Liru3D.Models.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EndGame.Utility;

namespace Liru3D.Models
{
    /// <summary> A single mesh of a <see cref="SkinnedModel"/>. </summary>
    public sealed class SkinnedMesh
    {
        #region Backing Fields
        private string name;
        private BoundingSphere boundingSphere;
        #endregion

        #region Dependencies
        private readonly GraphicsDevice graphicsDevice;
        #endregion

        #region Properties
        /// <summary> The name of the mesh. </summary>
        public string Name => name;

        /// <summary> The vertex buffer object that contains the vertex data of this mesh. </summary>
        public readonly VertexBuffer VertexBuffer;

        /// <summary> The index buffer object that contains the index data of this mesh. </summary>
        public readonly IndexBuffer IndexBuffer;

        /// <summary> The bounding sphere of the mesh without any animations applied. </summary>
        public ref readonly BoundingSphere BoundingSphere => ref boundingSphere;
        #endregion

        #region Constructors
        private SkinnedMesh(GraphicsDevice graphicsDevice, VertexBuffer vertexBuffer, IndexBuffer indexBuffer, BoundingSphere boundingSphere, string name)
        {
            this.graphicsDevice = graphicsDevice ?? throw new System.ArgumentNullException(nameof(graphicsDevice));
            VertexBuffer = vertexBuffer ?? throw new System.ArgumentNullException(nameof(vertexBuffer));
            IndexBuffer = indexBuffer ?? throw new System.ArgumentNullException(nameof(indexBuffer));
            this.boundingSphere = boundingSphere;
            this.name = name;
        }
        #endregion

        #region Data Functions
        /// <summary>
        /// Updates this mesh's data from the given data.
        /// </summary>
        /// <param name="data"> The data object holding the new data to use. </param>
        public void UpdateDataFrom(SkinnedMeshData data)
        {
            // Set the data from the given data object.
            name = data.Name ?? name;
            if (data.VertexCount > 0)
            {
                VertexBuffer.SetData(data.Vertices);
                boundingSphere = data.CalculateBoundingSphere();
            }
            if (data.IndexCount > 0) IndexBuffer.SetData(data.Indices);
        }
        #endregion

        #region Creation Functions
        /// <summary> Creates and returns a new skinned mesh from the given <paramref name="data"/>, uploaded onto the given <paramref name="graphicsDevice"/>. </summary>
        /// <param name="graphicsDevice"> The graphics device onto which the mesh will be uploaded. </param>
        /// <param name="data"> The mesh data. </param>
        /// <returns> The created skinned mesh. </returns>
        public static SkinnedMesh CreateFrom(GraphicsDevice graphicsDevice, SkinnedMeshData data)
        {
            // Create a vertex buffer.
            var vertexBuffer = new VertexBuffer(graphicsDevice, SkinnedVertex.VertexDeclaration, data.Vertices.Length * SkinnedVertex.VertexDeclaration.VertexStride, BufferUsage.None);
            vertexBuffer.SetData(0, data.Vertices, 0, data.Vertices.Length, SkinnedVertex.VertexDeclaration.VertexStride);

            // Create an index buffer.
            var indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, data.Indices.Length, BufferUsage.None);
            indexBuffer.SetData(data.Indices);

            // Create the skinned mesh using the created data.
            var skinnedMesh = new SkinnedMesh(graphicsDevice, vertexBuffer, indexBuffer, data.CalculateBoundingSphere(), data.Name);

            // Return the created mesh.
            return skinnedMesh;
        }
        #endregion

        #region Draw Functions
        /// <summary> Draws this mesh. </summary>
        public void Draw()
        {
            graphicsDevice.SetVertexBuffer(VertexBuffer);
            graphicsDevice.Indices = IndexBuffer;
            graphicsDevice.DrawIndexedTriangle(0, 0, VertexBuffer.VertexCount);
        }
        #endregion
    }
}
