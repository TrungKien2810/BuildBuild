using System;
using System.Collections.Generic;
using UnityEngine;

namespace BuildBuild.Core
{
    /// <summary>
    /// Lop dong cua MOT biome khi nen no khong co vat the chuyen dong nao khac (`Background/Field/` do
    /// `FieldAmbience` lo: chim + buom + tuc co). Moi "mo" = mot bo frame lap vo han, chay theo kieu chuyen dong
    /// rieng trong mot khung world, nen cung mot component phuc vu duoc ca ba biome con lai:
    ///
    ///   dam lay  — dom dom bay lang thang, bot khi noi len mat nuoc
    ///   dat chay — tan lua bay len, khoi cuon
    ///   tuyet    — tuyet roi, gio cuon tuy
    ///
    /// Vi sao khong nhet vao `FieldAmbience`: file do gan chat voi mot biome (dan chim se + buom cua nen co) va da
    /// dung hinh tu 2026-10-03. Them bien the cho ba biome khac vao cung file la bien no thanh "moi thu cua moi nen".
    ///
    /// Nam trong nen wave nen `ConquestArena` bat/tat ca container va `WaveBiome` bat dung con (xem `WaveBiome.Apply`).
    /// </summary>
    public sealed class DriftAmbience : MonoBehaviour
    {
        public enum Motion
        {
            /// <summary>Bay lang thang trong khung theo Perlin — cham, doi huong muot, khong ra khoi khung.</summary>
            Wander,
            /// <summary>Noi len roi quay lai day khung (tan lua, bot khi).</summary>
            Rise,
            /// <summary>Roi xuong roi quay lai dinh khung (tuyet, tan tro).</summary>
            Fall,
        }

        [Serializable]
        public sealed class Mote
        {
            public string name = "Mote";
            public Sprite[] frames;
            [Tooltip("Nhip phat frame.")]
            public float fps = 8f;
            [Tooltip("So ban the cung luc.")]
            public int count = 6;
            [Tooltip("Khung WORLD ma ban the duoc phep chay trong do (dung Rect.MinMaxRect cho de doc).")]
            public Rect area;
            public Motion motion = Motion.Wander;
            [Tooltip("Toc do chay (world/giay). Wander dung lam bien do, Rise/Fall la toc do len/xuong.")]
            public float speed = 0.25f;
            [Tooltip("Lech pha giua cac ban the: ban the i lech i x so nay (world theo truc di chuyen).")]
            public float stagger = 0.6f;
            [Tooltip("Thu tu ve trong sorting layer Default — dat theo lop cua nen (xem `SortOrder`).")]
            public int order = SortOrder.Background + 2;
        }

        public Mote[] motes;

        const float Pixel = 0.02f;   // nen ve tren luoi 2 px

        sealed class Body
        {
            public Transform root;
            public Transform sprite;
            public Mote mote;
            public float phase;
            public float offset;
        }

        readonly List<Body> _bodies = new List<Body>();

        void Start()
        {
            if (motes == null) return;
            foreach (var mote in motes)
            {
                if (mote == null || mote.frames == null || mote.frames.Length == 0) continue;
                for (int i = 0; i < Mathf.Max(1, mote.count); i++) _bodies.Add(Create(mote, i));
            }
        }

        Body Create(Mote mote, int index)
        {
            var root = new GameObject(mote.name).transform;
            root.SetParent(transform, false);

            var sprite = new GameObject("Body", typeof(SpriteRenderer)).transform;
            sprite.SetParent(root, false);
            var renderer = sprite.GetComponent<SpriteRenderer>();
            renderer.sprite = mote.frames[0];
            renderer.sortingOrder = mote.order;
            sprite.gameObject.AddComponent<BackdropLoop>().Setup(mote.frames, mote.fps);

            var body = new Body
            {
                root = root,
                sprite = sprite,
                mote = mote,
                phase = UnityEngine.Random.value,
                offset = index * mote.stagger + UnityEngine.Random.Range(-0.15f, 0.15f) * mote.stagger,
            };
            Place(body);
            return body;
        }

        void Update()
        {
            for (int i = 0; i < _bodies.Count; i++) Place(_bodies[i]);
        }

        void Place(Body body)
        {
            var mote = body.mote;
            var area = mote.area;
            Vector3 local;
            switch (mote.motion)
            {
                case Motion.Rise:
                case Motion.Fall:
                {
                    float span = Mathf.Max(0.01f, area.height);
                    float t = Mathf.Repeat(Time.time * (mote.speed / span) + body.phase + body.offset, 1f);
                    float y = mote.motion == Motion.Rise
                        ? Mathf.Lerp(area.yMin, area.yMax, t)
                        : Mathf.Lerp(area.yMax, area.yMin, t);
                    // Ngang: troi cham theo sin, moi ban the mot nhip khac nhau, luon o trong khung.
                    float width = Mathf.Max(0f, area.width * 0.5f);
                    float x = area.center.x + Mathf.Sin(Time.time * 0.35f + body.phase * 6.28f + body.offset) * width;
                    local = new Vector3(x, y, 0f);
                    break;
                }
                default:
                {
                    float t = Time.time * 0.12f;
                    float x = Mathf.Lerp(area.xMin, area.xMax, Mathf.PerlinNoise(t, body.phase * 7.3f));
                    float y = Mathf.Lerp(area.yMin, area.yMax, Mathf.PerlinNoise(body.phase * 3.1f, t));
                    y += Mathf.Sin(Time.time * 2.4f + body.phase * 6.28f) * mote.speed * 0.15f;
                    local = new Vector3(x, y, 0f);
                    break;
                }
            }
            body.sprite.position = new Vector3(Snap(local.x), Snap(local.y), 0f);
        }

        static float Snap(float v) => Mathf.Round(v / Pixel) * Pixel;
    }
}
