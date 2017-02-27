using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TreeIterationParam))]

public class TreeIterationParamEditor : Editor
{

    public bool visible = false;

    public void OnEnable()
    {
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (visible)
        { // don't draw anything unless it is set to visible. (= hide in normal Inspector)
            GUIStyle boldText = new GUIStyle();
            boldText.fontStyle = FontStyle.Bold;
            EditorGUILayout.LabelField(new GUIContent("Iteration " + serializedObject.FindProperty("iterationCounter").intValue), boldText);
            EditorGUI.indentLevel++;

            // Endpoints
            EditorGUILayout.PropertyField(serializedObject.FindProperty("endpointMethod"), new GUIContent("Endpoint selection "));
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("nBranches"), new GUIContent("nBranches"));

            SerializedProperty m = serializedObject.FindProperty("endpointMethod");
            TreeIterationParam.PlacementType t = (TreeIterationParam.PlacementType)m.intValue;

            switch (t)
            {
                case TreeIterationParam.PlacementType.VOLUME:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("volume"), new GUIContent("Volume"));
                    break;
                case TreeIterationParam.PlacementType.HOPS:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("nHops"), new GUIContent("nHops"));
                    break;
            }
            EditorGUI.indentLevel--;
            // Twisting
            EditorGUILayout.PropertyField(serializedObject.FindProperty("BranchRotation"), new GUIContent("Branch twist"));
            EditorGUI.indentLevel++;
            SerializedProperty sr = serializedObject.FindProperty("BranchRotation");
            TreeIterationParam.RotationType r = (TreeIterationParam.RotationType)sr.intValue;

            switch (r)
            {
                case TreeIterationParam.RotationType.TWIST:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("twistHop"), new GUIContent("Twist hop"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("alpha0"), new GUIContent("alpha 0"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("alpha1"), new GUIContent("alpha 1"));
                    break;
                //case TreeIterationParam.RotationType.FREE:
                //    EditorGUILayout.PropertyField(serializedObject.FindProperty("twistFunction"), new GUIContent("Twist function"));
                //    break;
            }
            EditorGUI.indentLevel--;

            EditorGUI.indentLevel--;

        }

        serializedObject.ApplyModifiedProperties();
    }

}
