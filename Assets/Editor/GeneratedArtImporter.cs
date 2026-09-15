using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Ap dat luat import cua du an cho moi texture duoi Assets/Art/Generated/.
/// Luat lay tu docs/project-state/art-pipeline.md muc "Unity import".
///
/// Vi sao dat cap maxTextureSize NGAY TRONG OnPreprocessTexture: atlas rong hon maxTextureSize bi
/// Unity thu nho IM LANG, keo theo moi slice rect tinh sai va mat frame cuoi, khong co error nao
/// bao. Dat o menu item hay cho khac se bi postprocessor ghi de lai o lan import ke tiep.
/// </summary>
public class GeneratedArtImporter : AssetPostprocessor
{
    const string Root = "Assets/Art/Generated/";
    const int PixelsPerUnit = 100;   // mot PPU chung cho ca game; khong bu scale rieng tung action

    [Serializable]
    class ScaleProfile
    {
        public float[] output_origin;   // [x, y] trong toa do anh, y tinh TU TREN xuong
    }

    [Serializable]
    class MetaFrame
    {
        public int[] aligned_bbox;      // [x0, y0, x1, y1]; y1 la day sau khi da can
    }

    [Serializable]
    class PipelineMeta
    {
        public string align;            // "feet" | "bottom" | "center"
        public int cell_size;
        public MetaFrame[] frames;
    }

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Root, StringComparison.Ordinal)) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        // Doc kich thuoc GOC, khong doc tex.width (do la kich thuoc SAU import, da bi thu nho).
        int w, h;
        importer.GetSourceTextureWidthAndHeight(out w, out h);
        importer.maxTextureSize = FitMaxTextureSize(Math.Max(w, h));

        ApplyFootPivot(importer, w, h);
        ApplySliceBorder(importer);
    }

    /// <summary>Nac maxTextureSize nho nhat con >= canh dai nhat cua anh goc.</summary>
    static int FitMaxTextureSize(int longestSide)
    {
        int[] steps = { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384 };
        foreach (int s in steps)
            if (s >= longestSide) return s;
        return 16384;
    }

    /// <summary>
    /// Pivot chan do tu duong dat THAT cua chinh action do (`aligned_bbox` trong pipeline-meta.json
    /// nam cung thu muc), khong copy hang so tu asset khac va KHONG dung chung mot pivot cho ca unit.
    ///
    /// Vi sao khong dung `output_origin` cua character-scale-profile.json cho moi action: profile
    /// ghim theo idle, nhung action doi posture lon duoc xu ly voi anchor rieng. Do that tren
    /// Ironblade (2026-09-11): idle/walk co day alpha = 92 dung bang output_origin, nhung attack
    /// lech +15..+17 px, hurt +14..+15, death +13..+14 — dung pivot cua profile cho ca bo thi ba
    /// action do bi CHON xuong dat ~15 px va khong co loi nao bao. Goblin thi moi action deu = 85
    /// nen loi nay khong lo ra o do.
    ///
    /// FX (`align: center`) khong co chan — giu pivot mac dinh.
    /// Khong tim thay meta lan profile (UI, background, castle...) thi cung giu mac dinh.
    /// </summary>
    void ApplyFootPivot(TextureImporter importer, int sourceWidth, int sourceHeight)
    {
        if (sourceWidth <= 0 || sourceHeight <= 0) return;

        float groundRow;
        if (!TryGetGroundRow(out groundRow, sourceHeight)) return;

        // groundRow tinh tu TREN xuong; pivot cua Unity chuan hoa tu GOC DUOI-TRAI.
        // TextureImporter khong co spriteAlignment — alignment chi dat duoc qua TextureImporterSettings.
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(0.5f, (sourceHeight - groundRow) / sourceHeight);
        importer.SetTextureSettings(settings);
    }

    [Serializable]
    class SliceBorder
    {
        public int left, right, top, bottom;
    }

    /// <summary>
    /// Border 9-slice doc tu `slice.json` cung thu muc (lane UI giao kem). Khong co border thi
    /// Image.type = Sliced ve nhu Simple — keo dan ca goc, khong co loi nao bao.
    /// </summary>
    void ApplySliceBorder(TextureImporter importer)
    {
        string dir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
        string slicePath = dir + "/slice.json";
        if (!File.Exists(slicePath)) return;

        // JsonUtility khong doc duoc dictionary -> boc tung entry theo ten file.
        string json = File.ReadAllText(slicePath);
        string key = "\"" + Path.GetFileName(assetPath) + "\"";
        int at = json.IndexOf(key, StringComparison.Ordinal);
        if (at < 0) return;
        int open = json.IndexOf('{', at);
        int close = json.IndexOf('}', open);
        var b = JsonUtility.FromJson<SliceBorder>(json.Substring(open, close - open + 1));

        // Thu tu Vector4 cua Unity: (left, bottom, right, top).
        importer.spriteBorder = new Vector4(b.left, b.bottom, b.right, b.top);
    }

    /// <summary>Duong dat cua action: trung vi `aligned_bbox[3]` cua cac frame trong action do.</summary>
    bool TryGetGroundRow(out float groundRow, int sourceHeight)
    {
        groundRow = 0f;
        string dir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
        string metaPath = dir + "/pipeline-meta.json";

        if (File.Exists(metaPath))
        {
            PipelineMeta meta = null;
            try { meta = JsonUtility.FromJson<PipelineMeta>(File.ReadAllText(metaPath)); }
            catch (Exception e) { Debug.LogWarning($"[GeneratedArtImporter] Doc {metaPath} that bai: {e.Message}"); }

            if (meta != null && meta.align == "center") return false;   // FX: khong co chan

            if (meta?.frames != null && meta.frames.Length > 0)
            {
                var bottoms = meta.frames
                    .Where(f => f.aligned_bbox != null && f.aligned_bbox.Length >= 4)
                    .Select(f => f.aligned_bbox[3])
                    .OrderBy(v => v)
                    .ToArray();
                if (bottoms.Length > 0)
                {
                    groundRow = bottoms[bottoms.Length / 2];
                    return true;
                }
            }
        }

        // Du phong: output_origin cua unit (dung cho identity/preview khong co meta rieng).
        string profilePath = FindScaleProfile(assetPath);
        if (profilePath == null) return false;
        try
        {
            var profile = JsonUtility.FromJson<ScaleProfile>(File.ReadAllText(profilePath));
            if (profile?.output_origin == null || profile.output_origin.Length < 2) return false;
            groundRow = profile.output_origin[1];
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GeneratedArtImporter] Doc {profilePath} that bai: {e.Message}");
            return false;
        }
    }

    /// <summary>Di nguoc len tung thu muc cha tim character-scale-profile.json, dung o Root.</summary>
    static string FindScaleProfile(string path)
    {
        string dir = Path.GetDirectoryName(path)?.Replace('\\', '/');
        while (!string.IsNullOrEmpty(dir) && dir.StartsWith(Root.TrimEnd('/'), StringComparison.Ordinal))
        {
            string candidate = dir + "/character-scale-profile.json";
            if (File.Exists(candidate)) return candidate;
            dir = Path.GetDirectoryName(dir)?.Replace('\\', '/');
        }
        return null;
    }

    // ---------------------------------------------------------------- menu

    [MenuItem("BuildBuild/Art/Reimport Generated Art")]
    static void ReimportAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Root.TrimEnd('/') });
        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                EditorUtility.DisplayProgressBar("Reimport Generated Art", path, (float)i / guids.Length);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
        Debug.Log($"[GeneratedArtImporter] Da reimport {guids.Length} texture duoi {Root}");
    }

    /// <summary>
    /// In ra thu Unity THAT SU doc duoc sau import, khong phai thu ghi trong .meta.
    /// Da co tien le meta ghi mot dang ma Unity doc ra gia tri khac, nen nghiem thu bang bao cao nay.
    /// </summary>
    [MenuItem("BuildBuild/Art/Verify Generated Art Import")]
    static void VerifyAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Root.TrimEnd('/') });
        var sb = new StringBuilder();
        sb.AppendLine("path | src WxH | imported WxH | maxSize | filter | compression | mipmap | PPU | pivot");
        int problems = 0;

        foreach (string guid in guids.OrderBy(g => AssetDatabase.GUIDToAssetPath(g)))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (importer == null || tex == null) continue;

            int sw, sh;
            importer.GetSourceTextureWidthAndHeight(out sw, out sh);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Vector2 pivot = sprite != null
                ? new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height)
                : importer.spritePivot;

            bool shrunk = tex.width != sw || tex.height != sh;
            bool blurry = tex.filterMode != FilterMode.Point;
            if (shrunk || blurry) problems++;

            sb.AppendLine($"{(shrunk || blurry ? "!! " : "   ")}{path} | {sw}x{sh} | {tex.width}x{tex.height} | " +
                          $"{importer.maxTextureSize} | {tex.filterMode} | {importer.textureCompression} | " +
                          $"{importer.mipmapEnabled} | {importer.spritePixelsPerUnit} | " +
                          $"({pivot.x:F4}, {pivot.y:F4})");
        }

        sb.AppendLine($"\nTong: {guids.Length} texture, {problems} co van de (bi thu nho hoac khong Point filter).");
        Debug.Log(sb.ToString());
    }
}
