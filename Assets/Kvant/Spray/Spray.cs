//
// Spray - particle system
//
using UnityEngine;
using UnityEngine.Rendering;

namespace Kvant
{
    [ExecuteInEditMode]
    [AddComponentMenu("Kvant/Spray")]
    public partial class Spray : MonoBehaviour
    {
        #region Basic Properties

        [SerializeField]
        int _maxParticles = 1000;

        public int maxParticles {
            get {
                // Returns actual number of particles.
                if (_bulkMesh == null || _bulkMesh.copyCount < 1) return 0;
                return (_maxParticles / _bulkMesh.copyCount + 1) * _bulkMesh.copyCount;
            }
        }

        #endregion

        #region Emitter Parameters

        [SerializeField]
        Vector3 _emitterCenter = Vector3.zero;

        public Vector3 emitterCenter {
            get { return _emitterCenter; }
            set { _emitterCenter = value; }
        }

        [SerializeField]
        Vector3 _emitterSize = Vector3.one;

        public Vector3 emitterSize {
            get { return _emitterSize; }
            set { _emitterSize = value; }
        }

        [SerializeField, Range(0, 1)]
        float _throttle = 1.0f;

        public float throttle {
            get { return _throttle; }
            set { _throttle = value; }
        }

        #endregion

        #region Particle Life Parameters

        [SerializeField]
        float _life = 4.0f;

        public float life {
            get { return _life; }
            set { _life = value; }
        }

        [SerializeField, Range(0, 1)]
        float _lifeRandomness = 0.6f;

        public float lifeRandomness {
            get { return _lifeRandomness; }
            set { _lifeRandomness = value; }
        }

        #endregion

        #region Velocity Parameters

        [SerializeField]
        Vector3 _initialVelocity = Vector3.forward * 4.0f;

        public Vector3 initialVelocity {
            get { return _initialVelocity; }
            set { _initialVelocity = value; }
        }

        [SerializeField, Range(0, 1)]
        float _directionSpread = 0.2f;

        public float directionSpread {
            get { return _directionSpread; }
            set { _directionSpread = value; }
        }

        [SerializeField, Range(0, 1)]
        float _speedRandomness = 0.5f;

        public float speedRandomness {
            get { return _speedRandomness; }
            set { _speedRandomness = value; }
        }

        #endregion

        #region Acceleration Parameters

        [SerializeField]
        Vector3 _acceleration = Vector3.zero;

        public Vector3 acceleration {
            get { return _acceleration; }
            set { _acceleration = value; }
        }

        [SerializeField, Range(0, 4)]
        float _drag = 0.1f;

        public float drag {
            get { return _drag; }
            set { _drag = value; }
        }

        #endregion

        #region Rotation Parameters

        [SerializeField]
        float _spin = 20.0f;

        public float spin {
            get { return _spin; }
            set { _spin = value; }
        }

        [SerializeField]
        float _speedToSpin = 60.0f;

        public float speedToSpin {
            get { return _speedToSpin; }
            set { _speedToSpin = value; }
        }

        [SerializeField, Range(0, 1)]
        float _spinRandomness = 0.3f;

        public float spinRandomness {
            get { return _spinRandomness; }
            set { _spinRandomness = value; }
        }

        #endregion

        #region Turbulent Noise Parameters

        [SerializeField]
        float _noiseAmplitude = 1.0f;

        public float noiseAmplitude {
            get { return _noiseAmplitude; }
            set { _noiseAmplitude = value; }
        }

        [SerializeField]
        float _noiseFrequency = 0.2f;

        public float noiseFrequency {
            get { return _noiseFrequency; }
            set { _noiseFrequency = value; }
        }

        [SerializeField]
        float _noiseMotion = 1.0f;

        public float noiseMotion {
            get { return _noiseMotion; }
            set { _noiseMotion = value; }
        }

        #endregion

        #region Render Settings

        [SerializeField]
        Mesh[] _shapes = new Mesh[1];

        [SerializeField]
        float _scale = 1.0f;

        public float scale {
            get { return _scale; }
            set { _scale = value; }
        }

        [SerializeField, Range(0, 1)]
        float _scaleRandomness = 0.5f;

        public float scaleRandomness {
            get { return _scaleRandomness; }
            set { _scaleRandomness = value; }
        }

        [SerializeField]
        Material _material;
        bool _owningMaterial; // whether owning the material

        public Material sharedMaterial {
            get { return _material; }
            set { _material = value; }
        }

        public Material material {
            get {
                if (!_owningMaterial) {
                    _material = Instantiate<Material>(_material);
                    _owningMaterial = true;
                }
                return _material;
            }
            set {
                if (_owningMaterial) Destroy(_material, 0.1f);
                _material = value;
                _owningMaterial = false;
            }
        }

        [SerializeField]
        ShadowCastingMode _castShadows;

        public ShadowCastingMode shadowCastingMode {
            get { return _castShadows; }
            set { _castShadows = value; }
        }

        [SerializeField]
        bool _receiveShadows = false;

        public bool receiveShadows {
            get { return _receiveShadows; }
            set { _receiveShadows = value; }
        }

        #endregion

        #region Misc Settings

        [SerializeField]
        int _randomSeed = 0;

        public int randomSeed {
            get { return _randomSeed; }
            set { _randomSeed = value; }
        }

        [SerializeField]
        bool _debug;

        #endregion

        #region Built-in Resources

        [SerializeField] Material _defaultMaterial;
        [SerializeField] Shader _kernelShader;
        [SerializeField] Shader _debugShader;

        #endregion

        #region Private Variables And Properties

        Vector3 _noiseOffset;
        RenderTexture _positionBuffer1;
        RenderTexture _positionBuffer2;
        RenderTexture _velocityBuffer1;
        RenderTexture _velocityBuffer2;
        RenderTexture _rotationBuffer1;
        RenderTexture _rotationBuffer2;
        BulkMesh _bulkMesh;
        Material _kernelMaterial;
        Material _debugMaterial;
        bool _needsReset = true;

        static float deltaTime {
            get {
                var isEditor = !Application.isPlaying || Time.frameCount < 2;
                return isEditor ? 1.0f / 10 : Time.deltaTime;
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

            var invLifeMax = 1.0f / Mathf.Max(_life, 0.01f);
            var invLifeMin = invLifeMax / Mathf.Max(1 - _lifeRandomness, 0.01f);
            m.SetVector("_LifeParams", new Vector2(invLifeMin, invLifeMax));

            if (_initialVelocity == Vector3.zero)
            {
                m.SetVector("_Direction", new Vector4(0, 0, 1, 0));
                m.SetVector("_SpeedParams", Vector4.zero);
            }
            else
            {
                var speed = _initialVelocity.magnitude;
                var dir = _initialVelocity / speed;
                m.SetVector("_Direction", new Vector4(dir.x, dir.y, dir.z, _directionSpread));
                m.SetVector("_SpeedParams", new Vector2(speed, _speedRandomness));
            }

            var drag = Mathf.Exp(-_drag * deltaTime);
            var aparams = new Vector4(_acceleration.x, _acceleration.y, _acceleration.z, drag);
            m.SetVector("_Acceleration", aparams);

            var pi360 = Mathf.PI / 360;
            var sparams = new Vector3(_spin * pi360, _speedToSpin * pi360, _spinRandomness);
            m.SetVector("_SpinParams", sparams);

            m.SetVector("_NoiseParams", new Vector2(_noiseFrequency, _noiseAmplitude));

            // Move the noise field backward in the direction of the
            // acceleration vector, or simply pull up when no acceleration.
            if (_acceleration == Vector3.zero)
                _noiseOffset += Vector3.up * _noiseMotion * deltaTime;
            else
                _noiseOffset += _acceleration.normalized * _noiseMotion * deltaTime;

            m.SetVector("_NoiseOffset", _noiseOffset);

            m.SetVector("_Config", new Vector4(_throttle, _randomSeed, deltaTime, Time.time));
        }

        void ResetResources()
        {
            if (_bulkMesh == null)
                _bulkMesh = new BulkMesh(_shapes);
            else
                _bulkMesh.Rebuild(_shapes);

            if (_positionBuffer1) DestroyImmediate(_positionBuffer1);
            if (_positionBuffer2) DestroyImmediate(_positionBuffer2);
            if (_velocityBuffer1) DestroyImmediate(_velocityBuffer1);
            if (_velocityBuffer2) DestroyImmediate(_velocityBuffer2);
            if (_rotationBuffer1) DestroyImmediate(_rotationBuffer1);
            if (_rotationBuffer2) DestroyImmediate(_rotationBuffer2);

            _positionBuffer1 = CreateBuffer();
            _positionBuffer2 = CreateBuffer();
            _velocityBuffer1 = CreateBuffer();
            _velocityBuffer2 = CreateBuffer();
            _rotationBuffer1 = CreateBuffer();
            _rotationBuffer2 = CreateBuffer();

            if (!_kernelMaterial) _kernelMaterial = CreateMaterial(_kernelShader);
            if (!_debugMaterial)  _debugMaterial  = CreateMaterial(_debugShader);

            // Warming up
            InitializeAndPrewarmBuffers();

            _needsReset = false;
        }

        void InitializeAndPrewarmBuffers()
        {
            _noiseOffset = Vector3.zero;

            UpdateKernelShader();

            Graphics.Blit(null, _positionBuffer2, _kernelMaterial, 0);
            Graphics.Blit(null, _velocityBuffer2, _kernelMaterial, 1);
            Graphics.Blit(null, _rotationBuffer2, _kernelMaterial, 2);

            for (var i = 0; i < 8; i++) {
                SwapBuffersAndInvokeKernels();
                UpdateKernelShader();
            }
        }

        void SwapBuffersAndInvokeKernels()
        {
            // Swap the buffers.
            var tempPosition = _positionBuffer1;
            var tempVelocity = _velocityBuffer1;
            var tempRotation = _rotationBuffer1;

            _positionBuffer1 = _positionBuffer2;
            _velocityBuffer1 = _velocityBuffer2;
            _rotationBuffer1 = _rotationBuffer2;

            _positionBuffer2 = tempPosition;
            _velocityBuffer2 = tempVelocity;
            _rotationBuffer2 = tempRotation;

            // Invoke the position update kernel.
            _kernelMaterial.SetTexture("_PositionBuffer", _positionBuffer1);
            _kernelMaterial.SetTexture("_VelocityBuffer", _velocityBuffer1);
            _kernelMaterial.SetTexture("_RotationBuffer", _rotationBuffer1);
            Graphics.Blit(null, _positionBuffer2, _kernelMaterial, 3);

            // Invoke the velocity and rotation update kernel
            // with the updated position.
            _kernelMaterial.SetTexture("_PositionBuffer", _positionBuffer2);
            Graphics.Blit(null, _velocityBuffer2, _kernelMaterial, 4);
            Graphics.Blit(null, _rotationBuffer2, _kernelMaterial, 5);
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
            if (_velocityBuffer1) DestroyImmediate(_velocityBuffer1);
            if (_velocityBuffer2) DestroyImmediate(_velocityBuffer2);
            if (_rotationBuffer1) DestroyImmediate(_rotationBuffer1);
            if (_rotationBuffer2) DestroyImmediate(_rotationBuffer2);
            if (_kernelMaterial)  DestroyImmediate(_kernelMaterial);
            if (_debugMaterial)   DestroyImmediate(_debugMaterial);
        }

        void Update()
        {
            if (_needsReset) ResetResources();

            if (Application.isPlaying)
            {
                UpdateKernelShader();
                SwapBuffersAndInvokeKernels();
            }
            else
            {
                InitializeAndPrewarmBuffers();
            }

            // Make a material property block for the following drawcalls.
            var props = new MaterialPropertyBlock();
            props.SetTexture("_PositionBuffer", _positionBuffer2);
            props.SetTexture("_RotationBuffer", _rotationBuffer2);
            props.SetFloat("_ScaleMin", _scale * (1 - _scaleRandomness));
            props.SetFloat("_ScaleMax", _scale);
            props.SetFloat("_RandomSeed", _randomSeed);

            // Temporary variables
            var mesh = _bulkMesh.mesh;
            var position = transform.position;
            var rotation = transform.rotation;
            var material = _material ? _material : _defaultMaterial;
            var uv = new Vector2(0.5f / _positionBuffer2.width, 0);

            // Draw a bulk mesh repeatedly.
            for (var i = 0; i < _positionBuffer2.height; i++)
            {
                uv.y = (0.5f + i) / _positionBuffer2.height;
                props.SetVector("_BufferOffset", uv);
                Graphics.DrawMesh(
                    mesh, position, rotation,
                    material, 0, null, 0, props,
                    _castShadows, _receiveShadows);
            }
        }

        void OnGUI()
        {
            if (_debug && Event.current.type.Equals(EventType.Repaint))
            {
                if (_debugMaterial && _positionBuffer2 && _velocityBuffer2 && _rotationBuffer2)
                {
                    var w = _positionBuffer2.width;
                    var h = _positionBuffer2.height;

                    var rect = new Rect(0, 0, w, h);
                    Graphics.DrawTexture(rect, _positionBuffer2, _debugMaterial);

                    rect.y += h;
                    Graphics.DrawTexture(rect, _velocityBuffer2, _debugMaterial);

                    rect.y += h;
                    Graphics.DrawTexture(rect, _rotationBuffer2, _debugMaterial);
                }
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
