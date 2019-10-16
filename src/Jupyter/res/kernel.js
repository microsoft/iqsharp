// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

define(
    [
        'codemirror/lib/codemirror',
        'codemirror/addon/mode/simple'
    ],
    (CodeMirror, Simple, CLike) => {
        "use strict";
        console.log(CodeMirror);

        return {
            onload: () => {
                console.log(CodeMirror.defineSimpleMode);
                CodeMirror.defineSimpleMode('qsharp', {
                    start: [
                        {
                            token: "comment",
                            // include % to support kata special commands
                            regex: /(\/\/|%).*/
                        },
                        {
                            token: "string",
                            regex: /^\"(?:[^\"\\]|\\[\s\S])*(?:\"|$)/
                        },
                        {
                            // a group of keywords that can typically occur in the beginning of the line but not in the end of a phrase
                            token: "keyword",
                            regex: /(^|\W)(?:namespace|open|as|operation|function|body|adjoint|controlled)\b/
                        },
                        {
                            token: "keyword",
                            regex: /\W(?:if|elif|else|repeat|until|fixup|for|in|return|fail|within|apply)\b/
                        },
                        {
                            token: "keyword",
                            regex: /\W(?:Adjoint|Controlled|Adj|Ctl|is|self|auto|distribute|invert|intrinsic)\b/
                        },
                        {
                            token: "keyword",
                            regex: /\W(?:let|set|w\/|new|not|and|or|using|borrowing|newtype|mutable)\b/
                        },
                        {
                            token: "meta",
                            regex: /\W(?:Int|BigInt|Double|Bool|Qubit|Pauli|Result|Range|String|Unit)\b/
                        },
                        {
                            token: "atom",
                            regex: /\W(?:true|false|Pauli(I|X|Y|Z)|One|Zero)\b/
                        },
                        {
                            token: "builtin",
                            regex: /(\\n|\W)(?:X|Y|Z|H|HY|S|T|SWAP|CNOT|CCNOT|MultiX|R|RFrac|Rx|Ry|Rz|R1|R1Frac|Exp|ExpFrac|Measure|M|MultiM)\b/
                        },
                        {
                            token: "builtin",
                            regex: /(\\n|\W)(?:Message|Length|Assert|AssertProb|AssertEqual)\b/
                        }
                    ]
                });
                CodeMirror.defineMIME("text/x-qsharp", "qsharp");
            }
        };
    }
);
