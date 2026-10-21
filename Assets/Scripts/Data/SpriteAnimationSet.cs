using System;
using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>
    /// Mot action: day frame theo dung thu tu pipeline da xuat (idle-1 … idle-n).
    /// `loop = false` dung cho attack / hurt / death — chay het roi dung o frame cuoi.
    /// </summary>
    [Serializable]
    public class SpriteAnimationClip
    {
        public string name = "idle";
        public Sprite[] frames;
        public bool loop = true;
        [Tooltip("Frame (tinh tu 1) vu khi cham / dan roi tay — sat thuong va dan ra dung frame nay. 0 = cuoi clip.")]
        public int hitFrame;
        [Tooltip("Seconds per frame for an approved clip with its own timing. 0 uses the set FPS.")]
        public float frameSeconds;

        public bool IsValid => frames != null && frames.Length > 0;
    }

    /// <summary>
    /// Bo animation cua mot unit. Moi unit cua du an co dung 5 action: idle, walk, attack, hurt, death
    /// (quy uoc da chot: skill/FX bo sung KHONG thay the attack co ban).
    ///
    /// `framesPerSecond` mac dinh 9: pipeline xuat GIF o 110ms/frame (~9 FPS) va nguoi dung duyet
    /// animation BANG GIF do — doi FPS o day tuc la duyet lai mot thu khac voi cai da duyet.
    /// </summary>
    [CreateAssetMenu(menuName = "BuildBuild/Sprite Animation Set", fileName = "Anim_")]
    public class SpriteAnimationSet : ScriptableObject
    {
        [Tooltip("9 FPS = 110ms/frame, khop voi GIF ma nguoi dung da duyet.")]
        public float framesPerSecond = 9f;

        public SpriteAnimationClip idle = new SpriteAnimationClip { name = "idle", loop = true };
        public SpriteAnimationClip walk = new SpriteAnimationClip { name = "walk", loop = true };
        public SpriteAnimationClip attack = new SpriteAnimationClip { name = "attack", loop = false };
        public SpriteAnimationClip hurt = new SpriteAnimationClip { name = "hurt", loop = false };
        public SpriteAnimationClip death = new SpriteAnimationClip { name = "death", loop = false };
        [Tooltip("Tuy chon: than unit luc cast ky nang (Cragfist petrify, Ironblade cone-sweep). Trong thi dung attack.")]
        public SpriteAnimationClip skill = new SpriteAnimationClip { name = "skill", loop = false };

        public SpriteAnimationClip Get(string clipName)
        {
            switch (clipName)
            {
                case "walk":   return walk;
                case "attack": return attack;
                case "hurt":   return hurt;
                case "death":  return death;
                case "skill":  return skill.IsValid ? skill : attack;
                default:       return idle;
            }
        }
    }
}
