using UnityEngine;

namespace BuildBuild.Core
{
    /// <summary>
    /// Font pixel cua toan game: fusion-pixel-10px-proportional (OFL, xem
    /// `Assets/Resources/Fonts/FusionPixel10-LICENSES/`), co thiet ke <see cref="DesignPx"/> px.
    ///
    /// Vi sao la asset trong `Assets/` chu khong phai font built-in: `Resources.GetBuiltinResource`
    /// mat tham chieu sau lan `NewScene` thu hai khi build scene (man Gacha tung luu 13 cho
    /// `m_Font: {fileID: 0}` — nut/bang trong tron, khong loi console).
    ///
    /// Co chu phai di qua <see cref="Snap"/>: co lech khoi boi so cua DesignPx thi khoi pixel cua
    /// font bi ve le 1 px (net so le).
    /// </summary>
    public static class PixelFont
    {
        /// <summary>Co thiet ke cua font, don vi px — moi co chu trong UI phai la boi so cua so nay.</summary>
        public const int DesignPx = 10;

        static Font _asset;

        /// <summary>Font dung cho moi chu trong game (UI + TextMesh), co cache.</summary>
        public static Font Asset
        {
            get
            {
                if (_asset == null)
                {
                    _asset = Resources.Load<Font>("Fonts/FusionPixel10");
                    if (_asset == null)
                    {
                        Debug.LogWarning("[PixelFont] Thieu Assets/Resources/Fonts/FusionPixel10.ttf, tam dung font built-in.");
                        _asset = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    }
                }
                return _asset;
            }
        }

        /// <summary>Lam tron co chu ve boi so nguyen cua <see cref="DesignPx"/> (nho nhat 1 lan).</summary>
        public static int Snap(int size)
        {
            return Mathf.Max(DesignPx, Mathf.RoundToInt(size / (float)DesignPx) * DesignPx);
        }
    }
}
