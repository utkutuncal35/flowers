using UnityEditor;
using UnityEngine;

namespace Flowers.Editor
{
    /// <summary>
    /// Sets [SerializeField] private fields from editor scripts and fails loudly (and remembers it) when the
    /// field name is wrong or the value is missing, so Setup Scene can't silently leave a "None" behind.
    /// </summary>
    public static class Wiring
    {
        public static bool Failed { get; private set; }

        public static void Reset() => Failed = false;

        public static void Fail(string message)
        {
            Debug.LogError($"[Flowers] Setup: {message}");
            Failed = true;
        }

        public static void Wire(Object target, string fieldName, Object value)
        {
            if (value == null)
            {
                Fail($"nothing to assign to {target.GetType().Name}.{fieldName} (the value is null/destroyed).");
                return;
            }

            var property = FindProperty(target, fieldName, out var so);
            if (property == null) return;
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void WireArray(Object target, string fieldName, Object[] values)
        {
            var property = FindProperty(target, fieldName, out var so);
            if (property == null) return;
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty FindProperty(Object target, string fieldName, out SerializedObject so)
        {
            so = new SerializedObject(target);
            var property = so.FindProperty(fieldName);
            if (property == null) Fail($"{target.GetType().Name} has no serialized field '{fieldName}'.");
            return property;
        }
    }
}
