// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Data;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.Simulation.Simulators;

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// Extensions for Q# components.
    /// </summary>
    public static partial class Extensions
    {
        public static readonly QsQualifiedName UNKNOWN_OPERATION = new QsQualifiedName("UNKNOWN", "UNKNOWN");

        /// <summary>
        /// Returns the source of the given QsNamespaceElement (either QsCallable or QsCustomTypes)
        /// </summary>
        public static string SourceFile(this QsNamespaceElement e) =>
            (e switch
            {
                QsNamespaceElement.QsCallable { Item: var callable } => callable.Source,
                QsNamespaceElement.QsCustomType { Item: var type } => type.Source,
                _ => null
            })?.AssemblyOrCodeFile ?? "[Unknown]";

        /// <summary>
        /// Returns the name of the given QsNamespaceElement (either QsCallable or QsCustomTypes)
        /// </summary>
        public static string ToFullName(this QsNamespaceElement e)
        {
            var name = UNKNOWN_OPERATION;

            if (e is QsNamespaceElement.QsCallable c)
            {
                name = c.Item.FullName;
            }
            else if (e is QsNamespaceElement.QsCustomType t)
            {
                name = t.Item.FullName;
            }

            return $"{name.Namespace}.{name.Name}";
        }

        /// <summary>
        ///      Formats a qualified name using dotted-name syntax.
        /// <summary>
        public static string ToFullName(this QsQualifiedName name) =>
            name?.Namespace + "." + name?.Name;

        /// <summary>
        /// Removes the given namespace, from the given name, iff name starts with namespace.
        /// </summary>
        public static string WithoutNamespace(this string name, string ns) => name.StartsWith(ns) ? name.Substring(ns.Length + 1) : name;

    }
}
