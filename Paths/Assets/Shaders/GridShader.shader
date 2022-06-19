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

            // Every shape below is drawn by EVERY cell it can reach into, not just the cell that
            // owns it. The old per-state dispatch clipped each shape at its own cell border, which
            // is exactly where the notch/seam artifacts came from - a disc or capsule spilling into
            // a neighbouring cell just stopped dead at the boundary. The fields are unions over the
            // 3x3 neighbourhood, so they're continuous across borders by construction.

            // signed distance to a rounded box of half-extent b, corner radius r, centered at 0
            float RoundedBox(float2 p, float2 b, float r)
            {
                float2 q = abs(p) - b + r;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            // distance to the segment ab - the path ribbon is built from these
            float Segment(float2 p, float2 a, float2 b)
            {
                float2 pa = p - a, ba = b - a;
                float h = saturate(dot(pa, ba) / dot(ba, ba));
                return length(pa - ba * h);
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

            // the 3x3 state block around the pixel's cell, fetched once per fragment
            float StateAt(float s[9], int ox, int oy)
            {
                return s[(oy + 1) * 3 + ox + 1];
            }

            bool WallAt(float s[9], int ox, int oy)
            {
                if (abs(ox) > 1 || abs(oy) > 1)
                    return false; // outside the block; only a box's far faces land here, and those
                                  // can't influence pixels on its near side anyway
                return StateAt(s, ox, oy) == 1.0;
            }

            // how far a tile edge sits from the cell centre. a fused side (or a gap too thin for
            // the AA to resolve - grid lines off, or zoomed way out) pushes the edge just past the
            // boundary, so abutting coverage overlaps instead of meeting in an AA hairline.
            float SideExtent(bool fused, float gap, float pixel)
            {
                if (fused || gap <= pixel)
                    return 0.5 + pixel;
                return 0.5 - gap;
            }

            // rounded box with an independent radius per corner (r = TR, BR, TL, BL) - a wall
            // rounds only the corners that face open space and stays square on a fused edge
            float RoundedBoxCorners(float2 p, float2 b, float4 r)
            {
                r.xy = p.x > 0.0 ? r.xy : r.zw;
                r.x = p.y > 0.0 ? r.x : r.y;
                float2 q = abs(p) - b + r.x;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r.x;
            }

            // one wall cell's box: overreaching fused sides, gap-inset exposed ones, corners
            // rounded only where both adjacent sides are exposed. p in that cell's local frame.
            float WallBox(float2 p, bool fL, bool fR, bool fB, bool fT, float m, float rr, float pixel)
            {
                float2 lo = -float2(SideExtent(fL, m, pixel), SideExtent(fB, m, pixel));
                float2 hi = float2(SideExtent(fR, m, pixel), SideExtent(fT, m, pixel));
                float4 radii;
                radii.x = !fR && !fT ? rr : 0.0; // TR
                radii.y = !fR && !fB ? rr : 0.0; // BR
                radii.z = !fL && !fT ? rr : 0.0; // TL
                radii.w = !fL && !fB ? rr : 0.0; // BL
                return RoundedBoxCorners(p - (lo + hi) * 0.5, (hi - lo) * 0.5, radii);
            }

            // concave carve at a re-entrant corner. V = the cell-corner vertex (cell-local frame),
            // s = (+/-1,+/-1) toward the one non-wall cell of the 2x2 (the notch). the box union
            // fills an inner corner with a protruding convex nub - the diagonal cell fuses to the
            // vertex while the two arms inset by the gap. carving a disc tucked into the notch,
            // tangent to both inset arm faces, eats the nub and rounds the reflex concavely.
            float CornerCarve(float2 p, float2 V, float2 s, float m, float rf)
            {
                float2 Kc = V - s * m;      // the inset arms meet here (the sharp reflex corner)
                float2 C = Kc + s * rf;     // disc centre: tangent to both arm faces, into the notch
                return rf - length(p - C);  // >0 inside the disc = carve this pixel out of the wall
            }

            // the inner-corner carves: at each of the cell's four corners, a re-entrant corner
            // (exactly 3 of the 4 cells wall) carves a concave notch toward the odd cell out. every
            // cell around the vertex carves the same disc, so it stays continuous across the border.
            // returns the max carve (union of discs to remove); big-negative when nothing qualifies.
            float WallCarve(float s[9], float2 p, float m, float rf)
            {
                float c = -1e5;
                if (rf <= 0.0)
                    return c;
                for (int b = -1; b <= 1; b += 2)
                for (int a = -1; a <= 1; a += 2) {
                    bool c0 = WallAt(s, 0, 0);
                    bool cA = WallAt(s, a, 0);
                    bool cB = WallAt(s, 0, b);
                    bool cD = WallAt(s, a, b);
                    int walls = (c0 ? 1 : 0) + (cA ? 1 : 0) + (cB ? 1 : 0) + (cD ? 1 : 0);
                    if (walls != 3)
                        continue;
                    // s points from the vertex toward the single non-wall cell
                    float2 sd = !c0 ? float2(-a, -b) : !cA ? float2(a, -b)
                        : !cB ? float2(-a, b) : float2(a, b);
                    c = max(c, CornerCarve(p, float2(a, b) * 0.5, sd, m, rf));
                }
                return c;
            }

            // the wall mass: the union of every wall box in the 3x3, so a box spilling over its
            // own cell border (the fused-edge overlap and its sub-pixel corner poke) is drawn by
            // every cell it reaches instead of clipping there. walls fuse orthogonally only;
            // diagonal touches keep their gap. inner corners are then carved concave (WallCarve).
            float WallField(float s[9], float2 p, float m, float rr, float pixel)
            {
                float d = 1e5;
                for (int oy = -1; oy <= 1; oy++)
                for (int ox = -1; ox <= 1; ox++) {
                    if (StateAt(s, ox, oy) != 1.0)
                        continue;
                    d = min(d, WallBox(p - float2(ox, oy),
                        WallAt(s, ox - 1, oy), WallAt(s, ox + 1, oy),
                        WallAt(s, ox, oy - 1), WallAt(s, ox, oy + 1), m, rr, pixel));
                }
                return max(d, WallCarve(s, p, m, rr));
            }

            // the path connects path cells to each other and docks into the start/end markers
            bool LinkEnd(float s)
            {
                return s == 6.0 || s == 2.0 || s == 3.0;
            }

            // both ends connectable and at least one is actual path - start touching end with no
            // route between them draws nothing
            bool Link(float a, float b)
            {
                return LinkEnd(a) && LinkEnd(b) && (a == 6.0 || b == 6.0);
            }

            // a diagonal link only draws when the route can't turn through an orthogonal cell at
            // that corner instead. the states carry no step order, so without this every
            // right-angle turn also grows a spurious diagonal chord across its inside corner.
            bool DiagLink(float s[9], int ax, int ay, int bx, int by)
            {
                if (!Link(StateAt(s, ax, ay), StateAt(s, bx, by)))
                    return false;
                return !LinkEnd(StateAt(s, ax, by)) && !LinkEnd(StateAt(s, bx, ay));
            }

            // the path ribbon: a constant-width capsule per link, centre to centre. every capsule
            // is drawn by each of the (up to four) cells it can cross - the two endpoint cells, and
            // for a diagonal link the two cells flanking the corner it cuts through.
            float PathField(float s[9], float2 p, float hw)
            {
                float self = StateAt(s, 0, 0);
                float d = 1e5;
                if (self == 6.0)
                    d = length(p) - hw; // a lone path cell still reads

                if (LinkEnd(self)) {
                    if (Link(self, StateAt(s, -1, 0))) d = min(d, Segment(p, 0.0, float2(-1, 0)) - hw);
                    if (Link(self, StateAt(s, 1, 0))) d = min(d, Segment(p, 0.0, float2(1, 0)) - hw);
                    if (Link(self, StateAt(s, 0, -1))) d = min(d, Segment(p, 0.0, float2(0, -1)) - hw);
                    if (Link(self, StateAt(s, 0, 1))) d = min(d, Segment(p, 0.0, float2(0, 1)) - hw);
                    if (DiagLink(s, 0, 0, -1, -1)) d = min(d, Segment(p, 0.0, float2(-1, -1)) - hw);
                    if (DiagLink(s, 0, 0, 1, -1)) d = min(d, Segment(p, 0.0, float2(1, -1)) - hw);
                    if (DiagLink(s, 0, 0, -1, 1)) d = min(d, Segment(p, 0.0, float2(-1, 1)) - hw);
                    if (DiagLink(s, 0, 0, 1, 1)) d = min(d, Segment(p, 0.0, float2(1, 1)) - hw);
                }

                // chords between this cell's flanking neighbours cut through this cell's corners
                if (DiagLink(s, 1, 0, 0, 1)) d = min(d, Segment(p, float2(1, 0), float2(0, 1)) - hw);
                if (DiagLink(s, 1, 0, 0, -1)) d = min(d, Segment(p, float2(1, 0), float2(0, -1)) - hw);
                if (DiagLink(s, -1, 0, 0, 1)) d = min(d, Segment(p, float2(-1, 0), float2(0, 1)) - hw);
                if (DiagLink(s, -1, 0, 0, -1)) d = min(d, Segment(p, float2(-1, 0), float2(0, -1)) - hw);
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

            // indices 4/5 are Seen/Expanded - the search layer, dimmed toward the plain tile (or
            // painted with the cost heatmap) so the board stays one readable surface
            float3 Shade(int s, float3 c, float3 heat)
            {
                if (s == 4 || s == 5) {
                    if (_ShowDistances > 0.5) c = heat;
                    c = lerp(_EmptyColor.rgb, c, _Fade);
                }
                return c;
            }

            float Mask(float d, float aa)
            {
                return 1.0 - smoothstep(-aa * 0.5, aa * 0.5, d);
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

                // screen density in cell units - how big a pixel is across a cell. drives the edge
                // antialiasing, the seam overlap, and the zoom-out degrade below. the fields all
                // have unit gradient, so this doubles as the AA width (fwidth of a min() union
                // glitches at the creases, a fixed width doesn't).
                float2 dd = fwidth(cellCoord);
                float pixel = max(max(dd.x, dd.y), 1e-5);
                float aa = pixel;

                // fade the gap and rounding out as cells shrink toward a few pixels, so the far
                // view collapses to clean flat color instead of shimmering sub-pixel gaps
                float k = saturate((1.0 / pixel - 3.0) / 4.0);

                float m = _Gap * 0.5 * k;
                // corners only round into a gap - with grid lines off everything stays square
                float rr = _Radius * k * smoothstep(0.0, 0.03, _Gap);

                float st[9];
                for (int oy = -1; oy <= 1; oy++)
                for (int ox = -1; ox <= 1; ox++)
                    st[(oy + 1) * 3 + ox + 1] = NeighbourState(id + float2(ox, oy), grid);

                // layers, back to front: plain tiles, walls, the path ribbon, start/end markers.
                // start/end sit above the ribbon so the route visibly docks INTO them.
                float baseD = 1e5, termD = 1e5;
                if (index == 0 || index == 4 || index == 5 || index == 2 || index == 3) {
                    // cells appearing out of empty scale in as they ease
                    float pop = prev == 0 && index != 0 ? lerp(0.55, 1.0, ease) : 1.0;
                    float b = SideExtent(false, m, pixel) * pop;
                    float tile = RoundedBox(p, b.xx, rr * pop);
                    if (index == 2 || index == 3) termD = tile;
                    else baseD = tile;
                }
                float wallD = WallField(st, p, m, rr, pixel);
                float pathD = PathField(st, p, 0.5 - _PathInset);

                // ease the fill from the state the cell left toward the one it's in
                float3 heat = Heat(tex2D(_DistTex, cellUv).r);
                float3 fade = lerp(Shade(prev, colors[prev].rgb, heat),
                    Shade(index, colors[index].rgb, heat), ease);

                bool hovered = _HoverStrength > 0.001 && all(abs(id - _HoverCell.xy) < 0.5);

                float3 baseCol = fade;
                // an empty cell under the cursor previews the wall the brush would draw
                if (hovered && _BrushPreview > 0.5 && index == 0)
                    baseCol = lerp(baseCol, _WallColor.rgb, 0.4 * _HoverStrength);

                float3 outCol = lerp(_LineColor.rgb, baseCol, Mask(baseD, aa));
                // own wall/path cells crossfade; spill from a neighbour draws settled
                outCol = lerp(outCol, index == 1 ? fade : _WallColor.rgb, Mask(wallD, aa));
                outCol = lerp(outCol, index == 6 ? fade : _PathColor.rgb, Mask(pathD, aa));
                outCol = lerp(outCol, fade, Mask(termD, aa));

                // cursor overlay - a gentle lift plus a brighter rim on whatever shape the hovered
                // cell renders. all uniform-driven, so the touch path (which never sets them) skips it.
                if (hovered) {
                    float dist = min(min(baseD, wallD), min(pathD, termD));
                    float rim = (1.0 - smoothstep(0.0, 0.07, abs(dist))) * k;
                    outCol += _HoverColor.rgb * (0.10 + 0.6 * rim) * _HoverStrength * Mask(dist, aa);
                }

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
