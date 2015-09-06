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
        MaterialProperty _albedoMap;
        MaterialProperty _normalMap;
        MaterialProperty _normalScale;
        MaterialProperty _occlusionMap;
        MaterialProperty _occlusionStr;
        MaterialProperty _emission;

        static GUIContent _albedoText    = new GUIContent("Albedo");
        static GUIContent _normalMapText = new GUIContent("Normal Map");
        static GUIContent _occlusionText = new GUIContent("Occlusion");

        bool _initial = true;

        void FindProperties(MaterialProperty[] props)
        {
            _colorMode    = FindProperty("_ColorMode", props);
            _color        = FindProperty("_Color", props);
            _color2       = FindProperty("_Color2", props);
            _metallic     = FindProperty("_Metallic", props);
            _smoothness   = FindProperty("_Smoothness", props);
            _albedoMap    = FindProperty("_MainTex", props);
            _normalMap    = FindProperty("_NormalMap", props);
            _normalScale  = FindProperty("_NormalScale", props);
            _occlusionMap = FindProperty("_OcclusionMap", props);
            _occlusionStr = FindProperty("_OcclusionStr", props);
            _emission     = FindProperty("_Emission", props);
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

            EditorGUILayout.Space();

            materialEditor.ShaderProperty(_metallic, "Metallic");
            materialEditor.ShaderProperty(_smoothness, "Smoothness");

            EditorGUILayout.Space();

            materialEditor.TexturePropertySingleLine(_albedoText, _albedoMap, null);
            materialEditor.TexturePropertySingleLine(_normalMapText, _normalMap, _normalMap.textureValue ? _normalScale : null);
            materialEditor.TexturePropertySingleLine(_occlusionText, _occlusionMap, _occlusionMap.textureValue ? _occlusionStr : null);
			materialEditor.TextureScaleOffsetProperty(_albedoMap);

            EditorGUILayout.Space();

            materialEditor.ShaderProperty(_emission, "Emission");

            return EditorGUI.EndChangeCheck();
        }

        static void SetMaterialKeywords(Material material)
        {
            SetKeyword(material, "_ALBEDOMAP", material.GetTexture("_MainTex"));
            SetKeyword(material, "_NORMALMAP", material.GetTexture("_NormalMap"));
            SetKeyword(material, "_OCCLUSIONMAP", material.GetTexture("_OcclusionMap"));

            var emissive = material.GetColor("_Emission").maxColorComponent > 0.1f / 255;
            SetKeyword(material, "_EMISSION", emissive);
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
