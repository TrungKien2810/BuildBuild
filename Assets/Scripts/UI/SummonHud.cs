using BuildBuild.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// Thanh TAI NGUYEN TRIEU HOI linh tren HUD (duoi thanh mau lau dai). Tach khoi `BattleHud`: so doi moi frame
    /// trong luc danh (tich theo thoi gian), con BattleHud chi ve lai theo su kien.
    /// </summary>
    public class SummonHud : MonoBehaviour
    {
        [SerializeField] SoldierCamp camp;
        [SerializeField] Image fill;
        [SerializeField] Text valueText;

        int _shown = -1;

        void Update()
        {
            if (camp == null) return;
            float cap = camp.ResourceCap;
            if (fill != null) fill.fillAmount = cap > 0f ? Mathf.Clamp01(camp.Resource / cap) : 0f;

            // Chi doi chu khi so NGUYEN doi: dung lai chuoi moi frame la rac GC khong can thiet.
            int value = Mathf.FloorToInt(camp.Resource);
            if (value == _shown || valueText == null) return;
            _shown = value;
            valueText.text = $"{value} / {Mathf.FloorToInt(cap)}";
        }
    }
}
