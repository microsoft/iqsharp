/**
 * Provides configuration for CSS styles of visualization.
 */
export interface StyleConfig {
    /** Line stroke style. */
    lineStroke?: string;
    /** Line width. */
    lineWidth?: number;
    /** Text colour. */
    textColour?: string;
    /** Single qubit unitary fill colour. */
    unitary?: string;
    /** Oplus circle fill colour. */
    oplus?: string;
    /** Measurement gate fill colour. */
    measure?: string;
    /** Measurement unknown primary colour. */
    classicalUnknown?: string;
    /** Measurement zero primary colour. */
    classicalZero?: string;
    /** Measurement one primary colour. */
    classicalOne?: string;
}

const defaultStyle: StyleConfig = {
    lineStroke: "#000000",
    lineWidth: 1,
    textColour: "#000000",
    unitary: "#D9F1FA",
    oplus: "#FFFFFF",
    measure: "#FFDE86",
    classicalUnknown: "#E5E5E5",
    classicalZero: "#C40000",
    classicalOne: "#4059BD",
};

const blackAndWhiteStyle: StyleConfig = {
    lineStroke: "#000000",
    lineWidth: 1,
    textColour: "#000000",
    unitary: "#FFFFFF",
    oplus: "#FFFFFF",
    measure: "#FFFFFF",
    classicalUnknown: "#FFFFFF",
    classicalZero: "#FFFFFF",
    classicalOne: "#FFFFFF",
};

const invertedStyle: StyleConfig = {
    lineStroke: "#FFFFFF",
    lineWidth: 1,
    textColour: "#FFFFFF",
    unitary: "#000000",
    oplus: "#000000",
    measure: "#000000",
    classicalUnknown: "#000000",
    classicalZero: "#000000",
    classicalOne: "#000000",
};

export const STYLES = {
    DEFAULT: defaultStyle,
    BLACK_AND_WHITE: blackAndWhiteStyle,
    INVERTED: invertedStyle,
};

/**
 * CSS style script to be injected into visualization HTML string.
 * 
 * @param customStyle Custom style configuration.
 * 
 * @returns String containing CSS style script.
 */
export const style = (customStyle: StyleConfig = {}) => {
    const styleConfig = { ...defaultStyle, ...customStyle };
    return `
<style>
    line,
    circle,
    rect {
        stroke: ${styleConfig.lineStroke};
        stroke-width: ${styleConfig.lineWidth};
    }
    text {
        color: ${styleConfig.textColour};
        dominant-baseline: middle;
        text-anchor: middle;
        font-family: Arial;
    }
    .control-dot {
        fill: ${styleConfig.lineStroke};
    }
    .oplus {
        fill: ${styleConfig.oplus};
    }
    .gate-unitary {
        fill: ${styleConfig.unitary};
    }
    .gate-measure {
        fill: ${styleConfig.measure};
    }
    .arc-measure {
        stroke: ${styleConfig.lineStroke};
        fill: none;
        stroke-width: ${styleConfig.lineWidth};
    }
    .register-classical {
        stroke-width: ${styleConfig.lineWidth / 2};
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
        stroke: ${styleConfig.classicalOne};
        stroke-width: ${styleConfig.lineWidth + 0.3};
    }
    .cls-control-zero .cls-container,
    .cls-control-zero .cls-line {
        stroke: ${styleConfig.classicalZero};
        stroke-width: ${styleConfig.lineWidth + 0.3};
    }
    <!-- Control button -->
    .cls-control-btn {
        cursor: pointer;
    }
    .cls-control-unknown .cls-control-btn {
        fill: ${styleConfig.classicalUnknown};
    }
    .cls-control-one .cls-control-btn {
        fill: ${styleConfig.classicalOne};
    }
    .cls-control-zero .cls-control-btn {
        fill: ${styleConfig.classicalZero};
    }
    <!-- Control button text -->
    .cls-control-unknown .cls-control-text {
        fill: ${styleConfig.textColour};
        stroke: none;
    }
    .cls-control-one .cls-control-text,
    .cls-control-zero .cls-control-text {
        color: white;
        stroke: none;
    }
</style>
`
};
