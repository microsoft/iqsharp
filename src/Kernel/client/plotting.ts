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
                backgroundColor: "#4c4cff",
                    borderColor: "#4c4cff",
                }
            ],
        },
    options: {
            responsive: true,
            scales: {
                xAxes: [{
                    ticks: {
                        maxRotation: 0,
                        minRotation: 0
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

