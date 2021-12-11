Shader "PDT Shaders/TestGrid"
{
    Properties
    {
        _LineColor ("Line Color", Color) = (1,1,1,1)
        _EmptyColor ("Empty Cell Color", Color) = (0,0,0,1)
        _WallColor ("Wall Cell Color", Color) = (0.02, 0.02, 0.02, 1)
        _StartColor ("Start Cell Color", Color) = (0, 1, 0, 1)
        _EndColor ("End Cell Color", Color) = (1,0,0,1)
        _SeenColor ("Seen Cell Color", Color) = (0.99, 0.93, 0.01, 1)
        _ExpandedColor ("Expanded Cell Color", Color) = (0.89, 0.25, 0.04, 1)
        _PathColor ("Path Cell Color", Color) = (0.65, 0.01, 0.2, 1)

        // one texel per cell, the state index packed into the red channel. set from GridController.
        [PerRendererData] _GridTex ("Grid State", 2D) = "black" {}
        _LineSize("Line Size", Range(0,1)) = 0.15
        _Fade("Search Fade", Range(0,1)) = 0.7
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 pos : SV_POSITION; };

            // point-sampled, so each cell reads exactly its texel - no bleed between states
            sampler2D _GridTex;

            float4 _LineColor;
            float4 _EmptyColor;
            float4 _WallColor;
            float4 _StartColor;
            float4 _EndColor;
            float4 _SeenColor;
            float4 _ExpandedColor;
            float4 _PathColor;

            float _GridWidth;
            float _GridHeight;
            float _LineSize;
            float _Fade; // search-layer dim, driven from GridController (milder idle, stronger while editing)

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 colors[7] = {
                    _EmptyColor, _WallColor, _StartColor, _EndColor,
                    _SeenColor, _ExpandedColor, _PathColor
                };

                float2 uv = i.uv;
                float gw = floor(_GridWidth) + _LineSize;
                float gh = floor(_GridHeight) + _LineSize;

                // the thin gap between cells draws as the line colour and never fades
                if (_LineSize > 0.0 && (frac(uv.x * gw) <= _LineSize || frac(uv.y * gh) <= _LineSize))
                    return float4(_LineColor.rgb * _LineColor.a, 1.0);

                // which cell we're in, then read that cell's state out of the texture
                float2 id = float2(floor(uv.x / (1.0 / gw)), floor(uv.y / (1.0 / gh)));
                float2 cellUv = (id + 0.5) / float2(_GridWidth, _GridHeight);
                int index = (int) floor(tex2D(_GridTex, cellUv).r * 255.0 + 0.5);

                float4 color = colors[index];
                // indices 4/5/6 are Seen/Expanded/Path - the only cells that dim
                float fade = index >= 4 ? _Fade : 1.0;
                return float4(color.rgb * color.a * fade, 1.0);
            }
            ENDCG
        }
    }
}
