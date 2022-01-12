// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { IPython } from "./ipython";
declare var IPython: IPython;

import { Telemetry, ClientInfo } from "./telemetry";
import { Chart } from "chart.js";
import { DisplayableState, addToolbarButton as addToolbarButton, attachDumpMachineToolbar, createNewCanvas, createToolbarContainer, PlotStyle, updateChart } from "./plotting";
import { defineQSharpMode } from "./syntax";
import { draw, Circuit, StyleConfig, STYLES } from "@microsoft/quantum-viz.js";

declare global {
    interface Window {
        iqsharp: Kernel;
    }
}

interface Complex {
    Real: number;
    Imaginary: number;
    Phase: number;
    Magnitude: number;
}

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
            chart: Chart,
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
                        // Create necessary elements so a large chart will scroll
                        let chartWrapperDiv = document.createElement("div");
                        chartWrapperDiv.style.overflowX = "scroll";
                        let innerChartWrapperDiv = document.createElement("div");
                        innerChartWrapperDiv.style.height = "350px";
                        stateDiv.appendChild(chartWrapperDiv);
                        chartWrapperDiv.appendChild(innerChartWrapperDiv);

                        let { chart: chart } = createNewCanvas(innerChartWrapperDiv);
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
                update(debugSession, activeSessions.get(debugSession).plotStyle);
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
        IPython.notebook.kernel.comm_manager.register_target<{state: DisplayableState, id: string}>(
            "iqsharp_state_dump",
            (comm, message) => {
                console.log("iqsharp_state_dump comm session opened", message);
                let state = message.content.data.state;
                //let stateDivId = state.div_id;
                let stateDivId = message.content.data.id;
                if (stateDivId != null) {
                    let stateDiv = document.getElementById(stateDivId);
                    if (stateDiv != null) {
                        let { chart: chart } = createNewCanvas(stateDiv, state);
                        attachDumpMachineToolbar(chart, state);
                    }
                }
                comm.close();
            }
        );
    }

    requestEcho() {
        let value = "hello!";
        let comm = IPython.notebook.kernel.comm_manager.new_comm("iqsharp_echo");
        comm.on_msg((message) => {
            console.log("Got echo output via comms:", message);
        });
        comm.on_close((message) => {
            console.log("Echo comm closed:", message);
        })
        comm.send(value);
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
        let comm_session = IPython.notebook.kernel.comm_manager.new_comm(
            "iqsharp_clientinfo",
            {
                "user_agent": navigator.userAgent,
                "client_language": navigator.language,
                "client_host": location.hostname,
                "client_origin": this.getOriginQueryString(),
            }
        );
        comm_session.on_msg((message) => {
            let client_info = message.content.data;
            console.log("clientinfo_reply message", client_info);
            this.hostingEnvironment = client_info.hosting_environment;
            this.iqsharpVersion = client_info.iqsharp_version;
            this.telemetryOptOut = client_info.telemetry_opt_out;
            console.log(`Using IQ# version ${this.iqsharpVersion} on hosting environment ${this.hostingEnvironment}.`);

            this.initTelemetry();
        });
    }

    initTelemetry() {
        if (this.telemetryOptOut) {
            console.log("Telemetry is turned-off");
            return;
        }

        const isLocalEnvironment =
            location.hostname == "localhost"
            || location.hostname == "127.0.0.1"
            || this.hostingEnvironment == null
            || this.hostingEnvironment == "";
        const forceEnableClientTelemetry =
            this.hostingEnvironment == "FORCE_ENABLE_CLIENT_TELEMETRY";

        if (!forceEnableClientTelemetry && isLocalEnvironment) {
            console.log("Client telemetry disabled on local environment");
            return;
        }

        Telemetry.origin = this.getOriginQueryString();
        Telemetry.clientInfoAvailable.on((clientInfo: ClientInfo) => {
            IPython.notebook.kernel.comm_manager.new_comm(
                "iqsharp_clientinfo",
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

    addCopyListener(elementId: string, data: string) {
        document.getElementById(elementId).onclick = async (ev: MouseEvent) => {
            await navigator.clipboard.writeText(data);
        };
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

                const container = document.getElementById(id);
                if (container != null) {
                    draw(executionPath, container, userStyleConfig);
                }
            }
        );
    }
}

export function onload() {
    defineQSharpMode();
    window.iqsharp = new Kernel();
    console.log("Loaded IQ# kernel-specific extension!");
}
