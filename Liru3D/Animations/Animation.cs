using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;

namespace Liru3D.Animations
{
    /// <summary> A collection of keyframes creating a single animation. </summary>
    /// <remarks> Note that no changes can be made to an animation at runtime. Instances of this class are loaded through assets and an AnimationPlayer instance handles runtime changes. </remarks>
    [DebuggerDisplay("{Name} with {ChannelCount} channels taking {DurationInSeconds} seconds.")]
    public sealed class Animation
    {
        #region Properties
        /// <summary> The name of this animation. </summary>
        public readonly string Name;

        /// <summary> How many ticks (frames) long this animation is. </summary>
        public readonly int DurationInTicks;

        /// <summary> How many seconds long this animation is. </summary>
        public float DurationInSeconds => (float)DurationInTicks / TicksPerSecond;

        /// <summary> How many ticks (frames) per second this animation plays at at 100% speed. </summary>
        public readonly int TicksPerSecond;

        /// <summary> The collection of bone channels keyed by bone name. </summary>
        public readonly IReadOnlyDictionary<string, BoneChannel> ChannelsByBoneName;

        /// <summary> The number of bone channels in this animation. </summary>
        public int ChannelCount => ChannelsByBoneName.Count;

        public bool Blending;

        // Via registering a method to this delegate, when they are created, you can change readonly values of this animation with send reference arguments.
        public static AnimationCreatedDel AnimationCreated;

        #endregion

        #region Constructors
        /// <summary> Creates a new animation with the given data. </summary>
        /// <param name="name"> The name of the animation. </param>
        /// <param name="ticksPerSecond"> The playback speed of the animation in ticks. </param>
        /// <param name="durationInTicks"> How long the animation is in ticks. </param>
        /// <param name="channelsByBoneName"> The collection of bone channels. </param>
        public Animation(string name, int ticksPerSecond, int durationInTicks, IReadOnlyDictionary<string, BoneChannel> channelsByBoneName, bool blending = false)
        {
            TicksPerSecond = ticksPerSecond;
            ChannelsByBoneName = channelsByBoneName;
            Name = name;
            DurationInTicks = durationInTicks;
            Blending = blending;
            AnimationCreated?.Invoke(this, ref Name, ref DurationInTicks, ref TicksPerSecond, ref ChannelsByBoneName, ref Blending);
        }
        #endregion
    }
}
