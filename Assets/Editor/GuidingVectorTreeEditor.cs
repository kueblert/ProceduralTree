using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(GuidingVectorTree))]

public class GuidingVectorTreeEditor : Editor {


    public void OnEnable()
    {
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();

        GUILayout.Space(10.0f);         // Some vertical spacing

        GuidingVectorTree t = target as GuidingVectorTree;

        adjustIterationObjects();
        TreeIterationParam[] iterations = t.gameObject.GetComponents<TreeIterationParam>();

        for (int i=0; i < iterations.Length; i++)
        {
            Editor e = Editor.CreateEditor(iterations[i]);
            ((TreeIterationParamEditor)e).visible = true;
            e.OnInspectorGUI();
        }

        serializedObject.ApplyModifiedProperties();
    }

    // Adjust the number of iteration scripts that exist towards the number of iterations requested via the settings.
    private void adjustIterationObjects()
    {
        GuidingVectorTree t = target as GuidingVectorTree;
        int iterations_should = t.nIterations;

        TreeIterationParam[] iterations_is = t.gameObject.GetComponents<TreeIterationParam>();

        if (iterations_should == iterations_is.Length) return; // nothing to do (this is not required as the loops will iterate 0 times, but just to be sure)

        // Add Iterations, if too few
        for (int i = iterations_is.Length; i < iterations_should; i++)
        {
            TreeIterationParam p = t.gameObject.AddComponent<TreeIterationParam>();
            p.iterationCounter = i + 1;
        }


        // Remove Iterations, if too many
        List<TreeIterationParam> deleteList = new List<TreeIterationParam>();
        for (int i= iterations_is.Length-1; i >= iterations_should; i--) {
            deleteList.Add(iterations_is[i]);
                }
        deleteIterations(deleteList);
    }

    private void deleteIterations(List<TreeIterationParam> deleteList)
    {
        foreach (TreeIterationParam it in deleteList)
        {
            DestroyImmediate(it);     // then delete object
        }

        // Avoid warning of deleted objects being in use. This must not be called every iteration but only when a change appears, otherwise nothing will show up.
        // see http://answers.unity3d.com/questions/48309/editor-destroyimmediate-remove-component-via-scrip.html
        if (deleteList.Count > 0) { 
            EditorGUIUtility.ExitGUI();
        }
    }


        void OnDestroy()
    {
    }
}
