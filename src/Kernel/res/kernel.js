requirejs.config({
    bundles: {
        'bundle': ['kernel'],
        'chart': ['chart.js'],
        'quantum-viz': ['@microsoft/quantum-viz.js'],
    },
    paths: {
        'bundle': window.IPython.notebook.base_url + 'kernelspecs/iqsharp/bundle',
        'chart': window.IPython.notebook.base_url + 'kernelspecs/iqsharp/chart',
        'quantum-viz': window.IPython.notebook.base_url + 'kernelspecs/iqsharp/quantum-viz',
    },
});

define(["exports", "kernel"], function (exports, kernel) {
    Object.defineProperty(exports, "onload", { enumerable: true, get: function () { return kernel.onload; } });
});
