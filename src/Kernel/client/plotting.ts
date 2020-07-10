// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import * as ChartJs from "chart.js";


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

    var measurementHistogram = new window.Chart(element, {
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
                    backgroundColor: "#ff0000",
                    borderColor: "#ff0000",
                }
            ],
        },
    options: {
            responsive: false,
            scales: {
                xAxes: [{
                    ticks: {
                        maxRotation: 90,
                        minRotation: 80
                    }
                }],
                yAxes: [{
                    ticks: {
                        beginAtZero: true
                    }
                }]
            }
        }
    });

};

