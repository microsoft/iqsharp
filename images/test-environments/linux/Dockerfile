ARG CONDA_VER=latest
ARG UBUNTU_VER=20.04
ARG OS_TYPE=x86_64
ARG PY_VER=3.7
ARG SOURCE=""

FROM ${SOURCE}ubuntu:${UBUNTU_VER}

RUN apt-get update && apt-get install -yq curl wget jq

ARG CONDA_VER
ARG OS_TYPE
# Install miniconda to /miniconda
RUN curl -LO "http://repo.continuum.io/miniconda/Miniconda3-${CONDA_VER}-Linux-${OS_TYPE}.sh" && \
    bash Miniconda3-${CONDA_VER}-Linux-${OS_TYPE}.sh -p /miniconda -b && \
    rm Miniconda3-${CONDA_VER}-Linux-${OS_TYPE}.sh

ARG UBUNTU_VER
# Download the Microsoft repository GPG keys
RUN wget -q "https://packages.microsoft.com/config/ubuntu/${UBUNTU_VER}/packages-microsoft-prod.deb"
# Register the Microsoft repository GPG keys
RUN dpkg -i packages-microsoft-prod.deb

ARG DEBIAN_FRONTEND=noninteractive
# Update the list of packages after we added packages.microsoft.com
RUN apt-get update && \
    apt-get -y install \
        git \
        powershell && \
    # We clean the apt cache at the end of each apt command so that the caches
    # don't get stored in each layer.
    apt-get clean && rm -rf /var/lib/apt/lists/
