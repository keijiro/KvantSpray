//
// Common parts of Spray shaders
//

sampler2D _PositionBuffer;
sampler2D _RotationBuffer;

half _ColorMode;
half4 _Color;
half4 _Color2;
float _ScaleMin;
float _ScaleMax;
float _RandomSeed;
float2 _BufferOffset;

// PRNG function
float nrand(float2 uv, float salt)
{
    uv += float2(salt, _RandomSeed);
    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
}

// Quaternion multiplication
// http://mathworld.wolfram.com/Quaternion.html
float4 qmul(float4 q1, float4 q2)
{
    return float4(
        q2.xyz * q1.w + q1.xyz * q2.w + cross(q1.xyz, q2.xyz),
        q1.w * q2.w - dot(q1.xyz, q2.xyz)
    );
}

// Vector rotation with a quaternion
// http://mathworld.wolfram.com/Quaternion.html
float3 rotate_vector(float3 v, float4 r)
{
    float4 r_c = r * float4(-1, -1, -1, 1);
    return qmul(r, qmul(float4(v, 0), r_c)).xyz;
}

// Scale factor function
float calc_scale(float2 uv, float time01)
{
    float s = lerp(_ScaleMin, _ScaleMax, nrand(uv, 14));
    // Linear scaling animation with life.
    // (0, 0) -> (0.1, 1) -> (0.9, 1) -> (1, 0)
    return s * min(1.0, 5.0 - abs(5.0 - time01 * 10));
}

// Color function
float4 calc_color(float2 uv, float time01)
{
#if _COLORMODE_RANDOM
    return lerp(_Color, _Color2, nrand(uv, 15));
#else
    return lerp(_Color, _Color2, (1.0 - time01) * _ColorMode);
#endif
}
