// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// Extensions for C# Types.
    /// </summary>
    public static partial class Extensions
    {
        private static Type WithoutTypeParameters(this Type type) =>
            type.IsGenericType ? type.GetGenericTypeDefinition() : type;

        internal static bool IsSubclassOfGenericType(this Type subType, Type baseType)
        {
            // Remove any type parameters of subType as applicable.
            subType = subType.WithoutTypeParameters();
            if (subType == baseType)
            {
                return true;
            }
            else
            {
                // Check the next level up in inheritance from subtype, if it
                // exists.
                return subType?.BaseType?.IsSubclassOfGenericType(baseType) ?? false;
            }
        }
    }
}