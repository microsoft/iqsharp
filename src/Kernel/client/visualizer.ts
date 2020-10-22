// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { createExecutionPathVisualizer, Circuit, StyleConfig, Operation, ConditionalRender } from './ExecutionPathVisualizer';

type GateRegistry = {
    [id: string]: Operation;
};

// Flag to ensure that we only inject custom JS into browser once
let isScriptInjected = false;

// Event handler to visually signal to user that the gate can be zoomed out on ctrl-click
window.addEventListener('keydown', (ev) => {
    if (ev.key !== 'Control') return;
    addCtrlClickStyles();
});

// Event handler to visually signal to user that the gate can be zoomed in on click
window.addEventListener('keyup', (ev) => {
    if (ev.key !== 'Control') return;
    addDefaultStyles();
});

// Adds default cursor styles (i.e. zoom in on hover)
const addDefaultStyles = () => {
    document.querySelectorAll('[data-zoom-out="true"]:not([data-zoom-in="true"]),[data-expanded="true"]').forEach((el: Element) => {
        (el as HTMLElement).style.cursor = 'default';
    });
    document.querySelectorAll('[data-zoom-in="true"]:not([data-expanded="true"])').forEach((el: Element) => {
        (el as HTMLElement).style.cursor = 'zoom-in';
    });
};

// Adds cursor styles on control-click (i.e. zoom out on hover)
const addCtrlClickStyles = () => {
    document.querySelectorAll('[data-zoom-out="true"]:not([data-expanded="true"])').forEach((el: Element) => {
        (el as HTMLElement).style.cursor = 'zoom-out';
    });
};

export class Visualizer {
    userStyleConfig: StyleConfig = {};
    displayedCircuit: Circuit | null = null;
    id = '';
    gateRegistry: GateRegistry = {};

    constructor(id: string, userStyleConfig: StyleConfig) {
        this.id = id;
        this.userStyleConfig = userStyleConfig;
    }

    visualize(circuit: Circuit, renderDepth = 0): void {
        // Assign unique IDs to each operation
        circuit.operations.forEach((op, i) => this.fillGateRegistry(op, i.toString()));

        // Render operations at starting at given depth
        circuit.operations = this.selectOpsAtDepth(circuit.operations, renderDepth);
        this.renderCircuit(circuit);
    }

    // Depth-first traversal to assign unique ID to operation.
    // The operation is assigned the id `id` and its `i`th child is recursively given
    // the id `${id}-${i}`.
    fillGateRegistry(operation: Operation, id: string): void {
        if (operation.dataAttributes == null) operation.dataAttributes = {};
        operation.dataAttributes['id'] = id;
        operation.dataAttributes['zoom-out'] = 'false';
        this.gateRegistry[id] = operation;
        operation.children?.forEach((childOp, i) => {
            this.fillGateRegistry(childOp, `${id}-${i}`);
            if (childOp.dataAttributes == null) childOp.dataAttributes = {};
            childOp.dataAttributes['zoom-out'] = 'true';
        });
        operation.dataAttributes['zoom-in'] = (operation.children != null).toString();
    }

    private selectOpsAtDepth(operations: Operation[], renderDepth: number): Operation[] {
        if (renderDepth < 0) throw new Error(`Invalid renderDepth of ${renderDepth}. Needs to be >= 0.`);
        if (renderDepth === 0) return operations;
        return operations
            .map((op) => (op.children != null ? this.selectOpsAtDepth(op.children, renderDepth - 1) : op))
            .flat();
    }

    private renderCircuit(circuit: Circuit): void {
        // Generate HTML visualization
        const html: string = createExecutionPathVisualizer()
            .stylize(this.userStyleConfig)
            .compose(circuit)
            .asHtml(!isScriptInjected);

        isScriptInjected = true;

        // Inject into div
        const container: HTMLElement | null = document.getElementById(this.id);
        if (container == null) throw new Error(`Div with ID ${this.id} not found.`);
        container.innerHTML = html;
        this.displayedCircuit = circuit;

        // Handle click events
        this.addGateClickHandlers();

        // Add styles
        addDefaultStyles();
        container.querySelector('svg').style.maxWidth = 'none';
    }

    private addGateClickHandlers(): void {
        document.querySelectorAll(`#${this.id} .gate`).forEach((gate) => {
            // Zoom in on clicked gate
            gate.addEventListener('click', (ev: Event) => {
                ev.stopPropagation();
                if (this.displayedCircuit == null) return;

                const id: string | null = gate.getAttribute('data-id');
                if (id == null) return;

                // Don't handle clicks on an expanded container
                const isExpanded = gate.getAttribute('data-expanded') === 'true';
                if (isExpanded) return;

                const canZoomIn = gate.getAttribute('data-zoom-in') === 'true';
                const canZoomOut = gate.getAttribute('data-zoom-out') === 'true';

                if ((ev as MouseEvent).ctrlKey && canZoomOut) {
                    const parentId: string = (id.match(/(.*)-\d/) || ['', ''])[1];
                    this.collapseOperation(this.displayedCircuit.operations, parentId);
                } else if (canZoomIn) {
                    this.expandOperation(this.displayedCircuit.operations, id);
                }

                this.renderCircuit(this.displayedCircuit);
            });
        });
    }

    private expandOperation(operations: Operation[], id: string): void {
        operations.forEach((op) => {
            if (op.conditionalRender === ConditionalRender.AsGroup) this.expandOperation(op.children || [], id);
            if (op.dataAttributes == null) return op;
            const opId: string = op.dataAttributes['id'];
            if (opId === id && op.children != null) {
                op.conditionalRender = ConditionalRender.AsGroup;
                op.dataAttributes['expanded'] = 'true';
            }
        });
    }

    private collapseOperation(operations: Operation[], parentId: string): void {
        // Cannot collapse top-level operation
        if (parentId === '0') return;
        operations.forEach((op) => {
            if (op.conditionalRender === ConditionalRender.AsGroup) this.collapseOperation(op.children || [], parentId);
            if (op.dataAttributes == null) return op;
            const opId: string = op.dataAttributes['id'];
            // Collapse parent gate and its children
            if (opId.startsWith(parentId)) op.conditionalRender = ConditionalRender.Always;
            // Allow parent gate to be interactive again
            if (opId === parentId) op.dataAttributes['expanded'] = 'false';
        });
    }
}
