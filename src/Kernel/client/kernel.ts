// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
///<amd-dependency path="codemirror/lib/codemirror" />
///<amd-dependency path="codemirror/addon/mode/simple" />

import { IPython } from "./ipython";
declare var IPython: IPython;

import { Telemetry, ClientInfo } from "./telemetry";
import type * as ChartJs from "chart.js";
import { DisplayableState, addToolbarButton as addToolbarButton, attachDumpMachineToolbar, createNewCanvas, createToolbarContainer, PlotStyle, updateChart } from "./plotting";
import { defineQSharpMode } from "./syntax";
import { Visualizer } from "./visualizer";
import { Circuit, StyleConfig, STYLES } from "./ExecutionPathVisualizer";

class Kernel {
    hostingEnvironment: string | undefined;
    iqsharpVersion: string | undefined;
    telemetryOptOut?: boolean | null;

    constructor() {
        if (IPython.notebook.kernel.is_connected()) {
            this.onStart();
        } else {
            IPython.notebook.kernel.events.on("kernel_ready.Kernel", args => this.onStart());
        }
    }

    onStart() {
        this.requestEcho();
        this.requestClientInfo();
        this.setupMeasurementHistogramDataListener();
        this.setupDebugSessionHandlers();
        this.initExecutionPathVisualizer();
    }

    setupDebugSessionHandlers() {
        let activeSessions = new Map<string, {
            chart: ChartJs,
            lastState: DisplayableState | null,
            plotStyle: PlotStyle
        }>();
        let update = (debugSession: string, plotStyle: PlotStyle) => {
            activeSessions.get(debugSession).plotStyle = plotStyle;
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
                console.log("iqsharp_debug_sessionstart message received", message);

                let debugSession: string = message.content.debug_session;
                let stateDivId: (string | null) = message.content.div_id;
                
                if (stateDivId != null) {
                    let stateDiv = document.getElementById(stateDivId);
                    if (stateDiv != null) {
                        let { chart: chart } = createNewCanvas(stateDiv);
                        activeSessions.set(debugSession, {
                            chart: chart,
                            lastState: null,
                            plotStyle: "amplitude-squared"
                        });

                        // Create toolbar container and insert at the beginning of the state div
                        let toolbarContainer = createToolbarContainer("Chart options:");
                        stateDiv.insertBefore(toolbarContainer, stateDiv.firstChild);

                        // Create buttons to change plot style
                        addToolbarButton(toolbarContainer, "Measurement Probability", event => update(debugSession, "amplitude-squared"));
                        addToolbarButton(toolbarContainer, "Amplitude and Phase", event => update(debugSession, "amplitude-phase"));
                        addToolbarButton(toolbarContainer, "Real and Imaginary", event => update(debugSession,  "real-imag"));

                        // Create debug toolbar
                        let debugContainer = createToolbarContainer("Debug controls:");
                        debugContainer.className = "iqsharp-debug-toolbar";
                        stateDiv.insertBefore(debugContainer, stateDiv.firstChild);
                        addToolbarButton(debugContainer, "▶️ Next step", event => this.advanceDebugger(debugSession));
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
                update(debugSession, "amplitude-squared");
            }
        );

        IPython.notebook.kernel.register_iopub_handler(
            "iqsharp_debug_sessionend",
            message => {
                console.log("iqsharp_debug_sessionend message received", message);

                let stateDivId: (string | null) = message.content.div_id;                
                if (stateDivId != null) {
                    let stateDiv = document.getElementById(stateDivId);
                    if (stateDiv != null) {
                        // Disable any buttons in the debug toolbar
                        stateDiv.querySelectorAll(".iqsharp-debug-toolbar button").forEach(button => {
                            (<HTMLInputElement>button).disabled = true;
                        });
                    }
                }
            }
        );
    }

    advanceDebugger(debugSession: string) {
        console.log("Sending iqsharp_debug_advance message");
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
                console.log("iqsharp_state_dump message received", message);
                let state: DisplayableState = message.content.state;
                let stateDivId = state.div_id;
                if (stateDivId != null) {
                    let stateDiv = document.getElementById(stateDivId);
                    if (stateDiv != null) {
                        let { chart: chart} = createNewCanvas(stateDiv, state);
                        attachDumpMachineToolbar(chart, state);
                    }
                }
            }
        );
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
                const {
                    executionPath,
                    id,
                    renderDepth,
                    style,
                }: { executionPath: Circuit; id: string; renderDepth: number; style: string } = message.content;
                
                // Get styles
                const userStyleConfig: StyleConfig = STYLES[style] || {};
    
                // Visualize execution path
                const visualizer = new Visualizer(id, userStyleConfig);
                visualizer.visualize(executionPath, renderDepth);
            }
        );
    }
}

export function onload() {
    defineQSharpMode();
    let kernel = new Kernel();
    console.log("Loaded IQ# kernel-specific extension!");
}
