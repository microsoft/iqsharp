// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

///<amd-dependency path="codemirror/lib/codemirror" />
///<amd-dependency path="codemirror/addon/mode/simple" />

import { IPython } from "./ipython";
declare var IPython : IPython;

declare global {
    interface Window { IQSharp: IQSharp; }
}

interface IQSharp {
    start_debug(session_id: string) : void;
    advance_debug(session_id: string) : void;
}

function defineQSharpMode() {
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
}

class Kernel {
    hostingEnvironment : string | undefined;
    iqsharpVersion : string | undefined;

    constructor() {
        IPython.notebook.kernel.events.on("kernel_ready.Kernel", args => {
            this.requestEcho();
            this.requestClientInfo();
            this.registerDebugIoPubEvents();
        });
    }

    registerDebugIoPubEvents() {
        IPython.notebook.kernel.register_iopub_handler(
            "iqsharp_debug_opstart",
            message => {
                console.log("Starting operation", message.content.callable_name, message.content.callable_input);
                let sessionId = message.content.debug_session;
                $(`#iqsharp-current-${sessionId}`).text(message.content.callable_name);
            }
        )
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
            {value: value},
            {
                shell: {
                    reply: (message) => {
                        console.log("Got echo reply:", message);
                    }
                }
            }
        );
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
                "user_agent": navigator.userAgent
            },
            {
                shell: {
                    reply: (message) => {
                        let content = message?.content;
                        console.log("clientinfo_reply message and content", message, content);
                        this.hostingEnvironment = content?.hosting_environment;
                        this.iqsharpVersion = content?.iqsharp_version;
                        console.log(`Using IQ# version ${this.iqsharpVersion} on hosting environment ${this.hostingEnvironment}.`);
                    }
                }
            }
        );
    }
}

export function onload() {
    defineQSharpMode();
    let kernel = new Kernel();
    console.log("Loaded IQ# kernel-specific extension!");
    window.IQSharp = {
        start_debug: (session_id) => {
            $(`#iqsharp-next-${session_id}`).on("click", () => {
                window.IQSharp.advance_debug(`${session_id}`);
            });

            IPython.notebook.kernel.send_shell_message(
                "iqsharp_debug_request",
                {debug_session: session_id},
                {
                    shell: {
                        reply: (message) => {
                            console.log("Got reply", message);
                        }
                    }
                }
            );
        },

        advance_debug: (session_id) => {
            IPython.notebook.kernel.send_shell_message(
                "iqsharp_debug_advance",
                {debug_session: session_id}
            );
        }
    };
}

