// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

///<amd-dependency path="codemirror/lib/codemirror" />
///<amd-dependency path="codemirror/addon/mode/simple" />

import { IPython } from "./ipython";

export function onload() {
    console.log("Loading IQ# kernel-specific extension...");
    // NB: The TypeScript definitions for CodeMirror don't currently understand
    //     the simple mode plugin.
    let codeMirror: any = window.CodeMirror;
    codeMirror.defineSimpleMode('qsharp', {
        start: [
            {
                token: "comment",
                // include % to support kata special commands
                regex: /(\/\/|%kata|%version|%simulate|%package|%workspace|%check_kata).*/
            },
            {
                token: "string",
                regex: /^\"(?:[^\"\\]|\\[\s\S])*(?:\"|$)/
            },
            {
                // a group of keywords that can typically occur in the beginning of the line but not in the end of a phrase
                token: "keyword",
                regex: /(^|\W)(?:namespace|open|as|operation|function|body|adjoint|newtype|controlled)\b/
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
                regex: /[^\w(\s]*(?:Int|BigInt|Double|Bool|Qubit|Pauli|Result|Range|String|Unit)\b/
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
    codeMirror.defineMIME("text/x-qsharp", "qsharp");
    console.log("Loaded IQ# kernel-specific extension!");
}

