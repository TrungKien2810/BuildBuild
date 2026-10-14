using System.Collections.Generic;
using System.Linq;
using BuildBuild.Combat;
using BuildBuild.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>Bang doc ban chup dong gop cua wave vua xong; tran Chinh Phat co them tab xem phe dich.</summary>
    public class ContributionPanel : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] GameObject dim;
        [SerializeField] Text titleText;
        [SerializeField] Button closeButton;
        [SerializeField] Button allyTab;
        [SerializeField] Button enemyTab;
        [SerializeField] RectTransform content;
        [SerializeField] GameObject rowTemplate;
        [SerializeField] WaveStats stats;

        readonly List<GameObject> _rows = new List<GameObject>();
        ResultPopup _result;

        void Awake()
        {
            closeButton.onClick.AddListener(Hide);
            allyTab.onClick.AddListener(() => Render(Faction.Player));
            enemyTab.onClick.AddListener(() => Render(Faction.Npc));
            Hide();
        }

        public void Show(ResultPopup result)
        {
            _result = result;
            allyTab.gameObject.SetActive(stats.SnapshotConquest);
            enemyTab.gameObject.SetActive(stats.SnapshotConquest);
            Render(Faction.Player);
            dim.SetActive(true);
            root.SetActive(true);
        }

        void Render(Faction side)
        {
            titleText.text = stats.SnapshotConquest ? "Contribution — Conquest" : $"Contribution — Wave {stats.SnapshotWave}";
            allyTab.image.color = side == Faction.Player ? Color.white : UnselectedTab;
            enemyTab.image.color = side == Faction.Npc ? Color.white : UnselectedTab;
            foreach (var row in _rows) Destroy(row);
            _rows.Clear();
            var entries = stats.SnapshotOf(side).Where(x => x.damage > 0f || x.tank > 0f || x.heal > 0f)
                .OrderByDescending(Total).ToList();
            if (entries.Count == 0) AddEmpty();
            else AddRows(entries);
        }

        /// <summary>Xep hang theo tong sat thuong + do don + hoi mau, khong rieng sat thuong.</summary>
        static float Total(WaveStats.Contribution entry) => entry.damage + entry.tank + entry.heal;

        static readonly Color UnselectedTab = new Color(0.55f, 0.55f, 0.55f, 1f);

        public void Hide()
        {
            root.SetActive(false);
            dim.SetActive(false);
            if (_result != null) _result.ShowAgain();
        }

        void AddEmpty()
        {
            var row = NewRow();
            row.transform.Find("Name").GetComponent<Text>().text = "No Contribution Yet";
            row.transform.Find("Avatar").gameObject.SetActive(false);
            row.transform.Find("Damage").gameObject.SetActive(false);
            row.transform.Find("Tank").gameObject.SetActive(false);
            row.transform.Find("Heal").gameObject.SetActive(false);
            row.transform.Find("Highlight").gameObject.SetActive(false);
        }

        void AddRows(List<WaveStats.Contribution> entries)
        {
            float maxDamage = entries.Max(x => x.damage);
            float maxTank = entries.Max(x => x.tank);
            float maxHeal = entries.Max(x => x.heal);
            foreach (var entry in entries)
            {
                var row = NewRow();
                row.transform.Find("Avatar/Icon").GetComponent<Image>().sprite = entry.icon;
                row.transform.Find("Avatar/Icon").gameObject.SetActive(entry.icon != null);
                row.transform.Find("Name").GetComponent<Text>().text = entry.name;
                SetColumn(row.transform.Find("Damage"), entry.damage, maxDamage, entry.damage == maxDamage && entry.damage > 0f);
                SetColumn(row.transform.Find("Tank"), entry.tank, maxTank, entry.tank == maxTank && entry.tank > 0f);
                SetColumn(row.transform.Find("Heal"), entry.heal, maxHeal, entry.heal == maxHeal && entry.heal > 0f);
                row.transform.Find("Highlight").gameObject.SetActive(entry == entries[0]);   // xep theo tong -> dong dau la MVP
            }
        }

        GameObject NewRow()
        {
            var row = Instantiate(rowTemplate, content);
            row.SetActive(true);
            _rows.Add(row);
            return row;
        }

        static void SetColumn(Transform column, float value, float max, bool mvp)
        {
            column.Find("Bar/Fill").GetComponent<Image>().fillAmount = max > 0f ? value / max : 0f;
            column.Find("Value").GetComponent<Text>().text = Format(value);
            var badge = column.Find("Mvp").GetComponent<Image>();
            badge.gameObject.SetActive(mvp && badge.sprite != null);   // chua co asset thi an, khong hien o trang
        }

        /// <summary>Kieu so Viet: 950, 12,3k, 1,2M.</summary>
        static string Format(float value)
        {
            if (value < 1000f) return Mathf.RoundToInt(value).ToString();
            if (value < 1000000f) return (value / 1000f).ToString("0.0", Vi) + "k";
            return (value / 1000000f).ToString("0.0", Vi) + "M";
        }

        static readonly System.Globalization.CultureInfo Vi = new System.Globalization.CultureInfo("vi-VN");
    }
}
