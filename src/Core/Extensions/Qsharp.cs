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
        public static string SourceFile(this QsNamespaceElement e)
        {
            if (e is QsNamespaceElement.QsCallable c)
            {
                return c.Item.SourceFile;
            }
            else if (e is QsNamespaceElement.QsCustomType t)
            {
                return t.Item.SourceFile;
            }

            return "[Unknown]";
        }

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

        /// <summary>
        /// Returns the ResourcesEstimator data as metric=value
        /// </summary>
        public static Dictionary<string, double> AsDictionary(this ResourcesEstimator qsim)
        {
            var counts = new Dictionary<string, double>();

            foreach (DataRow row in qsim.Data.Rows)
            {
                string metric = (string)row["Metric"];
                double data = (double)row["Sum"];

                counts[metric] = data;
            }

            return counts;
        }
    }
}
