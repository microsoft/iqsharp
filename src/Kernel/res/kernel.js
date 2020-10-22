requirejs.config({
    bundles: {
        'bundle': ['kernel'],
    },
    paths: {
        'bundle': window.IPython.notebook.base_url + 'kernelspecs/iqsharp/bundle',
        'chart': window.IPython.notebook.base_url + 'kernelspecs/iqsharp/chart',
    },
});

define(["exports", "kernel"], function (exports, kernel) {
    Object.defineProperty(exports, "onload", { enumerable: true, get: function () { return kernel.onload; } });
});
