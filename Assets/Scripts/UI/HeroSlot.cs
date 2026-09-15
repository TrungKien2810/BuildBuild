using BuildBuild.Combat;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BuildBuild.UI
{
    /// <summary>
    /// Mot o dat hero tren san. Bat cham qua Physics2DRaycaster tren camera (can BoxCollider2D);
    /// moi quyet dinh (mo bang chon, go hero) nam o HeroBoard.
    ///
    /// O giu HAI thu khac nhau: `Definition` la DOI HINH (hero nao thuoc o nay — song qua ca tran,
    /// ke ca khi hero chet giua wave) va `Occupant` la than xac dang dung tren san o wave nay.
    /// O CHI la diem xuat hien: hero roi o de danh nhau, chet thi `Occupant` ve null trong khi
    /// `Definition` van con de dau wave sau spawn lai.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class HeroSlot : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] HeroBoard board;
        [SerializeField] SpriteRenderer frame;

        public HeroDefinition Definition { get; private set; }
        public Hero Occupant { get; private set; }

        public void OnPointerClick(PointerEventData eventData) => board.OnSlotClicked(this);

        /// <summary>Gan doi hinh cho o (null = de trong). Khung chi hien khi o chua co ai VA khong dang danh.</summary>
        public void Assign(HeroDefinition definition)
        {
            Definition = definition;
            ShowFrame(!WaveManager.Fighting);
        }

        /// <summary>Giua wave an khung (nguoi dung 2026-09-15): trong tran khong doi hero duoc, khung chi lam roi san.</summary>
        public void ShowFrame(bool show) => frame.enabled = show && Definition == null;

        public void SetOccupant(Hero hero) => Occupant = hero;
    }
}
