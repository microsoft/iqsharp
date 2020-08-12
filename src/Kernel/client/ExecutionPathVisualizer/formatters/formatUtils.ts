import { labelFontSize } from "../constants";

// Helper functions for basic SVG components

/**
 * Given an array of SVG elements, group them as an SVG group using the `<g>` tag.
 * 
 * @param svgElems Array of SVG elements.
 * 
 * @returns SVG string for grouped elements.
 */
export const group = (...svgElems: (string | string[])[]): string =>
    ['<g>', ...svgElems.flat(), '</g>'].join('\n');

/**
 * Generate the SVG representation of a control dot used for controlled operations.
 * 
 * @param x      x coord of circle.
 * @param y      y coord of circle.
 * @param radius Radius of circle.
 * 
 * @returns SVG string for control dot.
 */
export const controlDot = (x: number, y: number, radius: number = 5): string =>
    `<circle class="control-dot" cx="${x}" cy="${y}" r="${radius}"></circle>`;

/**
 * Generate an SVG line.
 * 
 * @param x1        x coord of starting point of line.
 * @param y1        y coord of starting point of line.
 * @param x2        x coord of ending point of line.
 * @param y2        y coord fo ending point of line.
 * @param className Class name of element.
 * 
 * @returns SVG string for line.
 */
export const line = (x1: number, y1: number, x2: number, y2: number, className?: string): string => {
    const clsString: string = (className != null) ? ` class="${className}"` : "";
    return `<line${clsString} x1="${x1}" x2="${x2}" y1="${y1}" y2="${y2}"></line>`;
};

/**
 * Generate the SVG representation of a unitary box that represents an arbitrary unitary operation.
 * 
 * @param x         x coord of box.
 * @param y         y coord of box.
 * @param width     Width of box.
 * @param height    Height of box.
 * @param className Class name of element.
 * 
 * @returns SVG string for unitary box.
 */
export const box = (x: number, y: number, width: number, height: number, className: string = "gate-unitary"): string =>
    `<rect class="${className}" x="${x}" y="${y}" width="${width}" height="${height}"></rect>`;

/**
 * Generate the SVG text element from a given text string.
 * 
 * @param text String to render as SVG text.
 * @param x    Middle x coord of text.
 * @param y    Middle y coord of text.
 * @param fs   Font size of text.
 * 
 * @returns SVG string for text.
 */
export const text = (text: string, x: number, y: number, fs: number = labelFontSize): string =>
    `<text font-size="${fs}" x="${x}" y="${y}">${text}</text>`;

/**
 * Generate the SVG representation of the arc used in the measurement box.
 * 
 * @param x  x coord of arc.
 * @param y  y coord of arc.
 * @param rx x radius of arc.
 * @param ry y radius of arc.
 * 
 * @returns SVG string for arc.
 */
export const arc = (x: number, y: number, rx: number, ry: number): string =>
    `<path class="arc-measure" d="M ${x + 2 * rx} ${y} A ${rx} ${ry} 0 0 0 ${x} ${y}"></path>`;

/**
 * Generate a dashed SVG line.
 * 
 * @param x1        x coord of starting point of line.
 * @param y1        y coord of starting point of line.
 * @param x2        x coord of ending point of line.
 * @param y2        y coord fo ending point of line.
 * @param className Class name of element.
 * 
 * @returns SVG string for dashed line.
 */
export const dashedLine = (x1: number, y1: number, x2: number, y2: number, className?: string): string => {
    const clsString: string = (className != null) ? ` class="${className}"` : "";
    return `<line${clsString} x1="${x1}" x2="${x2}" y1="${y1}" y2="${y2}" stroke-dasharray="8, 8"></line>`;
};

/**
 * Generate the SVG representation of the dashed box used for enclosing groups of operations controlled on a classical register.
 * 
 * @param x         x coord of box.
 * @param y         y coord of box.
 * @param width     Width of box.
 * @param height    Height of box.
 * @param className Class name of element.
 * 
 * @returns SVG string for dashed box.
 */
export const dashedBox = (x: number, y: number, width: number, height: number, className?: string): string => {
    const clsString: string = (className != null) ? ` class="${className}"` : "";
    return `<rect${clsString} x="${x}" y="${y}" width="${width}" height="${height}" fill-opacity="0" stroke-dasharray="8, 8"></rect>`;
};
