using BuildBuild.Core;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Tieng don CAN CHIEN: nghe `Health.AnyDamaged` chung (giong `CombatFeedback`) nen moi hero / linh / quai danh gan
    /// tu co tieng, khong nguon nao phai goi them. Don tu xa (dan hero, phao, dan quai) KHONG phat o day — `Projectile`
    /// phat tieng trung rieng theo `impactSfx` cua hero/phao, phat ca hai noi thi moi mui ten keu hai lan.
    /// Thanh trung don (bat ke nguon) keu `castle-hit`.
    /// </summary>
    static class CombatAudio
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Hook()
        {
            Health.AnyDamaged -= OnDamaged;
            Health.AnyDamaged += OnDamaged;
        }

        static void OnDamaged(Health target, Health source, float amount, float shieldAbsorbed, Health shieldCaster)
        {
            if (amount <= 0f && shieldAbsorbed <= 0f) return;
            if (target.GetComponent<Castle>() != null)
            {
                Sfx.Play("castle-hit");
                return;
            }
            if (source == null) return;     // phao / don khong chu: tieng nam o Projectile
            var attacker = source.GetComponent<Unit>();
            if (attacker is Hero hero && !hero.IsRangedUnit) Sfx.Play("hit-melee");
            else if (attacker is Enemy enemy && !enemy.Definition.IsRanged) Sfx.Play("hit-blunt");
        }
    }
}
