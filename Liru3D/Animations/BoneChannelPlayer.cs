using System;
using System.Threading.Channels;
using Microsoft.Xna.Framework;

namespace Liru3D.Animations
{
    /// <summary> Handles the playback of a single bone in an <see cref="AnimationPlayer"/>. </summary>
    /// <remarks> Creates a new channel player with the given <paramref name="animationPlayer"/>. </remarks>
    /// <param name="animationPlayer"> The animation player that is using this bone channel player. </param>
    public sealed class BoneChannelPlayer(AnimationPlayer animationPlayer)
    {
        #region Dependencies
        readonly AnimationPlayer animationPlayer = animationPlayer ?? throw new System.ArgumentNullException(nameof(animationPlayer));
        #endregion

        #region Fields
        public Keyframe<Vector3> CurrentScaleFrame;

        public Keyframe<Quaternion> CurrentRotationFrame;

        public Keyframe<Vector3> CurrentPositionFrame;
        #endregion

        #region Backing Fields
        BoneChannel channel;
        public Vector3 InterpolatedScale = Vector3.One;
        public Quaternion InterpolatedRotation = Quaternion.Identity;
        public Vector3 InterpolatedPosition = Vector3.Zero;
        public Matrix InterpolatedTransform = Matrix.Identity;

        readonly object channelLock = new();
        #endregion

        #region Properties
        /// <summary> The current scale of the bone at this exact time. </summary>
        // public ref readonly Vector3 InterpolatedScale => ref interpolatedScale;

        /// <summary> The current rotation of the bone at this exact time. </summary>
        // public ref readonly Quaternion InterpolatedRotation => ref interpolatedRotation;

        /// <summary> The current position of the bone at this exact time. </summary>
        // public ref readonly Vector3 InterpolatedPosition => ref interpolatedPosition;

        /// <summary> The current transform of the bone at this exact time. </summary>
        /// <remarks> Equal to <see cref="InterpolatedScale"/> * <see cref="InterpolatedRotation"/> * <see cref="InterpolatedPosition"/> (SRT). </remarks>
        // public ref /* readonly*/ Matrix InterpolatedTransform => ref interpolatedTransform;

        /// <summary> The immutable channel that this player is reading from. </summary>
        public BoneChannel Channel
        {
            get => channel;
            set
            {
                lock (channelLock)
                {
                    // Set the channel.
                    channel = value;

                    // Start on the first frame of the channel.
                    SetFrameIndex(0);
                }
            }
        }

        #endregion
        #region Constructors
        #endregion

        #region Frame Functions
        /// <summary> Handles smooth stepping between frames, and updating the current frame. </summary>
        public void Update()
        {
            lock (channelLock)
            {
                if (animationPlayer.PlaybackDirection == 0) return;

                // Handle scale.
                if (Channel.Scales.Count > 1)
                {
                    channel.Scales.CalculateInterpolatedFrameData(animationPlayer, ref CurrentScaleFrame, out Vector3 nextValue, out float tweenScalar);
                    InterpolatedScale = Vector3.SmoothStep(CurrentScaleFrame.Value, nextValue, tweenScalar);
                }

                // Handle rotation.
                if (Channel.Rotations.Count > 1)
                {
                    channel.Rotations.CalculateInterpolatedFrameData(animationPlayer, ref CurrentRotationFrame, out Quaternion nextValue, out float tweenScalar);
                    InterpolatedRotation = Quaternion.Slerp(CurrentRotationFrame.Value, nextValue, tweenScalar);
                }

                // Handle translation.
                if (Channel.Positions.Count > 1)
                {
                    channel.Positions.CalculateInterpolatedFrameData(animationPlayer, ref CurrentPositionFrame, out Vector3 nextValue, out float tweenScalar);
                    InterpolatedPosition = Vector3.SmoothStep(CurrentPositionFrame.Value, nextValue, tweenScalar);
                }

                // Create the final interpolated transform.
                InterpolatedTransform = Matrix.CreateScale(InterpolatedScale) * Matrix.CreateFromQuaternion(InterpolatedRotation) * Matrix.CreateTranslation(InterpolatedPosition);
            }
        }

        public void SmoothStepTransforms(float tweenScalar)
        {
            lock (channelLock)
            {
                // Handle scale.
                if (channel.Scales.Count > 1 && (CurrentScaleFrame.Index + 1) < channel.Scales.Keyframes.Count)
                {
                    InterpolatedScale = Vector3.SmoothStep(CurrentScaleFrame.Value, channel.Scales.Keyframes[CurrentScaleFrame.Index + 1].Value, tweenScalar);
                }
                // Handle rotation.
                if (channel.Rotations.Count > 1 && (CurrentRotationFrame.Index + 1) < channel.Rotations.Keyframes.Count)
                {
                    InterpolatedRotation = Quaternion.Slerp(CurrentRotationFrame.Value, channel.Rotations.Keyframes[CurrentRotationFrame.Index + 1].Value, tweenScalar);
                }
                // Handle translation.
                if (channel.Positions.Count > 1 && (CurrentPositionFrame.Index + 1) < channel.Positions.Keyframes.Count)
                {
                    InterpolatedPosition = Vector3.SmoothStep(CurrentPositionFrame.Value, channel.Positions.Keyframes[CurrentPositionFrame.Index + 1].Value, tweenScalar);
                }
                // Create the final interpolated transform.
                InterpolatedTransform = Matrix.CreateScale(InterpolatedScale) * Matrix.CreateFromQuaternion(InterpolatedRotation) * Matrix.CreateTranslation(InterpolatedPosition);
            }
        }

        public void StepNextFrame()
        {
            lock (channelLock)
            {
                if (CurrentScaleFrame.Index + 1 >= (channel.Scales.Keyframes.Count - 1) &&
                    CurrentRotationFrame.Index + 1 >= (channel.Rotations.Keyframes.Count - 1) &&
                    CurrentPositionFrame.Index + 1 >= (channel.Positions.Keyframes.Count - 1))
                {
                    SetFrameIndex(0);
                    animationPlayer.UpdateTransforms();
                    return;
                }

                // Handle scale.
                if (channel.Scales.Count > 1 && (CurrentScaleFrame.Index + 1) < channel.Scales.Keyframes.Count)
                {
                    CurrentScaleFrame = channel.Scales.Keyframes[CurrentScaleFrame.Index + 1];
                }
                // Handle rotation.
                if (channel.Rotations.Count > 1 && (CurrentRotationFrame.Index + 1) < channel.Rotations.Keyframes.Count)
                {
                    CurrentRotationFrame = channel.Rotations.Keyframes[CurrentRotationFrame.Index + 1];
                }
                // Handle translation.
                if (channel.Positions.Count > 1 && (CurrentPositionFrame.Index + 1) < channel.Positions.Keyframes.Count)
                {
                    CurrentPositionFrame = channel.Positions.Keyframes[CurrentPositionFrame.Index + 1];
                }
            }
        }

        /// <summary> Resets this channel so that it is set to the first frame. </summary>
        public void SetFrameIndex(int frameIndex)
        {
            lock (channelLock)
            {
                // Is frame index out of range ?
                if (frameIndex > (channel.Scales.Keyframes.Count - 1) &&
                    frameIndex > (channel.Rotations.Keyframes.Count - 1) &&
                    frameIndex > (channel.Positions.Keyframes.Count - 1))
                {
                    return; // Frame index is out of range for each bone's channel.
                }

                // Set the current frames to the very first ones, or identity if there is no channel.
                if (frameIndex < channel.Scales.Keyframes.Count)
                {
                    CurrentScaleFrame = channel == null ? new Keyframe<Vector3>(0, 0, Vector3.One) : channel.Scales.Keyframes[frameIndex];
                }
                if (frameIndex < channel.Rotations.Keyframes.Count)
                {
                    CurrentRotationFrame = channel == null ? new Keyframe<Quaternion>(0, 0, Quaternion.Identity) : channel.Rotations.Keyframes[frameIndex];
                }
                if (frameIndex < channel.Positions.Keyframes.Count)
                {
                    CurrentPositionFrame = channel == null ? new Keyframe<Vector3>(0, 0, Vector3.Zero) : channel.Positions.Keyframes[frameIndex];
                }

                // Set the interpolated transform to that of the first frame.
                InterpolatedScale = CurrentScaleFrame.Value;
                InterpolatedRotation = CurrentRotationFrame.Value;
                InterpolatedPosition = CurrentPositionFrame.Value;
                InterpolatedTransform = Matrix.CreateScale(InterpolatedScale) * Matrix.CreateFromQuaternion(InterpolatedRotation) * Matrix.CreateTranslation(InterpolatedPosition);
            }
        }
        #endregion
    }
}
