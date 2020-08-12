import { formatInputs } from "./formatters/inputFormatter";
import { formatGates } from "./formatters/gateFormatter";
import { formatRegisters } from "./formatters/registerFormatter";
import { processOperations } from "./process";
import { ExecutionPath } from "./executionPath";
import { Metadata } from "./metadata";
import { GateType } from "./constants";
import { StyleConfig, style } from "./styles";

/**
 * Custom JavaScript code to be injected into visualization HTML string.
 * Handles interactive elements, such as classically-controlled operations.
 */
const script: string = `
<script type="text/JavaScript">
    function toggleClassicalBtn(cls) {
        const textSvg = document.querySelector(\`.\${cls} text\`);
        const group = document.querySelector(\`.\${cls}-group\`);
        const currValue = textSvg.childNodes[0].nodeValue;
        const zeroGates = document.querySelector(\`.\${cls}-zero\`);
        const oneGates = document.querySelector(\`.\${cls}-one\`);
        switch (currValue) {
            case '?':
                textSvg.childNodes[0].nodeValue = '1';
                group.classList.remove('cls-control-unknown');
                group.classList.add('cls-control-one');
                break;
            case '1':
                textSvg.childNodes[0].nodeValue = '0';
                group.classList.remove('cls-control-one');
                group.classList.add('cls-control-zero');
                oneGates.classList.toggle('hidden');
                zeroGates.classList.toggle('hidden');
                break;
            case '0':
                textSvg.childNodes[0].nodeValue = '?';
                group.classList.remove('cls-control-zero');
                group.classList.add('cls-control-unknown');
                zeroGates.classList.toggle('hidden');
                oneGates.classList.toggle('hidden');
                break;
        }
    }
</script>
`;

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
