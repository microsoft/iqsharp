// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

function importScript(url: string) {
    let script = document.createElement("script");
    script.setAttribute("src", url);
    document.body.appendChild(script);
}

export function initializePlotting() {
    importScript("https://cdn.jsdelivr.net/npm/chart.js@2.8.0");
}
