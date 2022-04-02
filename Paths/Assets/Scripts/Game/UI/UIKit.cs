using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI {
    /// <summary>
    /// Factory helpers that build the kit's primitives - panels, buttons, fields, sliders, tabs - from
    /// the Theme tokens and the RoundedRect graphic. Everything is code-built and laid out with layout
    /// groups (uGUI's flexbox), so containers size to their content instead of being hand-placed.
    /// </summary>
    public static class UIKit {
        public enum Type { Caption, Body, Title }

        /// <summary>A rounded surface card with a vertical layout group. Returns its RectTransform.</summary>
        public static RectTransform Panel(Transform parent, Color fill, float radius = Theme.RadiusMd,
            float padding = Theme.Space4, float spacing = Theme.Space2, bool shadow = true, bool fit = true,
            bool fitWidth = false) {
            var rt = New("Panel", parent);
            var bg = rt.gameObject.AddComponent<RoundedRectGraphic>();
            bg.color = fill;
            bg.cornerRadius = radius;
            bg.shadowSize = shadow ? 22f : 0f;
            bg.raycastTarget = false;
            // a content-hugging card (HUD widgets) shouldn't force its children to fill its width
            Vertical(rt.gameObject, padding, spacing, expandWidth: !fitWidth);
            if (fit) Fit(rt.gameObject, fitWidth);
            return rt;
        }

        /// <summary>A transparent vertical stack.</summary>
        public static RectTransform Column(Transform parent, float spacing = Theme.Space2, float padding = 0f) {
            var rt = New("Column", parent);
            Vertical(rt.gameObject, padding, spacing);
            return rt;
        }

        /// <summary>A transparent horizontal stack (a form row, a button group).</summary>
        public static RectTransform Row(Transform parent, float spacing = Theme.Space3,
            TextAnchor align = TextAnchor.MiddleLeft) {
            var rt = New("Row", parent);
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childAlignment = align;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            return rt;
        }

        public static TextMeshProUGUI Label(Transform parent, TMP_FontAsset font, string text,
            Type type = Type.Body, Color? color = null) {
            var rt = New("Label", parent);
            var label = rt.gameObject.AddComponent<TextMeshProUGUI>();
            // titles read better in the heavier cut; everything else takes the caller's font
            TMP_FontAsset face = type == Type.Title ? Fonts.SemiBold : font;
            if (face != null) label.font = face;
            label.text = text;
            label.fontSize = Size(type);
            label.color = color ?? Theme.Text;
            label.raycastTarget = false;
            label.enableWordWrapping = false;
            return label;
        }

        /// <summary>
        /// A letter-spaced section heading with a hairline rule running off to the right - the header and
        /// the divider are one element, sharing the muted/border palette.
        /// </summary>
        public static void SectionHeader(Transform parent, TMP_FontAsset font, string text, string icon = null) {
            RectTransform row = Row(parent, Theme.Space3);
            row.gameObject.AddComponent<LayoutElement>().minHeight = 26f;
            if (icon != null) Icon.Make(row, icon, Theme.Caption + 1f, Theme.Muted);
            TextMeshProUGUI label = Label(row, Fonts.SemiBold, text.ToUpperInvariant(), Type.Caption, Theme.Muted);
            label.characterSpacing = 5f;

            RectTransform rule = New("Rule", row);
            var rle = rule.gameObject.AddComponent<LayoutElement>();
            rle.flexibleWidth = 1f;
            rle.minHeight = 12f;
            RectTransform line = New("Line", rule);
            var g = line.gameObject.AddComponent<RoundedRectGraphic>();
            g.color = Theme.Border;
            g.cornerRadius = 0f;
            g.raycastTarget = false;
            CenterBand(line, 1f);
        }

        /// <summary>A 1px horizontal rule.</summary>
        public static void Divider(Transform parent) {
            var rt = New("Divider", parent);
            var line = rt.gameObject.AddComponent<RoundedRectGraphic>();
            line.color = Theme.Border;
            line.cornerRadius = 0f;
            line.raycastTarget = false;
            var element = rt.gameObject.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = 1f;
            element.flexibleWidth = 1f;
        }

        private static float Size(Type type) {
            switch (type) {
                case Type.Caption: return Theme.Caption;
                case Type.Title: return Theme.Title;
                default: return Theme.Body;
            }
        }

        private static RectTransform New(string name, Transform parent) {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform) go.transform;
        }

        private static void Vertical(GameObject go, float padding, float spacing, bool expandWidth = true) {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset((int) padding, (int) padding, (int) padding, (int) padding);
            v.spacing = spacing;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = expandWidth;
            v.childForceExpandHeight = false;
        }

        private static void Fit(GameObject go, bool fitWidth = false) {
            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (fitWidth) fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void CenterBand(RectTransform rt, float height) {
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(0f, height);
            rt.anchoredPosition = Vector2.zero;
        }
    }
}
