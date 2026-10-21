using System;
using System.Collections;
using System.Collections.Generic;
using BuildBuild.Combat;
using BuildBuild.Data;
using BuildBuild.Meta;
using UnityEngine;

namespace BuildBuild.Core
{
    /// <summary>
    /// Danh TUNG wave mot (GAME_DESIGN.md muc 1): nguoi choi bam bat dau -> spawn het quai cua wave ->
    /// diet het thi THANG, cong vang roi DUNG lai de nguoi choi di nang cap cho wave sau.
    /// Thua (lau dai sup) cung dung tai wave do — lan bam sau danh lai dung wave vua thua, khong ve wave 1.
    /// Lau dai hoi day mau NGAY khi wave ket thuc (thang lan thua), khong doi den luc bam danh wave sau.
    ///
    /// Vang CHI tra khi THANG wave (`WaveTuning.WaveGold`), giet quai khong ra vang: thua ma van co tien thi de qua
    /// (nguoi dung chot 2026-09-29). Lop nay khong tu cham vao vi tien; no chi cong vang qua `Economy`.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        [SerializeField] WaveTuning tuning;
        [SerializeField] Enemy enemyPrefab;
        [SerializeField] Castle castle;
        [SerializeField] Economy economy;

        [Header("Duong di cua quai")]
        [Tooltip("Diem quai xuat hien (mep phai) — chi dung x; y boc ngau nhien trong dai dat cua Battlefield.")]
        [SerializeField] Transform spawnPoint;
        [SerializeField] Battlefield battlefield;

        readonly List<Enemy> _alive = new List<Enemy>();
        Coroutine _loop;

        /// <summary>
        /// Wave se danh o lan bam tiep theo (dang danh thi la wave hien tai). Chi tang khi THANG. Nam trong
        /// save chu khong trong component: sang man gacha la scene Battle bi huy, quay lai van dung wave cu.
        /// </summary>
        public int NextWave
        {
            get => Mathf.Max(1, SaveSystem.Data.nextWave);
            private set
            {
                SaveSystem.Data.nextWave = value;
                SaveSystem.Save();
            }
        }
        /// <summary>Ban tinh cua `IsFighting` cho unit khong giu tham chieu (Hero chi hoi mana khi dang danh).</summary>
        public static bool Fighting { get; private set; }
        /// <summary>
        /// Doi co nay la doi ca `GameSpeed`: ngoai tran animation phai chay 1x, chi trong tran moi ap muc tua
        /// nguoi choi da chon (x2/x3). Moi cho bat/tat tran deu di qua day nen khong the lech.
        /// </summary>
        public bool IsFighting
        {
            get => Fighting;
            private set
            {
                Fighting = value;
                GameSpeed.SetBattle(value);
            }
        }

        /// <summary>
        /// Dang o tran Chinh Phat (chuan bi hoac dang danh) chu khong phai wave: cac bang nghe `WaveStarted` doc co nay de
        /// dung phe NPC (`ConquestMatch`). Tat TRUOC khi ban WaveCleared/Defeated.
        /// </summary>
        public static bool InConquest { get; private set; }

        /// <summary>Tran vua ket thuc (WaveCleared/Defeated) la Chinh Phat — BattleHud doc de viet loi ket.</summary>
        public bool LastWasConquest { get; private set; }
        /// <summary>Dang o Boss Rush: 3 boss lien tiep tren CUNG mot thanh mau, thanh khong duoc sua giua cac hiep.</summary>
        public static bool InBossRush { get; private set; }
        /// <summary>Tran vua ket thuc la Boss Rush — BattleHud doi loi ket (khong hien "Wave N Complete").</summary>
        public bool LastWasBossRush { get; private set; }
        /// <summary>Dang o thu thach ngay (`Meta/DailyChallenge`) — mot wave duy nhat, khong doi `NextWave`.</summary>
        public static bool InDaily { get; private set; }
        /// <summary>Tran vua ket thuc la thu thach ngay.</summary>
        public bool LastWasDaily { get; private set; }

        /// <summary>Ba hiep cua Boss Rush — dung lai dung 3 wave boss co san (10/20/30) chu khong de ra quai moi.</summary>
        static readonly int[] BossRushWaves = { 10, 20, 30 };
        /// <summary>Manh nhan duoc o tran Chinh Phat vua thang (BattleHud viet vao loi ket).</summary>
        public int LastShards { get; private set; }
        /// <summary>Ghi chu thuong them cua wave vua thang (moc sau wave 30 / ky luc moi) — `BattleHud` in vao bang ket qua.</summary>
        public string LastBonus { get; private set; }

        public event Action<int> WaveStarted;
        /// <summary>Tham so: wave vua thang, vang thuong.</summary>
        public event Action<int, int> WaveCleared;
        /// <summary>Tham so: wave vua thua.</summary>
        public event Action<int> Defeated;

        void OnEnable()
        {
            if (castle != null) castle.Destroyed += OnCastleDestroyed;
        }

        void OnDisable()
        {
            if (castle != null) castle.Destroyed -= OnCastleDestroyed;
            Fighting = false;   // roi scene giua wave: co tinh khong duoc treo sang lan vao sau
            GameSpeed.SetBattle(false);
            EndConquest();
        }

        public void StartWave()
        {
            if (IsFighting) return;
            if (InConquest)
            {
                StartConquest();
                return;
            }
            if (tuning == null || enemyPrefab == null || spawnPoint == null)
            {
                Debug.LogError("[WaveManager] Thieu tuning / enemyPrefab / spawnPoint — khong chay duoc.");
                return;
            }

            ClearAlive();
            IsFighting = true;
            LastWasConquest = false;
            LastWasBossRush = false;
            LastWasDaily = false;
            LastBonus = null;
            _loop = StartCoroutine(RunWave(NextWave));
        }

        /// <summary>
        /// Vao tran Chinh Phat (GAME_DESIGN.md muc 8) — CHUA danh: dung thanh NPC doi xung ben phai, nguoi choi xep doi
        /// hinh nhu giua hai wave roi bam nut bat dau (`StartWave`). Khong ra quai; hero/phao/linh NPC do cac bang dung
        /// khi nghe `WaveStarted`. Ha thanh NPC = thang, thanh minh sap = thua (`OnCastleDestroyed`). Khong doi `NextWave`.
        /// </summary>
        public void EnterConquest(ConquestTarget target = null)
        {
            if (IsFighting || InConquest || castle == null) return;
            ConquestMatch.Begin(castle, target).Rival.Destroyed += OnRivalDestroyed;
            InConquest = true;
        }

        /// <summary>Bo tran Chinh Phat dang chuan bi (chua bam bat dau).</summary>
        public void LeaveConquest()
        {
            if (!IsFighting) EndConquest();
        }

        void StartConquest()
        {
            var target = ConquestMatch.Current?.Target;
            if (target != null)
            {
                if (!ConquestMap.CanAttack(target)) return;
                ConquestMap.ConsumeAttack(target);
            }
            ClearAlive();
            IsFighting = true;
            LastWasConquest = true;
            LastBonus = null;
            Sfx.Play("wave-start");
            WaveStarted?.Invoke(NextWave);
        }

        void OnRivalDestroyed()
        {
            if (!InConquest || !IsFighting) return;

            // TAM (chua can): thuong theo thanh tren ban do (`ConquestMap.Gold`), Admin vao thang thi = vang thang wave;
            // do roi theo luat thang wave (BattleHud).
            var target = ConquestMatch.Current?.Target;
            int gold = target != null ? ConquestMap.Gold(target, tuning) : tuning != null ? tuning.WaveGold(NextWave) : 0;
            LastShards = target != null ? ConquestMap.Finish(target, true) : 0;
            if (economy != null) economy.Add(gold);
            PlayerLevel.AddXp(PlayerLevel.ConquestXp);   // GDD 10.0: thang tran la nguon XP thu hai (khong co XP tu ruong AFK)
            IsFighting = false;
            if (castle != null) castle.Repair();
            EndConquest();
            WaveCleared?.Invoke(NextWave, gold);
        }

        void EndConquest()
        {
            if (!InConquest) return;
            InConquest = false;
            ConquestMatch.End();
        }

        IEnumerator RunWave(int wave)
        {
            Sfx.Play("wave-start");
            WaveStarted?.Invoke(wave);

            yield return SpawnWave(wave);

            // Wave xong khi khong con con nao song.
            while (IsFighting && _alive.Count > 0) yield return null;
            if (!IsFighting) yield break;

            // Tien do phai vao save TRUOC thuong: `economy.Add` / `PlayerLevel.AddXp` / `Armory.Add` deu tu ghi
            // file, nen de `NextWave` sau chung thi ban ghi cuoi cung da la wave CU — tat game o man ket qua la
            // danh lai duoc dung wave vua thang (va nhan lai vang + XP + manh moc).
            NextWave = wave + 1;
            int gold = tuning.WaveGold(wave);
            if (economy != null) economy.Add(gold);
            // Moc sau wave 30 + ky luc: ghi TRUOC khi cong XP / ban WaveCleared de bang ket qua doc duoc.
            bool record = WaveMilestones.Record(wave);
            string milestone = economy != null ? WaveMilestones.MaybeGrant(wave, economy.Add) : null;
            LastBonus = milestone ?? (record && wave > WaveMilestones.FromWave ? $"New record: wave {wave}" : null);
            // XP phai cong TRUOC `WaveCleared`: BattleHud nghe `LeveledUp` de cong vang va ghi ghi chu len
            // chinh bang ket qua cua wave nay.
            PlayerLevel.AddXp(PlayerLevel.WaveXp(wave));
            IsFighting = false;
            if (castle != null) castle.Repair();   // day mau ngay khi het tran, khong doi den luc bam danh tiep
            WaveCleared?.Invoke(wave, gold);
        }

        /// <summary>
        /// Boss Rush: ba hiep boss lien tiep (dung lai 3 wave boss co san 10/20/30) tren CUNG mot thanh mau —
        /// khong duoc sua thanh giua cac hiep, khong doi `NextWave` (day la che do phu, khong phai tien trinh chinh).
        /// Thuong: vang ba wave + bonus, XP ba wave, va mot luot roi trang bi theo luat thang wave 30.
        /// </summary>
        public void StartBossRush()
        {
            if (IsFighting || InConquest || tuning == null || enemyPrefab == null || spawnPoint == null) return;
            ClearAlive();
            IsFighting = true;
            InBossRush = true;
            LastWasConquest = false;
            LastWasBossRush = true;
            LastWasDaily = false;
            LastBonus = null;
            Sfx.Play("wave-start");
            WaveStarted?.Invoke(NextWave);
            _loop = StartCoroutine(RunBossRush());
        }

        /// <summary>
        /// Thu thach ngay (`Meta/DailyChallenge`): MOT wave dung lai bang quai co san, them bien the nhe
        /// (hom nay: sat thuong quai x1,35 hoac them quai). Khong doi `NextWave`; thua thi duoc thu lai trong ngay.
        /// </summary>
        public void StartDailyChallenge()
        {
            if (IsFighting || InConquest || tuning == null || enemyPrefab == null || spawnPoint == null) return;
            if (DailyChallenge.Cleared) return;
            ClearAlive();
            IsFighting = true;
            InDaily = true;
            LastWasConquest = false;
            LastWasDaily = true;
            LastWasBossRush = false;
            LastBonus = null;
            Sfx.Play("wave-start");
            WaveStarted?.Invoke(NextWave);
            _loop = StartCoroutine(RunDaily());
        }

        IEnumerator RunDaily()
        {
            int wave = DailyChallenge.Wave;
            yield return SpawnWave(wave, DailyChallenge.DamageScale, DailyChallenge.ExtraEnemies);
            while (IsFighting && _alive.Count > 0) yield return null;
            if (!IsFighting) yield break;

            IsFighting = false;
            InDaily = false;
            LastBonus = economy != null ? DailyChallenge.Grant(economy.Add) : null;
            PlayerLevel.AddXp(PlayerLevel.WaveXp(wave));
            if (castle != null) castle.Repair();
            WaveCleared?.Invoke(NextWave, DailyChallenge.GoldReward);
        }

        IEnumerator RunBossRush()
        {
            int gold = 0;
            int xp = 0;
            int rounds = 0;
            foreach (int wave in BossRushWaves)
            {
                if (!IsFighting) yield break;
                yield return SpawnWave(wave);
                while (IsFighting && _alive.Count > 0) yield return null;
                if (!IsFighting) yield break;
                rounds++;
                SaveSystem.Data.bossRushBest = Mathf.Max(SaveSystem.Data.bossRushBest, rounds);
                SaveSystem.Save();
                gold += tuning.WaveGold(wave);
                xp += PlayerLevel.WaveXp(wave);
            }

            gold += BossRushBonusGold;
            SaveSystem.Data.bossRushClears++;
            SaveSystem.Save();
            IsFighting = false;
            InBossRush = false;
            if (economy != null) economy.Add(gold);
            PlayerLevel.AddXp(xp);
            if (castle != null) castle.Repair();
            LastBonus = $"Boss Rush cleared: +{gold} Gold";
            WaveCleared?.Invoke(NextWave, gold);
        }

        /// <summary>Thuong them khi pha xong ca ba hiep — bu cho viec khong duoc nghi de nang cap giua chung.</summary>
        const int BossRushBonusGold = 500;

        IEnumerator SpawnWave(int wave, float damageScale = 1f, int extraEnemies = 0)
        {
            var band = tuning.BandFor(wave);
            var queue = BuildSpawnQueue(wave, band, damageScale, extraEnemies);
            if (queue.Count == 0)
            {
                Debug.LogWarning($"[WaveManager] Wave {wave} khong co loai quai nao trong bang.");
                yield break;
            }

            foreach (var entry in queue)
            {
                if (!IsFighting) yield break;
                Spawn(entry.definition, entry.health, entry.damage);
                yield return new WaitForSeconds(tuning.spawnInterval);
            }
        }

        struct SpawnEntry
        {
            public EnemyDefinition definition;
            public float health;
            public float damage;
        }

        /// <summary>
        /// Thu tu spawn cua mot wave (GAME_DESIGN.md muc 5): quai thuong boc ngau nhien tu bang, tinh anh (`eliteCount`)
        /// rai deu giua doan, boss ra SAU hai con ho tong dau tien (khong co ho tong thi ra ngay). Boss dung
        /// `WaveTuning.BossMultiplier` thay cho cong thuc len thang theo wave.
        /// </summary>
        List<SpawnEntry> BuildSpawnQueue(int wave, WaveBand band, float damageScale = 1f, int extraEnemies = 0)
        {
            var queue = new List<SpawnEntry>();
            if (band == null) return queue;

            float hp = tuning.HealthMultiplier(wave);
            float dmg = tuning.DamageMultiplier(wave) * damageScale;

            if (band.enemies != null && band.enemies.Length > 0)
            {
                int count = tuning.RegularCount(wave, band) + extraEnemies;
                for (int i = 0; i < count; i++)
                    queue.Add(new SpawnEntry
                    {
                        definition = band.enemies[UnityEngine.Random.Range(0, band.enemies.Length)],
                        health = hp,
                        damage = dmg,
                    });
            }

            var elite = band.elite != null ? band.elite : tuning.elite;
            if (elite != null && band.eliteCount > 0)
                for (int i = 0; i < band.eliteCount; i++)
                {
                    int at = (queue.Count + 1) * (i + 1) / (band.eliteCount + 1);
                    queue.Insert(Mathf.Clamp(at, 0, queue.Count), new SpawnEntry { definition = elite, health = hp, damage = dmg });
                }

            var mainBoss = band.boss != null ? band.boss : tuning.boss;
            if (band.hasBoss && mainBoss != null)
            {
                float boss = tuning.BossMultiplier(wave, band);
                int at = Mathf.Min(2, queue.Count);
                queue.Insert(at, new SpawnEntry { definition = mainBoss, health = boss, damage = boss });
                // Boss di kem ra ngay sau boss chinh (Minotaur danh gan dan truoc, phap su dung sau ban).
                if (band.extraBosses != null)
                    foreach (var extra in band.extraBosses)
                        if (extra != null) queue.Insert(++at, new SpawnEntry { definition = extra, health = boss, damage = boss });
            }

            return queue;
        }

        /// <summary>WaveManager cua scene dang chay (boss goi quai them giua wave qua day).</summary>
        public static WaveManager Current { get; private set; }

        void Awake()
        {
            Current = this;
            // Static song qua scene (tat Domain Reload): vao lai man Battle thi khong con o che do phu nao nua.
            InBossRush = false;
            InDaily = false;
            PlayerLevel.LeveledUp += PayLevelReward;
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
            PlayerLevel.LeveledUp -= PayLevelReward;
        }

        /// <summary>
        /// Tra vang khi len cap. Dat o DAY (Core) chu khong o HUD: moi XP trong game deu sinh ra tu lop nay
        /// (xem 4 cho `PlayerLevel.AddXp`), con "cong vang" la viec cua mien du lieu — de trong `BattleHud` thi
        /// giao dien dang quyet dinh ket qua tien bac cua mot su kien mien du lieu.
        /// So vang van do `PlayerLevel.GoldFor` quyet (mot nguon so duy nhat).
        /// </summary>
        void PayLevelReward(int level)
        {
            if (economy != null) economy.Add(PlayerLevel.GoldFor(level));
        }

        /// <summary>
        /// Quai sinh GIUA wave tai <paramref name="position"/> (Dreadwyrm goi Ma Hoa): tinh vao wave — phai diet ca no moi
        /// thang. Ngoai luc danh thi bo qua.
        /// </summary>
        public Enemy SpawnExtra(EnemyDefinition definition, Vector3 position, float healthMultiplier, float damageMultiplier)
        {
            if (!IsFighting || definition == null || enemyPrefab == null) return null;
            return Spawn(definition, healthMultiplier, damageMultiplier, position);
        }

        Enemy Spawn(EnemyDefinition definition, float healthMultiplier, float damageMultiplier, Vector3? at = null)
        {
            var position = at ?? spawnPoint.position;
            if (at == null && battlefield != null) position.y = battlefield.RandomLaneY();
            var enemy = Instantiate(enemyPrefab, position, Quaternion.identity, transform);
            enemy.Initialise(definition, healthMultiplier, damageMultiplier);
            if (castle != null) enemy.SetTarget(castle.Health);
            EnemyExtras.Attach(enemy, damageMultiplier);    // giap, vien da ban xa, co che boss

            enemy.Died += OnEnemyDied;
            _alive.Add(enemy);
            return enemy;
        }

        void OnEnemyDied(Enemy enemy)
        {
            enemy.Died -= OnEnemyDied;
            _alive.Remove(enemy);
        }

        void OnCastleDestroyed()
        {
            if (!IsFighting) return;
            IsFighting = false;
            InBossRush = false;
            InDaily = false;
            if (_loop != null) StopCoroutine(_loop);
            ClearAlive();
            if (castle != null) castle.Repair();   // day mau ngay khi het tran, khong doi den luc bam danh tiep
            if (InConquest && ConquestMatch.Current?.Target != null) ConquestMap.Finish(ConquestMatch.Current.Target, false);
            EndConquest();
            Defeated?.Invoke(NextWave);
        }

        /// <summary>
        /// Nguoi choi bam "Surrender" trong bang tam dung: thua wave dang danh. Dung lai y nguyen duong
        /// "thanh bi pha" — mat mau thanh trong tran, het tran thi thanh day lai, vang/do/level van giu
        /// (GDD muc 1: thua khong xoa gi).
        /// </summary>
        public void Surrender()
        {
            if (!IsFighting) return;
            OnCastleDestroyed();
        }

        /// <summary>Admin panel: nhay toi wave bat ky — chi giua hai wave.</summary>
        public void DevSetWave(int wave)
        {
            if (IsFighting || tuning == null) return;
            NextWave = Mathf.Max(1, wave);
        }

        /// <summary>Admin panel: giet moi quai dang song (van tinh la thang neu da spawn het).</summary>
        public void DevKillAll()
        {
            foreach (var enemy in _alive.ToArray())
                if (enemy != null && !enemy.IsDying)   // chia bu Armor: boss giap 30% an Current thi con song
                    enemy.Health.TakeDamage(enemy.Health.Current / Mathf.Max(0.01f, 1f - enemy.Health.Armor));
        }

        void ClearAlive()
        {
            foreach (var enemy in _alive)
            {
                if (enemy == null) continue;
                enemy.Died -= OnEnemyDied;
                // Tat NGAY: Destroy chi xoa cuoi frame, quai con lai trong frame van danh trung hero vua dung lai o het wave.
                enemy.gameObject.SetActive(false);
                Destroy(enemy.gameObject);
            }
            _alive.Clear();
        }
    }
}
