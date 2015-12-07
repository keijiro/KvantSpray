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
using UnityEditor;

namespace Kino
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Bloom))]
    public class Bloomditor : Editor
    {
        SerializedProperty _radius1;
        SerializedProperty _radius2;
        SerializedProperty _intensity1;
        SerializedProperty _intensity2;
        SerializedProperty _threshold;
        SerializedProperty _temporalFiltering;

        static string _configWarning =
            "This effect only works properly with HDR and linear rendering. " +
            "It's strongly recommended to enable these options.";

        static GUIContent _textRadius = new GUIContent("Radius");
        static GUIContent _textIntensity = new GUIContent("Intensity");

        bool CheckConfig()
        {
            // Check if HDR rendering is enabled.
            var cam = ((Bloom)target).GetComponent<Camera>();
            if (!cam.hdr) return false;

            // check if linear rendering is enabled.
            return QualitySettings.activeColorSpace == ColorSpace.Linear;
        }

        void OnEnable()
        {
            _radius1 = serializedObject.FindProperty("_radius1");
            _radius2 = serializedObject.FindProperty("_radius2");
            _intensity1 = serializedObject.FindProperty("_intensity1");
            _intensity2 = serializedObject.FindProperty("_intensity2");
            _threshold = serializedObject.FindProperty("_threshold");
            _temporalFiltering = serializedObject.FindProperty("_temporalFiltering");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (!CheckConfig())
                EditorGUILayout.HelpBox(_configWarning, MessageType.Warning);

            EditorGUILayout.LabelField("Fringe (small bloom)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_radius1, _textRadius);
            EditorGUILayout.PropertyField(_intensity1, _textIntensity);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Diffusion (large bloom)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_radius2, _textRadius);
            EditorGUILayout.PropertyField(_intensity2, _textIntensity);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_threshold);
            EditorGUILayout.PropertyField(_temporalFiltering);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
