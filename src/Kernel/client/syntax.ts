// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
///<amd-dependency path="codemirror/lib/codemirror" />
///<amd-dependency path="codemirror/addon/mode/simple" />

export function defineQSharpMode() {
    console.log("Loading IQ# kernel-specific extension...");

    let rules = [
        {
            token: "comment",
            regex: /(\/\/).*/,
            beginWord: false,
        },
        {
            token: "string",
            regex: String.raw`^\"(?:[^\"\\]|\\[\s\S])*(?:\"|$)`,
            beginWord: false,
        },
        {
            token: "keyword",
            regex: String.raw`(namespace|open|as|operation|function|body|adjoint|newtype|controlled|internal)\b`,
            beginWord: true,
        },
        {
            token: "keyword",
            regex: String.raw`(if|elif|else|repeat|until|fixup|for|in|return|fail|within|apply)\b`,
             beginWord: true,
        },
        {
            token: "keyword",
            regex: String.raw`(Adjoint|Controlled|Adj|Ctl|is|self|auto|distribute|invert|intrinsic)\b`,
            beginWord: true,
        },
        {
            token: "keyword",
            regex: String.raw`(let|set|w\/|new|not|and|or|use|borrow|using|borrowing|newtype|mutable)\b`,
            beginWord: true,
        },
        {
            token: "meta",
            regex: String.raw`(Int|BigInt|Double|Bool|Qubit|Pauli|Result|Range|String|Unit)\b`,
            beginWord: true,
        },
        {
            token: "atom",
            regex: String.raw`(true|false|Pauli(I|X|Y|Z)|One|Zero)\b`,
            beginWord: true,
        },
        {
            token: "builtin",
            regex: String.raw`(X|Y|Z|H|HY|S|T|SWAP|CNOT|CCNOT|MultiX|R|RFrac|Rx|Ry|Rz|R1|R1Frac|Exp|ExpFrac|Measure|M|MultiM)\b`,
            beginWord: true,
        },
        {
            token: "builtin",
            regex: String.raw`(Message|Length|Assert|AssertProb|AssertEqual)\b`,
            beginWord: true,
        },
        {
            // built-in magic commands
            token: "builtin",
            regex: String.raw`(%(config|debug|estimate|lsmagic|package|performance|simulate|toffoli|trace|version|who|workspace|qir))\b`,
            beginWord: true,
        },
        {
            // Azure magic commands
            token: "builtin",
            regex: String.raw`(%azure\.(connect|execute|jobs|output|status|submit|target|target\-capability))\b`,
            beginWord: true,
        },
        {
            // chemistry magic commands
            token: "builtin",
            regex: String.raw`(%chemistry\.(broombridge|encode|fh\.add_terms|fh\.load|inputstate\.load))\b`,
            beginWord: true,
        },
        {
            // katas magic commands
            token: "builtin",
            regex: String.raw`(%(check_kata|kata))\b`,
            beginWord: true,
        },
    ];

    let simpleRules = []
    for (let rule of rules) {
        simpleRules.push({
            "token": rule.token,
            "regex": new RegExp(rule.regex, "g"),
            "sol": rule.beginWord
        });
        if (rule.beginWord) {
            // Need an additional rule due to the fact that CodeMirror simple mode doesn't work with ^ token
            simpleRules.push({
                "token": rule.token,
                "regex": new RegExp(String.raw`\W` + rule.regex, "g"),
                "sol": false
            });
        }
    }

    // NB: The TypeScript definitions for CodeMirror don't currently understand
    //     the simple mode plugin.
    let codeMirror: any = window.CodeMirror;
    codeMirror.defineSimpleMode('qsharp', {
        start: simpleRules
    });
    codeMirror.defineMIME("text/x-qsharp", "qsharp");
}
