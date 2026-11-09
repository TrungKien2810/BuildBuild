using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// Esc / nut Back cua Android: bam nut dong CAO NHAT dang hien (ten "Close" hoac "BackButton" — dung ten
    /// SceneBuilder dat) nen khong phai sua tung popup va khong phai dung lai scene. Khong co popup nao mo thi bo qua.
    /// </summary>
    public class BackKey : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var host = new GameObject("BackKey");
            DontDestroyOnLoad(host);
            host.AddComponent<BackKey>();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;

            Button top = null;
            string topKey = null;
            foreach (var button in FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                if (!button.interactable || (button.name != "Close" && button.name != "BackButton")) continue;
                // Popup ket qua: "Close" o do la Continue (nhan thuong/chuyen wave) — khong de Esc bam nham.
                if (button.GetComponentInParent<ResultPopup>() != null) continue;
                string key = OrderKey(button.transform);
                if (top == null || string.CompareOrdinal(key, topKey) > 0)
                {
                    top = button;
                    topKey = key;
                }
            }
            if (top != null) top.onClick.Invoke();
        }

        /// <summary>Thu tu ve len man hinh: sortingOrder cua canvas goc, roi chi so anh em tung cap (lon hon = ve sau = tren cung).</summary>
        static string OrderKey(Transform t)
        {
            var canvas = t.GetComponentInParent<Canvas>();
            var root = canvas != null ? canvas.rootCanvas : null;
            string key = "";
            for (; t != null; t = t.parent) key = t.GetSiblingIndex().ToString("D4") + key;
            return (root != null ? (root.sortingOrder + 10000).ToString("D6") : "000000") + key;
        }
    }
}
