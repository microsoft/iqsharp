import { formatInputs } from './formatters/inputFormatter';
import { formatGates } from './formatters/gateFormatter';
import { formatRegisters } from './formatters/registerFormatter';
import { processOperations } from './process';
import { Circuit } from './circuit';
import { Metadata } from './metadata';
import { GateType } from './constants';
import { StyleConfig, style } from './styles';
import { createUUID } from './utils';

/**
 * Custom JavaScript code to be injected into visualization HTML string.
 * Handles interactive elements, such as classically-controlled operations.
 */
const script = `
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
                group.classList.remove('classically-controlled-unknown');
                group.classList.add('classically-controlled-one');
                break;
            case '1':
                textSvg.childNodes[0].nodeValue = '0';
                group.classList.remove('classically-controlled-one');
                group.classList.add('classically-controlled-zero');
                oneGates.classList.toggle('hidden');
                zeroGates.classList.toggle('hidden');
                break;
            case '0':
                textSvg.childNodes[0].nodeValue = '?';
                group.classList.remove('classically-controlled-zero');
                group.classList.add('classically-controlled-unknown');
                zeroGates.classList.toggle('hidden');
                oneGates.classList.toggle('hidden');
                break;
        }
    }
</script>
`;

/**
 * Generates the SVG visualization of the given circuit.
 *
 * @param circuit         Circuit to be visualized.
 * @param userStyleConfig Custom CSS style config for visualization.
 *
 * @returns SVG representation of circuit.
 */
export const circuitToSvg = (circuit: Circuit, userStyleConfig?: StyleConfig): string => {
    const { qubits, operations } = circuit;
    const { qubitWires, registers, svgHeight } = formatInputs(qubits);
    const { metadataList, svgWidth } = processOperations(operations, registers);
    const formattedGates: string = formatGates(metadataList);
    const measureGates: Metadata[] = metadataList.filter(({ type }) => type === GateType.Measure);
    const formattedRegs: string = formatRegisters(registers, measureGates, svgWidth);
    const uuid: string = createUUID();

    return `<svg xmlns="http://www.w3.org/2000/svg" version="1.1" id="${uuid}" width="${svgWidth}" height="${svgHeight}">
    ${script}
    ${style(userStyleConfig)}
    ${qubitWires}
    ${formattedRegs}
    ${formattedGates}
</svg>`;
};

/**
 * Generates the HTML visualization of the given circuit.
 *
 * @param circuit         Circuit to be visualized.
 * @param userStyleConfig Custom CSS style config for visualization.
 *
 * @returns HTML representation of circuit.
 */
export const circuitToHtml = (circuit: Circuit, userStyleConfig?: StyleConfig): string =>
    `<html>
    ${circuitToSvg(circuit, userStyleConfig)}
</html>`;

// Export types
export type { Circuit, StyleConfig };
export { STYLES } from './styles';
