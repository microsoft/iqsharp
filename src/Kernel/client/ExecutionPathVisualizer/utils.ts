import { Metadata } from './metadata';
import {
    GateType,
    minGateWidth,
    labelPadding,
    labelFontSize,
    argsFontSize,
} from './constants';
import fontSizes from './fontSizes';

/**
 * Calculate the width of a gate, given its metadata.
 * 
 * @param metadata Metadata of a given gate.
 * 
 * @returns Width of given gate (in pixels).
 */
const getGateWidth = ({ type, label, argStr, width }: Metadata): number => {
    switch (type) {
        case GateType.ClassicalControlled:
            // Already computed before.
            return width;
        case GateType.Measure:
        case GateType.Cnot:
        case GateType.Swap:
            return minGateWidth;
        default:
            const labelWidth = _getStringWidth(label);
            const argsWidth = (argStr != null) ? _getStringWidth(argStr, argsFontSize) : 0;
            const textWidth = Math.max(labelWidth, argsWidth) + labelPadding * 2;
            return Math.max(minGateWidth, textWidth);
    }
};

/**
 * Get the width of a string with font-size `fontSize` and font-family Arial.
 * 
 * @param str      Input string.
 * @param fontSize Font size of `str`. 
 * 
 * @returns Pixel width of given string.
 */
const _getStringWidth = (str: string, fontSize: number = labelFontSize): number => {
    const scale = fontSize / 100;
    const unScaledWidth = str.split('').reduce(
        (totalLen: number, ch: string) => totalLen + fontSizes[ch][0], 0);
    return scale * unScaledWidth;
};

export {
    getGateWidth,
    _getStringWidth,
};
