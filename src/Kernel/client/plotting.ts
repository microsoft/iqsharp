// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { Chart } from "chart.js";

export interface Complex {
    Real: number;
    Imag: number;
    Magnitude: number;
    Phase: number;
};

export interface IDisplayableState {
    n_qubits: number;
    div_id: string;
    amplitudes: {[idx: number]: number} | null;
};

export class DisplayableState implements IDisplayableState {
    n_qubits: number;
    div_id: string;
    amplitudes: {[idx: number]: number};

    constructor(state: IDisplayableState) {
        this.n_qubits = state.n_qubits;
        this.div_id = state.div_id;
        this.amplitudes = state.amplitudes;
    }

    getDenseAmplitudes(): Complex[] {
        let dense = [];
        for (var idx of Object.keys(this.amplitudes)) {
            dense[idx] = this.amplitudes[idx];
        }
        return dense;
    }
}

export type PlotStyle = "amplitude-phase" | "amplitude-squared" | "real-imag";

export function updateChart(plotStyle: PlotStyle, chart: Chart, state: DisplayableState) {
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

function fitChart(chart: Chart, state: DisplayableState) {
    let chartWidth = state.getDenseAmplitudes().length * 100;
    chart.canvas.parentElement.style.width = `${chartWidth}px`;
}

function updateWithAmplitudePhaseData(chart: Chart, state: DisplayableState) {
    let amps = state.getDenseAmplitudes();
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

function updateWithAmplitudeSquaredData(chart: Chart, state: DisplayableState) {
    let amps = state.getDenseAmplitudes();
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

function updateWithRealImagData(chart: Chart, state: DisplayableState) {
    let amps = state.getDenseAmplitudes();
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
): { chart: Chart } {
    let canvas = document.createElement("canvas");
    parentNode.style.position = "relative";
    parentNode.style.width = "100%";
    parentNode.style.height = "40vh";
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

export function attachDumpMachineToolbar(chart: Chart, state: DisplayableState, stateDiv: HTMLDivElement) {
    // Create toolbar container and insert at the beginning of the state div
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
    let amps = state.getDenseAmplitudes();
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
    let amps = state.getDenseAmplitudes();
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
    let amps = state.getDenseAmplitudes();
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
