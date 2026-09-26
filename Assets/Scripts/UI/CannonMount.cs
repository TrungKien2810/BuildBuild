using BuildBuild.Combat;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BuildBuild.UI
{
    /// <summary>
    /// Mot be phao tren mat phao dai (GAME_DESIGN.md muc 2: 3 be xep doc). Bat cham qua Physics2DRaycaster tren camera
    /// (can BoxCollider2D, phu ca be lan than phao); moi quyet dinh (mo bang chon, lap, go, nang cap) nam o `CannonBoard`.
    ///
    /// Khac `HeroSlot`: phao khong chet, khong roi be — `Occupant` song tu luc lap toi luc go, qua moi wave.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class CannonMount : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] CannonBoard board;
        [Tooltip("Khung vet tren be — chi hien khi be TRONG va dang chuan bi giua wave.")]
        [SerializeField] SpriteRenderer frame;

        public CannonDefinition Definition => Occupant != null ? Occupant.Definition : null;
        public Cannon Occupant { get; private set; }

        public void OnPointerClick(PointerEventData eventData) => board.OnMountClicked(this);

        /// <summary>Lap <paramref name="definition"/> (null = go). Phao cu bi huy, phao moi dung tai chan be.</summary>
        public void Assign(CannonDefinition definition, int level)
        {
            if (Occupant != null) Destroy(Occupant.gameObject);
            Occupant = null;

            if (definition != null)
            {
                var go = new GameObject("Cannon_" + definition.name);
                go.transform.SetParent(transform, false);
                go.AddComponent<SpriteRenderer>().sortingOrder = SortOrder.Castle + 1;
                go.AddComponent<SpriteAnimator>();
                Occupant = go.AddComponent<Cannon>();
                Occupant.Initialise(definition, level);
            }
            ShowFrame(!WaveManager.Fighting);
        }

        public void ShowFrame(bool show)
        {
            if (frame != null) frame.enabled = show && Occupant == null;
        }
    }
}
