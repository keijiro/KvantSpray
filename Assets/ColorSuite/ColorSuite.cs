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
using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
[ImageEffectTransformsToLDR]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Image Effects/Color Adjustments/Color Suite")]
public class ColorSuite : MonoBehaviour
{
    #region Public Properties

    // White balance.
    [SerializeField] float _colorTemp = 0.0f;
    [SerializeField] float _colorTint = 0.0f;

    public float colorTemp {
        get { return _colorTemp; }
        set { _colorTemp = value; }
    }
    public float colorTint {
        get { return _colorTint; }
        set { _colorTint = value; }
    }

    // Tone mapping.
    [SerializeField] bool _toneMapping = false;
    [SerializeField] float _exposure   = 1.0f;

    public bool toneMapping {
        get { return _toneMapping; }
        set { _toneMapping = value; }
    }
    public float exposure {
        get { return _exposure; }
        set { _exposure = value; }
    }

    // Color saturation.
    [SerializeField] float _saturation = 1.0f;

    public float saturation {
        get { return _saturation; }
        set { _saturation = value; }
    }

    // Curves.
    [SerializeField] AnimationCurve _rCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [SerializeField] AnimationCurve _gCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [SerializeField] AnimationCurve _bCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [SerializeField] AnimationCurve _cCurve = AnimationCurve.Linear(0, 0, 1, 1);

    public AnimationCurve redCurve {
        get { return _rCurve; }
        set { _rCurve = value; UpdateLUT(); }
    }
    public AnimationCurve greenCurve {
        get { return _gCurve; }
        set { _gCurve = value; UpdateLUT(); }
    }
    public AnimationCurve blueCurve {
        get { return _bCurve; }
        set { _bCurve = value; UpdateLUT(); }
    }
    public AnimationCurve rgbCurve {
        get { return _cCurve; }
        set { _cCurve = value; UpdateLUT(); }
    }

    // Dithering.
    public enum DitherMode { Off, Ordered, Triangular  }
    [SerializeField] DitherMode _ditherMode = DitherMode.Off;

    public DitherMode ditherMode {
        get { return _ditherMode; }
        set { _ditherMode = value; }
    }

    #endregion

    #region Internal Properties

    // Reference to the shader.
    [SerializeField] Shader shader;

    // Temporary objects.
    Material _material;
    Texture2D _lutTexture;

    #endregion

    #region Local Functions

    // RGBM encoding.
    static Color EncodeRGBM(float r, float g, float b)
    {
        var a = Mathf.Max(Mathf.Max(r, g), Mathf.Max(b, 1e-6f));
        a = Mathf.Ceil(a * 255) / 255;
        return new Color(r / a, g / a, b / a, a);
    }

    // An analytical model of chromaticity of the standard illuminant, by Judd et al.
    // http://en.wikipedia.org/wiki/Standard_illuminant#Illuminant_series_D
    // Slightly modifed to adjust it with the D65 white point (x=0.31271, y=0.32902).
    static float StandardIlluminantY(float x)
    {
        return 2.87f * x - 3.0f * x * x - 0.27509507f;
    }

    // CIE xy chromaticity to CAT02 LMS.
    // http://en.wikipedia.org/wiki/LMS_color_space#CAT02
    static Vector3 CIExyToLMS(float x, float y)
    {
        var Y = 1.0f;
        var X = Y * x / y;
        var Z = Y * (1.0f - x - y) / y;

        var L =  0.7328f * X + 0.4296f * Y - 0.1624f * Z;
        var M = -0.7036f * X + 1.6975f * Y + 0.0061f * Z;
        var S =  0.0030f * X + 0.0136f * Y + 0.9834f * Z;

        return new Vector3(L, M, S);
    }

    #endregion

    #region Private Methods

    // Set up the temporary assets.
    void Setup()
    {
        if (_material == null)
        {
            _material = new Material(shader);
            _material.hideFlags = HideFlags.DontSave;
        }

        if (_lutTexture == null)
        {
            _lutTexture = new Texture2D(512, 1, TextureFormat.ARGB32, false, true);
            _lutTexture.hideFlags = HideFlags.DontSave;
            _lutTexture.wrapMode = TextureWrapMode.Clamp;
            UpdateLUT();
        }
    }

    // Update the LUT texture.
    void UpdateLUT()
    {
        for (var x = 0; x < _lutTexture.width; x++)
        {
            var u = 1.0f / (_lutTexture.width - 1) * x;
            var r = _cCurve.Evaluate(_rCurve.Evaluate(u));
            var g = _cCurve.Evaluate(_gCurve.Evaluate(u));
            var b = _cCurve.Evaluate(_bCurve.Evaluate(u));
            _lutTexture.SetPixel(x, 0, EncodeRGBM(r, g, b));
        }
        _lutTexture.Apply();
    }

    // Calculate the color balance coefficients.
    Vector3 CalculateColorBalance()
    {
        // Get the CIE xy chromaticity of the reference white point.
        // Note: 0.31271 = x value on the D65 white point
        var x = 0.31271f - _colorTemp * (_colorTemp < 0.0f ? 0.1f : 0.05f);
        var y = StandardIlluminantY(x) + _colorTint * 0.05f;

        // Calculate the coefficients in the LMS space.
        var w1 = new Vector3(0.949237f, 1.03542f, 1.08728f); // D65 white point
        var w2 = CIExyToLMS(x, y);
        return new Vector3(w1.x / w2.x, w1.y / w2.y, w1.z / w2.z);
    }

    #endregion

    #region Monobehaviour Functions

    void Start()
    {
        Setup();
    }

    void OnValidate()
    {
        Setup();
        UpdateLUT();
    }

    void Reset()
    {
        Setup();
        UpdateLUT();
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        var linear = QualitySettings.activeColorSpace == ColorSpace.Linear;

        Setup();

        if (linear)
            _material.EnableKeyword("COLORSPACE_LINEAR");
        else
            _material.DisableKeyword("COLORSPACE_LINEAR");

        if (_colorTemp != 0.0f || _colorTint != 0.0f)
        {
            _material.EnableKeyword("BALANCING_ON");
            _material.SetVector("_Balance", CalculateColorBalance());
        }
        else
            _material.DisableKeyword("BALANCING_ON");

        if (_toneMapping && linear)
        {
            _material.EnableKeyword("TONEMAPPING_ON");
            _material.SetFloat("_Exposure", _exposure);
        }
        else
            _material.DisableKeyword("TONEMAPPING_ON");

        _material.SetTexture("_Curves", _lutTexture);
        _material.SetFloat("_Saturation", _saturation);

        if (_ditherMode == DitherMode.Ordered)
        {
            _material.EnableKeyword("DITHER_ORDERED");
            _material.DisableKeyword("DITHER_TRIANGULAR");
        }
        else if (_ditherMode == DitherMode.Triangular)
        {
            _material.DisableKeyword("DITHER_ORDERED");
            _material.EnableKeyword("DITHER_TRIANGULAR");
        }
        else
        {
            _material.DisableKeyword("DITHER_ORDERED");
            _material.DisableKeyword("DITHER_TRIANGULAR");
        }

        Graphics.Blit(source, destination, _material);
    }

    #endregion
}
