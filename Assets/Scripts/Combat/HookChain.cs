using System;
using System.Collections.Generic;
using BuildBuild.Core;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Hinh Nem Moc cua Chainwarden: dau moc (`WardenDefinition.hookFrames`) bay tu tay toi than quai, toi noi thi doi
    /// sang moc ngoam (`hookBite`), goi <c>onLatch</c> (Warden bat dau keo) roi bam theo quai trong `hookPullTime` giay.
    /// Doan xich (`chainLink`) rai lai tu tay toi moc MOI frame nen tu co gian theo quai. Chi la hinh — keo / choang /
    /// Xich nam o `Warden`. Quai chet giua duong thi moc bien mat, khong keo.
    /// Art ve QUAY TRAI: moc mo mieng sang trai, mat xich noi ve ben phai.
    /// </summary>
    public class HookChain : MonoBehaviour
    {
        const float FlySpeed = 40f;       // don vi the gioi / giay — tam 6 bay ~0,15 giay
        const float FrameDuration = 0.08f;
        const float ChainGap = 0.3f;      // xich noi vao duoi moc, khong vao tam moc (moc 35 art-px x UnitScale 2)

        Hero _hero;
        SpriteRenderer _heroRenderer;
        Vector3 _hand;          // toa do LOCAL art (quay trai), chua nhan scale
        Enemy _target;
        Sprite[] _fly;
        Sprite _bite;
        Sprite _link;
        float _linkLength;
        float _pullTime;
        Action<Enemy> _onLatch;

        SpriteRenderer _head;
        readonly List<SpriteRenderer> _links = new List<SpriteRenderer>();
        Vector3 _headPosition;
        float _latchedAt = -1f;
        float _frameTimer;
        int _frame;

        public static void Launch(Hero hero, Vector2 handLocal, Enemy target, Sprite[] fly, Sprite bite, Sprite link,
            float pullTime, Action<Enemy> onLatch)
        {
            var go = new GameObject("HookChain");
            var chain = go.AddComponent<HookChain>();
            chain._hero = hero;
            chain._heroRenderer = hero.GetComponent<SpriteRenderer>();
            chain._hand = handLocal;
            chain._target = target;
            chain._fly = fly;
            chain._bite = bite != null ? bite : fly[fly.Length - 1];
            chain._link = link;
            chain._linkLength = link != null ? link.bounds.size.x * Battlefield.UnitScale : 0f;
            chain._pullTime = pullTime;
            chain._onLatch = onLatch;

            chain._head = chain.NewRenderer("Hook", fly[0], SortOrder.Air + 1);
            chain._headPosition = chain.HandPosition();
            chain.Place();
        }

        void Update()
        {
            if (_hero == null || _hero.IsDying || _target == null || _target.IsDying)
            {
                Destroy(gameObject);
                return;
            }

            var aim = _target.BodyCenter;
            if (_latchedAt < 0f)
            {
                _headPosition = Vector3.MoveTowards(_headPosition, aim, FlySpeed * Time.deltaTime);
                _frameTimer += Time.deltaTime;
                if (_frameTimer >= FrameDuration)
                {
                    _frameTimer = 0f;
                    _frame = (_frame + 1) % _fly.Length;
                    _head.sprite = _fly[_frame];
                }
                if ((_headPosition - aim).sqrMagnitude < 0.0001f)
                {
                    _latchedAt = Time.time;
                    _head.sprite = _bite;
                    _onLatch?.Invoke(_target);
                }
            }
            else
            {
                _headPosition = aim;
                if (Time.time - _latchedAt >= _pullTime)
                {
                    Destroy(gameObject);
                    return;
                }
            }
            Place();
        }

        /// <summary>Tay cam xich cua Chainwarden luc nay (lat theo huong dang nhin).</summary>
        Vector3 HandPosition()
        {
            bool facingRight = _heroRenderer != null && _heroRenderer.flipX;
            var scale = _hero.transform.lossyScale;
            return _hero.transform.position +
                new Vector3((facingRight ? -_hand.x : _hand.x) * scale.x, _hand.y * scale.y, 0f);
        }

        void Place()
        {
            var hand = HandPosition();
            var delta = _headPosition - hand;
            float distance = delta.magnitude;
            var dir = distance > 0.0001f ? delta / distance : Vector3.left;

            // Art moc quay trai: bay sang phai thi lat X roi xoay theo huong, de mieng moc khong up nguoc.
            _head.transform.position = _headPosition;
            _head.flipX = dir.x > 0f;
            float headAngle = dir.x > 0f ? Mathf.Atan2(dir.y, dir.x) : Mathf.Atan2(-dir.y, -dir.x);
            _head.transform.rotation = Quaternion.Euler(0f, 0f, headAngle * Mathf.Rad2Deg);

            float span = Mathf.Max(0f, distance - ChainGap);
            int count = _link == null || _linkLength <= 0f ? 0 : Mathf.CeilToInt(span / _linkLength);
            var linkRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            while (_links.Count < count) _links.Add(NewRenderer("Link", _link, SortOrder.Air));
            for (int i = 0; i < _links.Count; i++)
            {
                bool on = i < count;
                _links[i].enabled = on;
                if (!on) continue;
                // Mat cuoi co the tho qua duoi moc mot chut — bi moc (ve sau cung) che.
                _links[i].transform.position = hand + dir * (_linkLength * (i + 0.5f));
                _links[i].transform.rotation = linkRotation;
            }
        }

        SpriteRenderer NewRenderer(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * Battlefield.UnitScale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }
    }
}
