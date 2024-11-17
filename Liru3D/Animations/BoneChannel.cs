using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Diagnostics;

namespace Liru3D.Animations
{
    /// <summary> Hey I'm Laura from Lovebirb, and you're watching the bone channel. This class holds collections of scale, rotation, and position frames for a specific bone. </summary>
    [DebuggerDisplay("Channel for {BoneName} bone with {Scales.Count} scales, {Rotations.Count} rotations, and {Positions.Count} positions")]
    public sealed class BoneChannel
    {
        #region Properties
        /// <summary> The name of the bone that this channel is for. </summary>
        public readonly string BoneName;

        /// <summary> The scales channel for the bone. </summary>
        public readonly ChannelComponent<Vector3> Scales;

        /// <summary> The rotations channel for the bone. </summary>
        public readonly ChannelComponent<Quaternion> Rotations;

        /// <summary> The positions channel for the bone. </summary>
        public readonly ChannelComponent<Vector3> Positions;
        #endregion

        #region Constructors
        /// <summary> Creates a new channel with the given animation parameters. </summary>
        /// <param name="boneName"> The name of the bone. </param>
        /// <param name="scaleFrames"> The scale frames. </param>
        /// <param name="rotationFrames"> The rotation frames. </param>
        /// <param name="positionFrames"> The position frames. </param>
        public BoneChannel(string boneName, IReadOnlyList<Keyframe<Vector3>> scaleFrames, IReadOnlyList<Keyframe<Quaternion>> rotationFrames, IReadOnlyList<Keyframe<Vector3>> positionFrames)
        {
            // Set the name.
            BoneName = boneName;

            // Create the channels.
            Scales = new ChannelComponent<Vector3>(scaleFrames);
            Rotations = new ChannelComponent<Quaternion>(rotationFrames);
            Positions = new ChannelComponent<Vector3>(positionFrames);
        }
        public BoneChannel(string boneName, ChannelComponent<Vector3> scales, ChannelComponent<Quaternion> rotations, ChannelComponent<Vector3> positions)
        {
            // Set the name.
            BoneName = boneName;

            // Assign the channels.
            Scales = scales;
            Rotations = rotations;
            Positions = positions;
        }

        public static BoneChannel Clone(BoneChannel other)
            => new(other.BoneName, ChannelComponent<Vector3>.Clone(other.Scales), ChannelComponent<Quaternion>.Clone(other.Rotations), ChannelComponent<Vector3>.Clone(other.Positions));
        #endregion
    }
}
