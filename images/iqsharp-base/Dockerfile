# The same image used by mybinder.org
FROM python:3.9-slim-bullseye

# Update pip.
RUN pip install --no-cache --upgrade pip

# Install Jupyter
RUN pip install --no-cache \
        setuptools==67.7.2 \
        ipython==8.8.0 \
        jupyter==1.0.0 \
        notebook==6.5.2

# Install APT prerequisites.
RUN apt-get update && \
    apt-get -y upgrade \
                       # Make sure SSL packages are up to date.
                       libsasl2-2 && \
    apt-get -y install \
                       # Dependencies for the .NET Core SDK.
                       wget \
                       pgp \
                       vim \
                       apt-transport-https \
                       # Dependencies for the Quantum Development Kit.
                       # Note that we install them here to minimize the number
                       # of layers.
                       libomp5-11 \
                       # Dependency for QIR
                       libc6 \
                       # Not strictly needed, but Git is useful for several
                       # interactive scenarios, so we finish by adding it as
                       # well. Thankfully, Git is a small dependency (~3 MiB)
                       # given what we have already installed.
                       git \
                       # Used to retrieve node version information.
                       curl && \
    # Upgrade optional dependencies brought in by the previous step.
    apt-get -y upgrade libidn2-0 && \
    # We clean the apt cache at the end of each apt command so that the caches
    # don't get stored in each layer.
    apt-get clean && rm -rf /var/lib/apt/lists/

# Trim down the size of the container by disabling the offline package
# cache. See also: https://github.com/dotnet/dotnet-docker/issues/237
ENV NUGET_XMLDOC_MODE=skip \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true

# Now that we have all the dependencies in place, we install the .NET Core SDK itself.
RUN wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.asc.gpg && \
    mv microsoft.asc.gpg /etc/apt/trusted.gpg.d/ && \
    wget -q https://packages.microsoft.com/config/debian/11/prod.list && \
    mv prod.list /etc/apt/sources.list.d/microsoft-prod.list && \
    chown root:root /etc/apt/trusted.gpg.d/microsoft.asc.gpg && \
    chown root:root /etc/apt/sources.list.d/microsoft-prod.list && \
    apt-get -y update && \
    apt-get -y install dotnet-sdk-6.0 && \
    apt-get -y install procps && \
    apt-get clean && rm -rf /var/lib/apt/lists/

# create user with a home directory
# Required for mybinder.org
ARG NB_USER=jovyan
ARG NB_UID=1000
ENV USER=${NB_USER} \
    UID=${NB_UID} \
    HOME=/home/${NB_USER} \
    IQSHARP_HOSTING_ENV=iqsharp-base \
    # Some ways of invoking this image will look at the $SHELL environment
    # variable instead of chsh, so we set the new user's shell here as well.
    SHELL=/bin/bash

RUN adduser --disabled-password \
    --gecos "Default user" \
    --uid ${UID} \
    ${USER} && \
    # Set the new user's shell to be bash when logging in interactively.
    chsh -s /bin/bash ${USER}
WORKDIR ${HOME}

# Provide local copies of all relevant packages.
ENV LOCAL_PACKAGES=${HOME}/.packages
ARG EXTRA_NUGET_SOURCES=
# Add the local NuGet packages folder as a source.
RUN mkdir -p ${HOME}/.nuget/NuGet && \
    echo "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n\
          <configuration>\n\
              <packageSources>\n\
                  <add key=\"nuget\" value=\"https://api.nuget.org/v3/index.json\" />\n\
                  <add key=\"context\" value=\"${LOCAL_PACKAGES}/nugets\" />\n\
                  ${EXTRA_NUGET_SOURCES}\n\
              </packageSources>\n\
          </configuration>\n\
    " > ${HOME}/.nuget/NuGet/NuGet.Config && \
    echo "Using NuGet.Config:" && \
    cat ${HOME}/.nuget/NuGet/NuGet.Config
# Add Python and NuGet packages from the build context
ADD nugets/*.nupkg ${LOCAL_PACKAGES}/nugets/
# When adding wheels, use *-any.whl to make sure that platform-specific wheels
# are not incorrectly added to the Docker image.
ADD wheels/*-any.whl ${LOCAL_PACKAGES}/wheels/

# Give the notebook user ownership over the packages and config copied from
# the context.
RUN chown ${USER} -R ${LOCAL_PACKAGES}/ && \
    chown ${USER} -R ${LOCAL_PACKAGES}/ ${HOME}/.nuget && \
    # Install all wheels from the build context.
    pip install $(ls ${LOCAL_PACKAGES}/wheels/*.whl)

# Get the Azure CLI tool
ENV AZURE_CLI_VERSION "2.44.1"

# Clean apt-cache after install, since Azure CLI install script does an apt-get update underneath, and does not clean cache afterwards
RUN curl -sL https://aka.ms/InstallAzureCLIDeb | bash; apt-get clean && rm -rf /var/lib/apt/lists/ && \
    # Get the az quantum extension
    az extension add --source https://msquantumpublic.blob.core.windows.net/az-quantum-cli/quantum-latest-py3-none-any.whl --yes && \
    # Assign the .azure folder back to the notebook user so that they
    # can run az commands.
    chown -R ${USER}:${USER} /home/${USER}/.azure

# Switch to the notebook user to finish the installation.
USER ${USER}
# Make sure that .NET Core is on the notebook users' path.
ENV PATH=$PATH:${HOME}/dotnet:${HOME}/.dotnet/tools \
    DOTNET_ROOT=${HOME}/dotnet
# Install IQ# and the project templates, using the NuGet packages from the
# build context.
ARG IQSHARP_VERSION
RUN dotnet new -i "Microsoft.Quantum.ProjectTemplates::0.28.302812" && \
    dotnet tool install \
           --global \
           Microsoft.Quantum.IQSharp \
           --version ${IQSHARP_VERSION} && \
    dotnet iqsharp install --user --path-to-tool="$(which dotnet-iqsharp)"

# Ensure that the necessary NuGet packages are cached.
ARG EXTRA_NUGET_PACKAGES=
RUN echo "Adding standard packages..." && \
    dotnet new classlib -n "foo" -lang Q# && \
    echo "Adding extra packages: ${EXTRA_NUGET_PACKAGES}..." && \
    for p in ${EXTRA_NUGET_PACKAGES}; do \
        dotnet add foo package $p --version ${IQSHARP_VERSION} && \
        echo "Added package: $p"; \
    done && \
    rm -Rdf foo
