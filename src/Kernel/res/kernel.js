requirejs.config({
    bundles: {
        '/kernelspecs/iqsharp/bundle.js': ['kernel'],
    }
});

define(["exports", "kernel"], function (exports, kernel) {
    Object.defineProperty(exports, "onload", { enumerable: true, get: function () { return kernel.onload; } });
});
