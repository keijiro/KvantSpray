//
// Transparent surface shader for Spray
//
// Vertex format:
// position.xyz = vertex position
// texcoord.xy  = uv for GPGPU buffers
//
// Texture format in position kernels:
// .xyz = particle position
// .w   = life
//
// Texture format in rotation kernels:
// .xyzw = particle rotation
//
Shader "Kvant/Spray/Transparent PBR"
{
    Properties
    {
        _PositionBuffer ("-", 2D) = "black"{}
        _RotationBuffer ("-", 2D) = "red"{}

        [KeywordEnum(Single, Animate, Random)]
        _ColorMode ("-", Float) = 0
        _Color     ("-", Color) = (1, 1, 1, 1)
        _Color2    ("-", Color) = (0.5, 0.5, 0.5, 1)

        _Metallic   ("-", Range(0,1)) = 0.5
        _Smoothness ("-", Range(0,1)) = 0.5

        _ScaleMin ("-", Float) = 1
        _ScaleMax ("-", Float) = 1

        _RandomSeed ("-", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        CGPROGRAM

        #pragma surface surf Standard vertex:vert nolightmap noshadow alpha:fade
        #pragma shader_feature _COLORMODE_RANDOM
        #pragma target 3.0

        #include "Common.cginc"

        half _Metallic;
        half _Smoothness;

        struct Input
        {
            half4 color : COLOR;
        };

        void vert(inout appdata_full v)
        {
            float4 uv = float4(v.texcoord.xy + _BufferOffset, 0, 0);

            float4 p = tex2Dlod(_PositionBuffer, uv);
            float4 r = tex2Dlod(_RotationBuffer, uv);

            float l = p.w + 0.5;
            float s = calc_scale(uv, l);

            v.vertex.xyz = rotate_vector(v.vertex.xyz, r) * s + p.xyz;
            v.normal = rotate_vector(v.normal, r);
            v.color = calc_color(uv, l);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = IN.color.rgb;
            o.Alpha = IN.color.a;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
        }

        ENDCG
    }
    CustomEditor "Kvant.SpraySurfaceMaterialEditor"
}
