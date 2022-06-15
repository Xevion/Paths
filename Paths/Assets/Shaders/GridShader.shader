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
        _GhostColor ("Ghost Color", Color) = (1, 1, 1, 1)

        // one texel per cell: state in R, previous state in G, change time in BA. set from GridController.
        [PerRendererData] _GridTex ("Grid State", 2D) = "black" {}
        // per-cell normalized g-cost for the heatmap, red byte. set from GridController.
        [PerRendererData] _DistTex ("Distance Field", 2D) = "black" {}
        _Gap ("Tile Gap", Range(0, 0.5)) = 0.12
        _Radius ("Corner Radius", Range(0, 0.5)) = 0.1
        _PathInset ("Path Ribbon Inset", Range(0.1, 0.45)) = 0.28
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
            sampler2D _DistTex;
            float _ShowDistances; // 1 = paint the search layer with the cost heatmap instead of state colors

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
            float _PathInset; // how far the path ribbon insets from the cell edge - a thinner route than a wall
            float _Fade; // search-layer dim, driven from GridController (milder idle, stronger while editing)
            float _Now; // Time.time wrapped to the stamp period, pushed per frame from GridController
            float _TransitionTime; // seconds a state change eases over; shortens at high playback speed
            float4 _HoverColor;
            float4 _HoverCell; // xy = grid cell under the cursor; negative when nothing is hovered
            float _HoverStrength; // eased 0..1, so the highlight doesn't snap on and off
            float _BrushPreview; // 1 when a left click would draw a wall on the hovered empty cell
            float4 _GhostColor;
            float4 _GhostPos; // xy = cursor position in cell units, unaligned; drives the drag token
            float _GhostStrength; // eased 0..1
            float _GhostInvalid; // 1 when the marker can't drop here - draw a ring instead of a disc

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

            // rounded box with an independent radius per corner (r = TR, BR, TL, BL) - lets a fused
            // tile round only the corners that face open space and stay square on a fused edge
            float RoundedBoxCorners(float2 p, float2 b, float4 r)
            {
                r.xy = p.x > 0.0 ? r.xy : r.zw;
                r.x = p.y > 0.0 ? r.x : r.y;
                float2 q = abs(p) - b + r.x;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r.x;
            }

            // state index of the cell at grid coord 'c', or -1 off the grid (so border cells don't
            // fuse to a phantom neighbour). explicit-LOD sample - it's called inside branches.
            float NeighbourState(float2 c, float2 grid)
            {
                if (c.x < 0.0 || c.y < 0.0 || c.x > grid.x - 1.0 || c.y > grid.y - 1.0)
                    return -1.0;
                float2 uv = (c + 0.5) / grid;
                return floor(tex2Dlod(_GridTex, float4(uv, 0, 0)).r * 255.0 + 0.5);
            }

            // walls fuse to walls and path fuses to path; everything else renders per-cell
            bool Connects(int self, float neighbour)
            {
                int n = (int) neighbour;
                if (self == 1) return n == 1;
                if (self == 6) return n == 6;
                return false;
            }

            // a small square filling the (sx,sy) corner out to the cell edge, to bridge two cells that
            // only touch diagonally - each cell fills the corner facing the other so the joint closes
            float CornerFill(float2 p, float2 s, float bw)
            {
                return RoundedBox(p - s * (0.5 - bw), float2(bw, bw), 0.0);
            }

            // signed distance for a fused state (wall or path): an asymmetric box that reaches the cell
            // edge on any side a like neighbour sits, rounds only its exposed outer corners (walls only -
            // the path stays cubic), and bridges diagonal-only joints
            float ConnectedDist(float2 p, float2 id, int index, float2 grid, float k)
            {
                bool cL = Connects(index, NeighbourState(id + float2(-1, 0), grid));
                bool cR = Connects(index, NeighbourState(id + float2(1, 0), grid));
                bool cB = Connects(index, NeighbourState(id + float2(0, -1), grid));
                bool cT = Connects(index, NeighbourState(id + float2(0, 1), grid));
                bool cBL = Connects(index, NeighbourState(id + float2(-1, -1), grid));
                bool cBR = Connects(index, NeighbourState(id + float2(1, -1), grid));
                bool cTL = Connects(index, NeighbourState(id + float2(-1, 1), grid));
                bool cTR = Connects(index, NeighbourState(id + float2(1, 1), grid));

                float m = (index == 6 ? _PathInset : _Gap * 0.5) * k;
                float2 lo = float2(-(0.5 - m), -(0.5 - m));
                float2 hi = float2(0.5 - m, 0.5 - m);
                if (cL) lo.x = -0.5;
                if (cR) hi.x = 0.5;
                if (cB) lo.y = -0.5;
                if (cT) hi.y = 0.5;

                float rr = index == 1 ? _Radius * k : 0.0; // path stays sharp-cornered
                float4 radii;
                radii.x = !cR && !cT ? rr : 0.0; // TR
                radii.y = !cR && !cB ? rr : 0.0; // BR
                radii.z = !cL && !cT ? rr : 0.0; // TL
                radii.w = !cL && !cB ? rr : 0.0; // BL
                float d = RoundedBoxCorners(p - (lo + hi) * 0.5, (hi - lo) * 0.5, radii);

                float bw = m;
                if (cTR && !cR && !cT) d = min(d, CornerFill(p, float2(1, 1), bw));
                if (cBR && !cR && !cB) d = min(d, CornerFill(p, float2(1, -1), bw));
                if (cTL && !cL && !cT) d = min(d, CornerFill(p, float2(-1, 1), bw));
                if (cBL && !cL && !cB) d = min(d, CornerFill(p, float2(-1, -1), bw));
                return d;
            }

            // cost gradient for the heatmap: cool near the start, warm far away
            float3 Heat(float t)
            {
                t = saturate(t);
                float3 c = lerp(float3(0.12, 0.22, 0.65), float3(0.10, 0.78, 0.80), saturate(t * 3.0));
                c = lerp(c, float3(0.95, 0.85, 0.20), saturate(t * 3.0 - 1.0));
                c = lerp(c, float3(0.90, 0.22, 0.16), saturate(t * 3.0 - 2.0));
                return c;
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

                // walls and the path fuse across cells; the rest stay per-cell tiles with a gap
                float dist;
                if (index == 1 || index == 6) {
                    dist = ConnectedDist(p, id, index, grid, k);
                } else {
                    // cells appearing out of empty scale in as they ease
                    float pop = prev == 0 && index != 0 ? lerp(0.55, 1.0, ease) : 1.0;
                    float2 b = (0.5 - _Gap * 0.5 * k) * pop;
                    // corners only round into a gap - with grid lines off (gap 0) tiles stay square
                    float r = _Radius * k * pop * smoothstep(0.0, 0.03, _Gap);
                    dist = RoundedBox(p, b, r);
                }

                float aa = fwidth(dist);
                float tile = 1.0 - smoothstep(-aa * 0.5, aa * 0.5, dist);

                // ease the fill from the state the cell left toward the one it's in. indices 4/5 are
                // Seen/Expanded - the search layer, dimmed toward the plain tile (or the cost heatmap)
                // so the board stays one readable surface. the Path (6) stays solid so the final route
                // reads over the dimmed sweep.
                float3 heat = Heat(tex2D(_DistTex, cellUv).r);
                float3 color = colors[index].rgb;
                if (index == 4 || index == 5) {
                    if (_ShowDistances > 0.5) color = heat;
                    color = lerp(_EmptyColor.rgb, color, _Fade);
                }
                float3 from = colors[prev].rgb;
                if (prev == 4 || prev == 5) {
                    if (_ShowDistances > 0.5) from = heat;
                    from = lerp(_EmptyColor.rgb, from, _Fade);
                }
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

                float3 outCol = lerp(_LineColor.rgb, color, tile);

                // drag token - follows the cursor (unaligned), composited over the gridlines so they
                // don't chop it. a filled disc where the marker can drop, a hollow ring where it can't,
                // so an invalid drop reads by shape and never looks like just another end cell.
                if (_GhostStrength > 0.001) {
                    float d = length(cellCoord - _GhostPos.xy);
                    float aaG = max(fwidth(d), 1e-5);
                    float mask;
                    if (_GhostInvalid > 0.5) {
                        float ring = 0.34, w = 0.06;
                        mask = 1.0 - smoothstep(w, w + aaG, abs(d - ring));
                    } else {
                        float rad = 0.40;
                        mask = 1.0 - smoothstep(rad - aaG, rad + aaG, d);
                    }
                    outCol = lerp(outCol, _GhostColor.rgb, saturate(mask) * _GhostStrength * 0.9);
                }

                return float4(outCol, 1.0);
            }
            ENDCG
        }
    }
}
