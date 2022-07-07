using System;
using TMPro;
using UnityEngine;

namespace Game.UI {
    /// <summary>
    /// FontAwesome glyph codepoints (FA 4.7) and a factory for icon labels. Icons are just text in the
    /// icon font, so they're vector-crisp, themeable, and lay out like any other label. Glyphs live in
    /// the Private Use Area, so they're built from codepoints rather than literals.
    /// </summary>
    public static class Icon {
        public static readonly string Close = Glyph(0xF00D);
        public static readonly string Check = Glyph(0xF00C);
        public static readonly string ChevronDown = Glyph(0xF078);
        public static readonly string AngleLeft = Glyph(0xF104);
        public static readonly string AngleRight = Glyph(0xF105);
        public static readonly string Search = Glyph(0xF002);
        public static readonly string Cog = Glyph(0xF013);
        public static readonly string Play = Glyph(0xF04B);
        public static readonly string Pause = Glyph(0xF04C);
        public static readonly string Github = Glyph(0xF09B);
        public static readonly string Info = Glyph(0xF05A);
        public static readonly string Eye = Glyph(0xF06E);
        public static readonly string Grid = Glyph(0xF00A);
        public static readonly string Brush = Glyph(0xF1FC);
        public static readonly string Sliders = Glyph(0xF1DE);
        public static readonly string Bolt = Glyph(0xF0E7);
        public static readonly string Arrows = Glyph(0xF047);
        public static readonly string Random = Glyph(0xF074);
        public static readonly string Signs = Glyph(0xF277);

        private static string Glyph(int codepoint) => char.ConvertFromUtf32(codepoint);

        public static TextMeshProUGUI Make(Transform parent, string glyph, float size, Color color) {
            var go = new GameObject("Icon", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.font = Fonts.Icons;
            label.text = glyph;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.raycastTarget = false;
            return label;
        }
    }
}
