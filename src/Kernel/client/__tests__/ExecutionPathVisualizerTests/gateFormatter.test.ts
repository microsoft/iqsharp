import {
    formatGates,
    _formatGate,
    _createGate,
    _measure,
    _unitary,
    _swap,
    _controlledGate,
    _classicalControlled,
} from '../../ExecutionPathVisualizer/formatters/gateFormatter';
import { Metadata } from '../../ExecutionPathVisualizer/metadata';
import { GateType, startX, startY, registerHeight, minGateWidth, gatePadding } from '../../ExecutionPathVisualizer/constants';

describe('Testing _classicalControlled', () => {
    test("one 'zero' child", () => {
        const metadata: Metadata = {
            type: GateType.ClassicalControlled,
            x: startX,
            controlsY: [startY + registerHeight * 2],
            targetsY: [startY, startY + registerHeight],
            width: minGateWidth + gatePadding * 2,
            label: 'if',
            children: [
                [
                    {
                        type: GateType.Unitary,
                        x: startX + minGateWidth / 2 + gatePadding,
                        label: 'X',
                        controlsY: [],
                        targetsY: [startY],
                        width: minGateWidth,
                    },
                ],
                [],
            ],
            htmlClass: 'classically-controlled-1',
        };
        expect(_classicalControlled(metadata)).toMatchSnapshot();
    });
    test("one 'one' child", () => {
        const metadata: Metadata = {
            type: GateType.ClassicalControlled,
            x: startX,
            controlsY: [startY + registerHeight * 2],
            targetsY: [startY, startY + registerHeight],
            width: minGateWidth + gatePadding * 2,
            label: 'if',
            children: [
                [],
                [
                    {
                        type: GateType.Unitary,
                        x: startX + minGateWidth / 2 + gatePadding,
                        label: 'X',
                        controlsY: [],
                        targetsY: [startY],
                        width: minGateWidth,
                    },
                ],
            ],
            htmlClass: 'classically-controlled-1',
        };
        expect(_classicalControlled(metadata)).toMatchSnapshot();
    });
    test("multiple 'zero'/'one' children", () => {
        const metadata: Metadata = {
            type: GateType.ClassicalControlled,
            x: startX,
            controlsY: [startY + registerHeight * 2],
            targetsY: [startY, startY + registerHeight],
            width: minGateWidth * 2 + gatePadding * 4,
            label: 'if',
            htmlClass: 'classically-controlled-1',
            children: [
                [
                    {
                        type: GateType.Unitary,
                        x: startX + minGateWidth / 2 + gatePadding,
                        label: 'X',
                        controlsY: [],
                        targetsY: [startY],
                        width: minGateWidth,
                    },
                ],
                [
                    {
                        type: GateType.Unitary,
                        x: startX + minGateWidth / 2 + gatePadding,
                        label: 'X',
                        controlsY: [],
                        targetsY: [startY],
                        width: minGateWidth,
                    },
                    {
                        type: GateType.Cnot,
                        x: startX + minGateWidth + minGateWidth / 2 + gatePadding * 3,
                        label: 'X',
                        controlsY: [startY + registerHeight],
                        targetsY: [startY],
                        width: minGateWidth,
                    },
                ],
            ],
        };
        expect(_classicalControlled(metadata)).toMatchSnapshot();
    });
    test('nested children', () => {
        const metadata: Metadata = {
            type: GateType.ClassicalControlled,
            x: startX,
            controlsY: [startY + registerHeight * 2],
            targetsY: [startY, startY + registerHeight],
            width: minGateWidth * 2 + gatePadding * 6,
            label: 'if',
            htmlClass: 'classically-controlled-1',
            children: [
                [],
                [
                    {
                        type: GateType.Unitary,
                        x: startX + minGateWidth / 2 + gatePadding,
                        label: 'X',
                        controlsY: [],
                        targetsY: [startY],
                        width: minGateWidth,
                    },
                    {
                        type: GateType.ClassicalControlled,
                        x: startX + minGateWidth + gatePadding * 3,
                        controlsY: [startY + registerHeight * 3],
                        targetsY: [startY, startY + registerHeight],
                        width: minGateWidth + gatePadding * 2,
                        label: 'if',
                        htmlClass: 'classically-controlled-1',
                        children: [
                            [],
                            [
                                {
                                    type: GateType.Cnot,
                                    x: startX + minGateWidth + gatePadding * 4 + minGateWidth / 2,
                                    label: 'X',
                                    controlsY: [startY + registerHeight],
                                    targetsY: [startY],
                                    width: minGateWidth,
                                },
                            ],
                        ],
                    },
                ],
            ],
        };
        expect(_classicalControlled(metadata)).toMatchSnapshot();
    });
    test('No htmlClass', () => {
        const metadata: Metadata = {
            type: GateType.ClassicalControlled,
            x: startX,
            controlsY: [startY + registerHeight * 2],
            targetsY: [startY, startY + registerHeight],
            width: minGateWidth * 2 + gatePadding * 4,
            label: 'if',
            children: [
                [],
                [
                    {
                        type: GateType.Unitary,
                        x: startX + minGateWidth / 2 + gatePadding,
                        label: 'X',
                        controlsY: [],
                        targetsY: [startY],
                        width: minGateWidth,
                    },
                ],
            ],
        };
        expect(_classicalControlled(metadata)).toMatchSnapshot();
    });
    test('change padding', () => {
        const metadata: Metadata = {
            type: GateType.ClassicalControlled,
            x: startX,
            controlsY: [startY + registerHeight * 2],
            targetsY: [startY, startY + registerHeight],
            width: minGateWidth * 2 + gatePadding * 4,
            label: 'if',
            children: [
                [],
                [
                    {
                        type: GateType.Unitary,
                        x: startX + minGateWidth / 2 + gatePadding,
                        label: 'X',
                        controlsY: [],
                        targetsY: [startY],
                        width: minGateWidth,
                    },
                ],
            ],
            htmlClass: 'classically-controlled-1',
        };
        expect(_classicalControlled(metadata, 20)).toMatchSnapshot();
    });
});

describe('Testing _controlledGate', () => {
    test('CNOT gate', () => {
        const metadata: Metadata = {
            type: GateType.Cnot,
            label: 'X',
            x: startX,
            controlsY: [startY],
            targetsY: [startY + registerHeight],
            width: minGateWidth,
        };
        let svg: string = _controlledGate(metadata);
        expect(svg).toMatchSnapshot();

        // Flip target and control
        metadata.controlsY = [startY + registerHeight];
        metadata.targetsY = [startY];
        svg = _controlledGate(metadata);
        expect(svg).toMatchSnapshot();
    });
    test('SWAP gate', () => {
        const metadata: Metadata = {
            type: GateType.Swap,
            label: '',
            x: startX,
            controlsY: [startY],
            targetsY: [startY + registerHeight, startY + registerHeight * 2],
            width: minGateWidth,
        };
        // Control on top
        let svg: string = _controlledGate(metadata);
        expect(svg).toMatchSnapshot();

        // Control on bottom
        metadata.controlsY = [startY + registerHeight * 2];
        metadata.targetsY = [startY, startY + registerHeight];
        svg = _controlledGate(metadata);
        expect(svg).toMatchSnapshot();

        // Control in middle
        metadata.controlsY = [startY + registerHeight];
        metadata.targetsY = [startY, startY + registerHeight * 2];
        svg = _controlledGate(metadata);
        expect(svg).toMatchSnapshot();
    });
    test('Controlled U gate with 1 control + 1 target', () => {
        const metadata: Metadata = {
            type: GateType.ControlledUnitary,
            label: 'Foo',
            x: startX,
            controlsY: [startY],
            targetsY: [startY + registerHeight],
            width: 45,
        };
        let svg: string = _controlledGate(metadata);
        expect(svg).toMatchSnapshot();

        // Flip target and control
        metadata.controlsY = [startY + registerHeight];
        metadata.targetsY = [startY];
        svg = _controlledGate(metadata);
        expect(svg).toMatchSnapshot();
    });
    test('Controlled U gate with multiple controls + 1 target', () => {
        const metadata: Metadata = {
            type: GateType.ControlledUnitary,
            label: 'Foo',
            x: startX,
            controlsY: [startY, startY + registerHeight],
            targetsY: [startY + registerHeight * 2],
            width: 45,
        };
        // Target on bottom
        let svg: string = _controlledGate(metadata);
        expect(svg).toMatchSnapshot();

        // Target on top
        metadata.controlsY = [startY + registerHeight, startY + registerHeight * 2];
        metadata.targetsY = [startY];
        svg = _controlledGate(metadata);
        expect(svg).toMatchSnapshot();

        // Target in middle
        metadata.controlsY = [startY, startY + registerHeight * 2];
        metadata.targetsY = [startY + registerHeight];
        svg = _controlledGate(metadata);
        expect(svg).toMatchSnapshot();
    });
    test('Controlled U gate with 1 control + 2 targets', () => {
        const metadata: Metadata = {
            type: GateType.ControlledUnitary,
            label: 'Foo',
            x: startX,
            controlsY: [startY + registerHeight * 2],
            targetsY: [startY, startY + registerHeight],
            width: 45,
        };
        // Control on bottom
        let svg: string = _controlledGate(metadata);
        expect(svg).toMatchSnapshot();

        // Control on top
        metadata.controlsY = [startY];
        metadata.targetsY = [startY + registerHeight, startY + registerHeight * 2];
        svg = _controlledGate(metadata);
        expect(svg).toMatchSnapshot();

        // Control in middle
        metadata.controlsY = [startY + registerHeight];
        metadata.targetsY = [startY, startY + registerHeight * 2];
        svg = _controlledGate(metadata);
        expect(svg).toMatchSnapshot();
    });
    test('Controlled U gate with 2 controls + 2 targets', () => {
        const metadata: Metadata = {
            type: GateType.ControlledUnitary,
            label: 'Foo',
            x: startX,
            controlsY: [startY + registerHeight * 2, startY + registerHeight * 3],
            targetsY: [startY, startY + registerHeight],
            width: 45,
        };
        // Controls on bottom
        let svg: string = _controlledGate(metadata);
        expect(svg).toMatchSnapshot();

        // Controls on top
        metadata.controlsY = [startY, startY + registerHeight];
        metadata.targetsY = [startY + registerHeight * 2, startY + registerHeight * 3];
        svg = _controlledGate(metadata);
        expect(svg).toMatchSnapshot();

        // Controls in middle
        metadata.controlsY = [startY + registerHeight, startY + registerHeight * 2];
        metadata.targetsY = [startY, startY + registerHeight * 3];
        svg = _controlledGate(metadata);
        expect(svg).toMatchSnapshot();

        // Interleaved controls/targets
        metadata.controlsY = [startY + registerHeight, startY + registerHeight * 3];
        metadata.targetsY = [startY, startY + registerHeight * 2];
        svg = _controlledGate(metadata);
        expect(svg).toMatchSnapshot();
    });
    test('Invalid gate', () => {
        const metadata: Metadata = {
            type: GateType.Measure,
            label: 'X',
            x: startX,
            controlsY: [startY],
            targetsY: [startY + registerHeight],
            width: minGateWidth,
        };
        expect(() => _controlledGate(metadata)).toThrowError(`ERROR: Unrecognized gate: X of type ${GateType.Measure}`);
    });
});

describe('Testing _swap', () => {
    test('Adjacent swap', () => {
        let svg: string = _swap(startX, [startY, startY + registerHeight]);
        expect(svg).toMatchSnapshot();
        // Flip target and control
        svg = _swap(startX, [startY + registerHeight, startY]);
        expect(svg).toMatchSnapshot();
    });
    test('Non-adjacent swap', () => {
        let svg: string = _swap(startX, [startY, startY + registerHeight * 2]);
        expect(svg).toMatchSnapshot();
        // Flip target and control
        svg = _swap(startX, [startY + registerHeight * 2, startY]);
        expect(svg).toMatchSnapshot();
    });
});

describe('Testing _unitary', () => {
    test('Single qubit unitary', () => {
        expect(_unitary('H', startX, [startY], minGateWidth)).toMatchSnapshot();
    });
    test('Multiqubit unitary on consecutive registers', () => {
        let svg: string = _unitary('ZZ', startX, [startY, startY + registerHeight], minGateWidth);
        expect(svg).toMatchSnapshot();
        svg = _unitary('ZZZ', startX, [startY, startY + registerHeight, startY + registerHeight * 2], minGateWidth);
        expect(svg).toMatchSnapshot();
    });
    test('Multiqubit unitary on non-consecutive registers', () => {
        // Dashed line between unitaries
        let svg: string = _unitary('ZZ', startX, [startY, startY + registerHeight * 2], minGateWidth);
        expect(svg).toMatchSnapshot();
        svg = _unitary('ZZZ', startX, [startY, startY + registerHeight * 2, startY + registerHeight * 3], minGateWidth);
        expect(svg).toMatchSnapshot();
        // Solid line
        svg = _unitary('ZZ', startX, [startY, startY + registerHeight * 2], minGateWidth, undefined, false);
        expect(svg).toMatchSnapshot();
        svg = _unitary(
            'ZZZ',
            startX,
            [startY, startY + registerHeight * 2, startY + registerHeight * 3],
            minGateWidth,
            undefined,
            false,
        );
        expect(svg).toMatchSnapshot();
    });
    test('No y coords', () => {
        const svg: string = _unitary('ZZ', startX, [], minGateWidth);
        expect(svg).toStrictEqual('');
    });
});

describe('Testing _measure', () => {
    test('1 qubit + 1 classical registers', () => {
        expect(_measure(startX, startY)).toMatchSnapshot();
    });
    test('2 qubit + 1 classical registers', () => {
        expect(_measure(startX, startY)).toMatchSnapshot();
    });
    test('2 qubit + 2 classical registers', () => {
        expect(_measure(startX, startY)).toMatchSnapshot();
        expect(_measure(startX, startY + registerHeight)).toMatchSnapshot();
    });
});

describe('Testing _createGate', () => {
    test('No metadata', () => {
        expect(_createGate(['<line />'])).toEqual("<g class='gate'>\n<line />\n</g>");
    });
    test('With metadata', () => {
        expect(_createGate(['<line />'], { a: 1, b: 2 })).toEqual(
            `<g class='gate' data-metadata='{"a":1,"b":2}'>\n<line />\n</g>`,
        );
    });
    test('With metadata containing string', () => {
        expect(_createGate(['<line />'], { foo: 'bar' })).toEqual(
            `<g class='gate' data-metadata='{"foo":"bar"}'>\n<line />\n</g>`,
        );
    });
});

describe('Testing _formatGate', () => {
    test('measure gate', () => {
        const metadata: Metadata = {
            type: GateType.Measure,
            x: startX,
            controlsY: [startY],
            targetsY: [startY + registerHeight],
            label: '',
            width: minGateWidth,
        };
        expect(_formatGate(metadata)).toMatchSnapshot();
    });
    test('single-qubit unitary gate', () => {
        const metadata: Metadata = {
            type: GateType.Unitary,
            x: startX,
            controlsY: [],
            targetsY: [startY],
            label: 'H',
            width: minGateWidth,
        };
        expect(_formatGate(metadata)).toMatchSnapshot();
    });
    test('single-qubit unitary gate with arguments', () => {
        const metadata: Metadata = {
            type: GateType.Unitary,
            x: startX,
            controlsY: [],
            targetsY: [startY],
            label: 'Ry',
            displayArgs: '(0.25)',
            width: 52,
        };
        expect(_formatGate(metadata)).toMatchSnapshot();
    });
    test('multi-qubit unitary gate', () => {
        const metadata: Metadata = {
            type: GateType.Unitary,
            x: startX,
            controlsY: [],
            targetsY: [startY, startY + registerHeight],
            label: 'U',
            width: minGateWidth,
        };
        expect(_formatGate(metadata)).toMatchSnapshot();
    });
    test('multi-qubit unitary gate with arguments', () => {
        const metadata: Metadata = {
            type: GateType.ControlledUnitary,
            x: startX,
            controlsY: [],
            targetsY: [startY, startY + registerHeight],
            label: 'U',
            displayArgs: "('foo', 'bar')",
            width: 77,
        };
        expect(_formatGate(metadata)).toMatchSnapshot();
    });
    test('swap gate', () => {
        const metadata: Metadata = {
            type: GateType.Swap,
            x: startX,
            controlsY: [],
            targetsY: [startY, startY + registerHeight],
            label: '',
            width: minGateWidth,
        };
        expect(_formatGate(metadata)).toMatchSnapshot();
    });
    test('controlled swap gate', () => {
        const metadata: Metadata = {
            type: GateType.Swap,
            x: startX,
            controlsY: [startY],
            targetsY: [startY + registerHeight, startY + registerHeight * 2],
            label: '',
            width: minGateWidth,
        };
        expect(_formatGate(metadata)).toMatchSnapshot();
    });
    test('CNOT gate', () => {
        const metadata: Metadata = {
            type: GateType.Cnot,
            x: startX,
            controlsY: [startY],
            targetsY: [startY + registerHeight],
            label: 'X',
            width: minGateWidth,
        };
        expect(_formatGate(metadata)).toMatchSnapshot();
    });
    test('controlled unitary gate', () => {
        const metadata: Metadata = {
            type: GateType.ControlledUnitary,
            x: startX,
            controlsY: [startY],
            targetsY: [startY + registerHeight],
            label: 'U',
            width: minGateWidth,
        };
        expect(_formatGate(metadata)).toMatchSnapshot();
    });
    test('controlled unitary gate with arguments', () => {
        const metadata: Metadata = {
            type: GateType.ControlledUnitary,
            x: startX,
            controlsY: [startY],
            targetsY: [startY + registerHeight],
            label: 'U',
            displayArgs: "('foo', 'bar')",
            width: 77,
        };
        expect(_formatGate(metadata)).toMatchSnapshot();
    });
    test('classically controlled gate', () => {
        const metadata: Metadata = {
            type: GateType.ClassicalControlled,
            x: startX,
            controlsY: [startY + registerHeight * 2],
            targetsY: [startY, startY + registerHeight],
            label: '',
            width: minGateWidth,
        };
        expect(_formatGate(metadata)).toMatchSnapshot();
    });
    test('gate with metadata', () => {
        const metadata: Metadata = {
            type: GateType.Unitary,
            x: startX,
            controlsY: [],
            targetsY: [startY],
            label: 'H',
            width: minGateWidth,
            customMetadata: { a: 1, b: 2 },
        };
        expect(_formatGate(metadata)).toMatchSnapshot();
    });
    test('invalid gate', () => {
        const metadata: Metadata = {
            type: GateType.Invalid,
            x: startX,
            controlsY: [startY],
            targetsY: [startY + registerHeight],
            label: 'Foo',
            width: 48,
        };
        expect(() => _formatGate(metadata)).toThrowError(`ERROR: unknown gate (Foo) of type ${GateType.Invalid}.`);
    });
});

describe('Testing formatGates', () => {
    test('Single gate', () => {
        const gates: Metadata[] = [
            {
                type: GateType.Cnot,
                x: startX,
                controlsY: [startY],
                targetsY: [startY + registerHeight],
                label: 'X',
                width: minGateWidth,
            },
        ];
        expect(formatGates(gates)).toMatchSnapshot();
    });
    test('Single null gate', () => {
        const gates: Metadata[] = [
            {
                type: GateType.Invalid,
                x: startX,
                controlsY: [startY],
                targetsY: [startY + registerHeight],
                label: '',
                width: minGateWidth,
            },
        ];
        expect(() => formatGates(gates)).toThrowError(`ERROR: unknown gate () of type ${GateType.Invalid}.`);
    });
    test('Multiple gates', () => {
        const gates: Metadata[] = [
            {
                type: GateType.Cnot,
                x: startX,
                controlsY: [startY + registerHeight],
                targetsY: [startY],
                label: 'X',
                width: minGateWidth,
            },
            {
                type: GateType.ControlledUnitary,
                x: startX,
                controlsY: [startY + registerHeight],
                targetsY: [startY + registerHeight * 2],
                label: 'X',
                width: minGateWidth,
            },
            {
                type: GateType.Unitary,
                x: startX,
                controlsY: [],
                targetsY: [startY + registerHeight * 2],
                label: 'X',
                width: minGateWidth,
            },
            {
                type: GateType.Measure,
                x: startX,
                controlsY: [startY],
                targetsY: [startY + registerHeight * 3],
                label: 'X',
                width: minGateWidth,
            },
        ];
        expect(formatGates(gates)).toMatchSnapshot();
    });
    test('Multiple gates with invalid gate', () => {
        const gates: Metadata[] = [
            {
                type: GateType.Unitary,
                x: startX,
                controlsY: [],
                targetsY: [startY + registerHeight * 2],
                label: 'X',
                width: minGateWidth,
            },
            {
                type: GateType.Cnot,
                x: startX,
                controlsY: [startY + registerHeight],
                targetsY: [startY],
                label: 'X',
                width: minGateWidth,
            },
            {
                type: GateType.Invalid,
                x: startX,
                controlsY: [],
                targetsY: [startY + registerHeight * 2],
                label: '',
                width: minGateWidth,
            },
            {
                type: GateType.Invalid,
                x: startX,
                controlsY: [],
                targetsY: [],
                label: '',
                width: minGateWidth,
            },
        ];
        expect(() => formatGates(gates)).toThrowError(`ERROR: unknown gate () of type ${GateType.Invalid}.`);
    });
});
