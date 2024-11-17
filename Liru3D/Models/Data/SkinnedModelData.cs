using Liru3D.Animations;
using System.Collections.Generic;

namespace Liru3D.Models.Data
{
    /// <summary> Represents the raw data of a model, residing within RAM (not yet uploaded to the graphics device). </summary>
    /// <remarks> Creates a new model data with the given collections. </remarks>
    /// <param name="meshes"> The collection of mesh data. </param>
    /// <param name="animations"> The collection of animations. </param>
    /// <param name="bones"> The collection of bones. </param>
    public readonly struct SkinnedModelData(IReadOnlyList<SkinnedMeshData> meshes, List<Animation> animations, IReadOnlyList<BoneData> bones)
    {
        #region Properties
        /// <summary> The collection of mesh data. </summary>
        public readonly IReadOnlyList<SkinnedMeshData> Meshes = meshes ?? throw new System.ArgumentNullException(nameof(meshes));

        /// <summary> The number of meshes in this data. </summary>
        public int MeshCount => Meshes.Count;

        /// <summary> The collection of animations. </summary>
        public readonly List<Animation> Animations = animations ?? throw new System.ArgumentNullException(nameof(animations));

        /// <summary> The number of animations in this data. </summary>
        public int AnimationCount => Animations.Count;

        /// <summary> The collection of bone data. </summary>
        public readonly IReadOnlyList<BoneData> Bones = bones;

        /// <summary> The number of bones in this data. </summary>
        public int BoneCount => Bones.Count;

        #endregion
        #region Constructors
        #endregion
    }
}
