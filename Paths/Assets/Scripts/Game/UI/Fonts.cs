using TMPro;
using UnityEngine;

namespace Game.UI {
    /// <summary>
    /// Loads the kit's typefaces (Inter, built into Resources by the editor FontBuilder). Regular for
    /// body copy, SemiBold for headings/buttons. Falls back to the TMP default if the assets are missing.
    /// </summary>
    public static class Fonts {
        private static TMP_FontAsset _regular;
        private static TMP_FontAsset _semiBold;
        private static TMP_FontAsset _icons;
        private static TMP_FontAsset _mono;

        public static TMP_FontAsset Regular => _regular != null ? _regular : (_regular = Load("Fonts/Inter"));
        public static TMP_FontAsset SemiBold => _semiBold != null ? _semiBold : (_semiBold = Load("Fonts/Inter-SemiBold"));
        public static TMP_FontAsset Icons => _icons != null ? _icons : (_icons = Load("Fonts/Icons"));
        public static TMP_FontAsset Mono => _mono != null ? _mono : (_mono = Load("Fonts/Mono"));

        private static TMP_FontAsset Load(string path) {
            TMP_FontAsset font = Resources.Load<TMP_FontAsset>(path);
            return font != null ? font : TMP_Settings.defaultFontAsset;
        }
    }
}
