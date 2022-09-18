using UnityEngine;

namespace Game.UI {
    /// <summary>
    /// Design tokens for the UI kit - palette, spacing, type scale, radii. One place to retheme the
    /// whole interface, the way CSS variables work. Dark surface with a single cyan accent.
    /// </summary>
    public static class Theme {
        public static readonly Color Window = Hex("#171A21");
        public static readonly Color Surface = Hex("#1B1F27");
        public static readonly Color SurfaceRaised = Hex("#232834");
        public static readonly Color Track = Hex("#2C3340");
        public static readonly Color Border = Hex("#313845");
        public static readonly Color Text = Hex("#E7E9EE");
        public static readonly Color Muted = Hex("#888F9C");
        public static readonly Color Accent = Hex("#29D3E6");
        public static readonly Color AccentText = Hex("#08191C");
        public static readonly Color Danger = Hex("#E0564E");
        public static readonly Color Warning = Hex("#E0A73E");

        // spacing scale (px @ reference resolution)
        public const float Space1 = 4f;
        public const float Space2 = 8f;
        public const float Space3 = 12f;
        public const float Space4 = 16f;
        public const float Space6 = 24f;

        // corner radii - lean square, very mild rounding (larger surfaces get a touch more)
        public const float RadiusSm = 2f;
        public const float RadiusMd = 3f;
        public const float RadiusLg = 5f;

        // type scale - small and dense reads more technical
        public const float Caption = 12f;
        public const float Body = 15f;
        public const float Title = 24f;

        private static Color Hex(string hex) {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }
    }
}
