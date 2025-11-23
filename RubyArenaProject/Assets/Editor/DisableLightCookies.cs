using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class DisableLightCookiesEditor
{
    static DisableLightCookiesEditor()
    {
        EditorApplication.delayCall += DisableLightCookies;
    }

    static void DisableLightCookies()
    {
        var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset == null)
        {
            Debug.LogWarning("No URP asset found in GraphicsSettings.");
            return;
        }

        SerializedObject so = new SerializedObject(urpAsset);
        var prop = so.FindProperty("m_SupportsLightCookies");
        if (prop != null && prop.boolValue)
        {
            prop.boolValue = false;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(urpAsset);
            AssetDatabase.SaveAssets();
            Debug.Log("Light Cookies have been disabled in URP Asset.");
        }
        else
        {
            Debug.Log("Light Cookies already disabled.");
        }
    }
}
