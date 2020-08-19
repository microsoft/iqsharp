import { Register } from './register';

/**
 * Circuit to be visualized.
 */
export interface Circuit {
    /** Array of qubit resources. */
    qubits: Qubit[];
    operations: Operation[];
}

/**
 * Represents a unique qubit resource bit.
 */
export interface Qubit {
    /** Qubit ID. */
    id: number;
    /** Number of classical registers attached to quantum register. */
    numChildren?: number;
}

export enum ConditionalRender {
    /** Always rendered. */
    Always,
    /** Render classically-controlled operation when measurement is a zero. */
    OnZero,
    /** Render classically-controlled operation when measurement is a one. */
    OnOne,
}

/**
 * Represents an operation and the registers it acts on.
 */
export interface Operation {
    /** Gate label. */
    gate: string;
    /** Formatted gate arguments to be displayed. */
    displayArgs?: string;
    /** Nested operations within this operation */
    children?: Operation[];
    /** Whether gate is a measurement operation. */
    isMeasurement: boolean;
    /** Whether gate is a conditional operation. */
    isConditional: boolean;
    /** Whether gate is a controlled operation. */
    isControlled: boolean;
    /** Whether gate is an adjoint operation. */
    isAdjoint: boolean;
    /** Control registers the gate acts on. */
    controls: Register[];
    /** Target registers the gate acts on. */
    targets: Register[];
    /** Specify conditions on when to render operation. */
    conditionalRender?: ConditionalRender;
    /** Custom user metadata. */
    customMetadata?: Record<string, unknown>;
}
