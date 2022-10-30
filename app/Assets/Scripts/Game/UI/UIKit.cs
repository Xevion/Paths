using System;
using System.Collections.Generic;
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
        public enum ButtonStyle { Primary, Secondary, Ghost }

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

        /// <summary>
        /// A labelled form row: a fixed-width caption on the left, then the caller's control. By default a
        /// flexible gap pushes the control flush to the right edge so every row's control aligns; pass
        /// fill=true (sliders) to let a stretching control eat the gap instead.
        /// </summary>
        public static RectTransform Field(Transform parent, TMP_FontAsset font, string label,
            bool fill = false, string tooltip = null, float labelWidth = 150f) {
            RectTransform row = Row(parent);
            row.gameObject.AddComponent<LayoutElement>().minHeight = 34f;
            if (tooltip != null) AddTooltip(row.gameObject, tooltip);
            TextMeshProUGUI caption = Label(row, font, label);
            caption.color = Theme.Muted;
            Width(caption.gameObject, labelWidth);
            if (tooltip != null) DashUnderline(caption); // quiet "hover for info" affordance
            if (!fill) Flexible(row);
            return row;
        }

        // a dashed rule just under a label's text (sized to the text, not the whole slot), marking it as
        // tooltip-bearing. GetPreferredValues gives the text box up front so the dashes match its width.
        private static void DashUnderline(TextMeshProUGUI caption) {
            Vector2 pref = caption.GetPreferredValues(caption.text);
            var rt = New("Underline", caption.transform);
            var u = rt.gameObject.AddComponent<DashedUnderline>();
            u.color = new Color(Theme.Muted.r, Theme.Muted.g, Theme.Muted.b, 0.32f);
            u.raycastTarget = false;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f); // top-left of the label rect
            rt.sizeDelta = new Vector2(pref.x, 1f);
            rt.anchoredPosition = new Vector2(0f, -pref.y - 1f); // tuck just under the text baseline
        }

        /// <summary>Make a control hoverable for a tooltip. Adds a transparent raycast surface if needed.</summary>
        private static void AddTooltip(GameObject go, string text) {
            // Unity fires pointer enter/exit on the whole ancestor chain, so a transparent surface on the
            // row lets a hover anywhere on it (including over the control) raise the tooltip.
            if (go.GetComponent<Graphic>() == null) {
                var hit = go.AddComponent<Image>();
                hit.color = new Color(0f, 0f, 0f, 0f);
            }
            go.AddComponent<Tooltip>().text = text;
        }

        /// <summary>A flexible spacer that eats leftover space in a row/column.</summary>
        public static void Flexible(Transform parent) {
            var rt = New("Spacer", parent);
            rt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
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
        /// the divider are one element, sharing the muted/border palette. Pass rule=false for a bare
        /// heading where the rules would stack up (a short list of grouped rows).
        /// </summary>
        public static void SectionHeader(Transform parent, TMP_FontAsset font, string text, string icon = null,
            bool rule = true) {
            RectTransform row = Row(parent, Theme.Space3);
            row.gameObject.AddComponent<LayoutElement>().minHeight = 26f;
            if (icon != null) Icon.Make(row, icon, Theme.Caption + 1f, Theme.Muted);
            TextMeshProUGUI label = Label(row, Fonts.SemiBold, text.ToUpperInvariant(), Type.Caption, Theme.Muted);
            label.characterSpacing = 5f;
            if (!rule) return;

            RectTransform slot = New("Rule", row);
            var rle = slot.gameObject.AddComponent<LayoutElement>();
            rle.flexibleWidth = 1f;
            rle.minHeight = 12f;
            RectTransform line = New("Line", slot);
            var g = line.gameObject.AddComponent<RoundedRectGraphic>();
            g.color = Theme.Border;
            g.cornerRadius = 0f;
            g.raycastTarget = false;
            CenterBand(line, 1f);
        }

        /// <summary>The width a label actually needs, before any layout group has had a say.</summary>
        public static float Measure(TextMeshProUGUI label) {
            return label.GetPreferredValues(label.text).x;
        }

        /// <summary>
        /// Give a control smaller forms for an AdaptiveRow to fall back on, widest first. Each form
        /// reconfigures the control and sets the honest min width that form needs.
        /// </summary>
        public static AdaptiveSteps Adaptive(GameObject go, int yield, params Action[] forms) {
            var steps = go.AddComponent<AdaptiveSteps>();
            steps.Build(yield, forms);
            return steps;
        }

        /// <summary>A raised key cap sized to the key it names.</summary>
        public static void KeyCap(Transform parent, string key) {
            RectTransform cap = Panel(parent, Theme.SurfaceRaised, Theme.RadiusSm, 0f, 0f,
                shadow: false, fit: false, fitWidth: true);
            var group = cap.GetComponent<VerticalLayoutGroup>();
            group.padding = new RectOffset(7, 7, 3, 4);
            group.childAlignment = TextAnchor.MiddleCenter;
            cap.GetComponent<RoundedRectGraphic>().shadowSize = 4f; // the lift that makes it a key, not a field
            var element = cap.gameObject.AddComponent<LayoutElement>();
            element.minWidth = 26f;
            element.minHeight = 24f;
            Label(cap, Fonts.Mono, key, Type.Caption).alignment = TextAlignmentOptions.Center;
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

        /// <summary>A square icon-only button (close, etc.).</summary>
        public static Button IconButton(Transform parent, string glyph, Action onClick,
            ButtonStyle style = ButtonStyle.Ghost) {
            Button button = Button(parent, null, null, onClick, style, 36f, glyph);
            button.GetComponent<LayoutElement>().minWidth = 36f;
            button.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(0, 0, 0, 0);
            return button;
        }

        public static Button Button(Transform parent, TMP_FontAsset font, string text, Action onClick,
            ButtonStyle style = ButtonStyle.Primary, float minWidth = 96f, string icon = null) {
            var rt = New("Button", parent);
            var bg = rt.gameObject.AddComponent<RoundedRectGraphic>();
            bg.cornerRadius = Theme.RadiusSm;
            bg.raycastTarget = true;

            Color fill, textColor;
            bool ghost = style == ButtonStyle.Ghost;
            switch (style) {
                case ButtonStyle.Secondary: fill = Theme.SurfaceRaised; textColor = Theme.Text; break;
                // ghost starts invisible; its hover is carried by alpha, so the base stays full-alpha white
                case ButtonStyle.Ghost: fill = Color.white; textColor = Theme.Text; break;
                default: fill = Theme.Accent; textColor = Theme.AccentText; break;
            }
            bg.color = fill;

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.ColorTint;
            // ColorTint multiplies the base colour, so a 0.04-alpha base can't brighten - ghost drives
            // alpha instead (transparent at rest, a faint white wash on hover).
            button.colors = ghost ? GhostTint() : Tint();
            AddCursor(rt.gameObject, CursorKind.Hand);
            button.onClick.AddListener(() => onClick());

            // an inner horizontal group with side padding lets the button hug its label (+ padding) so
            // text never overflows; the LayoutElement minWidth is just a floor for short labels.
            var pad = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            pad.padding = new RectOffset(16, 16, 0, 0);
            pad.spacing = 7f;
            pad.childAlignment = TextAnchor.MiddleCenter;
            pad.childControlWidth = pad.childControlHeight = true;
            // hug content on BOTH axes - forcing expand makes the button advertise flexible size to its
            // parent row and balloon (first wide About buttons, then extremely tall ones).
            pad.childForceExpandWidth = pad.childForceExpandHeight = false;

            float content = pad.padding.left + pad.padding.right;
            if (icon != null) {
                TextMeshProUGUI glyph = Icon.Make(rt, icon, Theme.Body, textColor);
                content += Measure(glyph) + pad.spacing;
            }
            if (!string.IsNullOrEmpty(text)) {
                TextMeshProUGUI label = Label(rt, Fonts.SemiBold, text, Type.Body, textColor);
                label.alignment = TextAlignmentOptions.Center;
                content += Measure(label);
            }

            // fixed size, no flex - the row can't stretch it. the floor is whatever it actually draws,
            // never the caller's nominal width: a min narrower than the content squeezes the label under
            // the icon rather than clipping, and the two render on top of each other.
            var element = rt.gameObject.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = 34f;
            element.minWidth = Mathf.Max(minWidth, content);
            element.flexibleWidth = element.flexibleHeight = 0f;

            // a sheen sweep on hover for the solid styles (ghost has no fill to catch the light). a dark
            // fill needs far less white to read the same as the bright accent, or the band looks blown out.
            if (!ghost) {
                var shine = rt.gameObject.AddComponent<ShineSweep>();
                shine.duration = 0.4f;
                shine.peakAlpha = style == ButtonStyle.Primary ? 0.13f : 0.06f;
                shine.Build();
            }
            return button;
        }

        /// <summary>A labelled checkbox row (label left, box right) matching the other fields. Returns the Toggle.</summary>
        public static Toggle Checkbox(Transform parent, TMP_FontAsset font, string text, bool on,
            Action<bool> onChange) {
            RectTransform row = Field(parent, font, text);

            var boxRt = New("Box", row);
            var box = boxRt.gameObject.AddComponent<RoundedRectGraphic>();
            box.color = on ? Theme.Accent : Theme.SurfaceRaised;
            box.cornerRadius = Theme.RadiusSm;
            Size(boxRt.gameObject, 24f, 24f);

            TextMeshProUGUI check = Icon.Make(boxRt, Icon.Check, 13f, Theme.AccentText);
            Stretch(check.rectTransform);
            check.enabled = on;

            var toggle = row.gameObject.AddComponent<Toggle>();
            toggle.transition = Selectable.Transition.ColorTint;
            toggle.targetGraphic = box;
            toggle.colors = Tint();
            toggle.isOn = on;
            toggle.onValueChanged.AddListener(v => {
                box.color = v ? Theme.Accent : Theme.SurfaceRaised;
                check.enabled = v;
                onChange(v);
            });
            AddCursor(row.gameObject, CursorKind.Hand);
            return toggle;
        }

        /// <summary>An iOS-style switch: a pill track and a sliding knob. Returns the Toggle.</summary>
        public static Toggle Switch(Transform parent, bool on, Action<bool> onChange) {
            var rt = New("Switch", parent);
            Size(rt.gameObject, 46f, 26f);
            rt.gameObject.AddComponent<LayoutElement>();

            var track = rt.gameObject.AddComponent<RoundedRectGraphic>();
            track.color = on ? Theme.Accent : Theme.Track;
            track.cornerRadius = 13f;

            var knobRt = New("Knob", rt);
            var knob = knobRt.gameObject.AddComponent<RoundedRectGraphic>();
            knob.color = Color.white;
            knob.cornerRadius = 11f;
            knob.raycastTarget = false;
            knobRt.sizeDelta = new Vector2(20f, 20f);
            knobRt.anchorMin = knobRt.anchorMax = new Vector2(0f, 0.5f);
            knobRt.pivot = new Vector2(0.5f, 0.5f);
            knobRt.anchoredPosition = new Vector2(on ? 33f : 13f, 0f);

            var anim = rt.gameObject.AddComponent<UISwitch>();
            anim.knob = knobRt;
            anim.knobGraphic = knob;
            anim.track = track;
            anim.Init(on);

            var toggle = rt.gameObject.AddComponent<Toggle>();
            toggle.transition = Selectable.Transition.None;
            toggle.targetGraphic = track;
            toggle.isOn = on;
            toggle.onValueChanged.AddListener(v => {
                anim.Set(v);
                onChange(v);
            });
            AddCursor(rt.gameObject, CursorKind.Hand);
            return toggle;
        }

        /// <summary>A track + fill + round handle slider. Returns the Slider.</summary>
        public static Slider Slider(Transform parent, float min, float max, float value, Action<float> onChange) {
            var rt = New("Slider", parent);
            var element = rt.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 24f;
            element.flexibleWidth = 1f;

            var trackRt = New("Track", rt);
            var track = trackRt.gameObject.AddComponent<RoundedRectGraphic>();
            track.color = Theme.Track;
            track.cornerRadius = 3f;
            track.innerShadow = 1f; // the groove reads recessed
            track.raycastTarget = true;
            CenterBand(trackRt, 6f);

            var fillAreaRt = New("Fill Area", rt);
            CenterBand(fillAreaRt, 6f);
            var fillRt = New("Fill", fillAreaRt);
            var fill = fillRt.gameObject.AddComponent<RoundedRectGraphic>();
            fill.color = Theme.Accent;
            fill.cornerRadius = 3f;
            fill.raycastTarget = false;
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(1f, 1f);
            fillRt.sizeDelta = Vector2.zero;

            // Slider drives the handle to anchorMin=(val,0)/anchorMax=(val,1), so it stretches over the
            // slide area's height - keep the area a fixed-height band (inset on x by the handle radius) so
            // the handle reads as a round 18px knob instead of a tall sliver.
            var handleAreaRt = New("Handle Slide Area", rt);
            handleAreaRt.anchorMin = new Vector2(0f, 0.5f);
            handleAreaRt.anchorMax = new Vector2(1f, 0.5f);
            handleAreaRt.pivot = new Vector2(0.5f, 0.5f);
            handleAreaRt.sizeDelta = new Vector2(-20f, 18f);
            handleAreaRt.anchoredPosition = Vector2.zero;
            var handleRt = New("Handle", handleAreaRt);
            var handle = handleRt.gameObject.AddComponent<RoundedRectGraphic>();
            handle.color = Color.white;
            handle.cornerRadius = 9f;
            handle.shadowSize = 7f;
            handleRt.pivot = new Vector2(0.5f, 0.5f);
            handleRt.sizeDelta = new Vector2(18f, 0f);

            var slider = rt.gameObject.AddComponent<UISlider>();
            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handle;
            AddCursor(rt.gameObject, CursorKind.ResizeH);
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.onValueChanged.AddListener(v => onChange(v));
            rt.gameObject.AddComponent<SliderScroll>().slider = slider;
            return slider;
        }

        /// <summary>
        /// A thick progress-bar style slider: drag anywhere on the bar, the value reads out (mono) inside it,
        /// and the fill brightens on hover. No floating handle - the fill edge is the position.
        /// </summary>
        public static Slider ValueSlider(Transform parent, float min, float max, float value,
            Func<float, string> format, Action<float> onChange, float height = 26f) {
            var rt = New("ValueSlider", parent);
            var element = rt.gameObject.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = height;
            element.flexibleWidth = 1f;

            var track = rt.gameObject.AddComponent<RoundedRectGraphic>();
            track.color = Theme.Track;
            track.cornerRadius = Theme.RadiusSm;
            track.innerShadow = 1f; // the bar reads recessed under the fill
            track.raycastTarget = true; // drag surface

            var fillAreaRt = New("Fill Area", rt);
            Stretch(fillAreaRt);
            var fillRt = New("Fill", fillAreaRt);
            var fill = fillRt.gameObject.AddComponent<RoundedRectGraphic>();
            fill.color = Theme.Accent;
            fill.cornerRadius = Theme.RadiusSm;
            fill.raycastTarget = false;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;

            var valueRt = New("Value", rt);
            Stretch(valueRt);
            valueRt.offsetMin = new Vector2(12f, 0f);
            valueRt.offsetMax = new Vector2(-12f, 0f);
            var valueLabel = valueRt.gameObject.AddComponent<TextMeshProUGUI>();
            valueLabel.font = Fonts.Mono;
            valueLabel.text = format(value);
            valueLabel.fontSize = 14f;
            valueLabel.color = Theme.Text;
            valueLabel.alignment = TextAlignmentOptions.MidlineRight;
            valueLabel.raycastTarget = false;

            var slider = rt.gameObject.AddComponent<UISlider>();
            slider.fillRect = fillRt;
            slider.targetGraphic = fill; // hover/press brightens the fill
            AddCursor(rt.gameObject, CursorKind.ResizeH);
            slider.transition = Selectable.Transition.ColorTint;
            slider.colors = Tint();
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.onValueChanged.AddListener(v => {
                valueLabel.text = format(v);
                onChange(v);
            });
            rt.gameObject.AddComponent<SliderScroll>().slider = slider;
            return slider;
        }

        /// <summary>A dark-themed dropdown over string options. Returns the TMP_Dropdown.</summary>
        public static TMP_Dropdown Dropdown(Transform parent, TMP_FontAsset font, IList<string> options,
            int index, Action<int> onChange, float width = 220f) {
            ControlShell(parent, out RectTransform inner, out RoundedRectGraphic shell, width);

            GameObject go = TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources());
            go.transform.SetParent(inner, false);
            Stretch((RectTransform) go.transform);

            var dropdown = go.GetComponent<TMP_Dropdown>();
            Transparent(go.GetComponent<Image>()); // our rounded shell is the background; keep it clickable
            // the popup is a child of the dropdown, so a Selectable ColorTint would light the shell up
            // whenever a row is hovered. Drive the shell tint ourselves and force the "open" look instead.
            dropdown.targetGraphic = shell;
            dropdown.transition = Selectable.Transition.None;
            var hover = inner.gameObject.AddComponent<ControlHover>();
            hover.target = shell;
            hover.normal = Theme.SurfaceRaised;
            hover.hover = Color.Lerp(Theme.SurfaceRaised, Theme.Border, 0.85f);
            hover.active = () => dropdown.IsExpanded;
            AddCursor(go, CursorKind.Hand);
            StyleDropdown(dropdown, font);

            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options));
            dropdown.value = Mathf.Clamp(index, 0, options.Count - 1);
            dropdown.RefreshShownValue();
            dropdown.onValueChanged.AddListener(i => onChange(i));

            // chevron pinned to the right edge of the shell, above the (transparent) dropdown graphic
            TextMeshProUGUI chevron = Icon.Make(inner, Icon.ChevronDown, 12f, Theme.Muted);
            RectTransform crt = chevron.rectTransform;
            crt.anchorMin = crt.anchorMax = new Vector2(1f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f); // rotate about the glyph centre
            crt.sizeDelta = new Vector2(18f, 18f);
            crt.anchoredPosition = new Vector2(-13f, 0f);

            var fx = go.AddComponent<DropdownChevron>();
            fx.dropdown = dropdown;
            fx.chevron = crt;
            go.AddComponent<DropdownPopupAnim>();
            return dropdown;
        }

        /// <summary>
        /// A single-select segmented button group - one click, no popup, no open-then-pick tax. Suits short
        /// exclusive sets; lay it out horizontally (a few options) or vertically (a taller list). Idempotent:
        /// re-picking the active segment does nothing.
        /// </summary>
        public static SegmentedControl Segmented(Transform parent, IList<string> options, int index,
            Action<int> onChange, bool vertical = false) {
            RectTransform rt = New("Segmented", parent);
            var groove = rt.gameObject.AddComponent<RoundedRectGraphic>();
            groove.color = Theme.Track;
            groove.cornerRadius = Theme.RadiusSm;
            groove.innerShadow = 1f;
            groove.raycastTarget = false;

            var group = vertical
                ? (HorizontalOrVerticalLayoutGroup) rt.gameObject.AddComponent<VerticalLayoutGroup>()
                : rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.padding = new RectOffset(3, 3, 3, 3);
            group.spacing = 3f;
            group.childControlWidth = group.childControlHeight = true;
            group.childForceExpandWidth = group.childForceExpandHeight = true;
            var element = rt.gameObject.AddComponent<LayoutElement>();
            element.flexibleWidth = 1f;
            if (!vertical) element.minHeight = element.preferredHeight = 32f;

            var control = rt.gameObject.AddComponent<SegmentedControl>();
            control.transition = Selectable.Transition.None; // the segments carry the visuals, not the group

            var fills = new List<RoundedRectGraphic>();
            var labels = new List<TextMeshProUGUI>();
            var segs = new List<RectTransform>();
            int current = Mathf.Clamp(index, 0, options.Count - 1);

            void Apply(int sel) {
                for (int i = 0; i < fills.Count; i++) {
                    bool on = i == sel;
                    fills[i].color = on ? Theme.Accent : new Color(1f, 1f, 1f, 0f);
                    // dark text on the accent fill when active, light text when not (like the checkbox check)
                    labels[i].color = on ? Theme.AccentText : Theme.Text;
                }
            }

            for (int i = 0; i < options.Count; i++) {
                RectTransform seg = New("Seg", rt);
                if (vertical) seg.gameObject.AddComponent<LayoutElement>().minHeight = 30f;
                var fill = seg.gameObject.AddComponent<RoundedRectGraphic>();
                fill.cornerRadius = Theme.RadiusSm;
                fill.raycastTarget = true;
                fills.Add(fill);

                TextMeshProUGUI label = Label(seg, Fonts.SemiBold, options[i], Type.Caption, Theme.Text);
                label.fontSize = Theme.Body; // match the field labels, not the smaller caption size
                label.alignment = TextAlignmentOptions.Center;
                Stretch(label.rectTransform);
                labels.Add(label);

                var click = seg.gameObject.AddComponent<SegmentClick>();
                click.control = control;
                click.index = i;
                click.fill = fill;
                AddCursor(seg.gameObject, CursorKind.Hand);
                segs.Add(seg);
            }

            control.segments = segs.ToArray();
            control.visuals = Apply;
            control.onChange = onChange;
            control.Init(current);
            Apply(current);
            return control;
        }

        /// <summary>
        /// A segmented control that keeps every option on one row by showing inactive options as a short
        /// tag and expanding the selected one to its full label (it absorbs the leftover width, so the row
        /// never grows). Tooltips carry the full name of each. Suits a primary pick with more options than
        /// a plain segmented row would fit. Idempotent.
        /// </summary>
        public static void SegmentedExpand(Transform parent, IList<string> tags, IList<string> full, int index,
            Action<int> onChange) {
            RectTransform rt = New("SegmentedExpand", parent);
            var groove = rt.gameObject.AddComponent<RoundedRectGraphic>();
            groove.color = Theme.Track;
            groove.cornerRadius = Theme.RadiusSm;
            groove.innerShadow = 1f;
            groove.raycastTarget = false;
            var group = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.padding = new RectOffset(3, 3, 3, 3);
            group.spacing = 3f;
            group.childControlWidth = group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = true;
            var element = rt.gameObject.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = 32f;
            element.flexibleWidth = 1f;

            var control = rt.gameObject.AddComponent<SegmentedControl>();
            control.transition = Selectable.Transition.None;

            var fills = new List<RoundedRectGraphic>();
            var labels = new List<TextMeshProUGUI>();
            var elements = new List<LayoutElement>();
            var segs = new List<RectTransform>();
            int current = Mathf.Clamp(index, 0, tags.Count - 1);

            // colours + label text (the width is eased separately by SegmentExpandAnim)
            void Visuals(int sel) {
                for (int i = 0; i < fills.Count; i++) {
                    bool on = i == sel;
                    fills[i].color = on ? Theme.Accent : new Color(1f, 1f, 1f, 0f);
                    labels[i].text = on ? full[i] : tags[i];
                    labels[i].color = on ? Theme.AccentText : Theme.Text;
                }
            }

            for (int i = 0; i < tags.Count; i++) {
                RectTransform seg = New("Seg", rt);
                var fill = seg.gameObject.AddComponent<RoundedRectGraphic>();
                fill.cornerRadius = Theme.RadiusSm;
                fill.raycastTarget = true;
                fills.Add(fill);
                seg.gameObject.AddComponent<RectMask2D>(); // clip the label so it reveals as the box grows
                var inner = seg.gameObject.AddComponent<HorizontalLayoutGroup>();
                inner.padding = new RectOffset(8, 8, 0, 0);
                inner.childAlignment = TextAnchor.MiddleCenter;
                inner.childControlWidth = inner.childControlHeight = true;
                inner.childForceExpandWidth = inner.childForceExpandHeight = false;
                elements.Add(seg.gameObject.AddComponent<LayoutElement>());

                TextMeshProUGUI label = Label(seg, Fonts.SemiBold, tags[i], Type.Caption, Theme.Text);
                label.fontSize = Theme.Body; // match the field labels, not the smaller caption size
                label.alignment = TextAlignmentOptions.Center;
                labels.Add(label);

                AddTooltip(seg.gameObject, full[i]);
                var click = seg.gameObject.AddComponent<SegmentClick>();
                click.control = control;
                click.index = i;
                click.fill = fill;
                AddCursor(seg.gameObject, CursorKind.Hand);
                segs.Add(seg);
            }

            var anim = rt.gameObject.AddComponent<SegmentExpandAnim>();
            anim.elements = elements.ToArray();
            anim.target = current;
            for (int i = 0; i < elements.Count; i++) elements[i].flexibleWidth = i == current ? 1f : 0f;

            control.segments = segs.ToArray();
            control.visuals = Visuals;
            control.animate = anim.Select;
            control.onChange = onChange;
            control.Init(current);
            Visuals(current);
        }

        /// <summary>
        /// A numeric field you drag horizontally (or scroll over) to change - no popup, no typing, no tiny
        /// handle to hit. Mono value flanked by faint scrub chevrons. Returns the ScrubControl.
        /// </summary>
        public static ScrubControl ScrubField(Transform parent, int value, int min, int max, string suffix,
            Action<int> onChange, float width = 96f) {
            RectTransform rt = New("Scrub", parent);
            var bg = rt.gameObject.AddComponent<RoundedRectGraphic>();
            bg.color = Theme.SurfaceRaised;
            bg.cornerRadius = Theme.RadiusSm;
            bg.raycastTarget = true; // the whole field is the drag surface
            var element = rt.gameObject.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = 34f;
            element.minWidth = element.preferredWidth = width;
            element.flexibleWidth = 0f;

            Pin(Icon.Make(rt, Icon.AngleLeft, 12f, Theme.Muted).rectTransform, 0f, 9f);
            Pin(Icon.Make(rt, Icon.AngleRight, 12f, Theme.Muted).rectTransform, 1f, -9f);

            RectTransform valueRt = New("Value", rt);
            Stretch(valueRt);
            var label = valueRt.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = Fonts.Mono;
            label.fontSize = 14f;
            label.color = Theme.Text;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;

            var scrub = rt.gameObject.AddComponent<ScrubControl>();
            scrub.transition = Selectable.Transition.None; // ControlHover drives the hover tint, not the Selectable
            scrub.targetGraphic = bg;
            scrub.label = label;
            scrub.value = value;
            scrub.min = min;
            scrub.max = max;
            scrub.suffix = suffix;
            scrub.onChange = onChange;
            scrub.Render();

            var hover = rt.gameObject.AddComponent<ControlHover>();
            hover.target = bg;
            hover.normal = Theme.SurfaceRaised;
            hover.hover = Color.Lerp(Theme.SurfaceRaised, Theme.Border, 0.85f);
            AddCursor(rt.gameObject, CursorKind.ResizeH);
            return scrub;
        }

        // pin a small element flush to the left (anchorX 0) or right (anchorX 1) edge, centred vertically
        private static void Pin(RectTransform rt, float anchorX, float x) {
            rt.anchorMin = rt.anchorMax = new Vector2(anchorX, 0.5f);
            rt.pivot = new Vector2(anchorX, 0.5f);
            rt.sizeDelta = new Vector2(14f, 14f);
            rt.anchoredPosition = new Vector2(x, 0f);
        }

        /// <summary>
        /// An underline tab strip (technical, not button-like): the active tab carries an accent underline
        /// and an emphasized label; the others are muted. Calls onSelect; index 0 starts active.
        /// </summary>
        public static void TabBar(Transform parent, TMP_FontAsset font, IList<string> tabs, Action<int> onSelect) {
            RectTransform bar = Row(parent, Theme.Space4);
            bar.gameObject.AddComponent<LayoutElement>().minHeight = 36f;
            var labels = new List<TextMeshProUGUI>();
            var underlines = new List<RoundedRectGraphic>();

            for (int i = 0; i < tabs.Count; i++) {
                int index = i;
                RectTransform tab = New("Tab", bar);
                var inner = tab.gameObject.AddComponent<HorizontalLayoutGroup>();
                // the bottom padding is the underline's lane, and the room the focus ring needs below the text
                inner.padding = new RectOffset(4, 4, 5, 9);
                inner.childAlignment = TextAnchor.MiddleCenter;
                inner.childControlWidth = inner.childControlHeight = true;
                inner.childForceExpandWidth = inner.childForceExpandHeight = false;
                var hit = tab.gameObject.AddComponent<Image>();
                hit.color = new Color(0f, 0f, 0f, 0f); // transparent click surface

                // every tab keeps the SemiBold cut so switching can't change a tab's width and shift the
                // row; the active state rides on colour + the underline, not the weight.
                TextMeshProUGUI label = Label(tab, Fonts.SemiBold, tabs[i], Type.Body, Theme.Muted);
                labels.Add(label);

                // the tab rail, sitting in the bottom padding outside the layout flow: accent on the active
                // tab, a hairline on the rest so the focus ring never wraps an empty lane. It runs the label's
                // width, not the tab's, so it can't be mistaken for the ring's own bottom edge.
                RectTransform under = New("Underline", tab);
                under.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                under.anchorMin = new Vector2(0f, 0f);
                under.anchorMax = new Vector2(1f, 0f);
                under.pivot = new Vector2(0.5f, 0f);
                under.sizeDelta = new Vector2(-8f, 2f);
                under.anchoredPosition = new Vector2(0f, 6f);
                var underG = under.gameObject.AddComponent<RoundedRectGraphic>();
                underG.cornerRadius = 1f;
                underG.raycastTarget = false;
                underlines.Add(underG);

                var button = tab.gameObject.AddComponent<Button>();
                button.targetGraphic = hit;
                button.transition = Selectable.Transition.None;
                AddCursor(tab.gameObject, CursorKind.Hand);
                button.onClick.AddListener(() => {
                    for (int j = 0; j < labels.Count; j++) {
                        bool active = j == index;
                        labels[j].color = active ? Theme.Text : Theme.Muted;
                        underlines[j].color = active ? Theme.Accent : Theme.Border;
                    }
                    onSelect(index);
                });
            }
            // first tab starts selected
            labels[0].color = Theme.Text;
            for (int i = 0; i < underlines.Count; i++)
                underlines[i].color = i == 0 ? Theme.Accent : Theme.Border;
        }

        private static void ControlShell(Transform parent, out RectTransform inner, out RoundedRectGraphic bg,
            float width = 220f) {
            var rt = New("Control", parent);
            bg = rt.gameObject.AddComponent<RoundedRectGraphic>();
            bg.color = Theme.SurfaceRaised;
            bg.cornerRadius = Theme.RadiusSm;
            bg.raycastTarget = false;
            var element = rt.gameObject.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = 34f;
            // a fixed control width, not flex - dropdowns/fields shouldn't stretch across the whole row
            element.preferredWidth = element.minWidth = width;
            element.flexibleWidth = 0f;
            inner = rt;
        }

        private static ColorBlock MakeTint(Color normal, Color highlighted, Color pressed, Color? selected = null) {
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = normal;
            colors.highlightedColor = highlighted;
            colors.pressedColor = pressed;
            colors.selectedColor = selected ?? normal;
            colors.fadeDuration = 0.1f;
            return colors;
        }

        private static void StyleDropdown(TMP_Dropdown dropdown, TMP_FontAsset font) {
            if (dropdown.captionText != null) {
                dropdown.captionText.font = font;
                dropdown.captionText.color = Theme.Text;
                dropdown.captionText.fontSize = Theme.Body;
            }
            if (dropdown.itemText != null) {
                dropdown.itemText.font = font;
                dropdown.itemText.color = Theme.Text;
                dropdown.itemText.fontSize = Theme.Body;
            }
            // TMP's default Resources have no sprites, so its arrow/checkmark Images draw as solid white
            // boxes - drop them. Selection in the open list reads off the row highlight instead.
            Transform arrow = dropdown.transform.Find("Arrow");
            var arrowImage = arrow != null ? arrow.GetComponent<Image>() : null;
            if (arrowImage != null) arrowImage.enabled = false;

            // recolour the popup template to the dark theme + give the rows room for the larger font
            RectTransform template = dropdown.template;
            if (template != null) {
                // a plain square surface + a built-in drop shadow for depth. (A RoundedRectGraphic can't be
                // used here: TMP reparents the open popup under a nested Canvas that strips the TexCoord1
                // channel the rounding rides in, so it'd lose its shape after the first frame.)
                var templateBg = template.GetComponent<Image>();
                if (templateBg != null) {
                    templateBg.enabled = true;
                    templateBg.sprite = null;
                    // a shade darker than the control so the open list reads as a separate surface
                    templateBg.color = Theme.Window;
                    var shadow = templateBg.GetComponent<Shadow>() ?? templateBg.gameObject.AddComponent<Shadow>();
                    shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
                    shadow.effectDistance = new Vector2(0f, -8f);
                }

                // stop the mask from drawing its (white, sprite-less) fill - but DON'T make it transparent,
                // a zero-alpha mask graphic clips every item away. showMaskGraphic=false keeps the clip,
                // hides the fill.
                Transform viewport = template.Find("Viewport");
                var viewportMask = viewport != null ? viewport.GetComponent<Mask>() : null;
                if (viewportMask != null) viewportMask.showMaskGraphic = false;

                // give the popup room so our short lists never need the (sprite-less, white-box) scrollbar,
                // and blank its images either way so a longer list scrolls invisibly instead of showing them
                template.sizeDelta = new Vector2(template.sizeDelta.x, 280f);
                Transform scrollbar = template.Find("Scrollbar");
                if (scrollbar != null)
                    foreach (Image img in scrollbar.GetComponentsInChildren<Image>(true))
                        img.enabled = false;
                Transform item = dropdown.itemText != null ? dropdown.itemText.transform.parent : null;
                if (item is RectTransform itemRt)
                    itemRt.sizeDelta = new Vector2(itemRt.sizeDelta.x, 30f);
                if (dropdown.itemText != null) {
                    dropdown.itemText.alignment = TextAlignmentOptions.MidlineLeft;
                    dropdown.itemText.margin = new Vector4(12f, 0f, 12f, 0f); // breathing room on the left
                }
                var itemToggle = item != null ? item.GetComponent<Toggle>() : null;
                if (itemToggle != null) {
                    var colors = itemToggle.colors;
                    colors.normalColor = Theme.Window;
                    colors.highlightedColor = Color.Lerp(Theme.Window, Theme.Accent, 0.22f);
                    colors.selectedColor = Color.Lerp(Theme.Window, Theme.Accent, 0.12f);
                    colors.pressedColor = Color.Lerp(Theme.Window, Theme.Accent, 0.30f);
                    colors.fadeDuration = 0.08f;
                    itemToggle.colors = colors;
                }
                // TMP marks the current option by enabling its checkmark Image. The default sprite is a
                // white box, so re-skin it into a slim accent bar down the active row's left edge.
                Transform checkmark = item != null ? item.Find("Item Checkmark") : null;
                var checkImage = checkmark != null ? checkmark.GetComponent<Image>() : null;
                if (checkImage != null) {
                    checkImage.enabled = true;
                    checkImage.sprite = WhiteSprite();
                    checkImage.color = Theme.Accent;
                    RectTransform mrt = checkImage.rectTransform;
                    mrt.anchorMin = new Vector2(0f, 0f);
                    mrt.anchorMax = new Vector2(0f, 1f);
                    mrt.pivot = new Vector2(0f, 0.5f);
                    mrt.sizeDelta = new Vector2(3f, 0f);
                    mrt.anchoredPosition = Vector2.zero;
                }
            }
        }

        // ColorTint multiplies targetGraphic.color, so keep white-ish multipliers and lift on hover
        private static ColorBlock Tint() =>
            MakeTint(Color.white, new Color(1.12f, 1.12f, 1.12f, 1f), new Color(0.86f, 0.86f, 0.86f, 1f));

        // ghost buttons: white base, alpha-only tint - invisible at rest, a faint wash on hover/press
        private static ColorBlock GhostTint() =>
            MakeTint(new Color(1f, 1f, 1f, 0f), new Color(1f, 1f, 1f, 0.10f), new Color(1f, 1f, 1f, 0.16f));

        private static void Transparent(Image image) {
            if (image != null) image.color = new Color(0f, 0f, 0f, 0f);
        }

        // swap the hardware cursor while this control is hovered (events bubble up from child graphics)
        private static void AddCursor(GameObject go, CursorKind kind) {
            go.AddComponent<HoverCursor>().kind = kind;
        }

        private static Sprite _white;

        /// <summary>A shared 1x1 white sprite for solid-fill Images (tinted by the Image's colour).</summary>
        private static Sprite WhiteSprite() {
            if (_white == null) {
                var tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                _white = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            }
            return _white;
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

        private static void Stretch(RectTransform rt) {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void Inset(RectTransform rt, float inset) {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        private static void CenterBand(RectTransform rt, float height) {
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(0f, height);
            rt.anchoredPosition = Vector2.zero;
        }

        private static void Width(GameObject go, float width) {
            var element = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            element.preferredWidth = element.minWidth = width;
        }

        private static void Size(GameObject go, float width, float height) {
            var element = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            element.preferredWidth = element.minWidth = width;
            element.preferredHeight = element.minHeight = height;
        }
    }
}
