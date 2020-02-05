// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.Simulation.Simulators;

namespace Microsoft.Quantum.IQSharp
{
    public class ResourcesEstimatorToHtmlResultEncoder : IResultEncoder
    {
        public string MimeType => MimeTypes.Html;

        public EncodedData? Encode(object displayable)
        {
            if (displayable is ResourcesEstimator estimator)
            {
                return $@"
                        <table>
                            <thead>
                                <th>Metric</th>
                                <th>Sum</th>
                            </thead>

                            <tbody>
                                {String.Join(
                                    "\n",
                                    estimator.AsDictionary()
                                        .OrderBy(item => item.Key)
                                        .Select(item => $@"
                                            <tr>
                                                <td>{item.Key}</td>
                                                <td>{item.Value}</td>
                                            </tr>
                                        ")
                                )}
                            </tbody>
                        </table>
                    ".ToEncodedData();
            }
            else return null;
        }
    }

}
