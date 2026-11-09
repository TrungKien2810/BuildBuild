using BuildBuild.Core;
using BuildBuild.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// Bang thong tin nguoi choi, mo bang cach bam huy hieu chan dung o goc tren-trai HUD. Chi doc so lieu da co trong save
    /// (khong them truong luu moi); chon avatar la IDEAS #3, chua lam.
    /// </summary>
    public class PlayerProfileScreen : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Text title, xpText, nextText;
        [SerializeField] Image xpFill;
        /// <summary>Thu tu khop nhan trong builder: wave cao nhat, Boss Rush best, Boss Rush clears, chuoi Chinh Phat, so unit, luot Summon free.</summary>
        [SerializeField] Text[] statValues;
        [SerializeField] Button openButton, closeButton;

        void Awake()
        {
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(Close);
            Close();
        }

        public void Open()
        {
            root.SetActive(true);
            // Cung ly do voi `ChallengeScreen.Open`: Awake co the chay ngay trong SetActive o tren va tat lai.
            if (!root.activeSelf) root.SetActive(true);
            Refresh();
            Sfx.Play("ui-open");
        }

        void Close() => root.SetActive(false);

        void Refresh()
        {
            var save = SaveSystem.Data;
            int level = PlayerLevel.Level;
            int next = level + 1;
            int need = PlayerLevel.XpFor(level);
            string reward = $"{PlayerLevel.GoldFor(next)} Gold" + (PlayerLevel.GrantsFreeRoll(next) ? " + 1 Free Summon" : "");
            title.text = $"Lv {level}";
            xpText.text = $"XP  {PlayerLevel.Xp} / {need}";
            xpFill.fillAmount = need > 0 ? Mathf.Clamp01((float)PlayerLevel.Xp / need) : 0f;
            nextText.text = $"Lv {next} reward:  {reward}";
            statValues[0].text = save.highestWave.ToString();
            statValues[1].text = $"{save.bossRushBest}/3";
            statValues[2].text = save.bossRushClears.ToString();
            statValues[3].text = save.conquest.streak.ToString();
            statValues[4].text = save.owned.Count.ToString();
            statValues[5].text = PlayerLevel.FreeRolls.ToString();
        }
    }
}
