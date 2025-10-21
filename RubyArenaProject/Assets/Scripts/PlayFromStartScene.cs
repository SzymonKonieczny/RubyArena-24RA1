using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class PlayFromStartScene
{
    static string startScenePath = "Assets/Scenes/InitializerScene.unity"; //"Assets/Scenes/MultiplayerStartScene.unity"; 

    static PlayFromStartScene()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            // Save current scene so you can return to it
            string currentScene = SceneManager.GetActiveScene().path;
            EditorPrefs.SetString("LastScene", currentScene);

            // Open the starting scene
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(startScenePath);
            }
            else
            {
                // Cancel play if user declined to save
                EditorApplication.isPlaying = false;
            }
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            // Return to the original scene after play mode
            string lastScene = EditorPrefs.GetString("LastScene", "");
            if (!string.IsNullOrEmpty(lastScene))
            {
                EditorSceneManager.OpenScene(lastScene);
            }
        }
    }
}
#endif