Shader "UI/FocusRing"
{
    // an accent ring hugging the OUTSIDE of a control's rounded rect, transparent in the centre so the
    // control shows through. Geometry rides the mesh (same packing as UI/RoundedRect); the pulse is driven
    // from C# through the vertex colour alpha.
    Properties
    {
        _Gap ("Gap", Float) = 2.0
        _RingWidth ("Ring Width", Float) = 2.0
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
                float4 uv0 : TEXCOORD0; // xy = offset from centre
                float4 uv1 : TEXCOORD1; // xy = half size, z = corner radius
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float4 local : TEXCOORD0; // xy offset, zw half size
                float radius : TEXCOORD1;
                float4 world : TEXCOORD2;
            };

            float _Gap;
            float _RingWidth;
            float4 _ClipRect;

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
                o.radius = v.uv1.z;
                o.world = v.vertex;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float d = sdRoundedBox(i.local.xy, i.local.zw, i.radius);
                float aa = max(fwidth(d), 1e-5) * 0.75;
                // band sitting just outside the edge: d in (_Gap, _Gap + _RingWidth)
                float a = smoothstep(_Gap - aa, _Gap + aa, d)
                          * (1.0 - smoothstep(_Gap + _RingWidth - aa, _Gap + _RingWidth + aa, d));
                float outA = i.color.a * a;
                #ifdef UNITY_UI_CLIP_RECT
                outA *= UnityGet2DClipping(i.world.xy, _ClipRect);
                #endif
                return fixed4(i.color.rgb, outA);
            }
            ENDCG
        }
    }
}
