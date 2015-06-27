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
        MaterialProperty _mainTex;

        bool _initial = true;

        void FindProperties(MaterialProperty[] props)
        {
            _blendMode = FindProperty("_BlendMode", props);
            _colorMode = FindProperty("_ColorMode", props);
            _color     = FindProperty("_Color", props);
            _color2    = FindProperty("_Color2", props);
            _mainTex   = FindProperty("_MainTex", props);
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            FindProperties(properties);

            if (ShaderPropertiesGUI(materialEditor) || _initial)
                foreach (Material m in materialEditor.targets)
                    SetMaterialKeywords(m);

            _initial = false;
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

            materialEditor.ShaderProperty(_mainTex, "Texture");

            return EditorGUI.EndChangeCheck();
        }

        static void SetMaterialKeywords(Material material)
        {
            SetKeyword(material, "_MAINTEX", material.GetTexture("_MainTex"));
        }

        static void SetKeyword(Material m, string keyword, bool state)
        {
            if (state)
                m.EnableKeyword(keyword);
            else
                m.DisableKeyword(keyword);
        }
    }
}
