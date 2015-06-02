//
// Custom editor class for Spray
//
using UnityEngine;
using UnityEditor;

namespace Kvant
{
    [CustomEditor(typeof(Spray)), CanEditMultipleObjects]
    public class SprayEditor : Editor
    {
        SerializedProperty _maxParticles;
        SerializedProperty _emitterCenter;
        SerializedProperty _emitterSize;
        SerializedProperty _throttle;

        SerializedProperty _minLife;
        SerializedProperty _maxLife;

        SerializedProperty _minSpeed;
        SerializedProperty _maxSpeed;
        SerializedProperty _direction;
        SerializedProperty _spread;
        SerializedProperty _minSpin;
        SerializedProperty _maxSpin;

        SerializedProperty _noiseFrequency;
        SerializedProperty _noiseAmplitude;
        SerializedProperty _noiseAnimation;

        SerializedProperty _shapes;
        SerializedProperty _minScale;
        SerializedProperty _maxScale;

        SerializedProperty _shadingMode;
        SerializedProperty _metallic;
        SerializedProperty _smoothness;
        SerializedProperty _castShadows;
        SerializedProperty _receiveShadows;

        SerializedProperty _colorMode;
        SerializedProperty _color;
        SerializedProperty _color2;

        SerializedProperty _randomSeed;
        SerializedProperty _debug;

        static GUIContent _textCenter    = new GUIContent("Center");
        static GUIContent _textSize      = new GUIContent("Size");
        static GUIContent _textLife      = new GUIContent("Life");
        static GUIContent _textSpeed     = new GUIContent("Speed");
        static GUIContent _textSpin      = new GUIContent("Spin");
        static GUIContent _textFrequency = new GUIContent("Frequency");
        static GUIContent _textAmplitude = new GUIContent("Amplitude");
        static GUIContent _textAnimation = new GUIContent("Animation");
        static GUIContent _textScale     = new GUIContent("Scale");
        static GUIContent _textNull      = new GUIContent("");
        static GUIContent _textEmpty     = new GUIContent(" ");

        void OnEnable()
        {
            _maxParticles  = serializedObject.FindProperty("_maxParticles");
            _emitterCenter = serializedObject.FindProperty("_emitterCenter");
            _emitterSize   = serializedObject.FindProperty("_emitterSize");
            _throttle      = serializedObject.FindProperty("_throttle");

            _minLife = serializedObject.FindProperty("_minLife");
            _maxLife = serializedObject.FindProperty("_maxLife");

            _minSpeed  = serializedObject.FindProperty("_minSpeed");
            _maxSpeed  = serializedObject.FindProperty("_maxSpeed");
            _direction = serializedObject.FindProperty("_direction");
            _spread    = serializedObject.FindProperty("_spread");
            _minSpin   = serializedObject.FindProperty("_minSpin");
            _maxSpin   = serializedObject.FindProperty("_maxSpin");

            _noiseFrequency = serializedObject.FindProperty("_noiseFrequency");
            _noiseAmplitude = serializedObject.FindProperty("_noiseAmplitude");
            _noiseAnimation = serializedObject.FindProperty("_noiseAnimation");

            _shapes   = serializedObject.FindProperty("_shapes");
            _minScale = serializedObject.FindProperty("_minScale");
            _maxScale = serializedObject.FindProperty("_maxScale");

            _shadingMode    = serializedObject.FindProperty("_shadingMode");
            _metallic       = serializedObject.FindProperty("_metallic");
            _smoothness     = serializedObject.FindProperty("_smoothness");
            _castShadows    = serializedObject.FindProperty("_castShadows");
            _receiveShadows = serializedObject.FindProperty("_receiveShadows");

            _colorMode = serializedObject.FindProperty("_colorMode");
            _color     = serializedObject.FindProperty("_color");
            _color2    = serializedObject.FindProperty("_color2");

            _randomSeed = serializedObject.FindProperty("_randomSeed");
            _debug      = serializedObject.FindProperty("_debug");
        }

        public override void OnInspectorGUI()
        {
            var targetSpray = target as Spray;

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(_maxParticles);
            if (!_maxParticles.hasMultipleDifferentValues)
                EditorGUILayout.LabelField(" ", "Allocated: " + targetSpray.maxParticles, EditorStyles.miniLabel);

            if (EditorGUI.EndChangeCheck())
                targetSpray.NotifyConfigChange();

            EditorGUILayout.LabelField("Emitter", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_emitterCenter, _textCenter);
            EditorGUILayout.PropertyField(_emitterSize, _textSize);
            EditorGUILayout.PropertyField(_throttle);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

            MinMaxSlider(_textLife, _minLife, _maxLife, 0.1f, 5.0f);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Velocity", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            MinMaxSlider(_textSpeed, _minSpeed, _maxSpeed, 0.0f, 30.0f);
            EditorGUILayout.PropertyField(_direction);
            EditorGUILayout.PropertyField(_spread);
            MinMaxSlider(_textSpin, _minSpin, _maxSpin, 0.0f, 1000.0f);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Turbulent Noise", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.Slider(_noiseFrequency, 0.01f, 1.0f, _textFrequency);
            EditorGUILayout.Slider(_noiseAmplitude, 0.0f, 20.0f, _textAmplitude);
            EditorGUILayout.Slider(_noiseAnimation, 0.0f, 10.0f, _textAnimation);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(_shapes, true);

            if (EditorGUI.EndChangeCheck())
                targetSpray.NotifyConfigChange();

            EditorGUILayout.Space();

            MinMaxSlider(_textScale, _minScale, _maxScale, 0.01f, 2.0f);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_shadingMode);
            if (_shadingMode.hasMultipleDifferentValues || _shadingMode.enumValueIndex < 2)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_metallic);
                EditorGUILayout.PropertyField(_smoothness);
                EditorGUILayout.PropertyField(_castShadows);
                EditorGUILayout.PropertyField(_receiveShadows);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_colorMode);
            if (_colorMode.hasMultipleDifferentValues || _colorMode.enumValueIndex != 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(_textEmpty);
                EditorGUILayout.PropertyField(_color, _textNull);
                EditorGUILayout.PropertyField(_color2, _textNull);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.PropertyField(_color, _textEmpty);
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_randomSeed);
            EditorGUILayout.PropertyField(_debug);

            serializedObject.ApplyModifiedProperties();
        }

        void MinMaxSlider(GUIContent label, SerializedProperty propMin, SerializedProperty propMax, float minLimit, float maxLimit)
        {
            var min = propMin.floatValue;
            var max = propMax.floatValue;

            EditorGUI.BeginChangeCheck();

            // Min-max slider.
            EditorGUILayout.MinMaxSlider(label, ref min, ref max, minLimit, maxLimit);

            var prevIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Float value boxes.
            var rect = EditorGUILayout.GetControlRect();
            rect.x += EditorGUIUtility.labelWidth;
            rect.width = (rect.width - EditorGUIUtility.labelWidth) / 2 - 2;

            if (EditorGUIUtility.wideMode)
            {
                EditorGUIUtility.labelWidth = 28;
                min = Mathf.Clamp(EditorGUI.FloatField(rect, "min", min), minLimit, max);
                rect.x += rect.width + 4;
                max = Mathf.Clamp(EditorGUI.FloatField(rect, "max", max), min, maxLimit);
                EditorGUIUtility.labelWidth = 0;
            }
            else
            {
                min = Mathf.Clamp(EditorGUI.FloatField(rect, min), minLimit, max);
                rect.x += rect.width + 4;
                max = Mathf.Clamp(EditorGUI.FloatField(rect, max), min, maxLimit);
            }

            EditorGUI.indentLevel = prevIndent;

            if (EditorGUI.EndChangeCheck()) {
                propMin.floatValue = min;
                propMax.floatValue = max;
            }
        }
    }
}
