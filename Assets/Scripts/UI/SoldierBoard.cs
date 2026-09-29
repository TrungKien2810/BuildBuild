using BuildBuild.Combat;
using BuildBuild.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// Bang chon linh cua bai sau thanh — cham giong het o hero (`HeroBoard`, nguoi dung 2026-09-28): giua wave cham cho
    /// co linh -> bo chon, cham cho trong -> bang chon loai linh DA SO HUU (loai dang dung o cho khac thi khoa). Doi linh
    /// va save nam o `SoldierCamp`; lop nay chi mo/dong bang.
    /// </summary>
    public class SoldierBoard : MonoBehaviour
    {
        [SerializeField] SoldierCamp camp;
        [SerializeField] GameObject pickerPanel;
        [Tooltip("Cung thu tu voi `SoldierCamp.roster`.")]
        [SerializeField] Button[] rosterButtons;
        [SerializeField] Button cancelButton;

        int _pending = -1;

        void Start()
        {
            for (int i = 0; i < rosterButtons.Length; i++)
            {
                int index = i;
                rosterButtons[i].onClick.AddListener(() => Choose(index));
            }
            cancelButton.onClick.AddListener(ClosePicker);
            ClosePicker();
        }

        // Vao tran thi dong bang: trong tran khong doi linh.
        void Update()
        {
            if (WaveManager.Fighting && pickerPanel.activeSelf) ClosePicker();
        }

        public void OnPostClicked(SoldierPost post)
        {
            if (WaveManager.Fighting) return;

            if (camp.Assigned(post.Index) != null)
            {
                camp.Assign(post.Index, null);
                return;
            }

            _pending = post.Index;
            for (int i = 0; i < rosterButtons.Length && i < camp.RosterCount; i++)
            {
                var soldier = camp.Soldier(i);
                rosterButtons[i].interactable = SoldierCamp.Owned(soldier) && !camp.InLineup(soldier);
            }
            pickerPanel.SetActive(true);
        }

        void Choose(int index)
        {
            if (_pending < 0 || index >= camp.RosterCount) return;
            camp.Assign(_pending, camp.Soldier(index));
            ClosePicker();
        }

        void ClosePicker()
        {
            _pending = -1;
            pickerPanel.SetActive(false);
        }
    }
}
