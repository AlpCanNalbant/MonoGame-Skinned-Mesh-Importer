using Liru3D.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Liru3D.Animations
{
    /// <summary> Stores a collection of transforms for a <see cref="SkinnedModel"/> and manipulates them by playing an <see cref="Animation"/>. </summary>
    public abstract class AnimationPlayer
    {
        #region Backing Fields
        public readonly Matrix[] ModelSpaceTransforms;

        public readonly Matrix[] BoneSpaceTransforms;

        private Matrix boneLocalTransform;

        private SkinnedModel model;

        /// <summary> Keeps track of each bone's current frame. </summary>
        public readonly BoneChannelPlayer[] ChannelPlayers;
        public readonly BoneChannelPlayer[] OldChannelPlayers;

        private Animation animation;

        private float currentTime;

        private float currentTick;

        private bool isPlaying = false;

        private bool isInterpolatedToNextAnimation;

        private float blendingTweenScalar;

        // private readonly object channelPlayersLock = new();
        #endregion

        #region Properties
        /// <summary> The current model that is being animated. </summary>
        public SkinnedModel Model
        {
            get => model;
            set
            {
                // If the given model is the same as the current model, do nothing.
                if (value == model) return;

                // If the given model is null, throw an exception.
                if (value == null) throw new Exception("Cannot set an animation's model to null.");

                // If the given model's bone count does not match the existing bone count, throw an exception.
                if (model != null && value.BoneCount != model.BoneCount) throw new Exception("Cannot switch the model of an animation player to a model with a different number of bones.");

                // Set the model.
                model = value;
            }
        }

        /// <summary> The current animation that is being played. </summary>
        public Animation Animation
        {
            get => animation;
            set
            {
                // lock (channelPlayersLock)
                // {
                // Do nothing if there is no change.
                if (Animation == value) return;

                // Set the animation.
                animation = value;

                // Get the channel from the name of the indexed bones. This ensures that the collection of current frames is in the same order as the bones.
                if (animation.Blending && (ChannelPlayers[0].Channel != null))
                {
                    for (int i = 0; i < Model.BoneCount; i++)
                    {
                        OldChannelPlayers[i].Channel = BoneChannel.Clone(ChannelPlayers[i].Channel);
                        ChannelPlayers[i].Channel = animation?.ChannelsByBoneName[Model.Bones[i].Name];
                    }
                    isInterpolatedToNextAnimation = false;
                }
                else
                {
                    for (int i = 0; i < Model.BoneCount; i++)
                    {
                        ChannelPlayers[i].Channel = animation?.ChannelsByBoneName[Model.Bones[i].Name];
                    }
                    isInterpolatedToNextAnimation = true;
                }

                // Update the transforms so that the animation is in its first frame.
                if ((!animation.Blending) || (animation.Blending && isInterpolatedToNextAnimation))
                {
                    UpdateTransforms();
                }

                // Reset the time.
                CurrentTime = 0f;

                // Stop playback.
                IsPlaying = false;
                // }
            }
        }

        /// <summary> The current time of this animation in seconds. </summary>
        public float CurrentTime
        {
            get => currentTime;
            set
            {
                currentTime = value;
                currentTick = (Animation != null) ? (Animation.TicksPerSecond * value) : value;
            }
        }

        /// <summary> The current whole tick of the animation. This is rounded down if the playback is forwards, and rounded up if it is backwards. </summary>
        public int CurrentWholeTick => (int)(PlaybackDirection == 1 ? Math.Floor(CurrentTick) : Math.Ceiling(CurrentTick));

        /// <summary> The current tick or "frame" of the animation. </summary>
        public float CurrentTick
        {
            get => currentTick;
            set
            {
                currentTick = value;
                currentTime = currentTick / Animation.DurationInTicks;
            }
        }

        /// <summary> Gets the direction of playback, which is <c>1</c> when playing forward, <c>-1</c> when playing backward, and <c>0</c> when <see cref="PlaybackSpeed"/> is <c>0</c>. </summary>
        /// <remarks> Note that this does not take <see cref="IsPlaying"/> into account, only <see cref="PlaybackDirection"/>. </remarks>
        public int PlaybackDirection => Math.Sign(PlaybackSpeed);

        /// <summary> The speed multiplier of the playback, where <c>1.0f</c> is 100% at normal speed, and <c>-1.0f</c> is 100% at reverse speed. </summary>
        public float PlaybackSpeed = 1f;
        public float BlendingSpeed = 2f;

        /// <summary> Gets or sets the value which determines if this animation is currently playing. </summary>
        public bool IsPlaying
        {
            get => isPlaying;
            set
            {
                // Do nothing if no change is given.
                if (isPlaying == value) return;

                // If there is no animation, do nothing.
                if (Animation == null) return;

                // Set the value.
                isPlaying = value;

                // If playback has started but the animation is finished, restart it.
                if (value && ((CurrentTime >= animation.DurationInSeconds) || (CurrentTime <= 0f)))
                {
                    CurrentTime = (PlaybackDirection == 1) ? 0f : Animation.DurationInSeconds;
                }
            }
        }

        /// <summary> Is <c>true</c> if this animation should loop; otherwise <c>false</c>. </summary>
        public bool IsLooping;

        /// <summary> The transform of each bone relative to the mesh. </summary>
        /// <remarks>
        /// This can be used to position objects around the bones, for example; a character holding an item.
        /// This collection is created in the same order as the <see cref="SkinnedModel.Bones"/> collection.
        /// </remarks>
        // public IReadOnlyList<Matrix> ModelSpaceTransforms => ModelSpaceTransforms;

        /// <summary> The transform of each bone in bone-space. </summary>
        /// <remarks>
        /// This is uploaded to the GPU in order to draw the skinned mesh.
        /// This collection is created in the same order as the <see cref="SkinnedModel.Bones"/> collection.
        /// </remarks>
        // public IReadOnlyList<Matrix> BoneSpaceTransforms => BoneSpaceTransforms;

        public ref readonly Matrix BoneLocalTransform => ref boneLocalTransform;

        public InterpolatedToNextAnimationDel InterpolatedToNextAnimation;
        public EndGame.AnimationEndDel AnimationEnd
        {
            get => animationEnd;
            set
            {
                if (animationEnd == null)
                {
                    animationEnd = value;
                }
                else
                {
                    animationEnd += value;
                }
            }
        }
        EndGame.AnimationEndDel animationEnd;
        #endregion

        #region Constructors
        /// <summary> Creates a new animation player based on the given <paramref name="model"/>. </summary>
        /// <param name="model"> The model that is to be animated. </param>
        public AnimationPlayer(SkinnedModel model)
        {
            // Set the mesh.
            Model = model ?? throw new ArgumentNullException(nameof(model));

            // Set up the transform arrays.
            ModelSpaceTransforms = new Matrix[model.BoneCount];
            BoneSpaceTransforms = new Matrix[model.BoneCount];

            // Set up the current frames.
            ChannelPlayers = new BoneChannelPlayer[model.BoneCount];
            OldChannelPlayers = new BoneChannelPlayer[model.BoneCount];
            for (int i = 0; i < Model.BoneCount; i++)
            {
                ChannelPlayers[i] = new BoneChannelPlayer(this);
                OldChannelPlayers[i] = new BoneChannelPlayer(this);
            }
        }
        #endregion

        #region Update Functions
        /// <summary> Updates the current animation so that it plays, if <see cref="IsPlaying"/> is <c>true</c>. </summary>
        /// <param name="gameTime"> The data used for timing. </param>
        protected void UpdateCurrentAnimation(in GameTime gameTime)
        {
            // lock (channelPlayersLock)
            // {
            // If there is no animation or the animation is not playing, do nothing.
            if (!IsPlaying || Animation == null) return;

            if (isInterpolatedToNextAnimation)
            {
                // Update the current frame, which handles interpolation and frame changes of all channels.
                for (int i = 0; i < Model.BoneCount; i++)
                {
                    ChannelPlayers[i].Update();
                    UpdateTransformAt(i);
                }

                // Update the current time.
                CurrentTime += (float)gameTime.ElapsedGameTime.TotalSeconds * PlaybackSpeed;

                // Handle the playback going out of bounds, either starting over or stopping playback.
                if ((CurrentTime >= animation.DurationInSeconds) || (CurrentTime <= 0f))
                {
                    AnimationEnd?.Invoke(this, animation);
                    if (IsLooping)
                    {
                        CurrentTime -= Animation.DurationInSeconds * PlaybackDirection;
                    }
                    else
                    {
                        IsPlaying = false;
                    }
                }
            }
            else // Handle the interpolation of animation blending.
            {
                for (int i = 0; i < Model.BoneCount; i++)
                {
                    // if (PlaybackDirection == 0) break;

                    // Handle scale.
                    // if (ChannelPlayers[i].Channel.Scales.Count > 1)
                    {
                        ChannelPlayers[i].InterpolatedScale = Vector3.Lerp(OldChannelPlayers[i].CurrentScaleFrame.Value, ChannelPlayers[i].CurrentScaleFrame.Value, blendingTweenScalar);
                    }

                    // Handle rotation.
                    // if (ChannelPlayers[i].Channel.Rotations.Count > 1)
                    {
                        ChannelPlayers[i].InterpolatedRotation = Quaternion.Slerp(OldChannelPlayers[i].CurrentRotationFrame.Value, ChannelPlayers[i].CurrentRotationFrame.Value, blendingTweenScalar);
                    }

                    // Handle translation.
                    // if (ChannelPlayers[i].Channel.Positions.Count > 1)
                    {
                        ChannelPlayers[i].InterpolatedPosition = Vector3.Lerp(OldChannelPlayers[i].CurrentPositionFrame.Value, ChannelPlayers[i].CurrentPositionFrame.Value, blendingTweenScalar);
                    }

                    // Create the final interpolated transform.
                    ChannelPlayers[i].InterpolatedTransform = Matrix.CreateScale(ChannelPlayers[i].InterpolatedScale) * Matrix.CreateFromQuaternion(ChannelPlayers[i].InterpolatedRotation) * Matrix.CreateTranslation(ChannelPlayers[i].InterpolatedPosition);

                    UpdateTransformAt(i);
                }

                blendingTweenScalar += (float)gameTime.ElapsedGameTime.TotalSeconds * PlaybackSpeed * BlendingSpeed;

                if (blendingTweenScalar > 1f)
                {
                    blendingTweenScalar = 0f;
                    isInterpolatedToNextAnimation = true;
                    InterpolatedToNextAnimation?.Invoke(this, Animation, Animation.Name);
                }
            }

            // Update the transform collections for model and bone space.
            // UpdateTransforms();
            // }
        }

        public void UpdateNextFrame(in GameTime gameTime)
        {
            // lock (channelPlayersLock)
            // {
            // If there is no animation, the currentTime which is the tween scalar in this case finished its job or the animation is not playing, do nothing.
            if (!isPlaying || animation == null)
                return;
            currentTime += (float)gameTime.ElapsedGameTime.TotalSeconds * PlaybackSpeed;
            isPlaying = currentTime < 1f;

            for (int i = 0; i < Model.BoneCount; i++)
            {
                ChannelPlayers[i].SmoothStepTransforms(currentTime);
                UpdateTransformAt(i);
            }

            // Update the transform collections for model and bone space.
            // UpdateTransforms();
            // }
        }
        public bool TryStepNextFrame()
        {
            // lock (channelPlayersLock)
            // {
            // If there is no animation or the interpolation not done yet because of that do nothing.
            if (currentTime < 1f || animation == null)
                return false;
            // Update the current frame, which handles interpolation and frame changes of all channels.
            for (int i = 0; i < Model.BoneCount; i++)
            {
                ChannelPlayers[i].StepNextFrame();
            }
            currentTime = 0f;
            return true;
            // }
        }
        public bool SetFrame(int frameIndex)
        {
            // lock (channelPlayersLock)
            // {
            // If there is no animation do nothing.
            if (animation == null)
                return false;
            // Update the current frame, which handles interpolation and frame changes of all channels.
            for (int i = 0; i < Model.BoneCount; i++)
            {
                ChannelPlayers[i].SetFrameIndex(frameIndex);
                UpdateTransformAt(i);
            }
            // UpdateTransforms();
            return true;
            // }
        }
        #endregion

        #region Bone Functions
        /// <summary> Calls <see cref="SkinnedEffect.SetBoneTransforms(Matrix[])"/> with the bone space transforms. </summary>
        /// <param name="skinnedEffect"> The effect to set the bones of. Has no effect if this is <c>null</c>. </param>
        public void SetEffectBones(SkinnedEffect skinnedEffect) => skinnedEffect?.SetBoneTransforms(BoneSpaceTransforms);

        /// <summary> Invokes the given <paramref name="setFunction"/> with the bone space transforms collection. </summary>
        /// <param name="setFunction"> The function that sets the bone space transforms of whatever needs them. </param>
        public void SetEffectBones(Action<Matrix[]> setFunction) => setFunction?.Invoke(BoneSpaceTransforms);

        public void SetBoneTransform(int boneIndex, in Vector3 position, in Quaternion rotation, in Vector3 scale)
        {
            var transform = Matrix.CreateScale(scale) * Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(position);
            Bone bone = Model.Bones[boneIndex];
            // Set the transforms.
            ModelSpaceTransforms[boneIndex] = bone.HasParent ? transform * ModelSpaceTransforms[bone.Parent.Index] : transform;
            BoneSpaceTransforms[boneIndex] = bone.Offset * ModelSpaceTransforms[boneIndex];
        }

            // Bone Index 1
            // Vector3 position     =>  // 0f -3.2666676f 0f
            // ModelSpaceTransforms =>  // 0f -3.2666676f 0f
            // BoneSpaceTransforms  =>  // 0f -3.2666676f 0f

        public void SetBonePosition(int boneIndex, in Vector3 position)
        {
            var transform = Matrix.CreateTranslation(position);
            Bone bone = Model.Bones[boneIndex];
            // Set the transforms.
            ModelSpaceTransforms[boneIndex] = bone.HasParent ? transform * ModelSpaceTransforms[bone.Parent.Index] : transform;
            BoneSpaceTransforms[boneIndex] = bone.Offset * ModelSpaceTransforms[boneIndex];
        }
        public void SetBoneRotation(int boneIndex, in Quaternion rotation)
        {
            var transform = Matrix.CreateFromQuaternion(rotation);
            Bone bone = Model.Bones[boneIndex];
            // Set the transforms.
            ModelSpaceTransforms[boneIndex] = bone.HasParent ? transform * ModelSpaceTransforms[bone.Parent.Index] : transform;
            BoneSpaceTransforms[boneIndex] = bone.Offset * ModelSpaceTransforms[boneIndex];
        }
        public void SetBoneScale(int boneIndex, in Vector3 scale)
        {
            var transform = Matrix.CreateScale(scale);
            Bone bone = Model.Bones[boneIndex];
            // Set the transforms.
            ModelSpaceTransforms[boneIndex] = bone.HasParent ? transform * ModelSpaceTransforms[bone.Parent.Index] : transform;
            BoneSpaceTransforms[boneIndex] = bone.Offset * ModelSpaceTransforms[boneIndex];
        }
        // public ref Matrix BoneTransformAt(int boneIndex)
        //     => ref ModelSpaceTransforms[boneIndex];
        // public ref readonly Matrix GetBoneTransform(int boneIndex)
        //     => ref ModelSpaceTransforms[boneIndex];
        // public Vector3 GetBonePosition(int boneIndex)
        //     => ModelSpaceTransforms[boneIndex].Translation;

        public void UpdateTransforms()
        {
            // Update each bone's transform.
            for (int i = 0; i < Model.BoneCount; i++)
            {
                UpdateTransformAt(i);
            }
        }
        public void UpdateTransformAt(int i)
        {
            // Get the current bone.
            Bone bone = Model.Bones[i];
            // Get the local interpolated transform of the bone.
            boneLocalTransform = ChannelPlayers[i].InterpolatedTransform;
            // Set the transforms.
            ModelSpaceTransforms[i] = bone.HasParent ? boneLocalTransform * ModelSpaceTransforms[bone.Parent.Index] : boneLocalTransform;
            BoneSpaceTransforms[i] = bone.Offset * ModelSpaceTransforms[i];
        }

        public void UpdateTransformAt(int i, in Matrix boneLocalTransform, in Matrix modelSpaceTransform, in Matrix boneSpaceTransform)
        {
            this.boneLocalTransform = boneLocalTransform;
            ModelSpaceTransforms[i] = modelSpaceTransform;
            BoneSpaceTransforms[i] = boneSpaceTransform;
        }
        public void UpdateTransformAtIgnoreParent(int i)
            => BoneSpaceTransforms[i] = Model.Bones[i].Offset * (ModelSpaceTransforms[i] = boneLocalTransform = ChannelPlayers[i].InterpolatedTransform);

        #endregion
    }
}
