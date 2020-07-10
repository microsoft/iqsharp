import * as fs from "fs";
import * as path from "path";
import { ExecutionPath } from "./executionPath";
import { jsonToSvg, jsonToHtml } from "./pathVisualizer";

const exampleJSON: ExecutionPath = {
    qubits: [
        { id: 0, numChildren: 1 },
        { id: 1 },
        { id: 2 },
        { id: 3 }
    ],
    operations: [
        {
            "gate": "H",
            "controlled": false,
            "adjoint": false,
            "controls": [],
            "targets": [{ type: 0, qId: 1 }]
        },
        {
            "gate": "RX",
            "argStr": "(0.25)",
            "controlled": true,
            "adjoint": false,
            "controls": [{ type: 0, qId: 1 }],
            "targets": [{ type: 0, qId: 0 }]
        },
        {
            "gate": "X",
            "controlled": true,
            "adjoint": false,
            "controls": [{ type: 0, qId: 1 }],
            "targets": [
                { type: 0, qId: 2 },
                { type: 0, qId: 3 }
            ]
        },
        {
            "gate": "X",
            "controlled": true,
            "adjoint": false,
            "controls": [
                { type: 0, qId: 2 },
                { type: 0, qId: 3 }
            ],
            "targets": [
                { type: 0, qId: 1 }
            ]
        },
        {
            "gate": "X",
            "controlled": true,
            "adjoint": false,
            "controls": [
                { type: 0, qId: 1 },
                { type: 0, qId: 3 }
            ],
            "targets": [{ type: 0, qId: 2 }]
        },
        {
            "gate": "X",
            "controlled": true,
            "adjoint": false,
            "controls": [{ type: 0, qId: 2 }],
            "targets": [
                { type: 0, qId: 1 },
                { type: 0, qId: 3 }
            ]
        },
        {
            "gate": "measure",
            "controlled": false,
            "adjoint": false,
            "controls": [{ type: 0, qId: 0 }],
            "targets": [{ type: 1, qId: 0, cId: 0 }]
        },
        {
            "gate": "if",
            "controlled": false,
            "adjoint": false,
            "controls": [{ type: 1, qId: 0, cId: 0 }],
            "targets": [],
            children: [[
                {
                    "gate": "H",
                    "controlled": false,
                    "adjoint": false,
                    "controls": [],
                    "targets": [{ type: 0, qId: 1 }]
                },
                {
                    "gate": "X",
                    "controlled": false,
                    "adjoint": false,
                    "controls": [],
                    "targets": [{ type: 0, qId: 1 }]
                }
            ],
            [
                {
                    "gate": "X",
                    "controlled": true,
                    "adjoint": false,
                    "controls": [{ type: 0, qId: 0 }],
                    "targets": [{ type: 0, qId: 1 }]
                },
                {
                    "gate": "Foo",
                    "controlled": false,
                    "adjoint": false,
                    "controls": [],
                    "targets": [{ type: 0, qId: 3 }]
                }
            ]]
        },
        {
            "gate": "SWAP",
            "controlled": false,
            "adjoint": false,
            "controls": [],
            "targets": [
                { type: 0, qId: 0 },
                { type: 0, qId: 2 }
            ]
        },
        {
            "gate": "ZZ",
            "controlled": false,
            "adjoint": false,
            "controls": [],
            "targets": [
                { type: 0, qId: 1 },
                { type: 0, qId: 3 }
            ]
        },
        {
            "gate": "ZZ",
            "controlled": false,
            "adjoint": false,
            "controls": [],
            "targets": [
                { type: 0, qId: 0 },
                { type: 0, qId: 1 }
            ]
        },
        {
            "gate": "XX",
            "controlled": true,
            "adjoint": false,
            "controls": [{ type: 0, qId: 0 }],
            "targets": [
                { type: 0, qId: 1 },
                { type: 0, qId: 3 }
            ]
        },
        {
            "gate": "XX",
            "controlled": true,
            "adjoint": false,
            "controls": [{ type: 0, qId: 2 }],
            "targets": [
                { type: 0, qId: 1 },
                { type: 0, qId: 3 }
            ]
        },
        {
            "gate": "XX",
            "controlled": true,
            "adjoint": false,
            "controls": [
                { type: 0, qId: 0 },
                { type: 0, qId: 2 }
            ],
            "targets": [
                { type: 0, qId: 1 },
                { type: 0, qId: 3 }
            ]
        },
    ]
};

const saveToFile = (fileName: string, contents: string): void => {
    const filePath: string = path.join(__dirname, fileName);
    fs.writeFile(filePath, contents, 'utf8', (err) => console.log(err));

};

// Example use case ()
const svg = jsonToSvg(exampleJSON);
saveToFile('../example.svg', svg);
const html = jsonToHtml(exampleJSON);
saveToFile('../example.html', html);
