/**
 * Provides configuration for CSS styles of visualization.
 */
export interface StyleConfig {
    /** Single qubit unitary fill colour. */
    unitary?: string;
    /** Measurement gate fill colour. */
    measure?: string;
    /** Measurement unknown primary colour. */
    classicalUnknown?: string;
    /** Measurement zero primary colour. */
    classicalZero?: string;
    /** Measurement one primary colour. */
    classicalOne?: string;
}

const defaultStyleConfig: StyleConfig = {
    unitary: "#D9F1FA",
    measure: "#FFDE86",
    classicalUnknown: "#E5E5E5",
    classicalZero: "#C40000",
    classicalOne: "#4059BD",
};

/**
 * CSS style script to be injected into visualization HTML string.
 * 
 * @param userConfig Custom style configuration.
 * 
 * @returns String containing CSS style script.
 */
export const style = (userConfig: StyleConfig = {}) => {
    const config = { ...defaultStyleConfig, ...userConfig };
    return `
<style>
    .box {
        fill: white;
    }
    .gate-unitary {
        fill: ${config.unitary};
    }
    .gate-measure {
        fill: ${config.measure};
    }
    <!-- Classically controlled gates -->
    .hidden {
        display: none;
    }
    .cls-control-unknown {
        opacity: 0.25;
    }
    <!-- Gate outline -->
    .cls-control-one .cls-container,
    .cls-control-one .cls-line {
        stroke: ${config.classicalOne};
        stroke-width: 1.3;
    }
    .cls-control-zero .cls-container,
    .cls-control-zero .cls-line {
        stroke: ${config.classicalZero};
        stroke-width: 1.3;
    }
    <!-- Control button -->
    .cls-control-btn {
        cursor: pointer;
    }
    .cls-control-unknown .cls-control-btn {
        fill: ${config.classicalUnknown};
    }
    .cls-control-one .cls-control-btn {
        fill: ${config.classicalOne};
    }
    .cls-control-zero .cls-control-btn {
        fill: ${config.classicalZero};
    }
    <!-- Control button text -->
    .cls-control-unknown .cls-control-text {
        fill: black;
        stroke: none;
    }
    .cls-control-one .cls-control-text,
    .cls-control-zero .cls-control-text {
        fill: white;
        stroke: none;
    }
</style>
`
};

/**
 * Custom JavaScript code to be injected into visualization HTML string.
 * Handles interactive elements, such as classically-controlled operations.
 */
export const script = `
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
