// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <summary>
    /// This class is used to set immutable and unitializable properties present
    /// in the Azure Quantum SDK data model.
    /// The data model is auto-generated and the generated classes have immutable
    /// properties (with only getters), and sometimes internal constructors and
    /// no public constructors.
    /// </summary>
    internal static class MockHelper
    {
        private static string FirstCharToLowerCase(this string str)
        {
            if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
                return str;

            return char.ToLower(str[0]) + str.Substring(1);
        }

        public static void SetReadOnlyProperty<T>(this T obj, string propertyName, object? value)
        {
            var property = typeof(T).GetProperty(propertyName);

            // Try to find/use private setter first
            var setMethod = property.GetSetMethod(true);
            if (setMethod != null)
            {
                setMethod.Invoke(obj, new object?[] { value });
            }

            // Attempt to find internal fields in this order
            // 1) compiler-generated field: <PropertyName>k__BackingField
            // 2) user-defined field: _PropertyName
            // 3) user-defined field: _propertyName
            // 4) user-defined field: propertyName
            var field = typeof(T).GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                        ?? typeof(T).GetField($"_{propertyName}", BindingFlags.Instance | BindingFlags.NonPublic)
                        ?? typeof(T).GetField($"_{propertyName.FirstCharToLowerCase()}", BindingFlags.Instance | BindingFlags.NonPublic)
                        ?? typeof(T).GetField($"{propertyName.FirstCharToLowerCase()}", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(obj, value);
                return;
            }
            var fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

            throw new NotSupportedException($"No setter or internal field found for property {propertyName} in {typeof(T).FullName}.");
        }

        public static T CreateWithNonPublicConstructor<T>(params object?[] parameters) =>
            typeof(T).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                     .Where((c) => c.GetParameters().Length == parameters.Length)
                     .Select((c) => (T)(c.Invoke(parameters)))
                     .FirstOrDefault() ?? throw new NotSupportedException($"No constructor found for type {typeof(T).FullName} accepting {parameters.Length} parameters.");
    }
}