// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using Microsoft.Jupyter.Core;

namespace Microsoft.Quantum.IQSharp.Jupyter
{

    /// <summary>
    ///     Represents the status of a task, including its description,
    ///     completion status, and possibly a subtask description.
    /// </summary>
    public class TaskStatus
    {
        /// <summary>
        ///     The last time at which the status was updated.
        /// </summary>
        public DateTime LastUpdated { get; private set; } = DateTime.Now;

        /// <summary>
        ///     Whether the status represents a completed task.
        /// </summary>
        public bool IsCompleted { get; set; } = false;
        private string _description = "";

        /// <summary>
        ///     A description of the task represented by this object.
        /// </summary>
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

        /// <summary>
        ///     A description of the current subtask for this task (e.g. if
        ///     the task represents downloading multiple files, which file is
        ///     currently being downloaded). If there is no applicable subtask,
        ///     this property should be <c>null</c>.
        /// </summary>
        public string? Subtask
        {
            get => _subtask;
            set
            {
                _subtask = value;
                LastUpdated = DateTime.Now;
            }
        }

        /// <summary>
        ///     Constructs a new status given the description of a task.
        /// </summary>
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
        /// <summary>
        ///     The MIME type returned by this encoder.
        /// </summary>
        public string MimeType => MimeTypes.PlainText;

        /// <summary>
        ///     Checks if a displayable object is a task status, and if so,
        ///     returns its encoding into plain text.
        /// </summary>
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
