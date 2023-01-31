Shader "Custom/Atlased"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Tex1 ("Albedo", 2D) = "white" {}
        _Tex3 ("Normal", 2D) = "bump" {}
        _Tex2 ("Emission", 2D) = "black" {}
        //_Glossiness ("Smoothness", Range(0,1)) = 0.5
        //_Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows addshadow 

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _Tex1;
        sampler2D _Tex2;
        sampler2D _Tex3;

        struct Input{
            float2 uv_Tex1;
            float2 uv2_Tex2;
            float2 uv3_Tex3;
        };
        
        //half _Glossiness;
        //half _Metallic;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o) {
            // Albedo comes from a texture tinted by color
            float2 uv = IN.uv_Tex1;
            float2 uv2 = IN.uv2_Tex2;
            float2 uv3 = IN.uv3_Tex3;
            uv = frac(uv)*uv3+uv2;
            fixed4 c = tex2D (_Tex1, uv) * _Color;
            clip(c.a-0.5);
            o.Albedo = c.rgb;
            fixed4 c2 = tex2D (_Tex3, uv) * _Color;
            o.Normal = UnpackNormal (float4(c2.rgb,1));
            fixed4 c3 = tex2D (_Tex2, uv) * _Color;
            o.Emission = c3;
            o.Metallic = c3.a;
            o.Smoothness = c2.a;
            o.Alpha = c.a;
        }
        ENDCG
    }
// Pass to render object as a shadow caster

    FallBack "Diffuse"
}
