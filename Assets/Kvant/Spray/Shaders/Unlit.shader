//
// Transparent unlit shader for Spray
//
// Vertex format:
// position.xyz = vertex position
// texcoord.xy  = uv for PositionTex/RotationTex
//
// Texture format:
// _PositionTex.xyz = particle position
// _PositionTex.w   = life
// _RotationTex.xyz = particle rotation
// _RotstionTex.w   = scale factor
//
Shader "Hidden/Kvant/Spray/Transparent Unlit"
{
    Properties
    {
        _PositionTex  ("-", 2D)     = ""{}
        _RotationTex  ("-", 2D)     = ""{}
        _Color        ("-", Color)  = (1, 1, 1, 1)
        _Color2       ("-", Color)  = (1, 1, 1, 1)
        _ScaleParams  ("-", Vector) = (1, 1, 0, 0)     // (min scale, max scale)
        _BufferOffset ("-", Vector) = (0, 0, 0, 0)
    }

    CGINCLUDE

    #pragma multi_compile COLOR_SINGLE COLOR_ANIMATE COLOR_RANDOM
    #pragma multi_compile BLEND_ALPHA BLEND_ADD

    #pragma multi_compile_fog

    #include "UnityCG.cginc"
    #include "Common.cginc"

    struct appdata
    {
        float4 vertex : POSITION;
        float2 texcoord : TEXCOORD0;
    };

    struct v2f
    {
        float4 position : SV_POSITION;
        half4 color : COLOR;
        UNITY_FOG_COORDS(0)
    };

    v2f vert(appdata v)
    {
        float4 uv = float4(v.texcoord.xy + _BufferOffset, 0, 0);

        float4 p = tex2Dlod(_PositionTex, uv);
        float4 r = tex2Dlod(_RotationTex, uv);
        float4 q = normalize_quaternion(r);
        float s = calc_scale(r.w, p.w);

        v.vertex.xyz = rotate_vector(v.vertex.xyz, q) * s + p.xyz;

        v2f o;

        o.position = mul(UNITY_MATRIX_MVP, v.vertex);
        o.color = calc_color(uv, p.w);

        UNITY_TRANSFER_FOG(o, o.position);

        return o;
    }

    half4 frag(v2f i) : SV_Target
    {
        half4 c = i.color;
        UNITY_APPLY_FOG_COLOR(i.fogCoord, c, (half4)0);
        c.rgb *= c.a;
#ifdef BLEND_ADD
        c.a = 0;
#endif
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
}
