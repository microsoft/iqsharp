# Self-contained environments for conda testing and packing

Testing CI runs conda testing and packing on the build agent host. This can make it difficult to find and diagnose issues caused by missing dependencies. For example the build agent host can inadvertently provide dependencies that should be provided by conda packages themselves. Conda testing and packing should be executed from within a brand-new Docker container, in order to emulate a minimal environment as may be installed by users.

Each Dockerfile in this directory represents the contained environments to be used from the build agent for conda testing and packing. Whenever any of these files are changed the new images will be built and pushed to the internal MCR `msint.azure.io`. MacOS is not supported as a pipeline Container job and requires further investigation.

Each Dockerfile includes instructions to download the following tools:

* PowerShell 7
* Miniconda3
* Python 3.9
* Git
