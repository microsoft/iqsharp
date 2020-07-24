import { Operation } from "../../ExecutionPathVisualizer/executionPath";
import { RegisterMap, RegisterType, Register } from "../../ExecutionPathVisualizer/register";
import { Metadata } from "../../ExecutionPathVisualizer/metadata";
import {
    processOperations,
    _groupOperations,
    _alignOps,
    _getColumnsX,
    _opToMetadata,
    _getRegY,
    _addClass,
    _fillMetadataX,
    _offsetChildrenX,
} from "../../ExecutionPathVisualizer/process";
import {
    GateType,
    minGateWidth,
    startX,
    startY,
    registerHeight,
    gatePadding,
    classicalRegHeight,
    controlBtnOffset,
    classicalBoxPadding
} from "../../ExecutionPathVisualizer/constants";
import { _getStringWidth } from "../../ExecutionPathVisualizer/utils";

describe("Testing _groupOperations", () => {
    const registers: RegisterMap = {
        0: { type: RegisterType.Qubit, y: startY },
        1: { type: RegisterType.Qubit, y: startY + registerHeight },
        2: { type: RegisterType.Qubit, y: startY + registerHeight * 2 },
        3: { type: RegisterType.Qubit, y: startY + registerHeight * 3 },
    };
    test("single qubit gates on 1 qubit register", () => {
        const operations: Operation[] = [
            { gate: "X", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
            { gate: "Y", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
            { gate: "Z", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
        ];
        expect(_groupOperations(operations, registers)).toEqual([[0, 1, 2], [], [], []]);
    });
    test("single qubit gates on multiple qubit registers", () => {
        const operations: Operation[] = [
            { gate: "X", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
            { gate: "Y", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 1 }] },
            { gate: "Z", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 2 }] },
            { gate: "H", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
            { gate: "T", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 1 }] },
        ];
        expect(_groupOperations(operations, registers)).toEqual([[0, 3], [1, 4], [2], []]);
    });
    test("single and multiple qubit(s) gates", () => {
        let operations: Operation[] = [
            { gate: "X", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
            { gate: "Y", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Qubit, qId: 0 }], targets: [{ type: RegisterType.Qubit, qId: 1 }] },
            { gate: "Z", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
        ];
        expect(_groupOperations(operations, registers)).toEqual([[0, 1, 2], [1], [], []]);
        operations = [
            { gate: "X", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
            { gate: "Y", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Qubit, qId: 1 }], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
            { gate: "Z", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
        ];
        expect(_groupOperations(operations, registers)).toEqual([[0, 1, 2], [1], [], []]);
        operations = [
            { gate: "X", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
            { gate: "Z", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
            { gate: "Y", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Qubit, qId: 1 }], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
        ];
        expect(_groupOperations(operations, registers)).toEqual([[0, 1, 2], [2], [], []]);
        operations = [
            { gate: "Y", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Qubit, qId: 1 }], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
            { gate: "X", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
            { gate: "Z", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
        ];
        expect(_groupOperations(operations, registers)).toEqual([[0, 1, 2], [0], [], []]);
        operations = [
            { gate: "Y", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Qubit, qId: 1 }], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
            { gate: "X", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
            { gate: "Z", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 1 }] },
        ];
        expect(_groupOperations(operations, registers)).toEqual([[0, 1], [0, 2], [], []]);
    });
    test("multiple qubit gates in ladder format", () => {
        const operations: Operation[] = [
            { gate: "X", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Qubit, qId: 1 }], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
            { gate: "Y", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Qubit, qId: 1 }], targets: [{ type: RegisterType.Qubit, qId: 2 }] },
            { gate: "Z", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Qubit, qId: 2 }], targets: [{ type: RegisterType.Qubit, qId: 3 }] },
            { gate: "H", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Qubit, qId: 2 }], targets: [{ type: RegisterType.Qubit, qId: 3 }] },
            { gate: "T", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Qubit, qId: 2 }], targets: [{ type: RegisterType.Qubit, qId: 1 }] },
            { gate: "X", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Qubit, qId: 0 }], targets: [{ type: RegisterType.Qubit, qId: 1 }] },
        ];
        expect(_groupOperations(operations, registers)).toEqual([[0, 5], [0, 1, 4, 5], [1, 2, 3, 4], [2, 3]]);

    });
    test("multiple qubit gates in ladder format with single qubit gate", () => {
        let numRegs: number = 4;
        let operations: Operation[] = [
            { gate: "X", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Qubit, qId: 1 }], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
            { gate: "Y", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 1 }] },
            { gate: "Y", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Qubit, qId: 1 }], targets: [{ type: RegisterType.Qubit, qId: 2 }] },
            { gate: "Z", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Qubit, qId: 2 }], targets: [{ type: RegisterType.Qubit, qId: 3 }] },
            { gate: "Z", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 2 }] },
            { gate: "H", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Qubit, qId: 2 }], targets: [{ type: RegisterType.Qubit, qId: 3 }] },
            { gate: "T", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Qubit, qId: 2 }], targets: [{ type: RegisterType.Qubit, qId: 1 }] },
            { gate: "Y", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 1 }] },
            { gate: "X", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Qubit, qId: 0 }], targets: [{ type: RegisterType.Qubit, qId: 1 }] },
        ];
        expect(_groupOperations(operations, registers)).toEqual([[0, 8], [0, 1, 2, 6, 7, 8], [2, 3, 4, 5, 6], [3, 5]]);

        numRegs = 3;
        operations = [
            { gate: "X", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Qubit, qId: 1 }], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
            { gate: "Y", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 1 }] },
            { gate: "Y", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Qubit, qId: 1 }], targets: [{ type: RegisterType.Qubit, qId: 2 }] },
            { gate: "Z", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 2 }] },
            { gate: "T", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Qubit, qId: 2 }], targets: [{ type: RegisterType.Qubit, qId: 1 }] },
            { gate: "Y", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 1 }] },
            { gate: "H", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
            { gate: "H", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
            { gate: "H", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
            { gate: "H", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
            { gate: "X", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Qubit, qId: 0 }], targets: [{ type: RegisterType.Qubit, qId: 1 }] },
        ];
        expect(_groupOperations(operations, registers)).toEqual([[0, 6, 7, 8, 9, 10], [0, 1, 2, 4, 5, 10], [2, 3, 4], []]);
    });
    test("interleaved multiqubit gates", () => {
        let operations: Operation[] = [
            { gate: "X", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Qubit, qId: 1 }], targets: [{ type: RegisterType.Qubit, qId: 3 }] },
            { gate: "X", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Qubit, qId: 0 }], targets: [{ type: RegisterType.Qubit, qId: 2 }] },
        ];
        expect(_groupOperations(operations, registers)).toEqual([[1], [0, 1], [0, 1], [0]]);
        operations = [
            { gate: "X", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Qubit, qId: 0 }, { type: RegisterType.Qubit, qId: 1 }], targets: [{ type: RegisterType.Qubit, qId: 3 }] },
            { gate: "X", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Qubit, qId: 0 }], targets: [{ type: RegisterType.Qubit, qId: 2 }, { type: RegisterType.Qubit, qId: 3 }] },
        ];
        expect(_groupOperations(operations, registers)).toEqual([[0, 1], [0, 1], [0, 1], [0, 1]]);
        operations = [
            { gate: "Foo", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 0 }, { type: RegisterType.Qubit, qId: 2 }, { type: RegisterType.Qubit, qId: 3 }] },
            { gate: "Bar", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 0 }, { type: RegisterType.Qubit, qId: 1 }, { type: RegisterType.Qubit, qId: 2 }] },
        ];
        expect(_groupOperations(operations, registers)).toEqual([[0, 1], [0, 1], [0, 1], [0]]);
    });
    test("classical control gates", () => {
        const registers: RegisterMap = {
            0: { type: RegisterType.Qubit, y: startY },
            1: {
                type: RegisterType.Qubit,
                y: startY + registerHeight,
                children: [{ type: RegisterType.Classical, y: startY + registerHeight + classicalRegHeight }]
            },
            2: {
                type: RegisterType.Qubit,
                y: startY + registerHeight + classicalRegHeight * 2,
                children: [{ type: RegisterType.Classical, y: startY + registerHeight + classicalRegHeight * 3 }]
            },
            3: { type: RegisterType.Qubit, y: startY + registerHeight + classicalRegHeight * 4 },
        }
        let operations: Operation[] = [
            { gate: "X", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Classical, qId: 2, cId: 0 }], targets: [{ type: RegisterType.Qubit, qId: 1 }] },
            { gate: "X", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
        ];
        expect(_groupOperations(operations, registers)).toEqual([[0, 1], [0], [0], [0]]);
        operations = [
            { gate: "X", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Classical, qId: 2, cId: 0 }], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
            { gate: "X", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 1 }] },
        ];
        expect(_groupOperations(operations, registers)).toEqual([[0], [0, 1], [0], [0]]);
        operations = [
            { gate: "X", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 1 }] },
            { gate: "X", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Classical, qId: 1, cId: 0 }], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
        ];
        expect(_groupOperations(operations, registers)).toEqual([[1], [0, 1], [1], [1]]);
    });
    test("skipped registers", () => {
        let operations: Operation[] = [
            { gate: "X", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
            { gate: "Z", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [{ type: RegisterType.Qubit, qId: 2 }] },
        ];
        expect(_groupOperations(operations, registers)).toEqual([[0], [], [1], []]);
        operations = [
            { gate: "X", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Qubit, qId: 1 }], targets: [{ type: RegisterType.Qubit, qId: 0 }] },
            { gate: "Z", isMeasurement: false, isControlled: true, isAdjoint: false, controls: [{ type: RegisterType.Qubit, qId: 1 }], targets: [{ type: RegisterType.Qubit, qId: 2 }] },
        ];
        expect(_groupOperations(operations, registers)).toEqual([[0], [0, 1], [1], []]);
    });
    test("no qubits", () => {
        const operations: Operation[] = [
            { gate: "NoOp1", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [] },
            { gate: "NoOp2", isMeasurement: false, isControlled: false, isAdjoint: false, controls: [], targets: [] },
        ];
        expect(_groupOperations(operations, registers)).toEqual([[], [], [], []]);
    });
});

describe("Testing _alignOps", () => {
    test("single qubit gates", () => {
        const ops: number[][] = [[0, 2, 5, 6], [1, 3, 4]];
        expect(_alignOps(ops)).toEqual(ops);
    });
    test("correct ordering of single qubit gate after multiqubit gate", () => {
        const ops: number[][] = [[0, 1, 3], [1, 2]];
        expect(_alignOps(ops)).toEqual([[0, 1, 3], [null, 1, 2]]);
    });
    test("padding of multiqubit register after single qubit gate", () => {
        const ops: number[][] = [[1], [0, 1]];
        expect(_alignOps(ops)).toEqual([[null, 1], [0, 1]]);
    });
    test("no padding of single qubit gate after multiqubit gate on different registers", () => {
        const ops: number[][] = [[0, 3], [2], [1, 2]];
        expect(_alignOps(ops)).toEqual([[0, 3], [null, 2], [1, 2]]);
    });
    test("ladder of cnots", () => {
        const ops: number[][] = [[0, 4], [0, 1, 3, 4], [1, 2, 3]];
        expect(_alignOps(ops)).toEqual([[0, null, null, null, 4], [0, 1, null, 3, 4], [null, 1, 2, 3]]);
    });
    test("interleaved multiqubit gates", () => {
        let ops: number[][] = [[0], [0, 1], [0, 1], [1]];
        expect(_alignOps(ops)).toEqual([[0], [0, 1], [0, 1], [null, 1]]);
        ops = [[0], [0], [0, 1], [1], [1], [1]];
        expect(_alignOps(ops)).toEqual([[0], [0], [0, 1], [null, 1], [null, 1], [null, 1]]);
    });
    test("skipped registers", () => {
        let ops: number[][] = [[0], [], [1], []];
        expect(_alignOps(ops)).toEqual([[0], [], [1], []]);
        ops = [[0], [], [1, 2], [2]];
        expect(_alignOps(ops)).toEqual([[0], [], [1, 2], [null, 2]]);
    });
});

describe("Testing _getColumnsX", () => {
    test("0 columns", () =>
        expect(_getColumnsX([])).toEqual({ columnsX: [], svgWidth: startX }));
    test("1 column", () => {
        expect(_getColumnsX([minGateWidth]))
            .toEqual({
                columnsX: [startX + minGateWidth / 2],
                svgWidth: startX + minGateWidth + gatePadding * 2
            });
        expect(_getColumnsX([100]))
            .toEqual({
                columnsX: [startX + 100 / 2],
                svgWidth: startX + 100 + gatePadding * 2
            });
    });
    test("2 columns", () => {
        expect(_getColumnsX([10, 10]))
            .toEqual({
                columnsX: [startX + 5, startX + 15 + gatePadding * 2],
                svgWidth: startX + 20 + gatePadding * 4
            });
        expect(_getColumnsX([20, 10]))
            .toEqual({
                columnsX: [startX + 10, startX + 25 + gatePadding * 2],
                svgWidth: startX + 30 + gatePadding * 4
            });
    });
});

describe("Testing _opToMetadata", () => {
    test("single qubit gate", () => {
        const op: Operation = {
            gate: "X",
            isMeasurement: false,
            isControlled: false,
            isAdjoint: false,
            controls: [],
            targets: [{ type: RegisterType.Qubit, qId: 1 }]
        };
        const registers: RegisterMap = {
            0: { type: RegisterType.Qubit, y: startY },
            1: { type: RegisterType.Qubit, y: startY + registerHeight },
        };
        const metadata: Metadata = {
            type: GateType.Unitary,
            x: 0,
            controlsY: [],
            targetsY: [startY + registerHeight],
            label: 'X',
            width: minGateWidth
        };
        expect(_opToMetadata(op, registers)).toEqual(metadata);
    });
    test("isAdjoint gate", () => {
        const op: Operation = {
            gate: "Foo",
            isMeasurement: false,
            isControlled: false,
            isAdjoint: true,
            controls: [],
            targets: [{ type: RegisterType.Qubit, qId: 1 }]
        };
        const registers: RegisterMap = {
            0: { type: RegisterType.Qubit, y: startY },
            1: { type: RegisterType.Qubit, y: startY + registerHeight },
        };
        const metadata: Metadata = {
            type: GateType.Unitary,
            x: 0,
            controlsY: [],
            targetsY: [startY + registerHeight],
            label: "Foo'",
            width: 48
        };
        expect(_opToMetadata(op, registers)).toEqual(metadata);
    });
    test("measure gate", () => {
        const op: Operation = {
            gate: "M",
            isMeasurement: true,
            isControlled: false,
            isAdjoint: false,
            controls: [{ type: RegisterType.Qubit, qId: 0 }],
            targets: [{ type: RegisterType.Classical, qId: 0, cId: 0 }]
        };
        const registers: RegisterMap = {
            0: { type: RegisterType.Qubit, y: startY, children: [{ type: RegisterType.Classical, y: startY + classicalRegHeight }] },
        };
        const metadata: Metadata = {
            type: GateType.Measure,
            x: 0,
            controlsY: [startY],
            targetsY: [startY + classicalRegHeight],
            label: '',
            width: minGateWidth
        };
        expect(_opToMetadata(op, registers)).toEqual(metadata);
    });
    test("swap gate", () => {
        const op: Operation = {
            gate: "SWAP",
            isMeasurement: false,
            isControlled: false,
            isAdjoint: false,
            controls: [],
            targets: [
                { type: RegisterType.Qubit, qId: 0 },
                { type: RegisterType.Qubit, qId: 1 }
            ]
        };
        const registers: RegisterMap = {
            0: { type: RegisterType.Qubit, y: startY },
            1: { type: RegisterType.Qubit, y: startY + registerHeight },
        };
        const metadata: Metadata = {
            type: GateType.Swap,
            x: 0,
            controlsY: [],
            targetsY: [startY, startY + registerHeight],
            label: '',
            width: minGateWidth
        };
        expect(_opToMetadata(op, registers)).toEqual(metadata);
    });
    test("isControlled swap gate", () => {
        const op: Operation = {
            gate: "SWAP",
            isMeasurement: false,
            isControlled: false,
            isAdjoint: false,
            controls: [{ type: RegisterType.Qubit, qId: 0 }],
            targets: [
                { type: RegisterType.Qubit, qId: 1 },
                { type: RegisterType.Qubit, qId: 2 }
            ]
        };
        const registers: RegisterMap = {
            0: { type: RegisterType.Qubit, y: startY },
            1: { type: RegisterType.Qubit, y: startY + registerHeight },
            2: { type: RegisterType.Qubit, y: startY + registerHeight * 2 },
        };
        const metadata: Metadata = {
            type: GateType.Swap,
            x: 0,
            controlsY: [startY],
            targetsY: [startY + registerHeight, startY + registerHeight * 2],
            label: '',
            width: minGateWidth
        };
        expect(_opToMetadata(op, registers)).toEqual(metadata);
    });
    test("single qubit unitary gate", () => {
        const op: Operation = {
            gate: "X",
            isMeasurement: false,
            isControlled: false,
            isAdjoint: false,
            controls: [],
            targets: [{ type: RegisterType.Qubit, qId: 0 }]
        };
        const registers: RegisterMap = {
            0: { type: RegisterType.Qubit, y: startY },
        };
        const metadata: Metadata = {
            type: GateType.Unitary,
            x: 0,
            controlsY: [],
            targetsY: [startY],
            label: 'X',
            width: minGateWidth
        };
        expect(_opToMetadata(op, registers)).toEqual(metadata);
    });
    test("multiqubit unitary gate", () => {
        const registers: RegisterMap = {
            0: { type: RegisterType.Qubit, y: startY },
            1: { type: RegisterType.Qubit, y: startY + registerHeight },
            2: { type: RegisterType.Qubit, y: startY + registerHeight * 2 },
        };
        let op: Operation = {
            gate: "ZZ",
            isMeasurement: false,
            isControlled: false,
            isAdjoint: false,
            controls: [],
            targets: [
                { type: RegisterType.Qubit, qId: 0 },
                { type: RegisterType.Qubit, qId: 1 }
            ]
        };
        let metadata: Metadata = {
            type: GateType.Unitary,
            x: 0,
            controlsY: [],
            targetsY: [startY, startY + registerHeight],
            label: 'ZZ',
            width: minGateWidth
        };
        expect(_opToMetadata(op, registers)).toEqual(metadata);
        op = {
            gate: "XX",
            isMeasurement: false,
            isControlled: false,
            isAdjoint: false,
            controls: [],
            targets: [
                { type: RegisterType.Qubit, qId: 1 },
                { type: RegisterType.Qubit, qId: 2 },
            ]
        };
        metadata = {
            type: GateType.Unitary,
            x: 0,
            controlsY: [],
            targetsY: [startY + registerHeight, startY + registerHeight * 2],
            label: 'XX',
            width: minGateWidth
        };
        expect(_opToMetadata(op, registers)).toEqual(metadata);
    });
    test("isControlled unitary gates", () => {
        const registers: RegisterMap = {
            0: { type: RegisterType.Qubit, y: startY },
            1: { type: RegisterType.Qubit, y: startY + registerHeight },
            2: { type: RegisterType.Qubit, y: startY + registerHeight * 2 },
            3: { type: RegisterType.Qubit, y: startY + registerHeight * 3 },
        };
        let op: Operation = {
            gate: "ZZ",
            isMeasurement: false,
            isControlled: true,
            isAdjoint: false,
            controls: [{ type: RegisterType.Qubit, qId: 1 }],
            targets: [{ type: RegisterType.Qubit, qId: 0 }]
        };
        let metadata: Metadata = {
            type: GateType.ControlledUnitary,
            x: 0,
            controlsY: [startY + registerHeight],
            targetsY: [startY],
            label: 'ZZ',
            width: minGateWidth
        };
        expect(_opToMetadata(op, registers)).toEqual(metadata);
        op = {
            gate: "XX",
            isMeasurement: false,
            isControlled: true,
            isAdjoint: false,
            controls: [{ type: RegisterType.Qubit, qId: 0 }],
            targets: [
                { type: RegisterType.Qubit, qId: 1 },
                { type: RegisterType.Qubit, qId: 2 },
            ]
        };
        metadata = {
            type: GateType.ControlledUnitary,
            x: 0,
            controlsY: [startY],
            targetsY: [startY + registerHeight, startY + registerHeight * 2],
            label: 'XX',
            width: minGateWidth
        };
        expect(_opToMetadata(op, registers)).toEqual(metadata);
        op = {
            gate: "Foo",
            isMeasurement: false,
            isControlled: true,
            isAdjoint: false,
            controls: [
                { type: RegisterType.Qubit, qId: 2 },
                { type: RegisterType.Qubit, qId: 3 },
            ],
            targets: [
                { type: RegisterType.Qubit, qId: 0 },
                { type: RegisterType.Qubit, qId: 1 },
            ]
        };
        metadata = {
            type: GateType.ControlledUnitary,
            label: 'Foo',
            x: 0,
            controlsY: [startY + registerHeight * 2, startY + registerHeight * 3],
            targetsY: [startY, startY + registerHeight],
            width: 45
        };
        expect(_opToMetadata(op, registers)).toEqual(metadata);
    });
    test("single-qubit unitary gates with arguments", () => {
        const registers: RegisterMap = {
            0: { type: RegisterType.Qubit, y: startY },
            1: { type: RegisterType.Qubit, y: startY + registerHeight },
        };
        let op: Operation = {
            gate: "RX",
            displayArgs: "(0.25)",
            isMeasurement: false,
            isControlled: false,
            isAdjoint: false,
            controls: [],
            targets: [{ type: RegisterType.Qubit, qId: 0 }]
        };
        let metadata: Metadata = {
            type: GateType.Unitary,
            x: 0,
            controlsY: [],
            targetsY: [startY],
            label: "RX",
            displayArgs: "(0.25)",
            width: 52
        };
        expect(_opToMetadata(op, registers)).toEqual(metadata);

        // Test long argument
        op = {
            gate: "RX",
            displayArgs: "(0.25, 1.0, 'foobar', (3.14, 6.67))",
            isMeasurement: false,
            isControlled: false,
            isAdjoint: false,
            controls: [],
            targets: [{ type: RegisterType.Qubit, qId: 0 }]
        };
        metadata = {
            type: GateType.Unitary,
            x: 0,
            controlsY: [],
            targetsY: [startY],
            label: "RX",
            displayArgs: "(0.25, 1.0, 'foobar', (3.14, 6.67))",
            width: 188
        };
        expect(_opToMetadata(op, registers)).toEqual(metadata);

        // Test isControlled
        op = {
            gate: "RX",
            displayArgs: "(0.25)",
            isMeasurement: false,
            isControlled: true,
            isAdjoint: false,
            controls: [{ type: RegisterType.Qubit, qId: 1 }],
            targets: [{ type: RegisterType.Qubit, qId: 0 }]
        };
        metadata = {
            type: GateType.ControlledUnitary,
            x: 0,
            controlsY: [startY + registerHeight],
            targetsY: [startY],
            label: "RX",
            displayArgs: "(0.25)",
            width: 52
        };
        expect(_opToMetadata(op, registers)).toEqual(metadata);
    });
    test("multi-qubit unitary gates with arguments", () => {
        const registers: RegisterMap = {
            0: { type: RegisterType.Qubit, y: startY },
            1: { type: RegisterType.Qubit, y: startY + registerHeight },
            2: { type: RegisterType.Qubit, y: startY + registerHeight * 2 },
        };
        let op: Operation = {
            gate: "U",
            displayArgs: "('foo', 'bar')",
            isMeasurement: false,
            isControlled: false,
            isAdjoint: false,
            controls: [],
            targets: [
                { type: RegisterType.Qubit, qId: 0 },
                { type: RegisterType.Qubit, qId: 1 },
            ]
        };
        let metadata: Metadata = {
            type: GateType.Unitary,
            x: 0,
            controlsY: [],
            targetsY: [startY, startY + registerHeight],
            label: "U",
            displayArgs: "('foo', 'bar')",
            width: 77
        };
        expect(_opToMetadata(op, registers)).toEqual(metadata);

        // Test long argument
        op = {
            gate: "U",
            displayArgs: "(0.25, 1.0, 'foobar', (3.14, 6.67))",
            isMeasurement: false,
            isControlled: false,
            isAdjoint: false,
            controls: [],
            targets: [
                { type: RegisterType.Qubit, qId: 0 },
                { type: RegisterType.Qubit, qId: 1 },
            ]
        };
        metadata = {
            type: GateType.Unitary,
            x: 0,
            controlsY: [],
            targetsY: [startY, startY + registerHeight],
            label: "U",
            displayArgs: "(0.25, 1.0, 'foobar', (3.14, 6.67))",
            width: 188
        };
        expect(_opToMetadata(op, registers)).toEqual(metadata);

        // Test isControlled
        op = {
            gate: "U",
            displayArgs: "('foo', 'bar')",
            isMeasurement: false,
            isControlled: true,
            isAdjoint: false,
            controls: [{ type: RegisterType.Qubit, qId: 1 }],
            targets: [
                { type: RegisterType.Qubit, qId: 0 },
                { type: RegisterType.Qubit, qId: 2 },
            ]
        };
        metadata = {
            type: GateType.ControlledUnitary,
            x: 0,
            controlsY: [startY + registerHeight],
            targetsY: [startY, startY + registerHeight * 2],
            label: "U",
            displayArgs: "('foo', 'bar')",
            width: 77
        };
        expect(_opToMetadata(op, registers)).toEqual(metadata);
    });
    test("classically isControlled gates", () => {
        const op: Operation = {
            gate: "X",
            isMeasurement: false,
            isControlled: true,
            isAdjoint: false,
            controls: [{ type: RegisterType.Classical, qId: 0, cId: 0 }],
            targets: [
                { type: RegisterType.Qubit, qId: 0 },
                { type: RegisterType.Qubit, qId: 1 },
            ],
            children: [
                [{
                    gate: "X",
                    isMeasurement: false,
                    isControlled: false,
                    isAdjoint: false,
                    controls: [],
                    targets: [{ type: RegisterType.Qubit, qId: 0 }]
                }],
                [{
                    gate: "H",
                    isMeasurement: false,
                    isControlled: false,
                    isAdjoint: false,
                    controls: [],
                    targets: [{ type: RegisterType.Qubit, qId: 1 }]
                }]
            ]
        };
        const registers: RegisterMap = {
            0: { type: RegisterType.Qubit, y: startY, children: [{ type: RegisterType.Classical, y: startY + classicalRegHeight }] },
            1: { type: RegisterType.Qubit, y: startY + classicalRegHeight * 2 },
        };
        const metadata: Metadata = {
            type: GateType.ClassicalControlled,
            x: 0,
            controlsY: [startY + classicalRegHeight],
            targetsY: [startY, startY + classicalRegHeight * 2],
            label: '',
            width: minGateWidth + controlBtnOffset + classicalBoxPadding * 2,
            children: [
                [{
                    type: GateType.Unitary,
                    x: startX + minGateWidth / 2,
                    controlsY: [],
                    targetsY: [startY],
                    label: 'X',
                    width: minGateWidth
                }],
                [{
                    type: GateType.Unitary,
                    x: startX + minGateWidth / 2,
                    controlsY: [],
                    targetsY: [startY + classicalRegHeight * 2],
                    label: 'H',
                    width: minGateWidth
                }]
            ]
        };
        expect(_opToMetadata(op, registers)).toEqual(metadata);
    });
    test("no render on null", () => {
        const metadata: Metadata = {
            type: GateType.Invalid,
            x: 0,
            controlsY: [],
            targetsY: [],
            label: '',
            width: minGateWidth
        };
        expect(_opToMetadata(null, [])).toEqual(metadata);
    });
    test("Invalid register", () => {
        let op: Operation = {
            gate: "X",
            isMeasurement: false,
            isControlled: false,
            isAdjoint: false,
            controls: [],
            targets: [{ type: RegisterType.Qubit, qId: 1 }]
        };
        const registers: RegisterMap = {
            0: { type: RegisterType.Qubit, y: startY },
        };
        expect(() => _opToMetadata(op, registers))
            .toThrowError('ERROR: Qubit register with ID 1 not found.');

        op = {
            gate: "X",
            isMeasurement: false,
            isControlled: false,
            isAdjoint: false,
            controls: [{ type: RegisterType.Classical, qId: 0, cId: 2 }],
            targets: []
        };
        expect(() => _opToMetadata(op, registers))
            .toThrowError('ERROR: No classical registers found for qubit ID 0.');
    });
    test("skipped registers", () => {
        const op: Operation = {
            gate: "X",
            isMeasurement: false,
            isControlled: false,
            isAdjoint: false,
            controls: [],
            targets: [{ type: RegisterType.Qubit, qId: 2 }]
        };
        const registers: RegisterMap = {
            0: { type: RegisterType.Qubit, y: startY },
            2: { type: RegisterType.Qubit, y: startY + registerHeight },
        };
        const metadata: Metadata = {
            type: GateType.Unitary,
            x: 0,
            controlsY: [],
            targetsY: [startY + registerHeight],
            label: "X",
            width: minGateWidth
        };
        expect(_opToMetadata(op, registers)).toEqual(metadata);
    });
});

describe("Testing _getRegY", () => {
    const registers: RegisterMap = {
        0: {
            type: RegisterType.Qubit,
            y: startY,
            children: [{ type: RegisterType.Classical, y: startY + classicalRegHeight }]
        }
    };
    test("quantum register", () => {
        const reg: Register = { type: RegisterType.Qubit, qId: 0 };
        expect(_getRegY(reg, registers)).toEqual(startY);
    });
    test("classical register", () => {
        const reg: Register = { type: RegisterType.Classical, qId: 0, cId: 0 };
        expect(_getRegY(reg, registers)).toEqual(startY + classicalRegHeight);
    });
    test("No children", () => {
        const registers: RegisterMap = {
            0: { type: RegisterType.Qubit, y: startY }
        };
        const reg: Register = { type: RegisterType.Classical, qId: 0, cId: 0 };
        expect(() => _getRegY(reg, registers)).toThrowError("ERROR: No classical registers found for qubit ID 0.");
    });
    test("Null cId", () => {
        const reg: Register = { type: RegisterType.Classical, qId: 0 };
        expect(() => _getRegY(reg, registers)).toThrowError("ERROR: No ID defined for classical register associated with qubit ID 0.");
    });
    test("Invalid cId", () => {
        const reg: Register = { type: RegisterType.Classical, qId: 0, cId: 1 };
        expect(() => _getRegY(reg, registers)).toThrowError("ERROR: Classical register ID 1 invalid for qubit ID 0 with 1 classical register(s).");
    });
    test("Invalid register type", () => {
        const reg: Register = { type: 2, qId: 0, cId: 1 };
        expect(() => _getRegY(reg, registers)).toThrowError("ERROR: Unknown register type 2.");
    });
});

describe("Testing _addClass", () => {
    test("No children", () => {
        const cls: string = 'classname';
        const metadata: Metadata = {
            type: GateType.Unitary,
            x: 0,
            controlsY: [],
            targetsY: [],
            children: [[], []],
            label: 'X',
            width: minGateWidth,
        };
        const expected: Metadata = {
            type: GateType.Unitary,
            x: 0,
            controlsY: [],
            targetsY: [],
            children: [[], []],
            label: 'X',
            width: minGateWidth,
            htmlClass: 'classname',
        };

        _addClass(metadata, cls);
        expect(metadata).toEqual(expected);
    });
    test("Undefined children", () => {
        const cls: string = 'classname';
        const metadata: Metadata = {
            type: GateType.Unitary,
            x: 0,
            controlsY: [],
            targetsY: [],
            label: 'X',
            width: minGateWidth,
        };
        const expected: Metadata = {
            type: GateType.Unitary,
            x: 0,
            controlsY: [],
            targetsY: [],
            label: 'X',
            width: minGateWidth,
            htmlClass: 'classname',
        };
        _addClass(metadata, cls);
        expect(metadata).toEqual(expected);
    });
    test("depth-1 children", () => {
        const cls: string = 'classname';
        const metadata: Metadata = {
            type: GateType.Unitary,
            x: 0,
            controlsY: [],
            targetsY: [],
            children: [[{
                type: GateType.Unitary,
                x: 0,
                controlsY: [],
                targetsY: [],
                label: 'X',
                width: minGateWidth,
            }], [{
                type: GateType.Unitary,
                x: 0,
                controlsY: [],
                targetsY: [],
                label: 'X',
                width: minGateWidth,
            }]],
            label: 'X',
            width: minGateWidth,
        };
        const expected: Metadata = {
            type: GateType.Unitary,
            x: 0,
            controlsY: [],
            targetsY: [],
            children: [[{
                type: GateType.Unitary,
                x: 0,
                controlsY: [],
                targetsY: [],
                label: 'X',
                width: minGateWidth,
                htmlClass: 'classname',
            }], [{
                type: GateType.Unitary,
                x: 0,
                controlsY: [],
                targetsY: [],
                label: 'X',
                width: minGateWidth,
                htmlClass: 'classname',
            }]],
            label: 'X',
            width: minGateWidth,
            htmlClass: 'classname',
        };

        _addClass(metadata, cls);
        expect(metadata).toEqual(expected);
    });
    test("depth-2 children", () => {
        const cls: string = 'classname';
        const metadata: Metadata = {
            type: GateType.Unitary,
            x: 0,
            controlsY: [],
            targetsY: [],
            children: [[{
                type: GateType.Unitary,
                x: 0,
                controlsY: [],
                targetsY: [],
                children: [[{
                    type: GateType.Unitary,
                    x: 0,
                    controlsY: [],
                    targetsY: [],
                    label: 'X',
                    width: minGateWidth,
                }], [{
                    type: GateType.Unitary,
                    x: 0,
                    controlsY: [],
                    targetsY: [],
                    label: 'X',
                    width: minGateWidth,
                }]],
                label: 'X',
                width: minGateWidth,
            }], [{
                type: GateType.Unitary,
                x: 0,
                controlsY: [],
                targetsY: [],
                label: 'X',
                width: minGateWidth,
            }]],
            label: 'X',
            width: minGateWidth,
        };
        const expected: Metadata = {
            type: GateType.Unitary,
            x: 0,
            controlsY: [],
            targetsY: [],
            children: [[{
                type: GateType.Unitary,
                x: 0,
                controlsY: [],
                targetsY: [],
                children: [[{
                    type: GateType.Unitary,
                    x: 0,
                    controlsY: [],
                    targetsY: [],
                    label: 'X',
                    width: minGateWidth,
                    htmlClass: 'classname',
                }], [{
                    type: GateType.Unitary,
                    x: 0,
                    controlsY: [],
                    targetsY: [],
                    label: 'X',
                    width: minGateWidth,
                    htmlClass: 'classname',
                }]],
                label: 'X',
                width: minGateWidth,
                htmlClass: 'classname',
            }], [{
                type: GateType.Unitary,
                x: 0,
                controlsY: [],
                targetsY: [],
                label: 'X',
                width: minGateWidth,
                htmlClass: 'classname',
            }]],
            label: 'X',
            width: minGateWidth,
            htmlClass: 'classname',
        };

        _addClass(metadata, cls);
        expect(metadata).toEqual(expected);
    });
});

describe("Testing _offsetChildrenX", () => {
    const offset: number = 50;
    test("no grandchildren", () => {
        const children: Metadata[][] = [[{
            type: GateType.Unitary,
            x: 0,
            controlsY: [],
            targetsY: [],
            width: minGateWidth,
            label: 'X',
        }]];
        const expected: Metadata[][] = [[{
            type: GateType.Unitary,
            x: 50,
            controlsY: [],
            targetsY: [],
            width: minGateWidth,
            label: 'X',
        }]];
        _offsetChildrenX(children, offset);
        expect(children).toEqual(expected);
    });
    test("has grandchildren", () => {
        const children: Metadata[][] = [[{
            type: GateType.Unitary,
            x: 0,
            controlsY: [],
            targetsY: [],
            width: minGateWidth,
            label: 'X',
            children: [[{
                type: GateType.Unitary,
                x: 0,
                controlsY: [],
                targetsY: [],
                width: minGateWidth,
                label: 'X',
            }], []]
        }]];
        const expected: Metadata[][] = [[{
            type: GateType.Unitary,
            x: 50,
            controlsY: [],
            targetsY: [],
            width: minGateWidth,
            label: 'X',
            children: [[{
                type: GateType.Unitary,
                x: 50,
                controlsY: [],
                targetsY: [],
                width: minGateWidth,
                label: 'X',
            }], []]
        }]];
        _offsetChildrenX(children, offset);
        expect(children).toEqual(expected);
    });
    test("undefined child", () => {
        expect(() => _offsetChildrenX(undefined, offset)).not.toThrow();
    });
});

describe("Testing _fillMetadataX", () => {
    test("Non-classically-isControlled gate", () => {
        const columnWidths: number[] = Array(1).fill(minGateWidth);
        const expectedEndX = startX + minGateWidth + gatePadding * 2;
        const opsMetadata: Metadata[][] = [
            [{
                type: GateType.Unitary,
                x: 0,
                controlsY: [],
                targetsY: [],
                label: 'X',
                width: minGateWidth,
            }]
        ];
        const expected: Metadata[][] = [
            [{
                type: GateType.Unitary,
                x: startX + minGateWidth / 2,
                controlsY: [],
                targetsY: [],
                label: 'X',
                width: minGateWidth,
            }]
        ];
        const endX: number = _fillMetadataX(opsMetadata, columnWidths);
        expect(opsMetadata).toEqual(expected);
        expect(endX).toEqual(expectedEndX);
    });
    test("classically-isControlled gate with no children", () => {
        const columnWidths: number[] = Array(1).fill(minGateWidth);
        const expectedEndX = startX + minGateWidth + gatePadding * 2;
        const opsMetadata: Metadata[][] = [
            [{
                type: GateType.ClassicalControlled,
                x: 0,
                controlsY: [],
                targetsY: [],
                label: 'X',
                width: minGateWidth,
            }]
        ];
        const expected: Metadata[][] = [
            [{
                type: GateType.ClassicalControlled,
                x: startX,
                controlsY: [],
                targetsY: [],
                label: 'X',
                width: minGateWidth,
            }]
        ];
        const endX: number = _fillMetadataX(opsMetadata, columnWidths);
        expect(opsMetadata).toEqual(expected);
        expect(endX).toEqual(expectedEndX);
    });
    test("depth-1 children", () => {
        const columnWidths: number[] = Array(1).fill(minGateWidth + gatePadding * 2);
        const expectedEndX = startX + minGateWidth + gatePadding * 4;
        const opsMetadata: Metadata[][] = [[{
            type: GateType.ClassicalControlled,
            x: 0,
            controlsY: [],
            targetsY: [],
            children: [[{
                type: GateType.Unitary,
                x: 0,
                controlsY: [],
                targetsY: [],
                label: 'X',
                width: minGateWidth,
            }], [{
                type: GateType.Unitary,
                x: 0,
                controlsY: [],
                targetsY: [],
                label: 'X',
                width: minGateWidth,
            }]],
            label: 'X',
            width: minGateWidth + controlBtnOffset + classicalBoxPadding * 2,
        }]];
        const expected: Metadata[][] = [[{
            type: GateType.ClassicalControlled,
            x: startX,
            controlsY: [],
            targetsY: [],
            children: [[{
                type: GateType.Unitary,
                x: controlBtnOffset + classicalBoxPadding,
                controlsY: [],
                targetsY: [],
                label: 'X',
                width: minGateWidth,
            }], [{
                type: GateType.Unitary,
                x: controlBtnOffset + classicalBoxPadding,
                controlsY: [],
                targetsY: [],
                label: 'X',
                width: minGateWidth,
            }]],
            label: 'X',
            width: minGateWidth + controlBtnOffset + classicalBoxPadding * 2,
        }]];

        const endX: number = _fillMetadataX(opsMetadata, columnWidths);
        expect(opsMetadata).toEqual(expected);
        expect(endX).toEqual(expectedEndX);
    });
});

describe("Testing processOperations", () => {
    test("single qubit gates", () => {
        const rxWidth: number = 52;
        const operations: Operation[] = [
            {
                gate: "H",
                isMeasurement: false,
                isControlled: false,
                isAdjoint: false,
                controls: [],
                targets: [{ type: RegisterType.Qubit, qId: 0 }]
            },
            {
                gate: "H",
                isMeasurement: false,
                isControlled: false,
                isAdjoint: false,
                controls: [],
                targets: [{ type: RegisterType.Qubit, qId: 0 }]
            },
            {
                gate: "H",
                isMeasurement: false,
                isControlled: false,
                isAdjoint: false,
                controls: [],
                targets: [{ type: RegisterType.Qubit, qId: 1 }]
            },
            {
                gate: "RX",
                displayArgs: "(0.25)",
                isMeasurement: false,
                isControlled: false,
                isAdjoint: false,
                controls: [],
                targets: [{ type: RegisterType.Qubit, qId: 1 }]
            },
        ];
        const registers: RegisterMap = {
            0: { type: RegisterType.Qubit, y: startY },
            1: { type: RegisterType.Qubit, y: startY + registerHeight },
        };
        const expectedOps: Metadata[] = [
            {
                type: GateType.Unitary,
                x: startX + minGateWidth / 2,
                controlsY: [],
                targetsY: [startY],
                label: "H",
                width: minGateWidth,
            },
            {
                type: GateType.Unitary,
                x: startX + (minGateWidth + gatePadding * 2) + rxWidth / 2,
                controlsY: [],
                targetsY: [startY],
                label: "H",
                width: minGateWidth,
            },
            {
                type: GateType.Unitary,
                x: startX + minGateWidth / 2,
                controlsY: [],
                targetsY: [startY + registerHeight],
                label: "H",
                width: minGateWidth,
            },
            {
                type: GateType.Unitary,
                x: startX + (minGateWidth + gatePadding * 2) + rxWidth / 2,
                controlsY: [],
                targetsY: [startY + registerHeight],
                label: "RX",
                displayArgs: "(0.25)",
                width: rxWidth,
            }
        ];
        const expectedWidth: number = startX + minGateWidth + rxWidth + gatePadding * 4;
        const { metadataList, svgWidth } = processOperations(operations, registers);
        expect(metadataList).toEqual(expectedOps);
        expect(svgWidth).toEqual(expectedWidth);
    });
    test("single wide qubit gates", () => {
        const expectedCustomWidth: number = 67;
        const operations: Operation[] = [
            {
                gate: "H",
                isMeasurement: false,
                isControlled: false,
                isAdjoint: false,
                controls: [],
                targets: [{ type: RegisterType.Qubit, qId: 0 }]
            },
            {
                gate: "FooBar",
                isMeasurement: false,
                isControlled: false,
                isAdjoint: false,
                controls: [],
                targets: [{ type: RegisterType.Qubit, qId: 0 }]
            },
            {
                gate: "H",
                isMeasurement: false,
                isControlled: false,
                isAdjoint: false,
                controls: [],
                targets: [{ type: RegisterType.Qubit, qId: 1 }]
            },
        ];
        const registers: RegisterMap = {
            0: { type: RegisterType.Qubit, y: startY },
            1: { type: RegisterType.Qubit, y: startY + registerHeight },
        };
        const expectedOps: Metadata[] = [
            {
                type: GateType.Unitary,
                x: startX + minGateWidth / 2,
                controlsY: [],
                targetsY: [startY],
                label: "H",
                width: minGateWidth,
            },
            {
                type: GateType.Unitary,
                x: startX + (minGateWidth + gatePadding * 2) + expectedCustomWidth / 2,
                controlsY: [],
                targetsY: [startY],
                label: "FooBar",
                width: expectedCustomWidth,
            },
            {
                type: GateType.Unitary,
                x: startX + minGateWidth / 2,
                controlsY: [],
                targetsY: [startY + registerHeight],
                label: "H",
                width: minGateWidth,
            }
        ];
        const expectedWidth: number = startX + minGateWidth + expectedCustomWidth + gatePadding * 4;
        const { metadataList, svgWidth } = processOperations(operations, registers);
        expect(metadataList).toEqual(expectedOps);
        expect(svgWidth).toEqual(expectedWidth);
    });
    test("single and multi qubit gates", () => {
        const operations: Operation[] = [
            {
                gate: "H",
                isMeasurement: false,
                isControlled: false,
                isAdjoint: false,
                controls: [],
                targets: [{ type: RegisterType.Qubit, qId: 0 }]
            },
            {
                gate: "X",
                isMeasurement: false,
                isControlled: true,
                isAdjoint: false,
                controls: [{ type: RegisterType.Qubit, qId: 1 }],
                targets: [{ type: RegisterType.Qubit, qId: 0 }]
            },
            {
                gate: "H",
                isMeasurement: false,
                isControlled: false,
                isAdjoint: false,
                controls: [],
                targets: [{ type: RegisterType.Qubit, qId: 1 }]
            },
        ];
        const registers: RegisterMap = {
            0: { type: RegisterType.Qubit, y: startY },
            1: { type: RegisterType.Qubit, y: startY + registerHeight },
        };
        const expectedOps: Metadata[] = [
            {
                type: GateType.Unitary,
                x: startX + minGateWidth / 2,
                controlsY: [],
                targetsY: [startY],
                label: "H",
                width: minGateWidth,
            },
            {
                type: GateType.Cnot,
                x: startX + (minGateWidth + gatePadding * 2) + minGateWidth / 2,
                controlsY: [startY + registerHeight],
                targetsY: [startY],
                label: "X",
                width: minGateWidth,
            },
            {
                type: GateType.Unitary,
                x: startX + (minGateWidth + gatePadding * 2) * 2 + minGateWidth / 2,
                controlsY: [],
                targetsY: [startY + registerHeight],
                label: "H",
                width: minGateWidth,
            }
        ];
        const expectedWidth: number = startX + (minGateWidth + gatePadding * 2) * 3;
        const { metadataList, svgWidth } = processOperations(operations, registers);
        expect(metadataList).toEqual(expectedOps);
        expect(svgWidth).toEqual(expectedWidth);
    });
    test("measure gates", () => {
        const operations: Operation[] = [
            {
                gate: "H",
                isMeasurement: false,
                isControlled: false,
                isAdjoint: false,
                controls: [],
                targets: [{ type: RegisterType.Qubit, qId: 0 }]
            },
            {
                gate: "M",
                isMeasurement: true,
                isControlled: false,
                isAdjoint: false,
                controls: [{ type: RegisterType.Qubit, qId: 0 }],
                targets: [{ type: RegisterType.Classical, qId: 0, cId: 0 }]
            },
            {
                gate: "H",
                isMeasurement: false,
                isControlled: false,
                isAdjoint: false,
                controls: [],
                targets: [{ type: RegisterType.Qubit, qId: 1 }]
            },
            {
                gate: "M",
                isMeasurement: true,
                isControlled: false,
                isAdjoint: false,
                controls: [{ type: RegisterType.Qubit, qId: 0 }],
                targets: [{ type: RegisterType.Classical, qId: 0, cId: 1 }]
            },
        ];
        const registers: RegisterMap = {
            0: {
                type: RegisterType.Qubit,
                y: startY,
                children: [
                    { type: RegisterType.Classical, y: startY + classicalRegHeight },
                    { type: RegisterType.Classical, y: startY + classicalRegHeight * 2 },
                ]
            },
            1: { type: RegisterType.Qubit, y: startY + classicalRegHeight * 3 },
        };
        const expectedOps: Metadata[] = [
            {
                type: GateType.Unitary,
                x: startX + minGateWidth / 2,
                controlsY: [],
                targetsY: [startY],
                label: "H",
                width: minGateWidth,
            },
            {
                type: GateType.Measure,
                x: startX + minGateWidth + gatePadding * 2 + minGateWidth / 2,
                controlsY: [startY],
                targetsY: [startY + classicalRegHeight],
                label: "",
                width: minGateWidth,
            },
            {
                type: GateType.Measure,
                x: startX + (minGateWidth + gatePadding * 2) * 2 + minGateWidth / 2,
                controlsY: [startY],
                targetsY: [startY + classicalRegHeight * 2],
                label: "",
                width: minGateWidth,
            },
            {
                type: GateType.Unitary,
                x: startX + minGateWidth / 2,
                controlsY: [],
                targetsY: [startY + classicalRegHeight * 3],
                label: "H",
                width: minGateWidth,
            },
        ];
        const expectedWidth: number = startX + (minGateWidth + gatePadding * 2) * 3;
        const { metadataList, svgWidth } = processOperations(operations, registers);
        expect(metadataList).toEqual(expectedOps);
        expect(svgWidth).toEqual(expectedWidth);
    });
    test("skipped registers", () => {
        const operations: Operation[] = [
            {
                gate: "H",
                isMeasurement: false,
                isControlled: false,
                isAdjoint: false,
                controls: [],
                targets: [{ type: RegisterType.Qubit, qId: 0 }]
            },
            {
                gate: "H",
                isMeasurement: false,
                isControlled: false,
                isAdjoint: false,
                controls: [],
                targets: [{ type: RegisterType.Qubit, qId: 2 }]
            },
        ];
        const registers: RegisterMap = {
            0: {
                type: RegisterType.Qubit,
                y: startY,
                children: [
                    { type: RegisterType.Classical, y: startY + classicalRegHeight },
                    { type: RegisterType.Classical, y: startY + classicalRegHeight * 2 },
                ]
            },
            2: { type: RegisterType.Qubit, y: startY + classicalRegHeight * 3 },
        };
        const expectedOps: Metadata[] = [
            {
                type: GateType.Unitary,
                x: startX + minGateWidth / 2,
                controlsY: [],
                targetsY: [startY],
                label: "H",
                width: minGateWidth,
            },
            {
                type: GateType.Unitary,
                x: startX + minGateWidth / 2,
                controlsY: [],
                targetsY: [startY + classicalRegHeight * 3],
                label: "H",
                width: minGateWidth,
            },
        ];
        const expectedWidth: number = startX + minGateWidth + gatePadding * 2;
        const { metadataList, svgWidth } = processOperations(operations, registers);
        expect(metadataList).toEqual(expectedOps);
        expect(svgWidth).toEqual(expectedWidth);
    });
});
