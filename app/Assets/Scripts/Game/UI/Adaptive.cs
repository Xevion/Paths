using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI {
    /// <summary>
    /// A control that knows how to take up less room. Level 0 is its design size and each level above
    /// gives up more of it - a caption, a label, then the control itself. Every level must report an
    /// honest min width on its own LayoutElement, so a container never squeezes a control past what it
    /// can actually draw (which is how a button ends up with its icon on top of its text).
    /// </summary>
    public interface IAdaptive {
        /// <summary>Levels available, counting level 0. A control with nothing to give returns 1.</summary>
        int Levels { get; }

        /// <summary>How readily this gives up space - a container steps the highest value down first.</summary>
        int Yield { get; }

        int Level { get; }

        void SetLevel(int level);
    }

    /// <summary>
    /// An IAdaptive assembled from a list of forms, widest first. UIKit builds controls rather than
    /// subclassing them, so their smaller forms are built the same way.
    /// </summary>
    public class AdaptiveSteps : MonoBehaviour, IAdaptive {
        private Action[] _forms;

        public int Yield { get; set; }
        public int Level { get; private set; }
        public int Levels => _forms == null ? 1 : _forms.Length;

        public void Build(int yield, Action[] forms) {
            Yield = yield;
            _forms = forms;
            Level = 0;
            if (_forms != null && _forms.Length > 0) _forms[0]();
        }

        public void SetLevel(int level) {
            level = Mathf.Clamp(level, 0, Levels - 1);
            if (level == Level && _forms != null) return;
            Level = level;
            _forms[level]();
        }
    }

    /// <summary>
    /// Keeps a horizontal layout group inside its width by stepping its adaptive children down, the most
    /// willing first, instead of letting uGUI compress every child toward a min size it lied about. The
    /// row only ever asks; what a smaller form looks like belongs to the control.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(HorizontalLayoutGroup))]
    public class AdaptiveRow : UIBehaviour {
        private readonly List<IAdaptive> _kids = new List<IAdaptive>();
        private HorizontalLayoutGroup _group;
        private float _width = -1f;

        /// <summary>Pick up the adaptive children. Call once the row is populated.</summary>
        public void Collect() {
            _kids.Clear();
            _group = GetComponent<HorizontalLayoutGroup>();
            foreach (IAdaptive a in GetComponentsInChildren<IAdaptive>(true))
                _kids.Add(a);
            _width = -1f;
        }

        protected override void OnRectTransformDimensionsChange() {
            _width = -1f;
        }

        private void LateUpdate() {
            float available = Available();
            if (Mathf.Approximately(available, _width)) return;
            _width = available;
            Fit(available);
        }

        // our own rect is no use: a layout group never sizes a child below its min, so an overflowing row
        // reads back its own inflated width and thinks it fits. the space we were offered is the parent's.
        private float Available() {
            var parent = transform.parent as RectTransform;
            if (parent == null) return ((RectTransform) transform).rect.width;
            float width = parent.rect.width;
            var group = parent.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (group != null) width -= group.padding.left + group.padding.right;
            return width;
        }

        /// <summary>
        /// Step everything down one level before asking anyone for a second, so the row degrades evenly
        /// instead of stripping the most willing control to nothing while the rest sit at full size. The
        /// result is a pure function of the width, so it can't oscillate at a boundary.
        /// </summary>
        private void Fit(float width) {
            for (int i = 0; i < _kids.Count; i++) _kids[i].SetLevel(0);

            int deepest = 0;
            for (int i = 0; i < _kids.Count; i++) deepest = Mathf.Max(deepest, _kids[i].Levels - 1);

            for (int level = 1; level <= deepest; level++) {
                for (int i = 0; i < _kids.Count; i++) {
                    if (_kids[i].Levels - 1 < level) continue;
                    if (Measure() <= width) return;
                    _kids[i].SetLevel(level);
                }
            }
        }

        private float Measure() {
            // a control whose min comes from its own layout group reports a cached value, so the level we
            // just applied wouldn't be visible here until the next frame's rebuild. only runs on a resize.
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform) transform);

            float total = _group.padding.left + _group.padding.right;
            int count = 0;
            foreach (RectTransform child in (RectTransform) transform) {
                if (!child.gameObject.activeSelf) continue;
                var element = child.GetComponent<LayoutElement>();
                if (element != null && element.ignoreLayout) continue;
                total += LayoutUtility.GetMinWidth(child);
                count++;
            }
            return total + Mathf.Max(0, count - 1) * _group.spacing;
        }
    }
}
