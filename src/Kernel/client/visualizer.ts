// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import { createExecutionPathVisualizer, Circuit, StyleConfig, Operation } from "./ExecutionPathVisualizer";

type GateRegistry = {
    [id: string]: Operation,
};

// Flag to ensure that we only inject custom JS into browser once
let isScriptInjected = false;

export class Visualizer {
    userStyleConfig: StyleConfig = {};
    displayedCircuit: Circuit = null;
    id: string = "";
    gateRegistry: GateRegistry = {};

    constructor(id: string, userStyleConfig: StyleConfig) {
        this.id = id;
        this.userStyleConfig = userStyleConfig;
    }

    visualize(circuit: Circuit, renderDepth: number = 0) {
        // Assign unique IDs to each operation
        circuit.operations.forEach((op, i) => this.fillGateRegistry(op, i.toString()));

        // Render operations at starting at given depth
        circuit.operations = this.selectOpsAtDepth(circuit.operations, renderDepth);
        this.renderCircuit(circuit);
    }

    // Depth-first traversal to assign unique ID to operation.
    // The operation is assigned the id `id` and its `i`th child is recursively given
    // the id `${id}-${i}`.
    fillGateRegistry(operation: Operation, id: string) {
        if (operation.dataAttributes == null) operation.dataAttributes = {};
        operation.dataAttributes["id"] = id;
        this.gateRegistry[id] = operation;
        operation.children?.forEach((childOp, i) => this.fillGateRegistry(childOp, `${id}-${i}`));
    }

    private selectOpsAtDepth(operations: Operation[], renderDepth: number): Operation[] {
        if (renderDepth < 0) throw new Error(`Invalid renderDepth of ${renderDepth}. Needs to be >= 0.`);
        if (renderDepth === 0) return operations;
        return operations.map(op => (op.children != null)
            ? this.selectOpsAtDepth(op.children, renderDepth - 1)
            : op
        ).flat();
    }

    private renderCircuit(circuit: Circuit): void {
        // Generate HTML visualization
        const html: string = createExecutionPathVisualizer()
            .stylize(this.userStyleConfig)
            .compose(circuit)
            .asHtml(!isScriptInjected);
        
        isScriptInjected = true;
    
        // Inject into div
        const container: HTMLElement = document.getElementById(this.id);
        if (container == null) throw new Error(`Div with ID ${this.id} not found.`);
        container.innerHTML = html;
        this.displayedCircuit = circuit;
    
        // Handle click events
        this.addGateClickHandlers();
    }

    private addGateClickHandlers(): void {
        document.querySelectorAll('.gate').forEach((gate) => {
            // Zoom in on clicked gate
            gate.addEventListener('click', (ev: MouseEvent) => {
                const { id }: { id: string } = JSON.parse(gate.getAttribute('data-metadata'));
                if (ev.ctrlKey) this.collapseOperation(this.displayedCircuit, id);
                else this.expandOperation(this.displayedCircuit, id);
            });
        });
    }

    private expandOperation(circuit: Circuit, id: string): void {
        let operations: Operation[] = circuit.operations;
        operations = operations.map(op => {
            if (op.dataAttributes == null) return op;
            const opId: string = op.dataAttributes["id"];
            if (opId === id && op.children != null) return op.children;
            return op;
        }).flat();
        circuit.operations = operations;
    
        this.renderCircuit(circuit);
    }

    private collapseOperation(circuit: Circuit, id: string): void {
        // Cannot collapse top-level operation
        if (id === "0") return;
        const parentId: string = id.match(/(.*)-\d/)[1];
        circuit.operations = circuit.operations
            .map(op => {
                if (op.dataAttributes == null) return op;
                const opId: string = op.dataAttributes["id"];
                // Replace with parent operation
                if (opId === id) return this.gateRegistry[parentId];
                // If operation is a descendant, don't render
                if (opId.startsWith(parentId)) return null;
                return op;
            })
            .filter(op => op != null);
        this.renderCircuit(circuit);
    }
}
