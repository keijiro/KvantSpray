//
// Transparent unlit shader for Spray
//
// Vertex format:
// position.xyz = vertex position
// texcoord.xy  = uv for GPGPU buffers
//
// Position buffer format:
// .xyz = particle position
// .w   = life (+0.5 -> -0.5)
//
// Rotation buffer format:
// .xyzw = particle rotation
//
Shader "Kvant/Spray/Transparent Unlit"
{
    Properties
    {
        _PositionBuffer ("-", 2D) = "black"{}
        _RotationBuffer ("-", 2D) = "red"{}

        [Enum(Add, 0, AlphaBlend, 1)]
        _BlendMode ("-", Float) = 0

        [KeywordEnum(Single, Animate, Random)]
        _ColorMode ("-", Float) = 0

        [HDR] _Color  ("-", Color) = (1, 1, 1, 1)
        [HDR] _Color2 ("-", Color) = (0.5, 0.5, 0.5, 1)

        _MainTex ("-", 2D) = "white"{}

        _ScaleMin ("-", Float) = 1
        _ScaleMax ("-", Float) = 1

        _RandomSeed ("-", Float) = 0
    }

    CGINCLUDE

    #pragma shader_feature _COLORMODE_RANDOM
    #pragma shader_feature _MAINTEX
    #pragma multi_compile_fog

    #include "UnityCG.cginc"
    #include "Common.cginc"

    sampler2D _MainTex;
    float4 _MainTex_ST;
    half _BlendMode;

    struct appdata
    {
        float4 vertex : POSITION;
        float2 texcoord0 : TEXCOORD0;
        float2 texcoord1 : TEXCOORD1;
    };

    struct v2f
    {
        float4 position : SV_POSITION;
        float2 texcoord : TEXCOORD;
        half4 color : COLOR;
        UNITY_FOG_COORDS(1)
    };

    v2f vert(appdata v)
    {
        float4 uv = float4(v.texcoord1.xy + _BufferOffset, 0, 0);

        float4 p = tex2Dlod(_PositionBuffer, uv);
        float4 r = tex2Dlod(_RotationBuffer, uv);

        float l = p.w + 0.5;
        float s = calc_scale(uv, l);

        v.vertex.xyz = rotate_vector(v.vertex.xyz, r) * s + p.xyz;

        v2f o;

        o.position = mul(UNITY_MATRIX_MVP, v.vertex);
        o.texcoord = TRANSFORM_TEX(v.texcoord0, _MainTex);
        o.color = calc_color(uv, l);

        UNITY_TRANSFER_FOG(o, o.position);

        return o;
    }

    half4 frag(v2f i) : SV_Target
    {
        half4 c = i.color;
    #if _MAINTEX
        c *= tex2D(_MainTex, i.texcoord);
    #endif
        UNITY_APPLY_FOG_COLOR(i.fogCoord, c, (half4)0);
        c *= float4(c.aaa, _BlendMode);
        return c;
    }

    ENDCG

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Blend One OneMinusSrcAlpha
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
    }
    CustomEditor "Kvant.SprayUnlitMaterialEditor"
}
