// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    /// <summary>
    ///     Represents different styles for displaying the Q# execution path
    ///     visualization as HTML.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TraceVisualizationStyle
    {
        /// <summary>
        ///     Default style with coloured gates.
        /// </summary>
        Default,
        /// <summary>
        ///     Black and white style.
        /// </summary>
        BlackAndWhite,
        /// <summary> 
        ///     Inverted black and white style (for black backgrounds).
        /// </summary>
        Inverted
    }
}