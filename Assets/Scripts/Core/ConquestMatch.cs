using BuildBuild.Combat;
using BuildBuild.Meta;
using UnityEngine;

namespace BuildBuild.Core
{
    /// <summary>
    /// Tran Chinh Phat (GAME_DESIGN.md muc 8): hai thanh doi xung. Lop nay chi dung THANH NPC — nhan ban phao dai minh,
    /// lat guong qua tam man, phu do — va doi bien phai cua san ve vach thanh do. Hero / phao / linh phe NPC do tung bang
    /// (`HeroBoard`, `CannonBoard`, `SoldierCamp`) tu dung khi nghe `WaveStarted` luc `WaveManager.InConquest`, dat theo
    /// `Mirror`. Thang / thua do `WaveManager` xu.
    /// </summary>
    public sealed class ConquestMatch
    {
        /// <summary>Tran Chinh Phat dang danh; null ngoai tran.</summary>
        public static ConquestMatch Current { get; private set; }

        /// <summary>Cung mau phu than hero NPC (`Hero.NpcTint`).</summary>
        static readonly Color RivalTint = new Color(1f, 0.55f, 0.55f);

        /// <summary>
        /// He so mau cho MOI unit hai phe + ca hai thanh trong Chinh Phat (TAM, nguoi dung chot 2026-10-02: tran qua nhanh).
        /// Doi xung nen ti le luc chien khong doi; tran dai ra va thanh mana kip day de chieu cuoi duoc tung.
        /// </summary>
        public const float HealthScale = 2.5f;

        /// <summary>
        /// Mau toi da de tinh hieu ung "% mau toi da" (hoi / sat thuong): bo phan `HealthScale` di. Khong bo thi % hoi mau
        /// (Ngo Khong) va % sat thuong (Wyrmspear) to len 2,5 lan trong khi don thuong giu nguyen — manh gap 2,5 lan so
        /// voi phan con lai. Hoi / khien / hut mau so PHANG thi giu nguyen: ti le voi sat thuong vao khong doi.
        /// </summary>
        public static float PercentBase(float maxHealth) => Current != null ? maxHealth / HealthScale : maxHealth;

        public Castle Rival { get; }
        public Castle Home => _home;
        /// <summary>Thanh chon tren ban do (doi hinh + suc manh NPC); null = Admin vao thang, NPC boc ngau nhien chi so nhu minh.</summary>
        public ConquestTarget Target { get; }
        readonly float _axis;
        readonly Castle _home;
        readonly float _homeMax;

        ConquestMatch(Castle home, ConquestTarget target)
        {
            Target = target;
            _home = home;
            _homeMax = home.Health.Max;
            ConquestArena.Show(true);   // camera ve tam nen Chinh Phat truoc: truc doi xung lay theo no
            _axis = Camera.main != null ? Camera.main.transform.position.x : 0f;

            var go = Object.Instantiate(home.gameObject, Mirror(home.transform.position), Quaternion.identity);
            go.name = "RivalCastle";
            // Chi giu than phao dai: be phao cua minh nam trong thanh, ban sao bam vao se mo bang chon phao cua minh.
            foreach (Transform child in go.transform)
                if (child.name != "Body")
                {
                    child.gameObject.SetActive(false);
                    Object.Destroy(child.gameObject);
                }

            var body = go.transform.Find("Body").GetComponent<SpriteRenderer>();
            var local = body.transform.localPosition;
            body.transform.localPosition = new Vector3(-local.x, local.y, local.z);
            body.flipX = true;
            body.color = RivalTint;

            Rival = go.GetComponent<Castle>();
            Rival.RefreshBaseShadow();   // body vua lat: dai bong chan theo lai
            Rival.Side = Faction.Npc;
            Rival.Health.Invulnerable = false;   // ban sao mang theo cong tac "Lau dai bat tu" cua Admin
            Rival.Health.ResetTo(_homeMax * HealthScale * (target != null ? target.castleScale : 1f));
            home.Health.ResetTo(_homeMax * HealthScale);
            var bounds = body.bounds;
            StatBar.AttachHealth(Rival, new Vector3(bounds.center.x, bounds.max.y + 0.15f, 0f));

            Battlefield.OverrideRight(Rival.transform.position.x);
        }

        /// <summary>Doi xung qua tam man (truc doc qua camera) — tam nen Chinh Phat, hai thanh ve doi xung qua do.</summary>
        public Vector3 Mirror(Vector3 position) => new Vector3(2f * _axis - position.x, position.y, position.z);

        public static ConquestMatch Begin(Castle home, ConquestTarget target) => Current = new ConquestMatch(home, target);

        public static void End()
        {
            if (Current == null) return;
            if (Current.Rival != null) Object.Destroy(Current.Rival.gameObject);
            if (Current._home != null) Current._home.Health.ResetTo(Current._homeMax);
            Battlefield.OverrideRight(null);
            ConquestArena.Show(false);
            Current = null;
        }
    }
}
