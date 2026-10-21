using System;
using BuildBuild.Core;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Cho nguoi choi THAY don danh trung: chop mau tren than + so bay len tren dau (-20 an don, +15 hoi / hut mau,
    /// "Chan" khi mien sat thuong). Chi nghe event cua `Health` — moi nguon (hero, linh, phao, boss, hut mau
    /// Chainwarden, hoi mau Vespera) tu dong co feedback, khong nguon nao phai goi gi them.
    ///
    /// Chop mau ghi vao `_Color` cua shader qua MaterialPropertyBlock, KHONG ghi `renderer.color`: kenh do da co
    /// Enemy / EnemyExtras / Hero dat tint dong bang, choang, cham, cuong no moi frame — ghi chung thi de nhau.
    /// `_Color` nhan them len tint do (Sprite-Lit-Default: vertex color * _Color * unity_SpriteColor).
    /// </summary>
    public class CombatFeedback : MonoBehaviour
    {
        static readonly int ColorId = Shader.PropertyToID("_Color");

        const float FlashSeconds = 0.12f;
        static readonly Color HurtFlash = new Color(1f, 0.3f, 0.3f);
        static readonly Color HealFlash = new Color(0.55f, 1f, 0.55f);

        // Quai an don: vang nhat (don cua ta). Phe ta an don: do (dang mat mau). Hoi / hut mau: xanh la.
        static readonly Color EnemyHurtText = new Color(1f, 0.98f, 0.9f);
        static readonly Color AllyHurtText = new Color(1f, 0.35f, 0.3f);
        static readonly Color HealText = new Color(0.45f, 1f, 0.45f);
        static readonly Color BlockedText = new Color(0.8f, 0.8f, 0.85f);

        Health _health;
        SpriteRenderer _renderer;
        MaterialPropertyBlock _block;
        Func<float> _bodyHeight;
        bool _ally;
        float _flashLeft;
        public static bool Enhanced = true;
        MeshRenderer _silhouette;
        MeshFilter _silhouetteMesh;
        Sprite _silhouetteSprite;
        MaterialPropertyBlock _silhouetteBlock;
        float _whiteAge = 1f;
        float _impactGap = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetEnhanced() => Enhanced = true;

        /// <summary>Lop feedback moi chi bat dau khi A/B dang bat va tran khong pause.</summary>
        public static bool PresentationOn => Enhanced && Time.timeScale > 0f;

        /// <summary>Tieng cua lop feedback moi — chung mot cua bat/tat voi lop nhin.</summary>
        public static void Cue(string sfx)
        {
            if (Enhanced) Sfx.Play(sfx);
        }

        /// <summary>
        /// Don nang da THUC SU tru mau: mot cho duy nhat quyet dinh rung + tieng va cham, de hero moi
        /// khong phai tu lap lai `if (Enhanced) Sfx.Play(...)` + `CombatCameraImpulse.Kick(...)`.
        /// </summary>
        public static void HeavyHit(Vector2 direction, float amplitude, string sfx = null)
        {
            if (!PresentationOn) return;
            CombatCameraImpulse.Kick(direction, amplitude);
            if (sfx != null) Sfx.Play(sfx);
        }

        // Gom cac don trong CUNG mot frame thanh mot so (Loc Xich trung 5 quai -> hut mau 5 lan cung frame).
        float _pendingDamage;
        float _pendingHeal;
        bool _pendingBlocked;

        /// <param name="ally">Phe ta (hero, linh) — so an don mau do; quai thi mau vang.</param>
        /// <param name="bodyHeight">Chieu cao than theo toa do the gioi — so hien ngay tren thanh mau.</param>
        public void Init(bool ally, Func<float> bodyHeight)
        {
            _ally = ally;
            _bodyHeight = bodyHeight;
        }

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _health = GetComponent<Health>();
            _block = new MaterialPropertyBlock();
            _health.Damaged += OnDamaged;
            _health.Impacted += OnImpact;
            _health.Healed += OnHealed;
            _health.Blocked += OnBlocked;
        }

        void OnDestroy()
        {
            if (_health == null) return;
            _health.Damaged -= OnDamaged;
            _health.Impacted -= OnImpact;
            _health.Healed -= OnHealed;
            _health.Blocked -= OnBlocked;
        }

        void OnDamaged(float amount)
        {
            _pendingDamage += amount;
            Flash(HurtFlash);
        }

        void OnHealed(float amount)
        {
            _pendingHeal += amount;
            // Dang chop do vi vua an don thi giu do: an don doc quan trong hon hoi mau.
            if (_flashLeft <= 0f) Flash(HealFlash);
        }

        void OnImpact(Vector2 direction)
        {
            if (!PresentationOn || _impactGap < 0.09f) return;
            _impactGap = 0f;
            _whiteAge = -Time.unscaledDeltaTime;
            if (_silhouette == null && _renderer != null)
            {
                var go = new GameObject("Hit silhouette");
                go.transform.SetParent(transform, false);
                go.layer = gameObject.layer;
                _silhouetteMesh = go.AddComponent<MeshFilter>();
                _silhouette = go.AddComponent<MeshRenderer>();
                _silhouette.sharedMaterial = CombatAccent.Material;
                _silhouetteBlock = new MaterialPropertyBlock();
            }
            float height = _bodyHeight != null ? _bodyHeight() : 0.5f;
            HitSpark.Spawn(transform.position + Vector3.up * height * 0.5f, direction, height);
        }

        void OnBlocked(float amount, Health source) => _pendingBlocked = true;

        // Cung mot dong ho cho ca hai lop: truoc day tint do dem bang Time.time (0,12 giay GAME = 40 ms
        // thuc o x3) con silhouette trang dem 65 ms THUC, nen o x3 trang nuot gan het do. Nay ca hai dem
        // giay thuc va chi tru khi tran dang chay (pause dong bang ca hai).
        void Flash(Color color)
        {
            _flashLeft = FlashSeconds;
            SetTint(color);
        }

        void SetTint(Color color)
        {
            if (_renderer == null) return;
            _renderer.GetPropertyBlock(_block);
            _block.SetColor(ColorId, color);
            _renderer.SetPropertyBlock(_block);
        }

        void LateUpdate()
        {
            if (Time.timeScale > 0f)
            {
                _whiteAge += Time.unscaledDeltaTime;
                _impactGap += Time.unscaledDeltaTime;
                if (_flashLeft > 0f)
                {
                    _flashLeft -= Time.unscaledDeltaTime;
                    if (_flashLeft <= 0f) SetTint(Color.white);
                }
            }
            if (_silhouette != null)
            {
                _silhouette.enabled = Enhanced && _whiteAge < 0.065f && _renderer.enabled && _renderer.sprite != null;
                // `sharedMesh` co the da bi `CombatAccent.Release()` tha luc scene unload: dung lai mesh.
                if (_silhouette.enabled && (_silhouetteSprite != _renderer.sprite || _silhouetteMesh.sharedMesh == null))
                {
                    _silhouetteSprite = _renderer.sprite;
                    _silhouetteMesh.sharedMesh = CombatAccent.Silhouette(_silhouetteSprite);
                    _silhouetteBlock.SetTexture("_MainTex", _silhouetteSprite.texture);
                }
                // This alpha-only shader uses ordinary mesh vertices; mirror the child transform explicitly.
                _silhouette.transform.localScale = new Vector3(_renderer.flipX ? -1f : 1f, _renderer.flipY ? -1f : 1f, 1f);
                _silhouette.sortingLayerID = _renderer.sortingLayerID;
                _silhouette.sortingOrder = _renderer.sortingOrder + 1;
                _silhouetteBlock.SetColor(ColorId, new Color(1f, 1f, 1f, 0.75f * _renderer.color.a));
                _silhouette.SetPropertyBlock(_silhouetteBlock);
            }
            if (_pendingDamage <= 0f && _pendingHeal <= 0f && !_pendingBlocked) return;

            float top = transform.position.y + (_bodyHeight != null ? _bodyHeight() : 0.5f);
            var head = new Vector3(transform.position.x, top, 0f);
            // Hai so cung luc (vua an don vua hut mau) thi xep chong, khong de len nhau.
            float stack = 0f;
            if (_pendingDamage > 0f)
            {
                FloatingNumber.Spawn(head, "-" + Round(_pendingDamage), _ally ? AllyHurtText : Enhanced ? EnemyHurtText : new Color(1f, 0.93f, 0.55f));
                stack += FloatingNumber.LineHeight;
            }
            if (_pendingHeal > 0f)
            {
                FloatingNumber.Spawn(head + Vector3.up * stack, "+" + Round(_pendingHeal), HealText);
                stack += FloatingNumber.LineHeight;
            }
            if (_pendingBlocked) FloatingNumber.Spawn(head + Vector3.up * stack, "Block", BlockedText);

            _pendingDamage = 0f;
            _pendingHeal = 0f;
            _pendingBlocked = false;
        }

        static int Round(float amount) => Mathf.Max(1, Mathf.RoundToInt(amount));
    }
}
