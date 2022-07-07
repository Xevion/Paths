using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Editor {
    /// <summary>
    /// Builds dynamic-atlas TMP font assets from the vendored Inter TTFs into Resources, so the UI kit
    /// can load Inter at runtime instead of the default Liberation Sans. Dynamic population keeps the
    /// glyphs rendering on demand (and shipping in player builds) without baking a fixed character set.
    /// Run once via -executeMethod Editor.FontBuilder.Build after the TTFs are imported.
    /// </summary>
    public static class FontBuilder {
        public static void Build() {
            AssetDatabase.Refresh();
            BuildOne("Assets/Fonts/Inter-Regular.ttf", "Assets/Resources/Fonts/Inter.asset");
            BuildOne("Assets/Fonts/Inter-SemiBold.ttf", "Assets/Resources/Fonts/Inter-SemiBold.asset");
            BuildOne("Assets/Fonts/FontAwesome.ttf", "Assets/Resources/Fonts/Icons.asset");
            BuildOne("Assets/Fonts/GeistMono.otf", "Assets/Resources/Fonts/Mono.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void BuildOne(string ttf, string output) {
            var font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
            if (font == null) {
                Debug.LogError("FontBuilder: source font not imported: " + ttf);
                return;
            }

            // padding 14 (was 9) gives the baked underlay room to spread without clipping at glyph bounds
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(
                font, 90, 14, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
            asset.name = Path.GetFileNameWithoutExtension(output);

            Directory.CreateDirectory(Path.GetDirectoryName(output));
            AssetDatabase.CreateAsset(asset, output);

            // the atlas texture + material are generated in memory; persist them inside the .asset
            if (asset.atlasTextures != null && asset.atlasTextures.Length > 0) {
                asset.atlasTextures[0].name = asset.name + " Atlas";
                AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            }
            // bake a soft drop shadow into the material so every label lifts off busy backgrounds (white
            // value text on the cyan fill was washing out). Baked, not enabled at runtime, so the underlay
            // shader variant ships in the player/WebGL build instead of being stripped.
            // a symmetric dark halo (offset 0 + dilate) rather than a directional drop, so white text reads
            // on the cyan fill from every edge. Strong on purpose; tune here if it gets heavy.
            Material mat = asset.material;
            mat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.9f));
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
            mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.15f);
            mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.3f);
            mat.name = asset.name + " Material";
            AssetDatabase.AddObjectToAsset(mat, asset);

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log("FontBuilder: built " + output);
        }
    }
}
