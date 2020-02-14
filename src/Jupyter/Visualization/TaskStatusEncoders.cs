// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using Microsoft.Jupyter.Core;

namespace Microsoft.Quantum.IQSharp.Jupyter
{

    public class TaskStatus
    {
        public DateTime LastUpdated { get; private set; } = DateTime.Now;
        public bool IsCompleted { get; set; } = false;
        private string _description = "";
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                LastUpdated = DateTime.Now;
            }
        }

        private string? _subtask = null;
        public string? Subtask
        {
            get => _subtask;
            set
            {
                _subtask = value;
                LastUpdated = DateTime.Now;
            }
        }

        public TaskStatus(string description)
        {
            Description = description;
        }
    }

    /// <summary>
    ///     Encodes the status of IQ# tasks into plain text.
    /// </summary>
    public class TaskStatusToTextEncoder : IResultEncoder
    {
        public string MimeType => MimeTypes.PlainText;

        public EncodedData? Encode(object displayable)
        {
            if (displayable is TaskStatus status)
            {
                var sinceLastUpdate = DateTime.Now - status.LastUpdated;
                var dots = status.IsCompleted
                           ? "!"
                           : new String('.', 1 + (sinceLastUpdate.Seconds % 3));
                var sep = String.IsNullOrEmpty(status.Subtask)
                          ? "" : ": ";
                return $"{status.Description}{sep}{status.Subtask}{dots}".ToEncodedData();
            }
            else return null;
        }
    }
}
