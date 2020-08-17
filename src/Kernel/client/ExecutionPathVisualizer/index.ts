import { formatInputs } from './formatters/inputFormatter';
import { formatGates } from './formatters/gateFormatter';
import { formatRegisters } from './formatters/registerFormatter';
import { processOperations } from './process';
import { Circuit } from './circuit';
import { Metadata } from './metadata';
import { GateType } from './constants';
import { StyleConfig, style, STYLES } from './styles';
import { createUUID } from './utils';

/**
 * Custom JavaScript code to be injected into visualization HTML string.
 * Handles interactive elements, such as classically-controlled operations.
 */
const script = `
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
`;

/**
 * Contains all metadata required to generate final output.
 */
class ComposedSqore {
    width: number;
    height: number;
    script: string;
    style: StyleConfig;
    elements: string[];

    /**
     * Initializes `ComposedSqore` with metadata required for visualization.
     *
     * @param width Width of SVG element.
     * @param height Height of SVG element.
     * @param script Script for interactivity.
     * @param style Visualization style.
     * @param elements SVG elements the make up circuit visualization.
     */
    constructor(width: number, height: number, script: string, style: StyleConfig, elements: string[]) {
        this.width = width;
        this.height = height;
        this.script = script;
        this.style = style;
        this.elements = elements;
    }

    /**
     * Generates visualization as an SVG string and optionally injects the custom script into the browser.
     *
     * @param injectScript Injects custom script into document manually. This is used when the visualization
     *                     is injected into the document via `innerHtml`.
     *
     * @returns SVG representation of circuit visualization.
     */
    asSvg(injectScript = false): string {
        const uuid: string = createUUID();
        let script = '';

        // Insert script into browser for interactivity
        if (injectScript) {
            const s = document.createElement('script');
            s.type = 'text/javascript';
            s.appendChild(document.createTextNode(this.script));
            document.documentElement.appendChild(s);
        } else {
            script = `<script type="text/JavaScript">${this.script}</script>`;
        }

        return `<svg xmlns="http://www.w3.org/2000/svg" version="1.1" id="${uuid}" width="${this.width}" height="${
            this.height
        }">
    ${script}
    ${style(this.style)}
    ${this.elements.join('\n')}
</svg>`;
    }

    /**
     * Generates visualization as an HTML string and optionally injects the custom script into the browser.
     *
     * @param injectScript Injects custom script into document manually. This is used when the visualization
     *                     is injected into the document via `innerHtml`.
     *
     * @returns HTML representation of circuit visualization.
     */
    asHtml(injectScript = false): string {
        const svg: string = this.asSvg(injectScript);
        return `<html>
    ${svg}
</html>`;
    }
}

/**
 * Entrypoint class for rendering circuit visualizations.
 */
class Sqore {
    style: StyleConfig;

    /**
     * Initializes Sqore object with custom styles.
     *
     * @param style Custom styles for visualization.
     */
    constructor(style: StyleConfig = {}) {
        this.style = style;
    }

    /**
     * Sets custom style for visualization.
     *
     * @param style Custom `StyleConfig` for visualization.
     */
    stylize(style: StyleConfig | string = {}): Sqore {
        if (typeof style === 'string' || style instanceof String) {
            const styleName: string = style as string;
            if (!STYLES.hasOwnProperty(styleName)) {
                console.error(`No style ${styleName} found in STYLES.`);
                return this;
            }
            style = STYLES[styleName] || {};
        }
        this.style = style;
        return this;
    }

    /**
     * Generates the components required for visualization.
     *
     * @param circuit Circuit to be visualized.
     *
     * @returns `ComposedSqore` object containing metadata for visualization.
     */
    compose(circuit: Circuit): ComposedSqore {
        const { qubits, operations } = circuit;
        const { qubitWires, registers, svgHeight } = formatInputs(qubits);
        const { metadataList, svgWidth } = processOperations(operations, registers);
        const formattedGates: string = formatGates(metadataList);
        const measureGates: Metadata[] = metadataList.filter(({ type }) => type === GateType.Measure);
        const formattedRegs: string = formatRegisters(registers, measureGates, svgWidth);

        const composition: ComposedSqore = new ComposedSqore(svgWidth, svgHeight, script, this.style, [
            qubitWires,
            formattedRegs,
            formattedGates,
        ]);
        return composition;
    }
}

/** Exported function for creating a new Sqore class. */
export const createSqore = (): Sqore => new Sqore();
export { STYLES } from './styles';

// Export types
export type { Circuit, StyleConfig, Sqore, ComposedSqore };
export type { Qubit, Operation } from './circuit';
