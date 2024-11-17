using System.Collections.Generic;
using Liru3D.Animations;
using Microsoft.Xna.Framework.Graphics;

namespace Liru3D.Animations;

public delegate void AnimationCreatedDel(Animation animation, ref string name, ref int durationInTicks, ref int ticksPerSecond,  ref IReadOnlyDictionary<string, BoneChannel> channelsByBoneName, ref bool blending);
