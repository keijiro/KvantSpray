//
// KinoBloom - Bloom effect
//
// Copyright (C) 2015 Keijiro Takahashi
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
Shader "Hidden/Kino/Bloom"
{
    Properties
    {
        _MainTex("-", 2D) = "" {}
        _AccTex("-", 2D) = "" {}
        _Blur1Tex("-", 2D) = "" {}
        _Blur2Tex("-", 2D) = "" {}
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float2 _MainTex_TexelSize;

    sampler2D _AccTex;
    sampler2D _Blur1Tex;
    sampler2D _Blur2Tex;

    float _Threshold;
    float _TempFilter;
    float _Intensity1;
    float _Intensity2;

    // Quarter downsampler
    half4 frag_downsample(v2f_img i) : SV_Target
    {
        float4 d = _MainTex_TexelSize.xyxy * float4(1, 1, -1, -1);
        half4 s;
        s  = tex2D(_MainTex, i.uv + d.xy);
        s += tex2D(_MainTex, i.uv + d.xw);
        s += tex2D(_MainTex, i.uv + d.zy);
        s += tex2D(_MainTex, i.uv + d.zw);
        return s * 0.25;
    }

    // Thresholding filter
    half4 frag_threshold(v2f_img i) : SV_Target
    {
        half4 cs = tex2D(_MainTex, i.uv);
        half lm = Luminance(cs.rgb);
        return cs * smoothstep(_Threshold, _Threshold * 1.5, lm);
    }

    // Thresholding filter with temporal filtering
    half4 frag_threshold_temp(v2f_img i) : SV_Target
    {
        half4 co = frag_threshold(i);
        half4 cp = tex2D(_AccTex, i.uv);
        return lerp(co, cp, _TempFilter);
    }

    // 9-tap Gaussian filter with linear sampling
    // http://rastergrid.com/blog/2010/09/efficient-gaussian-blur-with-linear-sampling/
    half4 gaussian_filter(float2 uv, float2 stride)
    {
        half4 s = tex2D(_MainTex, uv) * 0.227027027;

        float2 d1 = stride * 1.3846153846;
        s += tex2D(_MainTex, uv + d1) * 0.3162162162;
        s += tex2D(_MainTex, uv - d1) * 0.3162162162;

        float2 d2 = stride * 3.2307692308;
        s += tex2D(_MainTex, uv + d2) * 0.0702702703;
        s += tex2D(_MainTex, uv - d2) * 0.0702702703;

        return s;
    }

    half4 frag_gaussian_blur_h(v2f_img i) : SV_Target
    {
        return gaussian_filter(i.uv, float2(_MainTex_TexelSize.x, 0));
    }

    half4 frag_gaussian_blur_v(v2f_img i) : SV_Target
    {
        return gaussian_filter(i.uv, float2(0, _MainTex_TexelSize.y));
    }

    // 13-tap box filter with linear sampling
    half4 box_filter(float2 uv, float2 stride)
    {
        half4 s = tex2D(_MainTex, uv) / 2;

        float2 d1 = stride * 1.5;
        s += tex2D(_MainTex, uv + d1);
        s += tex2D(_MainTex, uv - d1);

        float2 d2 = stride * 3.5;
        s += tex2D(_MainTex, uv + d2);
        s += tex2D(_MainTex, uv - d2);

        float2 d3 = stride * 5.5;
        s += tex2D(_MainTex, uv + d3);
        s += tex2D(_MainTex, uv - d3);

        return s * 2 / 13;
    }

    half4 frag_box_blur_h(v2f_img i) : SV_Target
    {
        return box_filter(i.uv, float2(_MainTex_TexelSize.x, 0));
    }

    half4 frag_box_blur_v(v2f_img i) : SV_Target
    {
        return box_filter(i.uv, float2(0, _MainTex_TexelSize.y));
    }

    // Composite function
    half4 frag_composite(v2f_img i) : SV_Target
    {
        half4 cs = tex2D(_MainTex, i.uv);
        half3 c1 = LinearToGammaSpace(cs.rgb);
        half3 c2 = LinearToGammaSpace(tex2D(_Blur1Tex, i.uv).rgb);
        half3 c3 = LinearToGammaSpace(tex2D(_Blur2Tex, i.uv).rgb);
        half3 co = c1 + c2 * _Intensity1 + c3 * _Intensity2;
        return half4(GammaToLinearSpace(co), cs.a);
    }

    ENDCG
    SubShader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_downsample
            #pragma target 3.0
            ENDCG
        }
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_threshold
            #pragma target 3.0
            ENDCG
        }
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_threshold_temp
            #pragma target 3.0
            ENDCG
        }
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_gaussian_blur_h
            #pragma target 3.0
            ENDCG
        }
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_gaussian_blur_v
            #pragma target 3.0
            ENDCG
        }
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_box_blur_h
            #pragma target 3.0
            ENDCG
        }
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_box_blur_v
            #pragma target 3.0
            ENDCG
        }
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_composite
            #pragma target 3.0
            ENDCG
        }
    }
}
