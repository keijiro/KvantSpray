//
// Surface shader for Spray
//
// Vertex format:
// position.xyz = vertex position
// texcoord.xy  = uv for PositionTex/RotationTex
//
Shader "Hidden/Kvant/Spray/Surface"
{
    Properties
    {
        _PositionTex  ("-", 2D)     = ""{}
        _RotationTex  ("-", 2D)     = ""{}
        _Color        ("-", Color)  = (1, 1, 1, 1)
		_PbrParams    ("-", Vector) = (0.5, 0.5, 0, 0)
        _ScaleParams  ("-", Vector) = (1, 1, 0, 0)
        _BufferOffset ("-", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        
        CGPROGRAM

        #pragma surface surf Standard vertex:vert

        sampler2D _PositionTex;
        sampler2D _RotationTex;

        half4 _Color;
        half2 _PbrParams;

        float2 _ScaleParams;
        float4 _BufferOffset;

        struct Input
        {
            float dummy;
        };

        // Quaternion multiplication.
        // http://mathworld.wolfram.com/Quaternion.html
        float4 qmul(float4 q1, float4 q2)
        {
            return float4(
                q2.xyz * q1.w + q1.xyz * q2.w + cross(q1.xyz, q2.xyz),
                q1.w * q2.w - dot(q1.xyz, q2.xyz)
            );
        }

        // Rotate a vector with a rotation quaternion.
        // http://mathworld.wolfram.com/Quaternion.html
        float3 rotate_vector(float3 v, float4 r)
        {
            float4 r_c = r * float4(-1, -1, -1, 1);
            return qmul(r, qmul(float4(v, 0), r_c)).xyz;
        }

        void vert(inout appdata_full v)
        {
            float2 uv = v.texcoord + _BufferOffset;

            float4 p = tex2D(_PositionTex, uv);
            float4 r = tex2D(_RotationTex, uv);

            // Get the scale factor from life (p.w) and scale (r.w).
            float s = lerp(_ScaleParams.x, _ScaleParams.y, r.w);
            s *= min(1.0, 5.0 - abs(5.0 - p.w * 10));

            // Recover the scalar component of the unit quaternion.
            r.w = sqrt(1.0 - dot(r.xyz, r.xyz));

            // Apply the rotation and the scaling.
            v.vertex.xyz = rotate_vector(v.vertex.xyz, r) * s + p.xyz;
            v.normal = rotate_vector(v.normal, r);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = _Color.rgb;
            o.Metallic = _PbrParams.x;
            o.Smoothness = _PbrParams.y;
            o.Alpha = _Color.a;
        }

        ENDCG
    } 
}
