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
        
        static const float4 _gridColors[7] = {
        	float4(255 / 255.0, 255 / 255.0, 255 / 255.0, 1.0), // Empty
        	float4(0   / 255.0, 0   / 255.0, 0   / 255.0, 1.0), // Wall
        	float4(0   / 255.0, 255 / 255.0, 0   / 255.0, 1.0), // Start
        	float4(255 / 255.0, 0   / 255.0, 0   / 255.0, 1.0), // End
        	float4(252 / 255.0, 236 / 255.0, 3   / 255.0, 1.0), // Seen
        	float4(252 / 255.0, 127 / 255.0, 3   / 255.0, 1.0), // Expanded
        	float4(166 / 255.0, 2   / 255.0, 51  / 255.0, 1.0) // Path
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
			        float index = _values[pos];
			        color = _gridColors[index];
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
