using UnityEngine;
namespace BuildBuild.Data
{
    public class BloodswordsmanKit : ScriptableObject
    {
        public float offerCost = 0.15f, offerDuration = 6f, offerCooldown = 10f, offerPower = 0.4f;
        public float parryThreshold = 0.4f, parryDuration = 1.5f, parryCooldown = 8f;
        public float shatterCost = 0.2f, shardDelay = 0.8f, shardRange = 2.5f;
        public SpriteAnimationClip offer = new SpriteAnimationClip { name = "skill", loop = false };
        public SpriteAnimationClip parry = new SpriteAnimationClip { name = "skill", loop = false };
        public Sprite[] aura, parryHit, shardOut, shardStuck, shardReturn, reform;
    }
}
