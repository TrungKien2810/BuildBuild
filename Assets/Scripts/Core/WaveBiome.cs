using System.Collections;
using BuildBuild.Combat;
using UnityEngine;

namespace BuildBuild.Core
{
    /// <summary>
    /// Doi nen tran wave (nguoi dung chot 2026-10-03): thang moi wave thi boc ngau nhien 1 biome (co / dam lay / dat chay /
    /// tuyet), khong lap biome dang dung. Doi SAU khi het wave, sau man den chuyen canh: man den keo tu trai che kin man,
    /// thay nen o duoi, roi keo tiep sang phai lo giao dien voi nen moi (BattleHud goi `Advance` luc dong bang ket qua).
    /// Ca 4 nen giu nguyen bo cuc bg-field nen chi thay sprite nen + hang cay duoi, khong doi toa do. Lop dong cua biome
    /// (con cua container ma ConquestArena bat/tat) chi bat cho biome dang dung; biome chua co lop dong thi de null.
    /// SceneBuilder dien moi tham chieu.
    /// </summary>
    public sealed class WaveBiome : MonoBehaviour
    {
        [SerializeField] SpriteRenderer field;
        [SerializeField] SpriteRenderer trees;
        [Header("Moi phan tu mot biome, cung thu tu; phan tu 0 = nen co dang dung trong scene")]
        [SerializeField] Sprite[] fields;
        [SerializeField] Sprite[] treeLayers;
        [SerializeField] GameObject[] ambience;

        [Header("Man den chuyen canh (Image den phu kin canvas tren cung, chan cham khi dang chay)")]
        [SerializeField] RectTransform wipe;
        [SerializeField] float slideSeconds = 0.35f;
        [SerializeField] float holdSeconds = 0.15f;

        int _current;
        bool _running;

        /// <summary>
        /// Biome da boc, song qua doi scene: vao Gacha/Units roi quay lai Battle thi nen phai la nen dang danh,
        /// khong phai nen goc (nguoi dung bat 2026-10-14). Cung kieu static voi `UnitLight` o tren.
        /// </summary>
        static int _persisted;

        /// <summary>
        /// Anh sang chung cua tung biome — mau than moi unit NHAN voi so nay (`Unit.BodyColor`). Cung thu tu voi
        /// `fields`: 0 = nen goc trong scene (thu am, audit 2026-10-12 do o day) · 1 dam lay · 2 dat chay · 3 tuyet.
        /// So cua dam lay/dat chay la TAM, chinh lai theo anh trong game.
        /// </summary>
        static readonly Color[] UnitLights =
        {
            new Color(1f, 0.95f, 0.88f),      // thu am (nen goc)
            new Color(0.94f, 1f, 0.92f),      // dam lay (tam)
            new Color(1f, 0.92f, 0.85f),      // dat chay
            new Color(0.92f, 0.96f, 1f),      // tuyet
        };

        /// <summary>Anh sang chung cua biome dang dung; unit nhan vao mau than. Mac dinh trang = khong doi mau.</summary>
        public static Color UnitLight { get; private set; } = Color.white;

        static WaveBiome _scene;

        // Bien static song qua doi scene: dat lai ngay khi vao tran (biome dau cung co anh sang), tra ve trang khi roi tran.
        void Awake()
        {
            _scene = this;
            if (_persisted != _current) Apply(_persisted);   // 0 = nen goc trong scene, khong phai ve lai
            else SetLight(_current);
        }

        void OnDestroy()
        {
            if (_scene != this) return;
            _scene = null;
            SetLight(-1);
        }

        /// <summary>Chinh Phat co nen rieng (thu am) nen dung anh sang thu; tat Chinh Phat thi tra lai biome dang dung.</summary>
        public static void UseConquestLight(bool on) => SetLight(on ? 0 : _scene != null ? _scene._current : -1);

        /// <summary>Doi anh sang chung, than dang tren san phai ve lai. `biome` ngoai bang (-1) = trang, khong doi mau.</summary>
        static void SetLight(int biome)
        {
            UnitLight = biome >= 0 && biome < UnitLights.Length ? UnitLights[biome] : Color.white;
            foreach (var unit in Unit.All) unit.RepaintBody();
        }

        /// <summary>Chuyen canh sang biome ngau nhien khac biome dang dung.</summary>
        public void Advance()
        {
            if (_running) return;
            int next = Random.Range(0, fields.Length - 1);
            if (next >= _current) next++;   // bo qua biome dang dung
            StartCoroutine(Wipe(next));
        }

        IEnumerator Wipe(int biome)
        {
            _running = true;
            float width = ((RectTransform)wipe.parent).rect.width;
            wipe.gameObject.SetActive(true);
            yield return Slide(-width, 0f);
            Apply(biome);
            yield return new WaitForSecondsRealtime(holdSeconds);
            yield return Slide(0f, width);
            wipe.gameObject.SetActive(false);
            _running = false;
        }

        IEnumerator Slide(float from, float to)
        {
            for (float t = 0f; t < slideSeconds; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / slideSeconds);
                wipe.anchoredPosition = new Vector2(Mathf.Lerp(from, to, k), 0f);
                yield return null;
            }
            wipe.anchoredPosition = new Vector2(to, 0f);
        }

        void Apply(int biome)
        {
            _current = biome;
            _persisted = biome;
            field.sprite = fields[biome];
            trees.sprite = treeLayers[biome];
            for (int i = 0; i < ambience.Length; i++)
                if (ambience[i] != null) ambience[i].SetActive(i == biome);
            SetLight(biome);
        }
    }
}
