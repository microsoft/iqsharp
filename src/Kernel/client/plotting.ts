// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// This is a bit of a hack needed to map the requireJS
// call made by TypeScript onto the URL that Jupyter
// makes our kernelspec available at.
//
// Using this hack, we can split the type import and
// the runtime import apart, then glue them back
// together using a declare global to solve TS2686.
/// <amd-dependency path="/kernelspecs/iqsharp/chart.js" name="Chart" />
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


export function initializePlotting() {
    // importScript("https://cdn.jsdelivr.net/npm/chart.js@2.8.0");
};



export function createBarChart(element: HTMLCanvasElement, state: DisplayableState) {
    var amps = state.amplitudes;

    let newCount = amps.length;
    let nQubits = Math.log2(newCount) >>> 0;

    var measurementHistogram = new Chart(element, {
        type: 'bar',
        data: {
            labels: Array.from(Array(amps.length).keys()).map(idx => {
                var bitstring = (idx >>> 0).toString(2).padStart(nQubits, "0");
                return `|${bitstring}⟩`;
            }), //basis state labels
        datasets: [
                {
                data: Array.from(Array(amps.length).keys()).map(idx => {
                    return (amps[idx].Magnitude ** 2);
                }),
                backgroundColor: "#5390d9",
                borderColor: "#5390d9",
                }
            ],
        },
    options: {
            responsive: false,
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
                        labelString: 'Meas. Probability'
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
    var amps = state.amplitudes;

    let newCount = amps.length;
    let nQubits = Math.log2(newCount) >>> 0;

    var measurementHistogram = new Chart(element, {
        type: 'bar',
        data: {
            labels: Array.from(Array(amps.length).keys()).map(idx => {
                var bitstring = (idx >>> 0).toString(2).padStart(nQubits, "0");
                return `|${bitstring}⟩`;
            }), //basis state labels
            datasets: [
                {
                    data: Array.from(Array(amps.length).keys()).map(idx => {
                        return (amps[idx].Real);
                    }),
                    backgroundColor: "#5390d9",
                    borderColor: "#5390d9",
                    label: "Real"
                },
                {
                    data: Array.from(Array(amps.length).keys()).map(idx => {
                        return (amps[idx].Imag);
                    }),
                    backgroundColor: "#48bfe3",
                    borderColor: "#48bfe3",
                    label: "Imag"
                }
            ],
        },
        options: {
            responsive: false,
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
                        labelString: 'Meas. Probability'
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
    var amps = state.amplitudes;

    let newCount = amps.length;
    let nQubits = Math.log2(newCount) >>> 0;

    var measurementHistogram = new Chart(element, {
        type: 'bar',
        data: {
            labels: Array.from(Array(amps.length).keys()).map(idx => {
                var bitstring = (idx >>> 0).toString(2).padStart(nQubits, "0");
                return `|${bitstring}⟩`;
            }), //basis state labels
            datasets: [
                {
                    data: Array.from(Array(amps.length).keys()).map(idx => {
                        return (amps[idx].Magnitude);
                    }),
                    backgroundColor: "#4c4cff",
                    borderColor: "#4c4cff",
                    label: "Amplitude"
                },
                {
amps.map(amp => amp.Phase),
                        return (amps[idx].Phase);
                    }),
                    backgroundColor: "#4c4cff",
                    borderColor: "#4c4cff",
                    label: "Phase"
                }
            ],
        },
        options: {
            responsive: false,
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
                        labelString: 'Meas. Probability'
                    },
                    ticks: {
                        beginAtZero: true,
                    }
                }]
            }
        }
    });

};
