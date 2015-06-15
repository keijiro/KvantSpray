//
// Custom material editor for unlit shaders
//
using UnityEngine;
using UnityEditor;

namespace Kvant
{
    public class SprayUnlitMaterialEditor : ShaderGUI
    {
        MaterialProperty _blendMode;
        MaterialProperty _colorMode;
        MaterialProperty _color;
        MaterialProperty _color2;

        void FindProperties(MaterialProperty[] props)
        {
            _blendMode = FindProperty("_BlendMode", props);
            _colorMode = FindProperty("_ColorMode", props);
            _color     = FindProperty("_Color", props);
            _color2    = FindProperty("_Color2", props);
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            FindProperties(properties);
            ShaderPropertiesGUI(materialEditor);
        }

        bool ShaderPropertiesGUI(MaterialEditor materialEditor)
        {
            EditorGUI.BeginChangeCheck();

            materialEditor.ShaderProperty(_blendMode, "Blend Mode");
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

            return EditorGUI.EndChangeCheck();
        }
    }
}
