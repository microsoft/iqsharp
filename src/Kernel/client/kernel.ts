// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

///<amd-dependency path="codemirror/lib/codemirror" />
///<amd-dependency path="codemirror/addon/mode/simple" />

import { IPython } from "./ipython";
declare var IPython: IPython;

import { Telemetry, ClientInfo } from "./telemetry.js";
import renderExecutionPath from "./ExecutionPathVisualizer/pathVisualizer.js";

function defineQSharpMode() {
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
            regex: String.raw`(namespace|open|as|operation|function|body|adjoint|newtype|controlled)\b`,
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
            regex: String.raw`(let|set|w\/|new|not|and|or|using|borrowing|newtype|mutable)\b`,
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
            regex: String.raw`(%(config|estimate|lsmagic|lsopen|package|performance|simulate|toffoli|trace|version|who|workspace))\b`,
            beginWord: true,
        },
        {
            // Azure magic commands
            token: "builtin",
            regex: String.raw`(%azure\.(connect|execute|jobs|output|status|submit|target))\b`,
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

class Kernel {
    hostingEnvironment: string | undefined;
    iqsharpVersion: string | undefined;
    telemetryOptOut?: boolean | null;

    constructor() {
        IPython.notebook.kernel.events.on("kernel_ready.Kernel", args => {
            this.requestEcho();
            this.requestClientInfo();
            this.initExecutionPathVisualizer();
        });
    }

    requestEcho() {
        // Try sending something for the kernel to echo back in order to test
        // communicates with the kernel. Note that iqsharp_echo_request will get
        // two responses: an output over iopub, and a reply over shell. We thus
        // subscribe both callbacks in order to make sure that both work
        // correctly.
        let value = "hello!";
        // The output callback is registered with the kernel object itself,
        // since outputs aren't a reply to any particular message.
        IPython.notebook.kernel.register_iopub_handler(
            "iqsharp_echo_output",
            message => {
                console.log("Got echo output:", message);
            }
        );
        // By contrast, callbacks for replies are associated with the message
        // itself, since we don't want the callback to pick up echo replies that
        // are replies to other messages.
        IPython.notebook.kernel.send_shell_message(
            "iqsharp_echo_request",
            { value: value },
            {
                shell: {
                    reply: (message) => {
                        console.log("Got echo reply:", message);
                    }
                }
            }
        );
    }

    getOriginQueryString() {
        return (new URLSearchParams(window.location.search)).get("origin");
    }

    requestClientInfo() {
        // The other thing we will want to do as the kernel starts up is to
        // pass along information from the client that would not otherwise be
        // available. For example, the browser user agent isn't exposed to the
        // kernel by the Jupyter protocol, since the client may not even be in a
        // browser at all.
        IPython.notebook.kernel.send_shell_message(
            "iqsharp_clientinfo_request",
            {
                "user_agent": navigator.userAgent,
                "client_language": navigator.language,
                "client_host": location.hostname,
                "client_origin": this.getOriginQueryString(),
            },
            {
                shell: {
                    reply: (message) => {
                        let content = message?.content;
                        console.log("clientinfo_reply message and content", message, content);
                        this.hostingEnvironment = content?.hosting_environment;
                        this.iqsharpVersion = content?.iqsharp_version;
                        this.telemetryOptOut = content?.telemetry_opt_out;
                        console.log(`Using IQ# version ${this.iqsharpVersion} on hosting environment ${this.hostingEnvironment}.`);

                        this.initTelemetry();
                    }
                }
            }
        );
    }

    initTelemetry() {
        if (this.telemetryOptOut) {
            console.log("Telemetry is turned-off");
            return;
        }

        var isLocalEnvironment =
            location.hostname == "localhost"
            || location.hostname == "127.0.0.1"
            || this.hostingEnvironment == null
            || this.hostingEnvironment == "";

        if (isLocalEnvironment) {
            console.log("Client telemetry disabled on local environment");
            return;
        }

        Telemetry.origin = this.getOriginQueryString();
        Telemetry.clientInfoAvailable.on((clientInfo: ClientInfo) => {
            IPython.notebook.kernel.send_shell_message(
                "iqsharp_clientinfo_request",
                {
                    "client_country": clientInfo.CountryCode,
                    "client_id": clientInfo.Id,
                    "client_isnew": clientInfo.IsNew,
                    "client_first_origin": clientInfo.FirstOrigin,
                }
            );
        });
        Telemetry.initAsync();
    }

    initExecutionPathVisualizer() {
        IPython.notebook.kernel.register_iopub_handler(
            "render_execution_path",
            message => {
                const { executionPath, id } = message.content;
                renderExecutionPath(executionPath, id);
            }
        );
    }
}

export function onload() {
    defineQSharpMode();
    let kernel = new Kernel();
    console.log("Loaded IQ# kernel-specific extension!");
}
