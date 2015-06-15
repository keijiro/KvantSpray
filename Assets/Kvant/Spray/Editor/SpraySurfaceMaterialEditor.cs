//
// Custom material editor for surface shaders
//
using UnityEngine;
using UnityEditor;

namespace Kvant
{
    public class SpraySurfaceMaterialEditor : ShaderGUI
    {
        MaterialProperty _colorMode;
        MaterialProperty _color;
        MaterialProperty _color2;
        MaterialProperty _metallic;
        MaterialProperty _smoothness;

        void FindProperties(MaterialProperty[] props)
        {
            _colorMode  = FindProperty("_ColorMode", props);
            _color      = FindProperty("_Color", props);
            _color2     = FindProperty("_Color2", props);
            _metallic   = FindProperty("_Metallic", props);
            _smoothness = FindProperty("_Smoothness", props);
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            FindProperties(properties);
            ShaderPropertiesGUI(materialEditor);
        }

        bool ShaderPropertiesGUI(MaterialEditor materialEditor)
        {
            EditorGUI.BeginChangeCheck();

            materialEditor.ShaderProperty(_colorMode, "Color Mode");

            if (_colorMode.floatValue > 0)
            {
                var rect = EditorGUILayout.GetControlRect();
                rect.x += EditorGUIUtility.labelWidth;
                rect.width = (rect.width - EditorGUIUtility.labelWidth) / 2 - 2;
                materialEditor.ShaderProperty(rect, _color, "");
                rect.x += rect.width + 4;
                materialEditor.ShaderProperty(rect, _color2, "");
            }
            else
            {
                materialEditor.ShaderProperty(_color, " ");
            }

            materialEditor.ShaderProperty(_metallic, "Metallic");
            materialEditor.ShaderProperty(_smoothness, "Smoothness");

            return EditorGUI.EndChangeCheck();
        }
    }
}
