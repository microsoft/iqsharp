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

function removeData(element: Chart) {
    element.data.labels.pop();
    element.data.datasets.forEach((dataset) => {
        dataset.data.pop();
    });
    element.update();

};

function addData(element: Chart, label: string, data) {
    element.data.labels.push(label);
    element.data.datasets.forEach((dataset) => {
        dataset.data.push(data);
    });
    element.update();

}

export type PlotStyle = "amplitude-phase" | "amplitude-squared" | "real-imag";

export function updateChart(plotStyle: PlotStyle, chart: ChartJs, state: DisplayableState) {
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

function updateWithAmplitudePhaseData(chart: ChartJs, state: DisplayableState) {
    let amps = state.amplitudes;
    let newCount = amps.length;
    chart.data = {
        labels: Array.from(Array(newCount).keys()).map(idx => {
            let bitstring = (idx >>> 0).toString(2).padStart(newCount, "0");
            return `|${bitstring}⟩`;
        }), //basis state labels
        datasets: [
            {
                data: Array.from(Array(newCount).keys()).map(idx => {
                    return (amps[idx].Magnitude);
                }),
                backgroundColor: "#4c4cff",
                borderColor: "#4c4cff",
                label: "Amplitude"
            },
            {
                data: Array.from(Array(newCount).keys()).map(idx => {
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
                labelString: 'Amplitude/Phase'
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
    let newCount = amps.length;
    chart.data = {
        labels: Array.from(Array(newCount).keys()).map(idx => {
            let bitstring = (idx >>> 0).toString(2).padStart(newCount, "0");
            return `|${bitstring}⟩`;
        }), //basis state labels
        datasets: [
            {
                data: Array.from(Array(newCount).keys()).map(idx => {
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
                labelString: 'Meas. Probability'
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
    let newCount = amps.length;
    
    chart.data = {
        labels: Array.from(Array(newCount).keys()).map(idx => {
            let bitstring = (idx >>> 0).toString(2).padStart(newCount, "0");
            return `|${bitstring}⟩`;
        }), //basis state labels
        datasets: [
            {
                data: Array.from(Array(newCount).keys()).map(idx => {
                    return (amps[idx].Real);
                }),
                backgroundColor: "#5390d9",
                borderColor: "#5390d9",
                label: "Real"
            },
            {
                data: Array.from(Array(newCount).keys()).map(idx => {
                    return (amps[idx].Imag);
                }),
                backgroundColor: "#48bfe3",
                borderColor: "#48bfe3",
                label: "Imag"
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
                labelString: 'Real/Imag'
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
    parentNode: HTMLElement, initalState?: DisplayableState | null
): { canvas: HTMLCanvasElement, chart: ChartJs } {
    let canvas = document.createElement("canvas");
    let measurementHistogram = new Chart(canvas, {
        type: 'bar',
        options: {
            responsive: false
        }
    });

    if (initalState !== null && initalState !== undefined) {
        updateWithAmplitudeSquaredData(measurementHistogram, initalState);
    }

    parentNode.appendChild(canvas);

    return {
        canvas: canvas,
        chart: measurementHistogram
    };
}

export function attachDumpMachineToolbar(element: HTMLCanvasElement, chart: ChartJs, state: DisplayableState) {
    //create legend and append to state div
    let legendDiv = document.createElement("div");
    legendDiv.id = "legendDiv";
   
    let state_div = state.div_id;
    let div = document.getElementById(state_div);
    div.appendChild(legendDiv);

    //style the legend
    document.getElementById("legendDiv").style.border = "thin solid #000000";
    document.getElementById("legendDiv").style.textAlign = "center";

    let legendTitle = document.createElement("p");
    legendTitle.innerHTML = "Select Graph";


    //create buttons
    let buttonAmplitudeSquared = document.createElement("button");
    buttonAmplitudeSquared.innerHTML = "amplitude^2";

    let buttonAmplitudePhase = document.createElement("button");
    buttonAmplitudePhase.innerHTML = "amplitude/phase";

    var buttonRealImag = document.createElement("button");
    buttonRealImag.innerHTML = "real/imag";

    //append button to legend
    legendDiv.appendChild(legendTitle);
    legendDiv.appendChild(buttonAmplitudeSquared);
    legendDiv.appendChild(buttonAmplitudePhase);
    legendDiv.appendChild(buttonRealImag);

    //create event listeners
    buttonAmplitudeSquared.addEventListener("click", function() {
        updateWithAmplitudeSquaredData(chart, state);
    });
    buttonAmplitudePhase.addEventListener("click", function () {
        updateWithAmplitudePhaseData(chart, state);
    });
    buttonRealImag.addEventListener("click", function () {
        updateWithRealImagData(chart, state);
    });

};

export function createBarChart(element: HTMLCanvasElement, state: DisplayableState) {
    let amps = state.amplitudes;
    let newCount = amps.length;

    const measurementHistogram = new Chart(element, {
        type: 'bar',
        data: {
            labels: Array.from(Array(newCount).keys()).map(idx => {
                let bitstring = (idx >>> 0).toString(2).padStart(newCount, "0");
                return `|${bitstring}⟩`;
            }), //basis state labels
        datasets: [
                {
                data: Array.from(Array(newCount).keys()).map(idx => {
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
    let amps = state.amplitudes;
    let newCount = amps.length;
    
    const measurementHistogram = new Chart(element, {
        type: 'bar',
        data: {
            labels: Array.from(Array(newCount).keys()).map(idx => {
                let bitstring = (idx >>> 0).toString(2).padStart(newCount, "0");
                return `|${bitstring}⟩`;
            }), //basis state labels
            datasets: [
                {
                    data: Array.from(Array(newCount).keys()).map(idx => {
                        return (amps[idx].Real);
                    }),
                    backgroundColor: "#5390d9",
                    borderColor: "#5390d9",
                    label: "Real"
                },
                {
                    data: Array.from(Array(newCount).keys()).map(idx => { 
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
                        labelString: 'Amplitude'
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
    let newCount = amps.length;

    const measurementHistogram = new Chart(element, {
        type: 'bar',
        data: {
            labels: Array.from(Array(newCount).keys()).map(idx => {
                let bitstring = (idx >>> 0).toString(2).padStart(newCount, "0");
                return `|${bitstring}⟩`;
            }), //basis state labels
            datasets: [
                {
                    data: Array.from(Array(newCount).keys()).map(idx => {
                        return (amps[idx].Magnitude);
                    }),
                    backgroundColor: "#4c4cff",
                    borderColor: "#4c4cff",
                    label: "Amplitude"
                },
                {
                    data: Array.from(Array(newCount).keys()).map(idx => {
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
                        labelString: 'Value'
                    },
                    ticks: {
                        beginAtZero: true,
                    }
                }]
            }
        }
    });

};
