//
// Copyright (C) 2014, 2015 Keijiro Takahashi
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

Shader "Hidden/ColorSuite"
{
    Properties
    {
        _MainTex    ("-", 2D) = ""{}
        _Curves     ("-", 2D) = ""{}
        _Exposure   ("-", Float) = 1.0
        _Saturation ("-", Float) = 1.0
        _Balance    ("-", Vector) = (1, 1, 1, 0)
    }
    
    CGINCLUDE

    // Multi-compilation options.
    #pragma multi_compile COLORSPACE_SRGB COLORSPACE_LINEAR
    #pragma multi_compile BALANCING_OFF BALANCING_ON
    #pragma multi_compile TONEMAPPING_OFF TONEMAPPING_ON
    #pragma multi_compile DITHER_OFF DITHER_ORDERED DITHER_TRIANGULAR

    #include "UnityCG.cginc"
    
    sampler2D _MainTex;
    float2 _MainTex_TexelSize;
    sampler2D _Curves;
    float _Exposure;
    float _Saturation;
    float4 _Balance;

#if COLORSPACE_LINEAR

    // Color space conversion between sRGB and linear space.
    // http://chilliant.blogspot.com/2012/08/srgb-approximations-for-hlsl.html

    float3 srgb_to_linear(float3 c)
    {
        return c * (c * (c * 0.305306011 + 0.682171111) + 0.012522878);
    }

    float3 linear_to_srgb(float3 c)
    {
        return max(1.055 * pow(c, 0.416666667) - 0.055, 0.0);
    }

#endif

#if BALANCING_ON

    // Color space conversion between linear RGB and LMS
    // based on the CIECAM02 model (CAT02).
    // http://en.wikipedia.org/wiki/LMS_color_space#CAT02

    float3 lrgb_to_lms(float3 c)
    {
        float3x3 m = {
            3.90405e-1f, 5.49941e-1f, 8.92632e-3f,
            7.08416e-2f, 9.63172e-1f, 1.35775e-3f,
            2.31082e-2f, 1.28021e-1f, 9.36245e-1f
        };
        return mul(m, c);
    }

    float3 lms_to_lrgb(float3 c)
    {
        float3x3 m = {
             2.85847e+0f, -1.62879e+0f, -2.48910e-2f,
            -2.10182e-1f,  1.15820e+0f,  3.24281e-4f,
            -4.18120e-2f, -1.18169e-1f,  1.06867e+0f
        };
        return mul(m, c);
    }

    // Color balance function.
    // - The gamma compression/expansion equation used in this function
    //   differs from the standard sRGB-Linear conversion.

    float3 apply_balance(float3 c)
    {
#if !COLORSPACE_LINEAR
        // Do the gamma expansion before applying the color balance.
        c = pow(c, 2.2);
#endif

        // Apply the color balance in the LMS color space.
        c = lms_to_lrgb(lrgb_to_lms(c) * _Balance);

        // It may return a minus value, which should be cropped out.
        c = max(c, 0.0);

#if !COLORSPACE_LINEAR
        // Gamma compression.
        c = pow(c, 1.0 / 2.2);
#endif

        return c;
    }

#endif

#if TONEMAPPING_ON

    // John Hable's filmic tone mapping operator.
    // http://filmicgames.com/archives/6

    float3 hable_op(float3 c)
    {
        float A = 0.15;
        float B = 0.50;
        float C = 0.10;
        float D = 0.20;
        float E = 0.02;
        float F = 0.30;
        return ((c * (c * A + B * C) + D * E) / (c * (c * A + B) + D * F)) - E / F;
    }

    float3 tone_mapping(float3 c)
    {
        c *= _Exposure * 4;
        c = hable_op(c) / hable_op(11.2);
        return pow(c, 1 / 2.2);
    }

#endif

    // Color saturation.

    float luma(float3 c)
    {
        return 0.212 * c.r + 0.701 * c.g + 0.087 * c.b;
    }

    float3 apply_saturation(float3 c)
    {
        return lerp((float3)luma(c), c, _Saturation);
    }

    // RGB curves.

    float3 apply_curves(float3 c)
    {
        float4 r = tex2D(_Curves, float2(c.r, 0));
        float4 g = tex2D(_Curves, float2(c.g, 0));
        float4 b = tex2D(_Curves, float2(c.b, 0));
        return float3(r.r * r.a, g.g * g.a, b.b * b.a);
    }

#if DITHER_ORDERED

    // Interleaved gradient function from CoD AW.
    // http://www.iryoku.com/next-generation-post-processing-in-call-of-duty-advanced-warfare

    float interleaved_gradient(float2 uv)
    {
        float3 magic = float3(0.06711056, 0.00583715, 52.9829189);
        return frac(magic.z * frac(dot(uv, magic.xy)));
    }

    float3 dither(float2 uv)
    {
        return (float3)(interleaved_gradient(uv / _MainTex_TexelSize) / 255);
    }

#endif

#if DITHER_TRIANGULAR

    // Triangular PDF.

    float nrand(float2 uv)
    {
        return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
    }

    float3 dither(float2 uv)
    {
        float r = nrand(uv) + nrand(uv + (float2)1.1) - 0.5;
        return (float3)(r / 255);
    }

#endif

    float4 frag(v2f_img i) : SV_Target 
    {
        float4 source = tex2D(_MainTex, i.uv); 
        float3 rgb = source.rgb;

#if BALANCING_ON
        rgb = apply_balance(rgb);
#endif

#if COLORSPACE_LINEAR
#if TONEMAPPING_ON
        // Apply the tone mapping.
        rgb = tone_mapping(rgb);
#else
        // Convert the color into the sRGB color space.
        rgb = linear_to_srgb(rgb);
#endif
#endif

        // Color saturation.
        rgb = apply_saturation(rgb);

        // RGB curves.
        rgb = apply_curves(rgb);

#if !DITHER_OFF
        rgb += dither(i.uv);
#endif

#if COLORSPACE_LINEAR
        // Take the color back into the linear color space.
        rgb = srgb_to_linear(rgb);
#endif

        return float4(rgb, source.a);
    }

    ENDCG 
    
    Subshader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            Fog { Mode off }      
            CGPROGRAM
            #pragma target 3.0
            #pragma glsl
            #pragma vertex vert_img
            #pragma fragment frag
            ENDCG
        }
    }
}
