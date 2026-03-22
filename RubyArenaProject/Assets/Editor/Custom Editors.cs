using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;



[CustomEditor(typeof(MinionSpawner))]
public class MinionSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        MinionSpawner minionSpawner = (MinionSpawner)target;

        // Add a button to the Inspector
        if (GUILayout.Button("Kill All Minions "))
        {
            // Call the function
            minionSpawner.KillAllMinions();
        }
    }
}
[CustomEditor(typeof(Movement))]
public class MovementEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        // Get a reference to the script
        Movement myScript = (Movement)target;

        // Add a button to the Inspector
        if (GUILayout.Button("Switch Camera Style "))
        {
            // Call the function
            myScript.SwitchCameraStyle();
        }
    }
}
[CustomEditor(typeof(SkillsUIManager))]

public class SkillsUIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        // Get a reference to the script
        SkillsUIManager myScript = (SkillsUIManager)target;

        // Add a button to the Inspector
        if (GUILayout.Button("Recalculate"))
        {
            // Call the function
            myScript.Recalculate();
        }
    }
}