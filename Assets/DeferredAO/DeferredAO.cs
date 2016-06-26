//
// Deferred AO - SSAO image effect for deferred shading
//
// Copyright (C) 2015 Keijiro Takahashi
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
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Image Effects/Rendering/Deferred AO")]
public class DeferredAO : MonoBehaviour
{
    #region Public Properties

    // Effect intensity

    [SerializeField]
    float _intensity = 1;

    public float intensity {
        get { return _intensity; }
        set { _intensity = value; }
    }

    // Sample radius

    [SerializeField]
    float _sampleRadius = 1;

    public float sampleRadius {
        get { return _sampleRadius; }
        set { _sampleRadius = value; }
    }

    // Range check (rejects distant samples)

    [SerializeField]
    bool _rangeCheck = true;

    public bool rangeCheck {
        get { return _rangeCheck; }
        set { _rangeCheck = value; }
    }

    // Fall-off distance

    [SerializeField]
    float _fallOffDistance = 100;

    public float fallOffDistance {
        get { return _fallOffDistance; }
        set { _fallOffDistance = value; }
    }

    // Sample count

    public enum SampleCount { Low, Medium, High, Overkill }

    [SerializeField]
    SampleCount _sampleCount = SampleCount.Medium;

    public SampleCount sampleCount {
        get { return _sampleCount; }
        set { _sampleCount = value; }
    }

    #endregion

    #region Private Resources

    [SerializeField]
    Shader _shader;

    Material _material;

    bool CheckDeferredShading()
    {
        var path = GetComponent<Camera>().actualRenderingPath;
        return path == RenderingPath.DeferredShading;
    }

    #endregion

    #region MonoBehaviour Functions

    [ImageEffectOpaque]
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!CheckDeferredShading()) {
            Graphics.Blit(source, destination);
            return;
        }

        if (_material == null) {
            _material = new Material(_shader);
            _material.hideFlags = HideFlags.DontSave;
        }

        _material.SetFloat("_Radius", _sampleRadius);
        _material.SetFloat("_Intensity", _intensity);
        _material.SetFloat("_FallOff", _fallOffDistance);

        _material.shaderKeywords = null;

        if (_rangeCheck)
            _material.EnableKeyword("_RANGE_CHECK");

        if (_sampleCount == SampleCount.Medium)
            _material.EnableKeyword("_SAMPLE_MEDIUM");
        else if (_sampleCount == SampleCount.High)
            _material.EnableKeyword("_SAMPLE_HIGH");
        else if (_sampleCount == SampleCount.Overkill)
            _material.EnableKeyword("_SAMPLE_OVERKILL");

        Graphics.Blit(source, destination, _material, 0);
    }

    #endregion
}
