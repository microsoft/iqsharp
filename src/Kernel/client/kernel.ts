// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
///<amd-dependency path="codemirror/lib/codemirror" />
///<amd-dependency path="codemirror/addon/mode/simple" />

import { IPython } from "./ipython";
declare var IPython : IPython;

import { Telemetry, ClientInfo } from "./telemetry";
import type * as ChartJs from "chart.js";
import { DisplayableState, createBarChart, createBarChartRealImagOption, createBarChartAmplitudePhaseOption, attachDumpMachineToolbar, createNewCanvas, PlotStyle, updateChart } from "./plotting";
import { defineQSharpMode } from "./syntax";

class Kernel {
    hostingEnvironment : string | undefined;
    iqsharpVersion : string | undefined;
    telemetryOptOut? : boolean | null;

    constructor() {
        IPython.notebook.kernel.events.on("kernel_ready.Kernel", args => {
            this.requestEcho();
            this.requestClientInfo();
            this.setupMeasurementHistogramDataListener();
            this.setupDebugSessionHandlers();
        });
    }

    setupDebugSessionHandlers() {
        let activeSessions = new Map<string, {
            chart: ChartJs,
            lastState: DisplayableState | null,
            plotStyle: PlotStyle
        }>();
        let update = (debugSession: string) => {
            let state = (activeSessions.get(debugSession)).lastState;
            if (state !== null) {
                updateChart(
                    activeSessions.get(debugSession).plotStyle,
                    activeSessions.get(debugSession).chart,
                    state
                );
            }
        };
        IPython.notebook.kernel.register_iopub_handler(
            "iqsharp_debug_sessionstart", message => {
                let debugSession: string = message.content.debug_session;
                let stateDiv: (string | null) = message.content.div_id;
                
                if (stateDiv != null) {
                    let div = document.getElementById(stateDiv);
                    if (div != null) {
                        let { canvas: graph, chart: chart } = createNewCanvas(div);
                        activeSessions.set(debugSession, {
                            chart: chart,
                            lastState: null,
                            plotStyle: "amplitude-phase"
                        });
                        let startDebugToolbar = document.createElement("button");
                        startDebugToolbar.appendChild(document.createTextNode("Step through Program"));
                        div.appendChild(startDebugToolbar);

                        startDebugToolbar.addEventListener("click", event => {
                            this.advanceMeasurementHistogramDebugger(debugSession);
                        });

                        //create buttons that will set the plotstyle to one of three options
                        //update function handles switching between plotstyle
                        let amplitudePhaseButton = document.createElement("button");
                        amplitudePhaseButton.appendChild(document.createTextNode("Amplitude/Phase"));
                        div.appendChild(amplitudePhaseButton);

                        amplitudePhaseButton.addEventListener("click", event => {
                            activeSessions.get(debugSession).plotStyle = "amplitude-phase";
                            update(debugSession);
                        });

                        let amplitudeSquaredButton = document.createElement("button");
                        amplitudeSquaredButton.appendChild(document.createTextNode("Amplitude^2"));
                        div.appendChild(amplitudeSquaredButton);

                        amplitudeSquaredButton.addEventListener("click", event => {
                            activeSessions.get(debugSession).plotStyle = "amplitude-squared";
                            update(debugSession);
                        });

                        let realImagButton = document.createElement("button");
                        realImagButton.appendChild(document.createTextNode("Real/Imag"));
                        div.appendChild(realImagButton);

                        realImagButton.addEventListener("click", event => {
                            activeSessions.get(debugSession).plotStyle = "real-imag";
                            update(debugSession);
                        });

                    }
                }
                
            }
        );

        IPython.notebook.kernel.register_iopub_handler(
            "iqsharp_debug_opstart",
            message => {
                console.log("iqsharp_debug_opstart message received", message);

                let state: DisplayableState = message.content.state;
                let debugSession: string = message.content.debug_session;
                activeSessions.get(debugSession).lastState = state;
                update(debugSession);
            }
        );
    }

    advanceMeasurementHistogramDebugger(debugSession: string) {
        console.log("entered advanceMeasurementHistogramDebugger");
        IPython.notebook.kernel.send_shell_message(
            "iqsharp_debug_advance",
            { debug_session: debugSession },
            {
                shell: {
                    reply: (message) => {
                        console.log("Got iqsharp_debug_advance reply:", message);
                    }
                }
            }
        );

    }

    setupMeasurementHistogramDataListener() {
        IPython.notebook.kernel.register_iopub_handler(
            "iqsharp_state_dump",
            message => {
                console.log("my message received", message);
                let state: DisplayableState = message.content.state;
                let state_div = state.div_id;
                if (state_div != null) {
                    let div = document.getElementById(state_div);
                    if (div != null) {
                        let { canvas: graph, chart: chart} = createNewCanvas(div, state);
                        attachDumpMachineToolbar(graph, chart, state);
                    }
                }
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
    let kernel = new Kernel();
    console.log("Loaded IQ# kernel-specific extension!");
}

