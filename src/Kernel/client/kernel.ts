// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

///<amd-dependency path="codemirror/lib/codemirror" />
///<amd-dependency path="codemirror/addon/mode/simple" />

import { IPython } from "./ipython";
declare var IPython : IPython;

import { Telemetry, ClientInfo } from "./telemetry";
import { initializePlotting, DisplayableState, createBarChart } from "./plotting";
import { defineQSharpMode } from "./syntax";
import * as ChartJs from "chart.js";

class Kernel {
    hostingEnvironment : string | undefined;
    iqsharpVersion : string | undefined;
    telemetryOptOut? : boolean | null;

    constructor() {
        IPython.notebook.kernel.events.on("kernel_ready.Kernel", args => {
            this.requestEcho();
            this.requestClientInfo();
            this.setupMeasurementHistogramDataListener();
        });
    }

    setupMeasurementHistogramDataListener() {
        IPython.notebook.kernel.register_iopub_handler(
            "iqsharp_state_dump",
            message => {
                console.log("my message received", message);

                //create buttons as DOM objects in order to attach unique event handlers
                let state: DisplayableState = message.content.state;
                let state_div = state.div_id;
                if (state_div != null) {
                    let div = document.getElementById(state_div);
                    if (div != null) {
                        let button = document.createElement("button");
                        let p = document.createElement("p");
                        button.appendChild(document.createTextNode("Number of Qubits"));
                        button.addEventListener("click", event => {
                            p.innerHTML = message.content.state.n_qubits;
                        });
                        div.appendChild(button);
                        div.appendChild(p);
                        // todo: make a new div
                        
                        let button2 = document.createElement("button");
                        let printthingy = document.createElement("p");

                        let graph = document.createElement("canvas");
                        button2.appendChild(document.createTextNode("Show Graph"));
                        button.addEventListener("click", event => {
                            var txt = "";
                            var x;
                            var amps = state.amplitudes;

                            let newCount = amps.length;
                            let nQubits = Math.log2(newCount) >>> 0;
                            let bit = [];
                            bit = Array.from(Array(amps.length).keys()).map(idx => {
                                var bitstring = (idx >>> 0).toString(2).padStart(nQubits, "0");
                                return `|${bitstring}⟩`;
                            });
                            for (x in amps) {  
                                txt += 'Basis State Label: ' + bit[x] + ' Percentage: ' + (amps[x].Magnitude ** 2)+ '\n';
                            }
                            printthingy.innerHTML = txt;
                        });
                        
                        div.appendChild(button2);
                        div.appendChild(printthingy);
                        createBarChart(graph, state);
                        div.appendChild(graph);
                    }
                
                }
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
}

export function onload() {
    defineQSharpMode();
    initializePlotting();
    let kernel = new Kernel();
    console.log("Loaded IQ# kernel-specific extension!");
}

