// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

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
        
        [PerRendererData] _MainTex ("Albedo (RGB)", 2D) = "white" {}
        [IntRange] _GridSize("Grid Size", Range(1,100)) = 10
        _LineSize("Line Size", Range(0,1)) = 0.15
        _Fade("Search Fade", Range(0,1)) = 0.7
    }
    SubShader
    {
        Tags
        {
            "Queue"="AlphaTest" "RenderType"="TransparentCutout"
        }
        LOD 200


        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows
        #pragma target 5.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness = 0.0;
        half _Metallic = 0.0;

        float4 _LineColor;
        float4 _EmptyColor;
        float4 _WallColor;
        float4 _StartColor;
        float4 _EndColor;
        float4 _SeenColor;
        float4 _ExpandedColor;
        float4 _PathColor;

        static const float4 _gridColors[7] = {
            _EmptyColor,
            _WallColor,
            _StartColor,
            _EndColor,
            _SeenColor,
            _ExpandedColor,
            _PathColor
        };

        float _GridWidth;
        float _GridHeight;
        float _LineSize;
        float _Fade; // search-layer dim, driven from GridController (milder idle, stronger while editing)

        // StructuredBuffer is what GridController actually binds (SetBuffer), so use it on
        // every SM5 target that supports it - not just DX11, or the grid is blank on GL/Vulkan.
        // The float[] is a last-ditch fallback for GLES/WebGL (and blows the constant limit, so
        // it doesn't really link there anyway - that path gets replaced later).
        #if defined(SHADER_API_D3D11) || defined(SHADER_API_D3D12) || defined(SHADER_API_GLCORE) || defined(SHADER_API_VULKAN) || defined(SHADER_API_METAL)
        StructuredBuffer<int> _values;
        #else
		float _values[1024];
        #endif
        float _valueLength;


        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
        // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color

            float2 uv = IN.uv_MainTex;

            fixed4 c = float4(0.0, 0.0, 0.0, 0.0);
            float gsize_width = floor(_GridWidth) + _LineSize;
            float gsize_height = floor(_GridHeight) + _LineSize;

            float2 id = float2(
                floor(uv.x / (1.0 / gsize_width)), floor(uv.y / (1.0 / gsize_height))
            );

            float4 color = _EmptyColor;
            float brightness = _EmptyColor.w;
            // only the search layer (seen/expanded/path) dims - walls/start/end/lines stay solid
            float cellFade = 1.0;

            // Line Color Check
            if (_LineSize > 0.0 && (frac(uv.x * gsize_width) <= _LineSize || frac(uv.y * gsize_height) <= _LineSize))
            {
                color = _LineColor;
                brightness = color.w;
                // Heatmap Value Fallback
            }
            else
            {
                float pos = id.y * _GridWidth + id.x;
                if (pos < _valueLength)
                {
                    float index = _values[pos];
                    color = _gridColors[index];
                    // color = lerp(_InactiveColor, _ActiveColor, _values[pos]);
                    brightness = color.w;
                    // indices 4/5/6 are Seen/Expanded/Path - the only cells we fade
                    if (index >= 3.5)
                        cellFade = _Fade;
                }
            }

            // Clip transparent spots using alpha cutout
            if (brightness == 0.0)
                clip(c.a - 1.0);

            o.Albedo = float4(color.x * brightness * cellFade, color.y * brightness * cellFade, color.z * brightness * cellFade, brightness);
            // Metallic and smoothness come from slider variables
            o.Metallic = 0.0;
            o.Smoothness = 0.0;
            o.Alpha = 0.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}