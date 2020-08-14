/**
 * Enum for the various gate operations handled.
 */
export enum GateType {
    /** Measurement gate. */
    Measure,
    /** CNOT gate. */
    Cnot,
    /** SWAP gate. */
    Swap,
    /** Single/multi qubit unitary gate. */
    Unitary,
    /** Single/multi controlled unitary gate. */
    ControlledUnitary,
    /** Nested group of classically-controlled gates. */
    ClassicalControlled,
    /** Invalid gate. */
    Invalid,
}

// Display attributes
/** Left padding of SVG. */
export const leftPadding = 20;
/** x coordinate for first operation on each register. */
export const startX = 80;
/** y coordinate of first register. */
export const startY = 40;
/** Minimum width of each gate. */
export const minGateWidth = 40;
/** Height of each gate. */
export const gateHeight = 40;
/** Padding on each side of gate. */
export const gatePadding = 10;
/** Padding on each side of gate label. */
export const labelPadding = 10;
/** Height between each qubit register. */
export const registerHeight: number = gateHeight + gatePadding * 2;
/** Height between classical registers. */
export const classicalRegHeight: number = gateHeight;
/** Classical box inner padding. */
export const classicalBoxPadding = 15;
/** Additional offset for control button. */
export const controlBtnOffset = 40;
/** Control button radius. */
export const controlBtnRadius = 15;
/** Default font size for gate labels. */
export const labelFontSize = 14;
/** Default font size for gate arguments. */
export const argsFontSize = 12;
/** Starting x coord for each register wire. */
export const regLineStart = 40;
