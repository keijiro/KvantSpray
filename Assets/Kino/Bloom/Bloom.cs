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
using UnityEngine;

namespace Kino
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Kino Image Effects/Bloom")]
    public class Bloom : MonoBehaviour
    {
        #region Public Properties

        // Bloom radius
        [SerializeField, Range(1, 10)]
        float _radius1 = 1.0f;

        public float radius1 {
            get { return _radius1; }
            set { _radius1 = value; }
        }

        [SerializeField, Range(1, 10)]
        float _radius2 = 4.0f;

        public float radius2 {
            get { return _radius2; }
            set { _radius2 = value; }
        }

        // Bloom intensity
        [SerializeField, Range(0, 1.0f)]
        float _intensity1 = 0.2f;

        public float intensity1 {
            get { return _intensity1; }
            set { _intensity1 = value; }
        }

        [SerializeField, Range(0, 1.0f)]
        float _intensity2 = 0.2f;

        public float intensity2 {
            get { return _intensity2; }
            set { _intensity2 = value; }
        }

        // Threshold
        [SerializeField, Range(0, 2)]
        float _threshold = 0.5f;

        public float threshold {
            get { return _threshold; }
            set { _threshold = value; }
        }

        // Temporal filtering
        [SerializeField, Range(0, 1.0f)]
        float _temporalFiltering = 0.0f;

        public float temporalFiltering {
            get { return _temporalFiltering; }
            set { _temporalFiltering = value; }
        }

        #endregion

        #region Private Properties

        [SerializeField] Shader _shader;
        Material _material;
        RenderTexture _accBuffer;

        #endregion

        #region MonoBehaviour Functions

        RenderTexture GetTempBuffer(int width, int height)
        {
            return RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.DefaultHDR);
        }

        void ReleaseTempBuffer(RenderTexture rt)
        {
            RenderTexture.ReleaseTemporary(rt);
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            // Set up the material object.
            if (_material == null)
            {
                _material = new Material(_shader);
                _material.hideFlags = HideFlags.DontSave;
            }

            // Common options
            _material.SetFloat("_Intensity1", _intensity1);
            _material.SetFloat("_Intensity2", _intensity2);
            _material.SetFloat("_Threshold", _threshold);
            _material.SetFloat("_TempFilter", 1.0f - Mathf.Exp(_temporalFiltering * -4));

            // Calculate the size of the blur buffers.
            var blur1Height = (int)(720 / _radius1);
            var blur2Height = (int)(256 / _radius2);
            var blur1Width = blur1Height * source.width / source.height;
            var blur2Width = blur2Height * source.width / source.height;

            // Blur image buffers
            RenderTexture rt1 = null, rt2 = null;

            // Small bloom
            if (_intensity1 > 0.0f)
            {
                var rt = source;

                // Shrink the source image with the quater downsampler
                while (rt.height > blur1Height * 4)
                {
                    var rt_next = GetTempBuffer(rt.width / 4, rt.height / 4);
                    Graphics.Blit(rt, rt_next, _material, 0);
                    if (rt != source) ReleaseTempBuffer(rt);
                    rt = rt_next;
                }

                // Allocate the blur buffer.
                rt1 = GetTempBuffer(blur1Width, blur1Height);

                // Shrink the source image and apply the threshold.
                Graphics.Blit(rt, rt1, _material, 1);
                if (rt != source) ReleaseTempBuffer(rt);

                // Apply the separable Gaussian filter.
                var rtb = GetTempBuffer(blur1Width, blur1Height);
                for (var i = 0; i < 2; i++)
                {
                    Graphics.Blit(rt1, rtb, _material, 3);
                    Graphics.Blit(rtb, rt1, _material, 4);
                }
                ReleaseTempBuffer(rtb);
            }

            // Large bloom
            if (_intensity2 > 0.0f)
            {
                var rt = source;

                // Shrink the source image with the quater downsampler
                while (rt.height > blur2Height * 4)
                {
                    var rt_next = GetTempBuffer(rt.width / 4, rt.height / 4);
                    Graphics.Blit(rt, rt_next, _material, 0);
                    if (rt != source) ReleaseTempBuffer(rt);
                    rt = rt_next;
                }

                // Allocate the blur buffer.
                rt2 = GetTempBuffer(blur2Width, blur2Height);

                // Shrink + thresholding + temporal filtering
                if (_accBuffer && _temporalFiltering > 0.0f)
                {
                    _material.SetTexture("_AccTex", _accBuffer);
                    Graphics.Blit(rt, rt2, _material, 2);
                }
                else
                {
                    Graphics.Blit(rt, rt2, _material, 1);
                }
                if (rt != source) ReleaseTempBuffer(rt);

                // Update the accmulation buffer.
                if (_accBuffer)
                {
                    ReleaseTempBuffer(_accBuffer);
                    _accBuffer = null;
                }
                if (_temporalFiltering > 0.0f)
                {
                    _accBuffer = GetTempBuffer(blur2Width, blur2Height);
                    Graphics.Blit(rt2, _accBuffer);
                }

                // Apply the separable box filter repeatedly.
                var rtb = GetTempBuffer(blur2Width, blur2Height);
                for (var i = 0; i < 4; i++)
                {
                    Graphics.Blit(rt2, rtb, _material, 5);
                    Graphics.Blit(rtb, rt2, _material, 6);
                }
                ReleaseTempBuffer(rtb);
            }

            // Compositing
            _material.SetTexture("_Blur1Tex", rt1 ? rt1 : source);
            _material.SetTexture("_Blur2Tex", rt2 ? rt2 : source);
            Graphics.Blit(source, destination, _material, 7);

            // Release the blur buffers.
            ReleaseTempBuffer(rt1);
            ReleaseTempBuffer(rt2);
        }

        #endregion
    }
}
