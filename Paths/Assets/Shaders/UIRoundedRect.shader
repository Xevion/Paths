Shader "UI/RoundedRect"
{
    Properties
    {
        // global look shared by every panel/button; per-panel geometry comes through the mesh
        _Border ("Border Width", Float) = 1.0
        _BorderColor ("Border Color", Color) = (1, 1, 1, 0.08)
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.45)
        _ShadowOffset ("Shadow Offset", Vector) = (0, -4, 0, 0)
        // a faint top-lit sheen across every surface so flat fills read with a little depth
        _GradientStrength ("Gradient Strength", Float) = 0.05
        // ordered dither to break up banding in the dark gradients (Linear space bands badly)
        _DitherStrength ("Dither Strength", Float) = 1.0
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"
            "PreviewType"="Plane" "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float4 uv0 : TEXCOORD0; // xy = pixel offset from the rect centre
                float4 uv1 : TEXCOORD1; // xy = half size, z = corner radius, w = shadow size
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float4 local : TEXCOORD0; // xy offset, zw half size
                float2 shape : TEXCOORD1; // x radius, y shadow size
                float4 world : TEXCOORD2; // canvas-space position, for RectMask2D clipping
            };

            float _Border;
            float4 _BorderColor;
            float4 _ShadowColor;
            float4 _ShadowOffset;
            float _GradientStrength;
            float _DitherStrength;
            float4 _ClipRect;

            // signed distance to a rounded box: <0 inside, 0 on the edge, >0 outside
            float sdRoundedBox(float2 p, float2 b, float r)
            {
                float2 q = abs(p) - b + r;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.local = float4(v.uv0.xy, v.uv1.xy);
                o.shape = v.uv1.zw;
                o.world = v.vertex;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.local.xy;
                float2 halfSize = i.local.zw;
                float radius = i.shape.x;
                float shadowSize = i.shape.y;

                float d = sdRoundedBox(p, halfSize, radius);
                float aa = max(fwidth(d), 1e-5) * 0.5; // ~1px AA band - crisp, not soft

                // a faint vertical sheen on the fill - brighter at the top edge, darker at the bottom
                float g = 1.0 + _GradientStrength * (p.y / max(halfSize.y, 1e-3));
                float3 fillRGB = i.color.rgb * g;

                // the shape: fill inside, a hairline border ring just within the edge
                float shapeAlpha = (1.0 - smoothstep(-aa, aa, d)) * i.color.a;
                float innerMask = 1.0 - smoothstep(-aa, aa, d + _Border);
                // border lives in the outer ring only, blended by its own alpha so it stays a hairline
                float3 rgb = lerp(fillRGB, _BorderColor.rgb, (1.0 - innerMask) * _BorderColor.a);

                // soft drop shadow behind the shape, only where the shape isn't already opaque
                float sd = sdRoundedBox(p - _ShadowOffset.xy, halfSize, radius);
                float shadowAlpha = (1.0 - smoothstep(-shadowSize, shadowSize, sd)) * _ShadowColor.a;
                shadowAlpha *= step(0.001, shadowSize) * (1.0 - shapeAlpha);

                float outA = shapeAlpha + shadowAlpha * (1.0 - shapeAlpha);
                float3 outRGB = (rgb * shapeAlpha + _ShadowColor.rgb * shadowAlpha * (1.0 - shapeAlpha)) / max(outA, 1e-5);

                // ordered-ish dither: half an 8-bit step of screen-space hash noise, kills banding
                float n = frac(sin(dot(i.pos.xy, float2(12.9898, 78.233))) * 43758.5453);
                outRGB += (n - 0.5) * (_DitherStrength / 255.0);

                #ifdef UNITY_UI_CLIP_RECT
                outA *= UnityGet2DClipping(i.world.xy, _ClipRect);
                #endif
                return fixed4(outRGB, outA);
            }
            ENDCG
        }
    }
}
