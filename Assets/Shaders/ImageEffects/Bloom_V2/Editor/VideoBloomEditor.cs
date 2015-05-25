using System;
using UnityEditor;
using UnityEngine;

[CustomEditor (typeof(VideoBloom))]
class VideoBloomEditor : Editor
{
	SerializedProperty tweakMode;
	SerializedProperty Threshold;
	SerializedProperty MasterAmount;
	SerializedProperty MediumAmount;
	SerializedProperty LargeAmount;
	SerializedProperty Tint;
	SerializedProperty KernelSize;
	SerializedProperty MediumKernelScale;
	SerializedProperty LargeKernelScale;
	SerializedProperty BlendMode;
	SerializedProperty HighQuality;



	SerializedObject serObj;

	void OnEnable ()
	{
		serObj = new SerializedObject (target);

		tweakMode = serObj.FindProperty("tweakMode");
		Threshold = serObj.FindProperty("Threshold");
		MasterAmount = serObj.FindProperty("MasterAmount");
		MediumAmount = serObj.FindProperty("MediumAmount");
		LargeAmount = serObj.FindProperty("LargeAmount");
		Tint = serObj.FindProperty("Tint");
		KernelSize = serObj.FindProperty("KernelSize");
		MediumKernelScale = serObj.FindProperty("MediumKernelScale");
		LargeKernelScale = serObj.FindProperty("LargeKernelScale");
		BlendMode = serObj.FindProperty("BlendMode");
		HighQuality = serObj.FindProperty("HighQuality");
	}


	public override void OnInspectorGUI ()
	{
		serObj.Update();

		EditorGUILayout.LabelField("Bloom for bright screen pixels", EditorStyles.miniLabel);

		EditorGUILayout.Separator ();

		EditorGUILayout.PropertyField (tweakMode, new GUIContent("Mode"));

		EditorGUILayout.Separator ();

		Threshold.floatValue = EditorGUILayout.Slider ("Threshold", Threshold.floatValue, 0.0f, 4.0f);
		MasterAmount.floatValue = EditorGUILayout.Slider ("Intensity", MasterAmount.floatValue, 0.0f, 5.0f);
		EditorGUILayout.PropertyField (BlendMode, new GUIContent("Blend Mode"));
		KernelSize.floatValue = EditorGUILayout.Slider ("Kernel Size", KernelSize.floatValue, 10.0f, 100.0f);
		EditorGUILayout.PropertyField(Tint, new GUIContent("Tint"));

		if (tweakMode.intValue == 1)
		{
			EditorGUILayout.Separator ();
			EditorGUILayout.Separator ();
			MediumAmount.floatValue = EditorGUILayout.Slider ("Medium Amount", MediumAmount.floatValue, 0.0f, 5.0f);
			LargeAmount.floatValue = EditorGUILayout.Slider ("Large Amount", LargeAmount.floatValue, 0.0f, 100.0f);
			EditorGUILayout.Separator ();
			MediumKernelScale.floatValue = EditorGUILayout.Slider ("Medium Kernel Scale", MediumKernelScale.floatValue, 1.0f, 20.0f);
			LargeKernelScale.floatValue = EditorGUILayout.Slider ("Large Kernel Scale", LargeKernelScale.floatValue, 3.0f, 20.0f);
			EditorGUILayout.Separator ();
			EditorGUILayout.PropertyField (HighQuality, new GUIContent("High Quality"));
		}

		EditorGUILayout.Separator ();


		serObj.ApplyModifiedProperties();
	}
}
