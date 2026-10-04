using System.Collections;
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
            field.sprite = fields[biome];
            trees.sprite = treeLayers[biome];
            for (int i = 0; i < ambience.Length; i++)
                if (ambience[i] != null) ambience[i].SetActive(i == biome);
        }
    }
}
