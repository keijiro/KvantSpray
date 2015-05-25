//
// Spray - mesh particle system
//

using UnityEngine;
using UnityEngine.Rendering;

namespace Kvant
{
    [ExecuteInEditMode, AddComponentMenu("Kvant/Spray")]
    public partial class Spray : MonoBehaviour
    {
        #region Parameters Exposed To Editor

        [SerializeField] int _maxParticles = 1000;

        [SerializeField] Vector3 _emitterCenter = Vector3.zero;
        [SerializeField] Vector3 _emitterSize = Vector3.one;
        [SerializeField] float _throttle = 1.0f;

        [SerializeField] float _minLife = 1.0f;
        [SerializeField] float _maxLife = 4.0f;

        [SerializeField] Vector3 _direction = Vector3.forward;
        [SerializeField] float _spread = 0.2f;

        [SerializeField] float _minSpeed = 2.0f;
        [SerializeField] float _maxSpeed = 10.0f;

        [SerializeField] float _minSpin = 30.0f;
        [SerializeField] float _maxSpin = 200.0f;

        [SerializeField] float _noiseFrequency = 0.2f;
        [SerializeField] float _noiseAmplitude = 5.0f;
        [SerializeField] float _noiseAnimation = 1.0f;

        [SerializeField] Mesh[] _shapes = new Mesh[1];

        [SerializeField] float _minScale = 0.1f;
        [SerializeField] float _maxScale = 1.2f;

        public enum ShadingMode {
            OpaquePBR, TransparentPBR, TransparentUnlit, AdditiveUnlit
        }
        [SerializeField] ShadingMode _shadingMode;

        [SerializeField] float _metallic = 0.5f;
        [SerializeField] float _smoothness = 0.5f;
        [SerializeField] ShadowCastingMode _castShadows;
        [SerializeField] bool _receiveShadows = false;

        public enum ColorMode {
            Single, Random, LinearAnimation
        }
        [SerializeField] ColorMode _colorMode;

        [ColorUsage(true, true, 0, 8, 0.125f, 3)]
        [SerializeField] Color _color = Color.white;
        [ColorUsage(true, true, 0, 8, 0.125f, 3)]
        [SerializeField] Color _color2 = Color.gray;

        [SerializeField] int _randomSeed = 0;
        [SerializeField] bool _debug;

        #endregion

        #region Public Properties

        public int maxParticles {
            get {
                // Returns the actual number of particles.
                if (_bulkMesh == null) return 0;
                return (_maxParticles / _bulkMesh.copyCount + 1) * _bulkMesh.copyCount;
            }
        }

        public Vector3 emitterCenter {
            get { return _emitterCenter; }
            set { _emitterCenter = value; }
        }

        public Vector3 emitterSize {
            get { return _emitterSize; }
            set { _emitterSize = value; }
        }

        public float throttle {
            get { return _throttle; }
            set { _throttle = value; }
        }

        public float minLife {
            get { return _minLife; }
            set { _minLife = value; }
        }

        public float maxLife {
            get { return _maxLife; }
            set { _maxLife = value; }
        }

        public Vector3 direction {
            get { return _direction; }
            set { _direction = value; }
        }

        public float spread {
            get { return _spread; }
            set { _spread = value; }
        }

        public float minSpeed {
            get { return _minSpeed; }
            set { _minSpeed = value; }
        }

        public float maxSpeed {
            get { return _maxSpeed; }
            set { _maxSpeed = value; }
        }

        public float minSpin {
            get { return _minSpin; }
            set { _minSpin = value; }
        }

        public float maxSpin {
            get { return _maxSpin; }
            set { _maxSpin = value; }
        }

        public float noiseFrequency {
            get { return _noiseFrequency; }
            set { _noiseFrequency = value; }
        }

        public float noiseAmplitude {
            get { return _noiseAmplitude; }
            set { _noiseAmplitude = value; }
        }

        public float noiseAnimation {
            get { return _noiseAnimation; }
            set { _noiseAnimation = value; }
        }

        public float minScale {
            get { return _minScale; }
            set { _minScale = value; }
        }

        public float maxScale {
            get { return _maxScale; }
            set { _maxScale = value; }
        }

        public ShadingMode shadingMode {
            get { return _shadingMode; }
            set { _shadingMode = value; }
        }

        public float metallic {
            get { return _metallic; }
            set { _metallic = value; }
        }

        public float smoothness {
            get { return _smoothness; }
            set { _smoothness = value; }
        }

        public ShadowCastingMode shadowCastingMode {
            get { return _castShadows; }
            set { _castShadows = value; }
        }

        public bool receiveShadows {
            get { return _receiveShadows; }
            set { _receiveShadows = value; }
        }

        public ColorMode colorMode {
            get { return _colorMode; }
            set { _colorMode = value; }
        }

        public Color color {
            get { return _color; }
            set { _color = value; }
        }

        public Color color2 {
            get { return _color2; }
            set { _color2 = value; }
        }

        public int randomSeed {
            get { return _randomSeed; }
            set { _randomSeed = value; }
        }

        #endregion

        #region Shader And Materials

        [SerializeField] Shader _kernelShader;
        [SerializeField] Shader _opaqueShader;
        [SerializeField] Shader _transparentShader;
        [SerializeField] Shader _unlitShader;
        [SerializeField] Shader _debugShader;

        Material _kernelMaterial;
        Material _displayMaterial;
        Material _debugMaterial;

        #endregion

        #region Private Variables And Objects

        RenderTexture _positionBuffer1;
        RenderTexture _positionBuffer2;
        RenderTexture _rotationBuffer1;
        RenderTexture _rotationBuffer2;
        BulkMesh _bulkMesh;
        bool _needsReset = true;

        #endregion

        #region Private Properties

        static float deltaTime {
            get {
                return Application.isPlaying && Time.frameCount > 1 ? Time.deltaTime : 1.0f / 10;
            }
        }

        Shader CurrentDisplayShader {
            get {
                if (_shadingMode == ShadingMode.OpaquePBR) return _opaqueShader;
                if (_shadingMode == ShadingMode.TransparentPBR) return _transparentShader;
                return _unlitShader;
            }
        }

        #endregion

        #region Resource Management

        public void NotifyConfigChange()
        {
            _needsReset = true;
        }

        Material CreateMaterial(Shader shader)
        {
            var material = new Material(shader);
            material.hideFlags = HideFlags.DontSave;
            return material;
        }

        RenderTexture CreateBuffer()
        {
            var width = _bulkMesh.copyCount;
            var height = _maxParticles / width + 1;
            var buffer = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
            buffer.hideFlags = HideFlags.DontSave;
            buffer.filterMode = FilterMode.Point;
            buffer.wrapMode = TextureWrapMode.Repeat;
            return buffer;
        }

        void UpdateKernelShader()
        {
            var m = _kernelMaterial;

            m.SetVector("_EmitterPos", _emitterCenter);
            m.SetVector("_EmitterSize", _emitterSize);

            m.SetVector("_LifeParams", new Vector2(1.0f / _minLife, 1.0f / _maxLife));

            var dir = new Vector4(_direction.x, _direction.y, _direction.z, _spread);
            m.SetVector("_Direction", dir);

            var pi360 = Mathf.PI / 360;
            var sparams = new Vector4(_minSpeed, _maxSpeed, _minSpin * pi360, _maxSpin * pi360);
            m.SetVector("_SpeedParams", sparams);

            var np = new Vector3(_noiseFrequency, _noiseAmplitude, _noiseAnimation);
            m.SetVector("_NoiseParams", np);

            m.SetVector("_Config", new Vector4(_throttle, _randomSeed, deltaTime, Time.time));
        }

        void UpdateDisplayShader()
        {
            var m = _displayMaterial;

            m.shader = CurrentDisplayShader;

            m.SetTexture("_PositionTex", _positionBuffer2);
            m.SetTexture("_RotationTex", _rotationBuffer2);
            m.SetVector("_PbrParams", new Vector2(_metallic, _smoothness));
            m.SetColor("_Color", _color);
            m.SetColor("_Color2", _color2);
            m.SetVector("_ScaleParams", new Vector2(_minScale, _maxScale));

            if (_shadingMode == ShadingMode.AdditiveUnlit)
                m.EnableKeyword("BLEND_ADD");
            else
                m.DisableKeyword("BLEND_ADD");

            if (_colorMode == ColorMode.Random)
            {
                m.EnableKeyword("COLOR_RANDOM");
                m.DisableKeyword("COLOR_ANIMATE");
            }
            else if (_colorMode == ColorMode.LinearAnimation)
            {
                m.DisableKeyword("COLOR_RANDOM");
                m.EnableKeyword("COLOR_ANIMATE");
            }
            else
            {
                m.DisableKeyword("COLOR_RANDOM");
                m.DisableKeyword("COLOR_ANIMATE");
            }
        }

        void ResetResources()
        {
            // Mesh object.
            if (_bulkMesh == null)
                _bulkMesh = new BulkMesh(_shapes);
            else
                _bulkMesh.Rebuild(_shapes);

            // Particle buffers.
            if (_positionBuffer1) DestroyImmediate(_positionBuffer1);
            if (_positionBuffer2) DestroyImmediate(_positionBuffer2);
            if (_rotationBuffer1) DestroyImmediate(_rotationBuffer1);
            if (_rotationBuffer2) DestroyImmediate(_rotationBuffer2);

            _positionBuffer1 = CreateBuffer();
            _positionBuffer2 = CreateBuffer();
            _rotationBuffer1 = CreateBuffer();
            _rotationBuffer2 = CreateBuffer();

            // Shader materials.
            if (!_kernelMaterial)  _kernelMaterial  = CreateMaterial(_kernelShader);
            if (!_displayMaterial) _displayMaterial = CreateMaterial(_opaqueShader);
            if (!_debugMaterial)   _debugMaterial   = CreateMaterial(_debugShader);

            // Warming up.
            UpdateKernelShader();
            InitializeAndPrewarmBuffers();

            _needsReset = false;
        }

        void InitializeAndPrewarmBuffers()
        {
            // Initialization.
            Graphics.Blit(null, _positionBuffer2, _kernelMaterial, 0);
            Graphics.Blit(null, _rotationBuffer2, _kernelMaterial, 1);

            // Execute the kernel shader repeatedly.
            for (var i = 0; i < 8; i++) {
                Graphics.Blit(_positionBuffer2, _positionBuffer1, _kernelMaterial, 2);
                Graphics.Blit(_rotationBuffer2, _rotationBuffer1, _kernelMaterial, 3);
                Graphics.Blit(_positionBuffer1, _positionBuffer2, _kernelMaterial, 2);
                Graphics.Blit(_rotationBuffer1, _rotationBuffer2, _kernelMaterial, 3);
            }
        }

        #endregion

        #region MonoBehaviour Functions

        void Reset()
        {
            _needsReset = true;
        }

        void OnDestroy()
        {
            if (_bulkMesh != null) _bulkMesh.Release();
            if (_positionBuffer1) DestroyImmediate(_positionBuffer1);
            if (_positionBuffer2) DestroyImmediate(_positionBuffer2);
            if (_rotationBuffer1) DestroyImmediate(_rotationBuffer1);
            if (_rotationBuffer2) DestroyImmediate(_rotationBuffer2);
            if (_kernelMaterial)  DestroyImmediate(_kernelMaterial);
            if (_displayMaterial) DestroyImmediate(_displayMaterial);
            if (_debugMaterial)   DestroyImmediate(_debugMaterial);
        }

        void Update()
        {
            if (_needsReset) ResetResources();

            UpdateKernelShader();

            if (Application.isPlaying)
            {
                // Swap the particle buffers.
                var temp = _positionBuffer1;
                _positionBuffer1 = _positionBuffer2;
                _positionBuffer2 = temp;

                temp = _rotationBuffer1;
                _rotationBuffer1 = _rotationBuffer2;
                _rotationBuffer2 = temp;

                // Execute the kernel shader.
                Graphics.Blit(_positionBuffer1, _positionBuffer2, _kernelMaterial, 2);
                Graphics.Blit(_rotationBuffer1, _rotationBuffer2, _kernelMaterial, 3);
            }
            else
            {
                InitializeAndPrewarmBuffers();
            }

            // Draw the bulk mesh.
            UpdateDisplayShader();

            var p = transform.position;
            var r = transform.rotation;
            var uv = new Vector2(0.5f / _positionBuffer2.width, 0);
            var offs = new MaterialPropertyBlock();

            for (var i = 0; i < _positionBuffer2.height; i++)
            {
                uv.y = (0.5f + i) / _positionBuffer2.height;
                offs.AddVector("_BufferOffset", uv);
                if (_shadingMode == ShadingMode.OpaquePBR)
                    Graphics.DrawMesh(_bulkMesh.mesh, p, r, _displayMaterial, 0, null, 0, offs, _castShadows, _receiveShadows);
                else
                    Graphics.DrawMesh(_bulkMesh.mesh, p, r, _displayMaterial, 0, null, 0, offs, false, false);
            }
        }

        void OnGUI()
        {
            if (_debug && Event.current.type.Equals(EventType.Repaint) && _debugMaterial)
            {
                var r1 = new Rect(0, 0, 256, 64);
                var r2 = new Rect(0, 64, 256, 64);
                if (_positionBuffer1) Graphics.DrawTexture(r1, _positionBuffer2, _debugMaterial);
                if (_rotationBuffer1) Graphics.DrawTexture(r2, _rotationBuffer2, _debugMaterial);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(_emitterCenter, _emitterSize);
        }

        #endregion
    }
}
