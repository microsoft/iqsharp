// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// This is a bit of a hack needed to map the requireJS
// call made by TypeScript onto the URL that Jupyter
// makes our kernelspec available at.
//
// Using this hack, we can split the type import and
// the runtime import apart, then glue them back
// together using a declare global to solve TS2686.
/// <amd-dependency path="chart" name="Chart" />
import type * as ChartJs from "chart.js";
declare global {
    const Chart: typeof ChartJs;
}

export interface Complex {
    Real: number;
    Imag: number;
    Magnitude: number;
    Phase: number;
};

export interface DisplayableState {
    n_qubits: number;
    div_id: string;
    amplitudes: Complex[] | null;
};

export type PlotStyle = "amplitude-phase" | "amplitude-squared" | "real-imag";

export function updateChart(plotStyle: PlotStyle, chart: ChartJs, state: DisplayableState) {
    fitChart(chart, state);
    switch (plotStyle) {
        case "amplitude-phase":
            updateWithAmplitudePhaseData(chart, state);
            break;

        case "amplitude-squared":
            updateWithAmplitudeSquaredData(chart, state);
            break;

        case "real-imag":
            updateWithRealImagData(chart, state);
            break;
    }
}

function fitChart(chart: ChartJs, state: DisplayableState) {
    let chartWidth = state.amplitudes.length * 100;
    chart.canvas.parentElement.style.width = `${chartWidth}px`;
}

function updateWithAmplitudePhaseData(chart: ChartJs, state: DisplayableState) {
    let amps = state.amplitudes;
    let nBasisStates = amps.length;
    let nBitLength = Math.ceil(Math.log2(nBasisStates));

    chart.data = {
        labels: Array.from(Array(nBasisStates), (_, idx) => {
            let bitstring = (idx >>> 0).toString(2).padStart(nBitLength, "0");
            return `|${bitstring}⟩`;
        }), //basis state labels
        datasets: [
            {
                data: Array.from(Array(nBasisStates), (_, idx) => {
                    return (amps[idx].Magnitude);
                }),
                backgroundColor: "#4c4cff",
                borderColor: "#4c4cff",
                label: "Amplitude"
            },
            {
                data: Array.from(Array(nBasisStates), (_, idx) =>{
                    return (amps[idx].Phase);
                }),
                backgroundColor: "#4c4cff",
                borderColor: "#4c4cff",
                label: "Phase"
            }  
        ],
    };
    chart.options.legend = {
        display: false,
    };
    chart.options.scales = {
        xAxes: [{
            scaleLabel: {
                display: true,
                labelString: 'Basis States'
            },
            ticks: {
                maxRotation: 0,
                minRotation: 0
            }
        }],
        yAxes: [{
            scaleLabel: {
                display: true,
                labelString: 'Amplitude and Phase'
            },
            ticks: {
                beginAtZero: true
            }
        }]
    };
    chart.update();

}

function updateWithAmplitudeSquaredData(chart: ChartJs, state: DisplayableState) {
    let amps = state.amplitudes;
    let nBasisStates = amps.length;
    let nBitLength = Math.ceil(Math.log2(nBasisStates));

    chart.data = {
        labels: Array.from(Array(nBasisStates), (_, idx) => {
            let bitstring = (idx >>> 0).toString(2).padStart(nBitLength, "0");
            return `|${bitstring}⟩`;
        }), //basis state labels
        datasets: [
            {
                data: Array.from(Array(nBasisStates), (_, idx) => {
                    return (amps[idx].Magnitude ** 2);
                }),
                backgroundColor: "#5390d9",
                borderColor: "#5390d9",
            }
        ],
    };
    chart.options.legend = {
        display: false,
    };
    chart.options.scales = {
        xAxes: [{
            scaleLabel: {
                display: true,
                labelString: 'Basis States'
            },
            ticks: {
                maxRotation: 0,
                minRotation: 0
            }
        }],
        yAxes: [{
            scaleLabel: {
                display: true,
                labelString: 'Measurement Probability'
            },
            ticks: {
                beginAtZero: true,
                suggestedMax: 1,
                suggestedMin: 0
            }
        }]
    };

    chart.update();
}

function updateWithRealImagData(chart: ChartJs, state: DisplayableState) {
    let amps = state.amplitudes;
    let nBasisStates = amps.length;
    let nBitLength = Math.ceil(Math.log2(nBasisStates));
    
    chart.data = {
        labels: Array.from(Array(nBasisStates), (_, idx) => {
            let bitstring = (idx >>> 0).toString(2).padStart(nBitLength, "0");
            return `|${bitstring}⟩`;
        }), //basis state labels
        datasets: [
            {
                data: Array.from(Array(nBasisStates), (_, idx) => {
                    return (amps[idx].Real);
                }),
                backgroundColor: "#5390d9",
                borderColor: "#5390d9",
                label: "Real"
            },
            {
                data: Array.from(Array(nBasisStates), (_, idx) => {
                    return (amps[idx].Imag);
                }),
                backgroundColor: "#48bfe3",
                borderColor: "#48bfe3",
                label: "Imaginary"
            }
        ],
    };
    chart.options.legend = {
        display: false,
    };
    chart.options.scales = {
        xAxes: [{
            scaleLabel: {
                display: true,
                labelString: 'Basis States'
            },
            ticks: {
                maxRotation: 0,
                minRotation: 0
            }
        }],
        yAxes: [{
            scaleLabel: {
                display: true,
                labelString: 'Real and Imaginary'
            },
            ticks: {
                beginAtZero: true,
                suggestedMax: 1,
                suggestedMin: -1
            }
        }]
    };

    chart.update();
}

export function createNewCanvas(
    parentNode: HTMLElement, initialState?: DisplayableState | null
): { chart: ChartJs } {
    let canvas = document.createElement("canvas");
    canvas.style.width = "100%"
    let measurementHistogram = new Chart(canvas, {
        type: 'bar',
        options: {
            responsive: true,
            maintainAspectRatio: false
        }
    });

    if (initialState !== null && initialState !== undefined) {
        updateWithAmplitudeSquaredData(measurementHistogram, initialState);
    }

    parentNode.appendChild(canvas);

    return { chart: measurementHistogram };
}

export function addToolbarButton(container: HTMLElement, label: string, onClick: EventListener) {
    let toolbarButton = document.createElement("button");
    toolbarButton.appendChild(document.createTextNode(label));
    container.appendChild(toolbarButton);
    toolbarButton.addEventListener("click", onClick);
    toolbarButton.className = "btn btn-default btn-sm"
    toolbarButton.style.marginRight = "10px";
}

export function createToolbarContainer(toolbarName: string) {
    let toolbarContainer = document.createElement("div");
    toolbarContainer.style.marginTop = "10px";
    toolbarContainer.style.marginBottom = "10px";

    let toolbarTitle = document.createElement("span");
    toolbarTitle.appendChild(document.createTextNode(toolbarName))
    toolbarTitle.style.marginRight = "10px";
    toolbarTitle.style.fontWeight = "bold";
    toolbarContainer.appendChild(toolbarTitle);

    return toolbarContainer;
}

export function attachDumpMachineToolbar(chart: ChartJs, state: DisplayableState) {
    // Create toolbar container and insert at the beginning of the state div
    let stateDiv = document.getElementById(state.div_id);
    let toolbarContainer = createToolbarContainer("Chart options:");
    stateDiv.insertBefore(toolbarContainer, stateDiv.firstChild);

    // Create buttons to change plot style
    addToolbarButton(toolbarContainer, "Measurement Probability", event => updateWithAmplitudeSquaredData(chart, state));
    addToolbarButton(toolbarContainer, "Amplitude and Phase", event => updateWithAmplitudePhaseData(chart, state));
    addToolbarButton(toolbarContainer, "Real and Imaginary", event => updateWithRealImagData(chart, state));
                        
    // Add horizontal rule above toolbar
    stateDiv.insertBefore(document.createElement("hr"), stateDiv.firstChild);
};

export function createBarChart(element: HTMLCanvasElement, state: DisplayableState) {
    let amps = state.amplitudes;
    let nBasisStates = amps.length;
    let nBitLength = Math.ceil(Math.log2(nBasisStates));
    

    const measurementHistogram = new Chart(element, {
        type: 'bar',
        data: {
            labels: Array.from(Array(nBasisStates), (_, idx) => {
                let bitstring = (idx >>> 0).toString(2).padStart(nBitLength, "0");
                return `|${bitstring}⟩`;
            }), //basis state labels
        datasets: [
                {
                data: Array.from(Array(nBasisStates), (_, idx) => {
                    return (amps[idx].Magnitude ** 2);
                }),
                backgroundColor: "#5390d9",
                borderColor: "#5390d9",
                }
            ],
        },
    options: {
            responsive: true,
        legend: {
            display: false,
        },
            scales: {
                xAxes: [{
                    scaleLabel: {
                        display: true,
                        labelString: 'Basis States'
                    },
                    ticks: {
                        maxRotation: 0,
                        minRotation: 0
                    }
                }],
                yAxes: [{
                    scaleLabel: {
                        display: true,
                        labelString: 'Measurement Probability'
                    },
                    ticks: {
                        beginAtZero: true,
                        suggestedMax: 1,
                        suggestedMin: 0
                    }
                }]
            }
        }
    });

};

export function createBarChartRealImagOption(element: HTMLCanvasElement, state: DisplayableState) {
    let amps = state.amplitudes;
    let nBasisStates = amps.length;
    let nBitLength = Math.ceil(Math.log2(nBasisStates));
    
    const measurementHistogram = new Chart(element, {
        type: 'bar',
        data: {
            labels: Array.from(Array(nBasisStates), (_, idx) => {
                let bitstring = (idx >>> 0).toString(2).padStart(nBitLength, "0");
                return `|${bitstring}⟩`;
            }), //basis state labels
            datasets: [
                {
                    data: Array.from(Array(nBasisStates), (_, idx) => {
                        return (amps[idx].Real);
                    }),
                    backgroundColor: "#5390d9",
                    borderColor: "#5390d9",
                    label: "Real"
                },
                {
                    data: Array.from(Array(nBasisStates), (_, idx) => { 
                        return (amps[idx].Imag);
                    }),
                    backgroundColor: "#48bfe3",
                    borderColor: "#48bfe3",
                    label: "Imaginary"
                }
            ],
        },
        options: {
            responsive: true,
            legend: {
                display: true,
            },
            scales: {
                xAxes: [{
                    scaleLabel: {
                        display: true,
                        labelString: 'Basis States'
                    },
                    ticks: {
                        maxRotation: 0,
                        minRotation: 0
                    }
                }],
                yAxes: [{
                    scaleLabel: {
                        display: true,
                        labelString: 'Real and Imaginary'
                    },
                    ticks: {
                        suggestedMax: 1,
                        suggestedMin: -1
                    }
                }]
            }
        }
    });

};

export function createBarChartAmplitudePhaseOption(element: HTMLCanvasElement, state: DisplayableState) {
    let amps = state.amplitudes;
    let nBasisStates = amps.length;
    let nBitLength = Math.ceil(Math.log2(nBasisStates));

    const measurementHistogram = new Chart(element, {
        type: 'bar',
        data: {
            labels: Array.from(Array(nBasisStates), (_, idx) => {
                let bitstring = (idx >>> 0).toString(2).padStart(nBitLength, "0");
                return `|${bitstring}⟩`;
            }), //basis state labels
            datasets: [
                {
                    data: Array.from(Array(nBasisStates), (_, idx) => {
                        return (amps[idx].Magnitude);
                    }),
                    backgroundColor: "#4c4cff",
                    borderColor: "#4c4cff",
                    label: "Amplitude"
                },
                {
                    data: Array.from(Array(nBasisStates), (_, idx) => {
                        return (amps[idx].Phase);
                    }),
                    backgroundColor: "#4c4cff",
                    borderColor: "#4c4cff",
                    label: "Phase"
                }
            ],
        },
        options: {
            responsive: true,
            legend: {
                display: false,
            },
            scales: {
                xAxes: [{
                    scaleLabel: {
                        display: true,
                        labelString: 'Basis States'
                    },
                    ticks: {
                        maxRotation: 0,
                        minRotation: 0
                    }
                }],
                yAxes: [{
                    scaleLabel: {
                        display: true,
                        labelString: 'Amplitude and Phase'
                    },
                    ticks: {
                        beginAtZero: true,
                    }
                }]
            }
        }
    });

};
