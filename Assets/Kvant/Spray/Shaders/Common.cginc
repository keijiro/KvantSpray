//
// Common parts of Spray shaders
//


sampler2D _PositionTex;
sampler2D _RotationTex;

half4 _Color;
half4 _Color2;
half2 _PbrParams;
float2 _ScaleParams;
float2 _BufferOffset;

// PRNG function.
float nrand(float2 uv)
{
    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
}

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

// Normalize a unit quaternion.
float4 normalize_quaternion(float4 q)
{
    q.w = sqrt(1.0 - dot(q.xyz, q.xyz));
    return q;
}

// Calculate a scaling value.
float calc_scale(float scale01, float time01)
{
    float s = lerp(_ScaleParams.x, _ScaleParams.y, scale01);
    // Linear scaling animation with life.
    // (0, 0) -> (0.1, 1) -> (0.9, 1) -> (1, 0)
    return s * min(1.0, 5.0 - abs(5.0 - time01 * 10));
}

// Calculate a color.
float4 calc_color(float2 uv, float time01)
{
#ifdef COLOR_ANIMATE
    return lerp(_Color2, _Color, time01);
#elif COLOR_RANDOM
    return lerp(_Color2, _Color, nrand(uv));
#else
    return _Color;
#endif
}
