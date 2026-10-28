using UnityEditor;
using UnityEngine;

/// <summary>
/// NGO's default NetworkBehaviourEditor only draws primitive NetworkVariables, so
/// NetworkVariable&lt;Stats&gt; shows as "Type not renderable". This draws the struct
/// field by field instead.
/// </summary>
[CustomEditor(typeof(EntityStats))]
public class EntityStatsEditor : Editor
{
    EntityStats stats;
    SerializedProperty basicStatsField;
    SerializedProperty modifiedStatsField;

    void OnEnable()
    {
        stats = (EntityStats)target;
        basicStatsField = serializedObject.FindProperty(nameof(EntityStats.basicStats));
        modifiedStatsField = serializedObject.FindProperty(nameof(EntityStats.modifiedStats));
    }

    public override void OnInspectorGUI()
    {
        if (basicStatsField == null || modifiedStatsField == null)
        {
            EditorGUILayout.HelpBox(
                "EntityStats.basicStats / modifiedStats must be plain fields (not properties) " +
                "for the Inspector to reach them.",
                MessageType.Error);
            DrawDefaultInspector();
            return;
        }

        serializedObject.Update();

        // Mirror NGO's IsBehaviourEditable: only the server (or an offline editor) may
        // author values. NetworkObject/NetworkManager are only touched in play mode, where
        // NGO guarantees they're resolvable.
        bool editable = !Application.isPlaying
                        || stats.NetworkManager == null
                        || !stats.NetworkManager.IsListening
                        || stats.NetworkManager.IsServer;

        EditorGUILayout.LabelField("Basic Stats", EditorStyles.boldLabel);
        if (editable)
        {
            // Editing in place: the NetworkVariable owns the instance, so a local copy
            // would be thrown away. Only assign when something actually changed, to
            // avoid dirtying the NetworkVariable every repaint.
            Stats edited = stats.basicStats.Value;
            EditorGUI.BeginChangeCheck();
            DrawStats(ref edited);
            if (EditorGUI.EndChangeCheck())
                stats.basicStats.Value = edited;
        }
        else
        {
            DrawStatsReadOnly(stats.basicStats.Value);
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Modified Stats", EditorStyles.boldLabel);
        DrawStatsReadOnly(stats.modifiedStats.Value);

        if (!editable)
            EditorGUILayout.HelpBox(
                "Read-only: this client is not the server.", MessageType.Info);
        else if (Application.isPlaying)
            EditorGUILayout.HelpBox(
                "Basic Stats is a NetworkVariable. Changes made here replicate to peers.",
                MessageType.None);

        serializedObject.ApplyModifiedProperties();
    }

    static void DrawStats(ref Stats s)
    {
        s.speed = EditorGUILayout.FloatField("Speed", s.speed);
        s.canFly = EditorGUILayout.Toggle("Can Fly", s.canFly);
        s.AttackDamage = EditorGUILayout.IntField("Attack Damage", s.AttackDamage);
        s.AttackSpeed = EditorGUILayout.FloatField("Attack Speed", s.AttackSpeed);
        s.AttackRange = EditorGUILayout.FloatField("Attack Range", s.AttackRange);
        s.MaxHP = EditorGUILayout.FloatField("Max HP", s.MaxHP);
    }

    static void DrawStatsReadOnly(Stats s)
    {
        using (new EditorGUI.DisabledScope(true))
        {
            DrawStats(ref s);
        }
    }
}
