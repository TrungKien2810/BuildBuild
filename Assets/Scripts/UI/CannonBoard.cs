using System.Collections;
using BuildBuild.Combat;
using BuildBuild.Core;
using BuildBuild.Data;
using BuildBuild.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// 3 be phao tren phao dai (GAME_DESIGN.md muc 2 + 4). Cham giong het o hero (`HeroBoard`, nguoi dung chot
    /// 2026-09-28): be trong -> bang chon phao DANG SO HUU; be dang co phao -> go phao. Chi doi duoc giua cac wave.
    /// Nang level KHONG lam o be — o man Doi quan (`UnitsScreen`), nhu hero.
    ///
    /// So huu + level doc tu save theo `CannonDefinition.unitId` (cung id voi gacha). Moi phao chi lap tren MOT be:
    /// phao dang o be khac thi dong do khoa, nhu hero dang dung tren san.
    ///
    /// Doi hinh phao nam trong save (`SaveData.cannons`): sang man gacha roi quay lai van con nguyen.
    ///
    /// So be theo cap thanh tri (3/4/5): scene dung san du be cho cap cao nhat, cap thap thi tat be thua
    /// (`Castle.TierChanged`). Be bi tat ma dang co phao thi phao duoc go, save giu ten de nang cap lai van con.
    /// </summary>
    public class CannonBoard : MonoBehaviour
    {
        [SerializeField] WaveManager waves;
        [SerializeField] Castle castle;
        [SerializeField] CannonMount[] mounts;

        int _activeMounts;

        /// <summary>So be phao dang co — luc chien Chinh Phat dem phao theo so nay.</summary>
        public int MountCount => _activeMounts;

        [Header("Bang chon phao")]
        [SerializeField] GameObject pickerPanel;
        [SerializeField] CannonDefinition[] roster;
        [Tooltip("Cung thu tu voi roster: bam de lap phao do len be dang chon.")]
        [SerializeField] Button[] rosterButtons;
        [Tooltip("Cung thu tu voi roster: dong trang thai (level + chi so, hoac 'Chua so huu').")]
        [SerializeField] Text[] rosterInfo;
        [SerializeField] Button cancelButton;

        CannonMount _pending;
        /// <summary>Phao tren thanh NPC cua tran Chinh Phat dang danh.</summary>
        readonly System.Collections.Generic.List<Cannon> _rivals = new System.Collections.Generic.List<Cannon>();

        void Start()
        {
            for (int i = 0; i < roster.Length; i++)
            {
                int index = i;
                if (i < rosterButtons.Length) rosterButtons[i].onClick.AddListener(() => Choose(index));
            }
            cancelButton.onClick.AddListener(ClosePicker);
            waves.WaveStarted += OnWaveStarted;
            waves.WaveCleared += OnWaveEnded;
            waves.Defeated += OnDefeated;
            if (castle != null) castle.TierChanged += OnTierChanged;
            ClosePicker();
            LayoutMounts();
            RestoreLoadout();
        }

        void OnTierChanged()
        {
            int before = _activeMounts;
            LayoutMounts();
            RestoreLoadout();
            if (_activeMounts > before) StartCoroutine(BlinkNewMounts(before));
        }

        /// <summary>Nhap nhay khung vet cac be vua mo (chi so tu <paramref name="from"/>) de nguoi choi biet cho lap phao moi.</summary>
        IEnumerator BlinkNewMounts(int from)
        {
            for (int blink = 0; blink < 8; blink++)
            {
                bool show = blink % 2 == 1;
                for (int i = from; i < _activeMounts; i++) mounts[i].ShowFrame(show && !WaveManager.Fighting);
                yield return new WaitForSeconds(0.2f);
            }
            for (int i = from; i < _activeMounts; i++) mounts[i].ShowFrame(!WaveManager.Fighting);
        }

        /// <summary>Dat + bat/tat be theo cap thanh hien tai. Scene cu khong co bang cap: giu nguyen moi be.</summary>
        void LayoutMounts()
        {
            var tier = castle != null ? castle.CurrentTier : null;
            _activeMounts = tier != null ? Mathf.Min(tier.mounts.Length, mounts.Length) : mounts.Length;
            for (int i = 0; i < mounts.Length; i++)
            {
                bool active = i < _activeMounts;
                if (!active && mounts[i].Occupant != null) mounts[i].Assign(null, 1);
                if (tier != null && active)
                {
                    mounts[i].transform.localPosition = tier.mounts[i];
                    float scale = tier.mountScales != null && i < tier.mountScales.Length ? tier.mountScales[i] : 1f;
                    mounts[i].transform.localScale = Vector3.one * scale;
                }
                mounts[i].gameObject.SetActive(active);
            }
        }

        void OnDestroy()
        {
            if (waves != null)
            {
                waves.WaveStarted -= OnWaveStarted;
                waves.WaveCleared -= OnWaveEnded;
                waves.Defeated -= OnDefeated;
            }
            if (castle != null) castle.TierChanged -= OnTierChanged;
        }

        public void OnMountClicked(CannonMount mount)
        {
            if (waves.IsFighting) return;

            if (mount.Occupant != null)
            {
                mount.Assign(null, 1);
                Sfx.Play("unit-remove");
                SaveLoadout();
                return;
            }

            _pending = mount;
            RefreshPicker();
            pickerPanel.SetActive(true);
            Sfx.Play("ui-open");
        }

        /// <summary>Admin panel: so huu ca roster phao (test khi chua quay gacha ra).</summary>
        public void DevOwnAll()
        {
            foreach (var cannon in roster)
                if (cannon != null && SaveSystem.Data.Find(cannon.unitId) == null)
                    SaveSystem.Data.owned.Add(new OwnedUnit { id = cannon.unitId });
            SaveSystem.Save();
            if (pickerPanel.activeSelf) RefreshPicker();
        }

        static OwnedUnit Owned(CannonDefinition cannon) => cannon != null ? SaveSystem.Data.Find(cannon.unitId) : null;

        /// <summary>
        /// Mot dong chi so cho bang chon phao. Phao thuong doc theo "toc ban", con Dau Rong Da phun lien tuc theo DPS nen
        /// dong do doi thanh nhip phun (`DragonCannonKit`) — hai kieu ra don khac nhau khong dung chung mot cau chu.
        /// </summary>
        static string StatLine(CannonDefinition cannon, int level)
        {
            string damage = $"Damage {Progression.CannonDamage(cannon, level):0.#}";
            if (cannon.dragonKit != null)
                return $"{damage}/s   Breath {cannon.dragonKit.breathSeconds:0.#}s   Range {cannon.range:0.#}";
            return $"{damage}   {cannon.fireRate:0.#}/s   Range {cannon.range:0.#}";
        }

        static int LevelOf(CannonDefinition cannon)
        {
            var owned = Owned(cannon);
            return owned != null ? Mathf.Max(1, owned.level) : 1;
        }

        void Choose(int index)
        {
            if (_pending == null || Owned(roster[index]) == null || OnMount(roster[index])) return;

            _pending.Assign(roster[index], LevelOf(roster[index]));
            Sfx.Play("unit-place");
            SaveLoadout();
            ClosePicker();
        }

        void RefreshPicker()
        {
            for (int i = 0; i < roster.Length; i++)
            {
                var cannon = roster[i];
                var owned = Owned(cannon);
                int level = LevelOf(cannon);

                if (i < rosterButtons.Length)
                    rosterButtons[i].interactable = owned != null && !OnMount(cannon);

                if (i < rosterInfo.Length)
                    rosterInfo[i].text = owned == null
                        ? "Not Owned — roll gacha to obtain"
                        : $"Lv {level}   {StatLine(cannon, level)}{OnMountNote(cannon)}";
            }
        }

        bool OnMount(CannonDefinition cannon)
        {
            foreach (var mount in mounts)
                if (mount.Definition == cannon) return true;
            return false;
        }

        string OnMountNote(CannonDefinition cannon)
        {
            for (int i = 0; i < mounts.Length; i++)
                if (mounts[i].Definition == cannon) return $"   (mount {i + 1})";
            return "";
        }

        void OnWaveStarted(int wave)
        {
            ClosePicker();
            ShowFrames(false);
            if (WaveManager.InConquest) SpawnRivals();
        }

        void OnWaveEnded(int wave, int gold) => OnDefeated(wave);

        void OnDefeated(int wave)
        {
            ShowFrames(true);
            foreach (var cannon in _rivals)
                if (cannon != null) Destroy(cannon.gameObject);
            _rivals.Clear();
        }

        /// <summary>
        /// Chinh Phat: moi be co mot phao NPC tai vi tri doi xung tren thanh NPC, loai boc ngau nhien tu roster, level theo
        /// save nhu phao minh — TAM, chua suy tu luc chien.
        /// </summary>
        void SpawnRivals()
        {
            var match = ConquestMatch.Current;
            var pool = System.Array.FindAll(roster, c => c != null);
            if (match == null || pool.Length == 0) return;
            for (int i = 0; i < _activeMounts; i++)
            {
                var mount = mounts[i];
                var target = match.Target;
                var definition = target == null ? pool[Random.Range(0, pool.Length)]
                    : i < target.cannons.Count ? ConquestMap.Find(roster, target.cannons[i]) : null;   // be ngoai so phao luc Do Tham: de trong
                if (definition == null) continue;
                var go = new GameObject("RivalCannon_" + definition.name);
                go.transform.position = match.Mirror(mount.transform.position);
                go.AddComponent<SpriteRenderer>().sortingOrder = SortOrder.Castle + 1;
                go.AddComponent<SpriteAnimator>();
                var cannon = go.AddComponent<Cannon>();
                cannon.Initialise(definition, target != null ? target.cannonLevel : LevelOf(definition), Faction.Npc);
                go.transform.localScale *= mount.transform.localScale.x;   // cung co voi be ben thanh minh
                _rivals.Add(cannon);
            }
        }

        void ShowFrames(bool show)
        {
            foreach (var mount in mounts) mount.ShowFrame(show);
        }

        void RestoreLoadout()
        {
            var loadout = SaveSystem.Data.cannons;
            // Save chua bao gio lap phao (`cannons` rong han): lap san phao dang so huu theo thu tu `roster`.
            // Khong co buoc nay thi 3 be tren thanh la 3 o vinh vien trong voi acc moi — bam vao chi thay
            // "Not Owned — roll gacha to obtain" o moi dong.
            if (loadout.Count == 0 && _activeMounts > 0)
            {
                FillDefaultLoadout();
                return;
            }

            for (int i = 0; i < _activeMounts; i++)
            {
                CannonDefinition definition = null;
                if (i < loadout.Count)
                    definition = System.Array.Find(roster, c => c != null && c.name == loadout[i]);
                // Phao bi xoa khoi kho (admin) thi be de trong.
                if (definition != null && Owned(definition) == null) definition = null;
                mounts[i].Assign(definition, LevelOf(definition));
            }
        }

        void FillDefaultLoadout()
        {
            int mount = 0;
            foreach (var definition in roster)
            {
                if (mount >= _activeMounts) break;
                if (definition == null || Owned(definition) == null) continue;
                mounts[mount].Assign(definition, LevelOf(definition));
                mount++;
            }
            if (mount > 0) SaveLoadout();
        }

        void SaveLoadout()
        {
            var loadout = SaveSystem.Data.cannons;
            for (int i = 0; i < _activeMounts; i++)
            {
                string name = mounts[i].Definition != null ? mounts[i].Definition.name : "";
                if (i < loadout.Count) loadout[i] = name;
                else loadout.Add(name);
            }
            SaveSystem.Save();
        }

        void ClosePicker()
        {
            _pending = null;
            pickerPanel.SetActive(false);
        }
    }
}
