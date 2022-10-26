Shader "UI/DashedLine"
{
    // a row of anti-aliased capsule dashes along one quad. Everything rides uv0, measured in units of the
    // rule's half thickness, so a dash is always a capsule of radius 1. A dash thinner than a pixel is
    // widened to one and faded to match, so the rule stays smooth at any canvas scale.
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
                // x = distance along the rule, y = offset from its centre line, z = dash length, w = period
                float4 uv0 : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float4 local : TEXCOORD0;
                float4 world : TEXCOORD1;
            };

            float4 _ClipRect;

            // signed distance to a horizontal capsule of half-length h and radius r, centred on the origin
            float sdCapsule(float2 p, float h, float r)
            {
                p.x -= clamp(p.x, -h, h);
                return length(p) - r;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.local = v.uv0;
                o.world = v.vertex;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float dash = i.local.z;
                float period = i.local.w;

                // a sub-pixel dash samples through the gaps, so widen it and give back the coverage in alpha
                float pixel = max(fwidth(i.local.y), 1e-5);
                float radius = max(1.0, pixel * 0.5);
                float reach = max(dash * 0.5 - radius, 0.0);

                // fold onto one period, then take the nearer of this dash and the next so the AA band at the
                // period seam comes from the dash that actually owns it
                float t = i.local.x - period * floor(i.local.x / period);
                float d = sdCapsule(float2(t - dash * 0.5, i.local.y), reach, radius);
                d = min(d, sdCapsule(float2(t - period - dash * 0.5, i.local.y), reach, radius));

                float aa = max(fwidth(d), 1e-5) * 0.5;
                float outA = (1.0 - smoothstep(-aa, aa, d)) * i.color.a / radius;

                #ifdef UNITY_UI_CLIP_RECT
                outA *= UnityGet2DClipping(i.world.xy, _ClipRect);
                #endif
                return fixed4(i.color.rgb, outA);
            }
            ENDCG
        }
    }
}
