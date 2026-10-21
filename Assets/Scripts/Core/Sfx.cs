using System.Collections.Generic;
using BuildBuild.Meta;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BuildBuild.Core
{
    /// <summary>
    /// Phat am thanh theo TEN file trong `Assets/Resources/Audio/` (SFX/, Jingles/, Music/) — khong can gan tham chieu
    /// trong scene, ten tieng rong = im. Nguon + license tung file: `Resources/Audio/SOURCES.md`.
    ///
    /// Tower defense co hang chuc don trung cung luc: moi tieng chi phat lai sau `MinGap` giay (don trong khoang do bi
    /// nuot), va chi co `Voices` kenh — het kenh thi tieng moi bi bo, khong cat tieng dang keu. Pitch lech ngau nhien
    /// +-`PitchJitter` de mot file lap lai khong nghe may moc.
    ///
    /// Tieng bam nut bat CHUNG o day (tha tay tren mot `Button`), vi nut trong the gacha / doi quan duoc Instantiate luc
    /// chay — gan listener tung nut se sot. Nut xam (khong bam duoc, vd thieu vang) keu `ui-error`. Chuot re vao mot nut
    /// bam duoc thi keu `ui-hover` (cung bat chung o day; cam ung khong co hover).
    /// </summary>
    public static class Sfx
    {
        const int Voices = 16;
        const float MinGap = 0.06f;
        const float PitchJitter = 0.06f;

        /// <summary>Am luong nhac (man Cai dat). Truoc day la hang so 0,4 — nay doc/ghi tu save va ap dung ngay.</summary>
        public static float MusicGain
        {
            get => Mathf.Clamp01(SaveSystem.Data.musicVolume);
            set
            {
                SaveSystem.Data.musicVolume = Mathf.Clamp01(value);
                SaveSystem.Save();
                ApplyVolume();
            }
        }

        /// <summary>Tat tieng hieu ung (nhac van theo `MusicGain`) — man Cai dat.</summary>
        public static bool Muted
        {
            get => SaveSystem.Data.sfxMuted;
            set
            {
                SaveSystem.Data.sfxMuted = value;
                SaveSystem.Save();
                ApplyVolume();
            }
        }

        static void ApplyVolume()
        {
            if (_music != null) _music.volume = MusicGain;
        }

        // Tieng nghe nhieu nhat trong tran thi nho lai de khong at nhac / tieng hiem.
        static readonly Dictionary<string, float> Gain = new Dictionary<string, float>
        {
            ["hit-melee"] = 0.55f, ["hit-blunt"] = 0.5f, ["hit-arrow"] = 0.6f, ["shot-bow"] = 0.6f,
            ["enemy-die"] = 0.6f, ["cannon-fire"] = 0.6f, ["castle-hit"] = 0.7f,
        };

        static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>();
        static readonly Dictionary<string, float> LastPlayed = new Dictionary<string, float>();
        static AudioSource[] _voices;
        static AudioSource _music;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Clips.Clear();
            LastPlayed.Clear();
            _voices = null;
            _music = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() => EnsureRoot();

        /// <summary>Tieng ngan trong `SFX/` (hoac `Jingles/`). `id` rong / khong co file thi im.</summary>
        public static void Play(string id)
        {
            if (string.IsNullOrEmpty(id) || Muted) return;
            float now = Time.unscaledTime;
            if (LastPlayed.TryGetValue(id, out float last) && now - last < MinGap) return;
            var clip = Load("SFX/" + id) ?? Load("Jingles/" + id);
            if (clip == null) return;
            EnsureRoot();
            foreach (var voice in _voices)
            {
                if (voice.isPlaying) continue;
                LastPlayed[id] = now;
                voice.clip = clip;
                voice.volume = Gain.TryGetValue(id, out float gain) ? gain : 1f;
                // Jingle la nhac: lech pitch la lech tong.
                voice.pitch = id.StartsWith("jingle") ? 1f : 1f + Random.Range(-PitchJitter, PitchJitter);
                voice.Play();
                return;
            }
        }

        /// <summary>Nhac nen lap trong `Music/`; goi lai cung bai thi khong phat lai tu dau.</summary>
        public static void Music(string id)
        {
            EnsureRoot();
            var clip = Load("Music/" + id);
            if (clip == null || _music.clip == clip) return;
            _music.clip = clip;
            _music.Play();
        }

        static AudioClip Load(string path)
        {
            if (!Clips.TryGetValue(path, out var clip))
            {
                clip = Resources.Load<AudioClip>("Audio/" + path);
                Clips[path] = clip;
            }
            return clip;
        }

        static void EnsureRoot()
        {
            if (_voices != null && _music != null) return;
            var root = new GameObject("Audio");
            Object.DontDestroyOnLoad(root);
            _voices = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                _voices[i] = root.AddComponent<AudioSource>();
                _voices[i].playOnAwake = false;
            }
            _music = root.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;
            _music.volume = MusicGain;
            root.AddComponent<SfxRunner>();
        }
    }

    /// <summary>Song tren object "Audio" xuyen scene: chon nhac theo man (sanh / tran Chinh Phat / tran thuong) + tieng nut (bam, re chuot).</summary>
    class SfxRunner : MonoBehaviour
    {
        static readonly List<RaycastResult> Hits = new List<RaycastResult>();
        Button _hovered;

        void Update()
        {
            // Scene "Battle" la tran; Gacha / Units la sanh.
            Sfx.Music(SceneManager.GetActiveScene().name != "Battle" ? "music-lobby"
                : ConquestMatch.Current != null ? "music-conquest" : "music-battle");

            var pointer = Pointer.current;
            var events = EventSystem.current;
            if (pointer == null || events == null) return;

            // Hover chi voi chuot: tren cam ung "con tro" chi co luc cham, keu hover ngay truoc tieng bam.
            if (pointer is Mouse)
            {
                var hovered = ButtonUnder(pointer, events);
                if (hovered != _hovered && hovered != null && hovered.interactable) Sfx.Play("ui-hover");
                _hovered = hovered;
            }

            if (!pointer.press.wasReleasedThisFrame) return;
            var button = ButtonUnder(pointer, events);
            if (button == null) return;
            if (!button.interactable) Sfx.Play("ui-error");
            else Sfx.Play(IsBack(button.name) ? "ui-back" : "ui-click");
        }

        static Button ButtonUnder(Pointer pointer, EventSystem events)
        {
            Hits.Clear();
            events.RaycastAll(new PointerEventData(events) { position = pointer.position.ReadValue() }, Hits);
            return Hits.Count == 0 ? null : Hits[0].gameObject.GetComponentInParent<Button>();
        }

        // Ten nut SceneBuilder dat: "Close", "Cancel", "BackButton".
        static bool IsBack(string name) =>
            name.Contains("Close") || name.Contains("Cancel") || name.Contains("Back");
    }
}
