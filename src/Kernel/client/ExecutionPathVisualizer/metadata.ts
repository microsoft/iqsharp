import { GateType } from './constants';
import { DataAttributes } from './circuit';

/**
 * Metadata used to store information pertaining to a given
 * operation for rendering its corresponding SVG.
 */
export interface Metadata {
    /** Gate type. */
    type: GateType;
    /** Centre x coord for gate position. */
    x: number;
    /** Array of y coords of control registers. */
    controlsY: number[];
    /** Array of y coords of target registers.
     *  For `GateType.Unitary` or `GateType.ControlledUnitary`, this is an array of groups of
     *  y coords, where each group represents a unitary box to be rendered separately.
     */
    targetsY: (number | number[])[];
    /** Gate label. */
    label: string;
    /** Gate arguments as string. */
    displayArgs?: string;
    /** Gate width. */
    width: number;
    /** Classically-controlled gates.
     *  - conditionalChildren[0]: metadata of gates rendered when classical control bit is 0.
     *  - conditionalChildren[1]: metadata of gates rendered when classical control bit is 1.
     */
    conditionalChildren?: [Metadata[], Metadata[]];
    /** HTML element class for interactivity. */
    htmlClass?: string;
    /** Custom data attributes to attach to gate element. */
    dataAttributes?: DataAttributes;
}
