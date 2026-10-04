using UnityEngine;
namespace BuildBuild.Data
{
    public class SoulbinderKit : ScriptableObject
    {
        public float linkCooldown = 10f, ripCooldown = 10f, linkRadius = 2.5f;
        public float minorDuration = 6f, minorShare = 0.25f, pactDuration = 8f, pactShare = 0.4f;
        public SpriteAnimationClip link = new SpriteAnimationClip { name = "skill", loop = false };
        public SpriteAnimationClip rip = new SpriteAnimationClip { name = "skill", loop = false };
        public Sprite[] marker, pactMarker, segment, fade, transfer, fragment, ripHit, pactHit;
    }
}
