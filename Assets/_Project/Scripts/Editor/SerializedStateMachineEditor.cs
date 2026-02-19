#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityPatterns.FiniteStateMachine;

[CustomEditor(typeof(SerializedStateMachine))]
public class SerializedStateMachineEditor : Editor
{
    private static readonly List<Type> StateTypes = TypeCache.GetTypesDerivedFrom<IState>()
        .Where(t => !t.IsAbstract && !t.IsInterface && !t.ContainsGenericParameters)
        .OrderBy(t => t.FullName)
        .ToList();

    private static readonly string[] PlayerDefaultStateTypeNames =
    {
        "PlayerStateDefault",
        "PlayerStateBlocking",
        "PlayerStateAttacking",
        "PlayerStateComboing",
        "PlayerStateComboEnding",
        "PlayerStateDodging",
        "PlayerStateJumping",
        "PlayerStateStaggered",
        "PlayerStateDead"
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawScriptField();

        SerializedProperty defaultState = serializedObject.FindProperty("defaultState");
        SerializedProperty states = serializedObject.FindProperty("states");
        SerializedProperty transitions = serializedObject.FindProperty("transitions");

        EditorGUILayout.Space();
        DrawManagedReferenceSelector(defaultState, "Default State", StateTypes);

        EditorGUILayout.Space();
        DrawStatesList(states);

        EditorGUILayout.Space();
        DrawPresetButtons(defaultState, states);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(transitions, includeChildren: true);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawScriptField()
    {
        using (new EditorGUI.DisabledScope(true))
        {
            MonoScript script = MonoScript.FromScriptableObject((ScriptableObject)target);
            EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
        }
    }

    private static void DrawPresetButtons(SerializedProperty defaultState, SerializedProperty states)
    {
        EditorGUILayout.LabelField("Quick Setup", EditorStyles.boldLabel);
        using (new EditorGUI.IndentLevelScope())
        {
            if (GUILayout.Button("Populate Player Default States"))
            {
                ApplyStatePreset(defaultState, states, PlayerDefaultStateTypeNames, "PlayerStateDefault");
            }

            if (GUILayout.Button("Clear All States"))
            {
                defaultState.managedReferenceValue = null;
                states.arraySize = 0;
            }
        }
    }

    private static void ApplyStatePreset(
        SerializedProperty defaultState,
        SerializedProperty states,
        IReadOnlyList<string> stateTypeNames,
        string defaultStateTypeName)
    {
        var presetTypes = stateTypeNames
            .Select(FindStateTypeByName)
            .Where(t => t != null)
            .Distinct()
            .ToList();

        if (presetTypes.Count == 0)
        {
            Debug.LogWarning("SerializedStateMachineEditor: no matching state types were found for the preset.");
            return;
        }

        states.arraySize = presetTypes.Count;
        for (int i = 0; i < presetTypes.Count; i++)
        {
            states.GetArrayElementAtIndex(i).managedReferenceValue = Activator.CreateInstance(presetTypes[i]);
        }

        Type defaultType = FindStateTypeByName(defaultStateTypeName) ?? presetTypes[0];
        defaultState.managedReferenceValue = Activator.CreateInstance(defaultType);
    }

    private static Type FindStateTypeByName(string typeName)
    {
        return StateTypes.FirstOrDefault(t => t.Name == typeName);
    }

    private void DrawStatesList(SerializedProperty states)
    {
        EditorGUILayout.LabelField("States", EditorStyles.boldLabel);

        using (new EditorGUI.IndentLevelScope())
        {
            for (int i = 0; i < states.arraySize; i++)
            {
                SerializedProperty stateProp = states.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                DrawManagedReferenceSelector(stateProp, $"Element {i}", StateTypes);

                if (GUILayout.Button("-", GUILayout.Width(24)))
                {
                    states.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ Add State"))
            {
                states.InsertArrayElementAtIndex(states.arraySize);
                SerializedProperty newState = states.GetArrayElementAtIndex(states.arraySize - 1);
                newState.managedReferenceValue = CreateDefaultStateInstance();
            }
        }
    }

    private static object CreateDefaultStateInstance()
    {
        if (StateTypes.Count == 0)
        {
            return null;
        }

        return Activator.CreateInstance(StateTypes[0]);
    }

    private static void DrawManagedReferenceSelector(
        SerializedProperty property,
        string label,
        IReadOnlyList<Type> candidateTypes)
    {
        EditorGUILayout.BeginVertical();

        Type currentType = GetManagedReferenceType(property);
        int currentIndex = currentType == null ? 0 : candidateTypes.IndexOf(currentType) + 1;

        string[] options = new string[candidateTypes.Count + 1];
        options[0] = "None";
        for (int i = 0; i < candidateTypes.Count; i++)
        {
            options[i + 1] = candidateTypes[i].Name;
        }

        int newIndex = EditorGUILayout.Popup(label, currentIndex, options);
        if (newIndex != currentIndex)
        {
            property.managedReferenceValue = newIndex == 0
                ? null
                : Activator.CreateInstance(candidateTypes[newIndex - 1]);
        }

        if (property.managedReferenceValue != null)
        {
            EditorGUILayout.PropertyField(property, GUIContent.none, includeChildren: true);
        }

        EditorGUILayout.EndVertical();
    }

    private static Type GetManagedReferenceType(SerializedProperty property)
    {
        if (property == null || string.IsNullOrEmpty(property.managedReferenceFullTypename))
        {
            return null;
        }

        string[] split = property.managedReferenceFullTypename.Split(' ');
        if (split.Length != 2)
        {
            return null;
        }

        string assemblyName = split[0];
        string typeName = split[1];
        return Type.GetType($"{typeName}, {assemblyName}");
    }
}
#endif
