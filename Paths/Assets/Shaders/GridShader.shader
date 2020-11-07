// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "PDT Shaders/TestGrid" {
	Properties {
		_LineColor ("Line Color", Color) = (1,1,1,1)
		_InactiveColor ("Inactive Color", Color) = (0,0,0,0)
		_ActiveColor ("Active Color", Color) = (1,0,0,1)
		[PerRendererData] _MainTex ("Albedo (RGB)", 2D) = "white" {}
		[IntRange] _GridSize("Grid Size", Range(1,100)) = 10
		_LineSize("Line Size", Range(0,1)) = 0.15
	}
	SubShader {
		Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
		LOD 200
	

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows
		#pragma target 5.0

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
		};

		half _Glossiness = 0.0;
		half _Metallic = 0.0;
		
		float4 _LineColor;
		float4 _InactiveColor;
		float4 _ActiveColor;
        
        static const float4 _gradient[250] = {
            float4(0.443, 0.0, 0.0, 1.0),
            float4(0.486, 0.0, 0.0, 1.0),
            float4(0.506, 0.0, 0.0, 1.0),
            float4(0.522, 0.0, 0.0, 1.0),
            float4(0.537, 0.0, 0.0, 1.0),
            float4(0.553, 0.0, 0.0, 1.0),
            float4(0.573, 0.0, 0.0, 1.0),
            float4(0.588, 0.0, 0.0, 1.0),
            float4(0.604, 0.0, 0.0, 1.0),
            float4(0.62, 0.0, 0.0, 1.0),
            float4(0.639, 0.0, 0.0, 1.0),
            float4(0.659, 0.0, 0.0, 1.0),
            float4(0.675, 0.0, 0.0, 1.0),
            float4(0.69, 0.0, 0.0, 1.0),
            float4(0.706, 0.0, 0.0, 1.0),
            float4(0.725, 0.0, 0.0, 1.0),
            float4(0.741, 0.0, 0.0, 1.0),
            float4(0.757, 0.0, 0.0, 1.0),
            float4(0.792, 0.0, 0.0, 1.0),
            float4(0.808, 0.0, 0.0, 1.0),
            float4(0.808, 0.0, 0.0, 1.0),
            float4(0.824, 0.0, 0.0, 1.0),
            float4(0.843, 0.0, 0.0, 1.0),
            float4(0.875, 0.012, 0.0, 1.0),
            float4(0.89, 0.027, 0.0, 1.0),
            float4(0.91, 0.039, 0.0, 1.0),
            float4(0.91, 0.039, 0.0, 1.0),
            float4(0.925, 0.055, 0.0, 1.0),
            float4(0.945, 0.078, 0.0, 1.0),
            float4(0.945, 0.094, 0.0, 1.0),
            float4(0.945, 0.11, 0.0, 1.0),
            float4(0.945, 0.125, 0.0, 1.0),
            float4(0.945, 0.133, 0.0, 1.0),
            float4(0.945, 0.149, 0.0, 1.0),
            float4(0.945, 0.165, 0.0, 1.0),
            float4(0.945, 0.176, 0.0, 1.0),
            float4(0.945, 0.192, 0.0, 1.0),
            float4(0.945, 0.204, 0.0, 1.0),
            float4(0.945, 0.216, 0.0, 1.0),
            float4(0.945, 0.231, 0.0, 1.0),
            float4(0.945, 0.247, 0.0, 1.0),
            float4(0.945, 0.263, 0.0, 1.0),
            float4(0.945, 0.271, 0.0, 1.0),
            float4(0.945, 0.286, 0.0, 1.0),
            float4(0.945, 0.302, 0.0, 1.0),
            float4(0.945, 0.314, 0.0, 1.0),
            float4(0.945, 0.329, 0.0, 1.0),
            float4(0.945, 0.341, 0.0, 1.0),
            float4(0.945, 0.353, 0.0, 1.0),
            float4(0.945, 0.369, 0.0, 1.0),
            float4(0.945, 0.384, 0.0, 1.0),
            float4(0.945, 0.4, 0.0, 1.0),
            float4(0.945, 0.408, 0.0, 1.0),
            float4(0.945, 0.424, 0.0, 1.0),
            float4(0.945, 0.439, 0.0, 1.0),
            float4(0.945, 0.451, 0.0, 1.0),
            float4(0.945, 0.467, 0.0, 1.0),
            float4(0.945, 0.478, 0.0, 1.0),
            float4(0.945, 0.494, 0.0, 1.0),
            float4(0.945, 0.506, 0.0, 1.0),
            float4(0.945, 0.522, 0.0, 1.0),
            float4(0.945, 0.537, 0.0, 1.0),
            float4(0.945, 0.545, 0.0, 1.0),
            float4(0.945, 0.576, 0.0, 1.0),
            float4(0.945, 0.576, 0.0, 1.0),
            float4(0.945, 0.588, 0.0, 1.0),
            float4(0.945, 0.604, 0.0, 1.0),
            float4(0.945, 0.616, 0.0, 1.0),
            float4(0.945, 0.643, 0.0, 1.0),
            float4(0.945, 0.659, 0.0, 1.0),
            float4(0.945, 0.675, 0.0, 1.0),
            float4(0.945, 0.675, 0.0, 1.0),
            float4(0.945, 0.682, 0.0, 1.0),
            float4(0.945, 0.714, 0.0, 1.0),
            float4(0.945, 0.725, 0.0, 1.0),
            float4(0.945, 0.741, 0.0, 1.0),
            float4(0.945, 0.753, 0.0, 1.0),
            float4(0.945, 0.769, 0.0, 1.0),
            float4(0.945, 0.78, 0.0, 1.0),
            float4(0.945, 0.796, 0.0, 1.0),
            float4(0.945, 0.808, 0.0, 1.0),
            float4(0.945, 0.82, 0.0, 1.0),
            float4(0.945, 0.835, 0.0, 1.0),
            float4(0.945, 0.851, 0.0, 1.0),
            float4(0.945, 0.867, 0.0, 1.0),
            float4(0.941, 0.875, 0.0, 1.0),
            float4(0.929, 0.89, 0.0, 1.0),
            float4(0.918, 0.906, 0.0, 1.0),
            float4(0.902, 0.918, 0.004, 1.0),
            float4(0.89, 0.933, 0.02, 1.0),
            float4(0.878, 0.945, 0.031, 1.0),
            float4(0.871, 0.945, 0.043, 1.0),
            float4(0.855, 0.945, 0.055, 1.0),
            float4(0.843, 0.945, 0.067, 1.0),
            float4(0.831, 0.945, 0.078, 1.0),
            float4(0.82, 0.945, 0.09, 1.0),
            float4(0.808, 0.945, 0.102, 1.0),
            float4(0.796, 0.945, 0.114, 1.0),
            float4(0.784, 0.945, 0.129, 1.0),
            float4(0.773, 0.945, 0.137, 1.0),
            float4(0.761, 0.945, 0.149, 1.0),
            float4(0.745, 0.945, 0.161, 1.0),
            float4(0.737, 0.945, 0.176, 1.0),
            float4(0.725, 0.945, 0.188, 1.0),
            float4(0.714, 0.945, 0.2, 1.0),
            float4(0.702, 0.945, 0.208, 1.0),
            float4(0.686, 0.945, 0.22, 1.0),
            float4(0.675, 0.945, 0.235, 1.0),
            float4(0.655, 0.945, 0.259, 1.0),
            float4(0.655, 0.945, 0.259, 1.0),
            float4(0.639, 0.945, 0.267, 1.0),
            float4(0.627, 0.945, 0.282, 1.0),
            float4(0.616, 0.945, 0.294, 1.0),
            float4(0.592, 0.945, 0.318, 1.0),
            float4(0.58, 0.945, 0.333, 1.0),
            float4(0.569, 0.945, 0.341, 1.0),
            float4(0.569, 0.945, 0.341, 1.0),
            float4(0.557, 0.945, 0.353, 1.0),
            float4(0.533, 0.945, 0.376, 1.0),
            float4(0.522, 0.945, 0.392, 1.0),
            float4(0.51, 0.945, 0.404, 1.0),
            float4(0.498, 0.945, 0.412, 1.0),
            float4(0.498, 0.945, 0.412, 1.0),
            float4(0.471, 0.945, 0.439, 1.0),
            float4(0.463, 0.945, 0.451, 1.0),
            float4(0.451, 0.945, 0.463, 1.0),
            float4(0.439, 0.945, 0.471, 1.0),
            float4(0.424, 0.945, 0.482, 1.0),
            float4(0.412, 0.945, 0.498, 1.0),
            float4(0.404, 0.945, 0.51, 1.0),
            float4(0.392, 0.945, 0.522, 1.0),
            float4(0.376, 0.945, 0.533, 1.0),
            float4(0.365, 0.945, 0.545, 1.0),
            float4(0.353, 0.945, 0.557, 1.0),
            float4(0.341, 0.945, 0.569, 1.0),
            float4(0.333, 0.945, 0.58, 1.0),
            float4(0.318, 0.945, 0.592, 1.0),
            float4(0.306, 0.945, 0.604, 1.0),
            float4(0.294, 0.945, 0.616, 1.0),
            float4(0.282, 0.945, 0.627, 1.0),
            float4(0.267, 0.945, 0.639, 1.0),
            float4(0.259, 0.945, 0.655, 1.0),
            float4(0.247, 0.945, 0.667, 1.0),
            float4(0.235, 0.945, 0.675, 1.0),
            float4(0.22, 0.945, 0.686, 1.0),
            float4(0.208, 0.945, 0.702, 1.0),
            float4(0.2, 0.945, 0.714, 1.0),
            float4(0.188, 0.945, 0.725, 1.0),
            float4(0.176, 0.945, 0.737, 1.0),
            float4(0.161, 0.945, 0.745, 1.0),
            float4(0.149, 0.945, 0.761, 1.0),
            float4(0.137, 0.945, 0.773, 1.0),
            float4(0.129, 0.945, 0.784, 1.0),
            float4(0.102, 0.945, 0.808, 1.0),
            float4(0.102, 0.945, 0.808, 1.0),
            float4(0.09, 0.945, 0.82, 1.0),
            float4(0.078, 0.945, 0.831, 1.0),
            float4(0.067, 0.937, 0.843, 1.0),
            float4(0.043, 0.906, 0.871, 1.0),
            float4(0.031, 0.89, 0.878, 1.0),
            float4(0.031, 0.89, 0.878, 1.0),
            float4(0.02, 0.875, 0.89, 1.0),
            float4(0.004, 0.859, 0.902, 1.0),
            float4(0.0, 0.831, 0.929, 1.0),
            float4(0.0, 0.816, 0.941, 1.0),
            float4(0.0, 0.804, 0.945, 1.0),
            float4(0.0, 0.788, 0.945, 1.0),
            float4(0.0, 0.788, 0.945, 1.0),
            float4(0.0, 0.757, 0.945, 1.0),
            float4(0.0, 0.741, 0.945, 1.0),
            float4(0.0, 0.729, 0.945, 1.0),
            float4(0.0, 0.714, 0.945, 1.0),
            float4(0.0, 0.698, 0.945, 1.0),
            float4(0.0, 0.682, 0.945, 1.0),
            float4(0.0, 0.671, 0.945, 1.0),
            float4(0.0, 0.655, 0.945, 1.0),
            float4(0.0, 0.639, 0.945, 1.0),
            float4(0.0, 0.62, 0.945, 1.0),
            float4(0.0, 0.608, 0.945, 1.0),
            float4(0.0, 0.596, 0.945, 1.0),
            float4(0.0, 0.58, 0.945, 1.0),
            float4(0.0, 0.565, 0.945, 1.0),
            float4(0.0, 0.549, 0.945, 1.0),
            float4(0.0, 0.537, 0.945, 1.0),
            float4(0.0, 0.522, 0.945, 1.0),
            float4(0.0, 0.502, 0.945, 1.0),
            float4(0.0, 0.49, 0.945, 1.0),
            float4(0.0, 0.475, 0.945, 1.0),
            float4(0.0, 0.463, 0.945, 1.0),
            float4(0.0, 0.447, 0.945, 1.0),
            float4(0.0, 0.427, 0.945, 1.0),
            float4(0.0, 0.416, 0.945, 1.0),
            float4(0.0, 0.404, 0.945, 1.0),
            float4(0.0, 0.388, 0.945, 1.0),
            float4(0.0, 0.369, 0.945, 1.0),
            float4(0.0, 0.357, 0.945, 1.0),
            float4(0.0, 0.341, 0.945, 1.0),
            float4(0.0, 0.329, 0.945, 1.0),
            float4(0.0, 0.298, 0.945, 1.0),
            float4(0.0, 0.298, 0.945, 1.0),
            float4(0.0, 0.282, 0.945, 1.0),
            float4(0.0, 0.267, 0.945, 1.0),
            float4(0.0, 0.251, 0.945, 1.0),
            float4(0.0, 0.224, 0.945, 1.0),
            float4(0.0, 0.208, 0.945, 1.0),
            float4(0.0, 0.208, 0.945, 1.0),
            float4(0.0, 0.192, 0.945, 1.0),
            float4(0.0, 0.18, 0.945, 1.0),
            float4(0.0, 0.149, 0.945, 1.0),
            float4(0.0, 0.133, 0.945, 1.0),
            float4(0.0, 0.122, 0.945, 1.0),
            float4(0.0, 0.106, 0.945, 1.0),
            float4(0.0, 0.106, 0.945, 1.0),
            float4(0.0, 0.071, 0.945, 1.0),
            float4(0.0, 0.063, 0.945, 1.0),
            float4(0.0, 0.047, 0.945, 1.0),
            float4(0.0, 0.031, 0.945, 1.0),
            float4(0.0, 0.012, 0.945, 1.0),
            float4(0.0, 0.0, 0.945, 1.0),
            float4(0.0, 0.0, 0.945, 1.0),
            float4(0.0, 0.0, 0.945, 1.0),
            float4(0.0, 0.0, 0.945, 1.0),
            float4(0.0, 0.0, 0.945, 1.0),
            float4(0.0, 0.0, 0.925, 1.0),
            float4(0.0, 0.0, 0.91, 1.0),
            float4(0.0, 0.0, 0.89, 1.0),
            float4(0.0, 0.0, 0.875, 1.0),
            float4(0.0, 0.0, 0.859, 1.0),
            float4(0.0, 0.0, 0.839, 1.0),
            float4(0.0, 0.0, 0.824, 1.0),
            float4(0.0, 0.0, 0.808, 1.0),
            float4(0.0, 0.0, 0.792, 1.0),
            float4(0.0, 0.0, 0.773, 1.0),
            float4(0.0, 0.0, 0.757, 1.0),
            float4(0.0, 0.0, 0.741, 1.0),
            float4(0.0, 0.0, 0.725, 1.0),
            float4(0.0, 0.0, 0.706, 1.0),
            float4(0.0, 0.0, 0.69, 1.0),
            float4(0.0, 0.0, 0.675, 1.0),
            float4(0.0, 0.0, 0.659, 1.0),
            float4(0.0, 0.0, 0.639, 1.0),
            float4(0.0, 0.0, 0.62, 1.0),
            float4(0.0, 0.0, 0.604, 1.0),
            float4(0.0, 0.0, 0.588, 1.0),
            float4(0.0, 0.0, 0.573, 1.0),
            float4(0.0, 0.0, 0.553, 1.0),
            float4(0.0, 0.0, 0.537, 1.0),
            float4(0.0, 0.0, 0.522, 1.0),
            float4(0.0, 0.0, 0.486, 1.0),
            float4(0.0, 0.0, 0.471, 1.0)
        };
        
		float _GridSize;
		float _LineSize;

        // DX11 needed to run shader at high grid sizes
		#ifdef SHADER_API_D3D11
		StructuredBuffer<float> _values;
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

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color

			float2 uv = IN.uv_MainTex;

			fixed4 c = float4(0.0,0.0,0.0,0.0);
			float gsize = floor(_GridSize);
			gsize += _LineSize;

			float2 id = float2(floor(uv.x/(1.0/gsize)), floor(uv.y/(1.0/gsize)));

			float4 color = _InactiveColor;
			float brightness = _InactiveColor.w;
            
            // Line Color Check
            if (_LineSize > 0.0 && (frac(uv.x*gsize) <= _LineSize || frac(uv.y*gsize) <= _LineSize)){
				color = _LineColor;
				brightness = color.w;
            // Heatmap Value Fallback
			} else {
			    float pos = id.y * _GridSize + id.x;
			    if(pos < _valueLength) {
			        float index = clamp(floor(_values[pos] * 250), 0, 99);
			        color = _gradient[index];
			        // color = lerp(_InactiveColor, _ActiveColor, _values[pos]);
			        brightness = color.w;
			    }
			}

			// Clip transparent spots using alpha cutout
			if (brightness == 0.0)
				clip(c.a - 1.0);
			
			o.Albedo = float4(color.x * brightness,color.y * brightness,color.z * brightness, brightness);
			// Metallic and smoothness come from slider variables
			o.Metallic = 0.0;
			o.Smoothness = 0.0;
			o.Alpha = 0.0;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
