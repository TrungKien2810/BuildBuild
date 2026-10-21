using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// Thong bao ngan giua tren man (`chrome/toast`), xep HANG DOI.
    ///
    /// Vi sao can: truoc day moi thong bao deu do vao mot dong `BattleHud.messageText`, nen cai sau xoa cai
    /// truoc va bang ket qua wave che mat luon — len cap, "Not enough Gold", "+1 Free Summon" deu mat dau.
    /// Toast dung `unscaledTime` nen van chay khi tran dang tam dung hoac dang tua nhanh.
    /// </summary>
    public class Toast : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Text label;
        [Tooltip("Moi thong bao giu tren man bao nhieu giay.")]
        [SerializeField] float holdSeconds = 2.2f;
        [Tooltip("Toi da bao nhieu thong bao xep hang — qua thi bo cai cu nhat.")]
        [SerializeField] int maxQueued = 4;

        readonly Queue<string> _queue = new Queue<string>();
        float _until;

        void Awake() => Close();

        void Close()
        {
            _queue.Clear();
            _until = 0f;
            if (root != null) root.SetActive(false);
        }

        public void Show(string text)
        {
            if (string.IsNullOrEmpty(text) || root == null) return;
            _queue.Enqueue(text);
            while (_queue.Count > maxQueued) _queue.Dequeue();
            // Dang ranh thi hien ngay o frame sau (khong doi het `holdSeconds` cua cai truoc).
            if (!root.activeSelf) _until = 0f;
        }

        void Update()
        {
            if (Time.unscaledTime < _until) return;
            if (_queue.Count == 0)
            {
                if (root.activeSelf) root.SetActive(false);
                return;
            }
            label.text = _queue.Dequeue();
            _until = Time.unscaledTime + holdSeconds;
            if (!root.activeSelf) root.SetActive(true);
        }
    }
}
