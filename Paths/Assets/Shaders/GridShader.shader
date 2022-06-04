Shader "Paths/Grid"
{
    Properties
    {
        _LineColor ("Gap Color", Color) = (0.09, 0.1, 0.13, 1)
        _EmptyColor ("Empty Cell Color", Color) = (0.1, 0.11, 0.14, 1)
        _WallColor ("Wall Cell Color", Color) = (0.16, 0.18, 0.22, 1)
        _StartColor ("Start Cell Color", Color) = (0.45, 0.82, 0.5, 1)
        _EndColor ("End Cell Color", Color) = (0.88, 0.34, 0.31, 1)
        _SeenColor ("Seen Cell Color", Color) = (0.16, 0.55, 0.6, 1)
        _ExpandedColor ("Expanded Cell Color", Color) = (0.13, 0.29, 0.33, 1)
        _PathColor ("Path Cell Color", Color) = (0.16, 0.83, 0.9, 1)
        _HoverColor ("Hover Color", Color) = (0.16, 0.83, 0.9, 1)

        // one texel per cell: state in R, previous state in G, change time in BA. set from GridController.
        [PerRendererData] _GridTex ("Grid State", 2D) = "black" {}
        _Gap ("Tile Gap", Range(0, 0.5)) = 0.12
        _Radius ("Corner Radius", Range(0, 0.5)) = 0.1
        _Fade ("Search Fade", Range(0, 1)) = 0.7
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
            float _Gap;
            float _Radius;
            float _Fade; // search-layer dim, driven from GridController (milder idle, stronger while editing)
            float _Now; // Time.time wrapped to the stamp period, pushed per frame from GridController
            float _TransitionTime; // seconds a state change eases over; shortens at high playback speed
            float4 _HoverColor;
            float4 _HoverCell; // xy = grid cell under the cursor; negative when nothing is hovered
            float _HoverStrength; // eased 0..1, so the highlight doesn't snap on and off
            float _BrushPreview; // 1 when a left click would draw a wall on the hovered empty cell

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // signed distance to a rounded box of half-extent b, corner radius r, centered at 0
            float RoundedBox(float2 p, float2 b, float r)
            {
                float2 q = abs(p) - b + r;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 colors[7] = {
                    _EmptyColor, _WallColor, _StartColor, _EndColor,
                    _SeenColor, _ExpandedColor, _PathColor
                };

                float2 grid = float2(_GridWidth, _GridHeight);
                float2 cellCoord = i.uv * grid;
                float2 id = floor(cellCoord);
                float2 p = frac(cellCoord) - 0.5; // cell-local, -0.5..0.5

                float2 cellUv = (id + 0.5) / grid;
                float4 texel = tex2D(_GridTex, cellUv);
                int index = (int) floor(texel.r * 255.0 + 0.5);
                int prev = (int) floor(texel.g * 255.0 + 0.5);

                // how far into this cell's transition we are: a 16-bit change stamp in BA
                // (0.02s units, same wrapped clock as _Now - see GridController.TimePeriod)
                float stamp = (texel.b * 255.0 + texel.a * 65280.0) * 0.02;
                float age = _Now - stamp;
                if (age < 0.0)
                    age += 65536.0 * 0.02; // the clock wrapped past the stamp
                float t = saturate(age / max(_TransitionTime, 1e-4));
                float ease = 1.0 - (1.0 - t) * (1.0 - t); // ease-out, snappy start

                // screen density in cell units - how big a pixel is across a cell. drives both the
                // edge antialiasing and the zoom-out degrade below.
                float2 dd = fwidth(cellCoord);
                float pixel = max(max(dd.x, dd.y), 1e-5);

                // fade the gap and rounding out as cells shrink toward a few pixels, so the far
                // view collapses to clean flat color instead of shimmering sub-pixel gaps
                float k = saturate((1.0 / pixel - 3.0) / 4.0);

                // cells appearing out of empty scale in as they ease
                float pop = prev == 0 && index != 0 ? lerp(0.55, 1.0, ease) : 1.0;
                float2 b = (0.5 - _Gap * 0.5 * k) * pop;
                float r = _Radius * k * pop;

                float dist = RoundedBox(p, b, r);
                float aa = fwidth(dist);
                float tile = 1.0 - smoothstep(-aa * 0.5, aa * 0.5, dist);

                // ease the fill from the state the cell left toward the one it's in. indices 4/5/6
                // are Seen/Expanded/Path - dimming pulls the tint back toward the plain tile rather
                // than toward black, so the board stays one readable surface
                float3 from = colors[prev].rgb;
                if (prev >= 4)
                    from = lerp(_EmptyColor.rgb, from, _Fade);
                float3 color = colors[index].rgb;
                if (index >= 4)
                    color = lerp(_EmptyColor.rgb, color, _Fade);
                color = lerp(from, color, ease);

                // cursor overlay - additive, all driven from uniforms, so the touch path (which
                // never sets them) just skips it. the hovered cell gets a gentle lift plus a
                // brighter inner rim, and an empty cell previews the wall the brush would draw.
                if (_HoverStrength > 0.001 && all(abs(id - _HoverCell.xy) < 0.5)) {
                    if (_BrushPreview > 0.5 && index == 0)
                        color = lerp(color, _WallColor.rgb, 0.4 * _HoverStrength);
                    float rim = (1.0 - smoothstep(0.0, 0.07, abs(dist))) * k;
                    color += _HoverColor.rgb * (0.10 + 0.6 * rim) * _HoverStrength;
                }

                return float4(lerp(_LineColor.rgb, color, tile), 1.0);
            }
            ENDCG
        }
    }
}
