import { formatInputs } from "./formatters/inputFormatter";
import { formatGates } from "./formatters/gateFormatter";
import { formatRegisters } from "./formatters/registerFormatter";
import { processOperations } from "./process";
import { ExecutionPath } from "./executionPath";
import { Metadata } from "./metadata";
import { GateType } from "./constants";
import { StyleConfig, script, style } from "./styles";

/**
 * Converts JSON representing an execution path of a Q# program given by the simulator and returns its HTML visualization.
 * 
 * @param json            JSON received from simulator.
 * @param userStyleConfig Custom CSS style config for visualization.
 * 
 * @returns HTML representation of circuit.
 */
export const executionPathToHtml = (json: ExecutionPath, userStyleConfig?: StyleConfig): string => {
    const { qubits, operations } = json;
    const { qubitWires, registers, svgHeight } = formatInputs(qubits);
    const { metadataList, svgWidth } = processOperations(operations, registers);
    const formattedGates: string = formatGates(metadataList);
    const measureGates: Metadata[] = metadataList.filter(({ type }) => type === GateType.Measure);
    const formattedRegs: string = formatRegisters(registers, measureGates, svgWidth);
    return `<html>
    <svg xmlns="http://www.w3.org/2000/svg" version="1.1" width="${svgWidth}" height="${svgHeight}">
        ${script}
        ${style(userStyleConfig)}
        ${qubitWires}
        ${formattedRegs}
        ${formattedGates}
    </svg>
</html>`;
};
