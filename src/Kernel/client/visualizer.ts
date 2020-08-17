// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import { createExecutionPathVisualizer, Circuit, StyleConfig, STYLES, Operation } from "./ExecutionPathVisualizer";

export const renderExecutionPath = (executionPath: Circuit, id: string, renderDepth: number, style: string): void => {
    const userStyleConfig: StyleConfig = STYLES[style] || {};

    // Render operations at given depth
    executionPath.operations = _selectOpsAtDepth(executionPath.operations, renderDepth);

    // Generate HTML visualization
    const html: string = createExecutionPathVisualizer()
        .stylize(userStyleConfig)
        .compose(executionPath)
        // We pass in `true` to inject custom JS into browser to enable interactive elements
        .asHtml(true);

    // Inject into div
    const container: HTMLElement = document.getElementById(id);
    if (container == null) throw new Error(`Div with ID ${id} not found.`);
    container.innerHTML = html;
};

const _selectOpsAtDepth = (operations: Operation[], renderDepth: number): Operation[] => {
    if (renderDepth < 1) throw new Error(`Invalid renderDepth of ${renderDepth}. Needs to be >= 1.`);
    if (renderDepth === 1) return operations;
    return operations.map(op => (op.children != null)
        ? _selectOpsAtDepth(op.children, renderDepth - 1)
        : op
    ).flat();
};
