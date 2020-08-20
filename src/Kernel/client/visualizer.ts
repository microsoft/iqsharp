// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { createExecutionPathVisualizer, Circuit, StyleConfig, Operation } from './ExecutionPathVisualizer';

type GateRegistry = {
    [id: string]: Operation;
};

// Flag to ensure that we only inject custom JS into browser once
let isScriptInjected = false;

// Event handler to visually signal to user that the gate can be zoomed out on ctrl-click
window.addEventListener('keydown', (ev) => {
    if (ev.key !== 'Control') return;
    document.querySelectorAll('[data-zoom-out="true"]').forEach((el: Element) => {
        (el as HTMLElement).style.cursor = 'zoom-out';
    });
});

// Event handler to visually signal to user that the gate can be zoomed in on click
window.addEventListener('keyup', (ev) => {
    if (ev.key !== 'Control') return;
    document.querySelectorAll('[data-zoom-out="true"]').forEach((el: Element) => {
        (el as HTMLElement).style.cursor = 'default';
    });
    document.querySelectorAll('[data-zoom-in="true"]').forEach((el: Element) => {
        (el as HTMLElement).style.cursor = 'zoom-in';
    });
});

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
        container.querySelector('svg').style.maxWidth = 'none';
        this.displayedCircuit = circuit;

        // Handle click events
        this.addGateClickHandlers();
    }

    private addGateClickHandlers(): void {
        document.querySelectorAll(`#${this.id} .gate`).forEach((gate) => {
            // Zoom in on clicked gate
            gate.addEventListener('click', (ev: Event) => {
                if (this.displayedCircuit == null) return;
                const id: string | null = gate.getAttribute('data-id');
                if (id == null) return;
                const canZoomIn = gate.getAttribute('data-zoom-in') === 'true';
                const canZoomOut = gate.getAttribute('data-zoom-out') === 'true';
                if ((ev as MouseEvent).ctrlKey && canZoomOut) this.collapseOperation(this.displayedCircuit, id);
                else if (canZoomIn) this.expandOperation(this.displayedCircuit, id);
            });
        });
    }

    private expandOperation(circuit: Circuit, id: string): void {
        let operations: Operation[] = circuit.operations;
        operations = operations
            .map((op) => {
                if (op.dataAttributes == null) return op;
                const opId: string = op.dataAttributes['id'];
                if (opId === id && op.children != null) return op.children;
                return op;
            })
            .flat();
        circuit.operations = operations;

        this.renderCircuit(circuit);
    }

    private collapseOperation(circuit: Circuit, id: string): void {
        // Cannot collapse top-level operation
        if (id === '0') return;
        const parentId: string = (id.match(/(.*)-\d/) || ['', ''])[1];
        circuit.operations = circuit.operations
            .map((op) => {
                if (op.dataAttributes == null) return op;
                const opId: string = op.dataAttributes['id'];
                // Replace with parent operation
                if (opId === id) return this.gateRegistry[parentId];
                // If operation is a descendant, don't render
                if (opId.startsWith(parentId)) return null;
                return op;
            })
            .filter((op): op is Operation => op != null);
        this.renderCircuit(circuit);
    }
}
