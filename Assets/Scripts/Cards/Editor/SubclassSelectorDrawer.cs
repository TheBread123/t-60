#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using T60.Cards.Attributes;

namespace T60.Cards.Editor
{
    [CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
    public class SubclassSelectorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            // Determine the base element type (e.g. Effect)
            Type baseType = fieldInfo.FieldType;
            if (baseType.IsArray)
            {
                baseType = baseType.GetElementType();
            }
            else if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(List<>))
            {
                baseType = baseType.GetGenericArguments()[0];
            }

            // Get current type name from managed reference
            string fullTypeName = property.managedReferenceFullTypename;
            string currentTypeName = "Null (None)";

            if (!string.IsNullOrEmpty(fullTypeName))
            {
                string[] split = fullTypeName.Split(' ');
                if (split.Length > 1)
                {
                    Type type = Type.GetType($"{split[1]}, {split[0]}");
                    if (type != null)
                    {
                        currentTypeName = type.Name;
                    }
                }
            }

            // Calculate rect for the type dropdown button on the single-line header
            float labelWidth = EditorGUIUtility.labelWidth;
            float buttonWidth = Mathf.Max(120f, position.width - labelWidth);
            Rect popupRect = new Rect(position.x + labelWidth, position.y, buttonWidth, EditorGUIUtility.singleLineHeight);

            // Draw and process dropdown button BEFORE PropertyField so mouse clicks are captured
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            if (GUI.Button(popupRect, $"Type: {currentTypeName}", EditorStyles.miniPullDown))
            {
                GenericMenu menu = new GenericMenu();

                menu.AddItem(new GUIContent("Null (None)"), string.IsNullOrEmpty(fullTypeName), () =>
                {
                    property.managedReferenceValue = null;
                    property.serializedObject.ApplyModifiedProperties();
                    property.serializedObject.Update();
                });

                var derivedTypes = TypeCache.GetTypesDerivedFrom(baseType)
                    .Where(t => !t.IsAbstract && !t.IsInterface && t.GetConstructor(Type.EmptyTypes) != null);

                foreach (Type type in derivedTypes)
                {
                    bool isSelected = currentTypeName == type.Name;
                    Type targetType = type;
                    menu.AddItem(new GUIContent(targetType.Name), isSelected, () =>
                    {
                        property.managedReferenceValue = Activator.CreateInstance(targetType);
                        property.serializedObject.ApplyModifiedProperties();
                        property.serializedObject.Update();
                    });
                }

                menu.ShowAsContext();
            }

            // Draw PropertyField for foldout header and child properties
            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
#endif
