//
// Spray - mesh particle system
//

using UnityEngine;

namespace Kvant
{
    [ExecuteInEditMode, AddComponentMenu("Kvant/Spray")]
    public partial class Spray : MonoBehaviour
    {
        #region Parameters Exposed To Editor

        [SerializeField] Mesh[] _shapes = new Mesh[1];
        [SerializeField] int _maxParticles = 1000;

        [SerializeField] Vector3 _emitterCenter = Vector3.zero;
        [SerializeField] Vector3 _emitterSize = Vector3.one;
        [SerializeField] float _throttle = 1.0f;

        [SerializeField] float _minLife = 1.0f;
        [SerializeField] float _maxLife = 4.0f;

        [SerializeField] float _minScale = 0.1f;
        [SerializeField] float _maxScale = 1.2f;

        [SerializeField] Vector3 _direction = Vector3.forward;
        [SerializeField] float _spread = 0.2f;

        [SerializeField] float _minSpeed = 2.0f;
        [SerializeField] float _maxSpeed = 10.0f;

        [SerializeField] float _minSpin = 30.0f;
        [SerializeField] float _maxSpin = 200.0f;

        [SerializeField] float _noiseFrequency = 0.2f;
        [SerializeField] float _noiseAmplitude = 5.0f;
        [SerializeField] float _noiseAnimation = 1.0f;

        [ColorUsage(true, true, 0, 8, 0.125f, 3)]
        [SerializeField] Color _color = Color.white;
        [SerializeField] float _metallic = 0.5f;
        [SerializeField] float _smoothness = 0.5f;

        [SerializeField] int _randomSeed = 0;
        [SerializeField] bool _debug;

        #endregion

        #region Public Properties

        // Returns the actual number of particles.
        public int maxParticles {
            get {
                if (_bulkMesh == null) return 0;
                return (_maxParticles / _bulkMesh.copyCount + 1) * _bulkMesh.copyCount;
            }
        }

        public float throttle {
            get { return _throttle; }
            set { _throttle = value; }
        }

        public Vector3 emitterCenter {
            get { return _emitterCenter; }
            set { _emitterCenter = value; }
        }

        public Vector3 emitterSize {
            get { return _emitterSize; }
            set { _emitterSize = value; }
        }

        public float minLife {
            get { return _minLife; }
            set { _minLife = value; }
        }

        public float maxLife {
            get { return _maxLife; }
            set { _maxLife = value; }
        }

        public float minScale {
            get { return _minScale; }
            set { _minScale = value; }
        }

        public float maxScale {
            get { return _maxScale; }
            set { _maxScale = value; }
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

        public Color color {
            get { return _color; }
            set { _color = value; }
        }

        #endregion

        #region Shader And Materials

        [SerializeField] Shader _kernelShader;
        [SerializeField] Shader _surfaceShader;
        [SerializeField] Shader _debugShader;

        Material _kernelMaterial;
        Material _surfaceMaterial;
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

        void ApplyKernelParameters()
        {
            _kernelMaterial.SetVector("_EmitterPos", _emitterCenter);
            _kernelMaterial.SetVector("_EmitterSize", _emitterSize);

            _kernelMaterial.SetVector("_LifeParams", new Vector2(1.0f / _minLife, 1.0f / _maxLife));

            var dir = new Vector4(_direction.x, _direction.y, _direction.z, _spread);
            _kernelMaterial.SetVector("_Direction", dir);

            var pi360 = Mathf.PI / 360;
            var sparams = new Vector4(_minSpeed, _maxSpeed, _minSpin * pi360, _maxSpin * pi360);
            _kernelMaterial.SetVector("_SpeedParams", sparams);

            var np = new Vector3(_noiseFrequency, _noiseAmplitude, _noiseAnimation);
            _kernelMaterial.SetVector("_NoiseParams", np);

            var delta = Application.isPlaying ? Time.deltaTime : 1.0f;
            _kernelMaterial.SetVector("_Config", new Vector3(_throttle, _randomSeed, delta));
        }

        void ResetResources()
        {
            // Mesh object.
            if (_bulkMesh == null)
                _bulkMesh = new BulkMesh(_shapes);
            else
                _bulkMesh.Rebuild(_shapes);

            // GPGPU buffers.
            if (_positionBuffer1) DestroyImmediate(_positionBuffer1);
            if (_positionBuffer2) DestroyImmediate(_positionBuffer2);
            if (_rotationBuffer1) DestroyImmediate(_rotationBuffer1);
            if (_rotationBuffer2) DestroyImmediate(_rotationBuffer2);

            _positionBuffer1 = CreateBuffer();
            _positionBuffer2 = CreateBuffer();
            _rotationBuffer1 = CreateBuffer();
            _rotationBuffer2 = CreateBuffer();

            // Shader materials.
            if (!_kernelMaterial)  _kernelMaterial  = CreateMaterial(_kernelShader );
            if (!_surfaceMaterial) _surfaceMaterial = CreateMaterial(_surfaceShader);
            if (!_debugMaterial)   _debugMaterial   = CreateMaterial(_debugShader  );

            // GPGPU buffer Initialization.
            ApplyKernelParameters();
            Graphics.Blit(null, _positionBuffer2, _kernelMaterial, 0);
            Graphics.Blit(null, _rotationBuffer2, _kernelMaterial, 1);

            _needsReset = false;
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
            if (_surfaceMaterial) DestroyImmediate(_surfaceMaterial);
            if (_debugMaterial)   DestroyImmediate(_debugMaterial);
        }

        void Update()
        {
            if (_needsReset) ResetResources();

            ApplyKernelParameters();

            if (Application.isPlaying)
            {
                // Swap the buffers.
                var temp = _positionBuffer1;
                _positionBuffer1 = _positionBuffer2;
                _positionBuffer2 = temp;

                temp = _rotationBuffer1;
                _rotationBuffer1 = _rotationBuffer2;
                _rotationBuffer2 = temp;
            }
            else
            {
                // Editor: initialize the buffer on every update.
                Graphics.Blit(null, _positionBuffer1, _kernelMaterial, 0);
                Graphics.Blit(null, _rotationBuffer1, _kernelMaterial, 1);
            }

            // Apply the kernel shaders.
            Graphics.Blit(_positionBuffer1, _positionBuffer2, _kernelMaterial, 2);
            Graphics.Blit(_rotationBuffer1, _rotationBuffer2, _kernelMaterial, 3);

            // Draw the bulk mesh.
            _surfaceMaterial.SetTexture("_PositionTex", _positionBuffer2);
            _surfaceMaterial.SetTexture("_RotationTex", _rotationBuffer2);
            _surfaceMaterial.SetColor("_Color", _color);
            _surfaceMaterial.SetVector("_PbrParams", new Vector2(_metallic, _smoothness));
            _surfaceMaterial.SetVector("_ScaleParams", new Vector2(_minScale, _maxScale));

            var uv = new Vector2(0.5f / _positionBuffer2.width, 0);
            var offset = new MaterialPropertyBlock();

            for (var i = 0; i < _positionBuffer2.height; i++)
            {
                uv.y = (0.5f + i) / _positionBuffer2.height;
                offset.AddVector("_BufferOffset", uv);
                Graphics.DrawMesh(_bulkMesh.mesh, transform.position, transform.rotation, _surfaceMaterial, 0, null, 0, offset);
            }
        }

        void OnGUI()
        {
            if (_debug && Event.current.type.Equals(EventType.Repaint) && _debugMaterial)
            {
                var r1 = new Rect(0, 0, 256, 64);
                var r2 = new Rect(0, 64, 256, 64);
                if (_positionBuffer1) Graphics.DrawTexture(r1, _positionBuffer1, _debugMaterial);
                if (_rotationBuffer1) Graphics.DrawTexture(r2, _rotationBuffer1, _debugMaterial);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(_emitterCenter, _emitterSize);
        }

        #endregion
    }
}
