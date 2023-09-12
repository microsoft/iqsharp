ARG CONDA_VER=latest
ARG UBUNTU_VER=20.04
ARG OS_TYPE=x86_64
ARG PY_VER=3.9

FROM mcr.microsoft.com/mirror/docker/library/ubuntu:${UBUNTU_VER}

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
        powershell \
        # install the below packages to pick up the latest security patch
        logsave \
        libext2fs2 \
        libss2 \
        libcom-err2 \
        e2fsprogs \
        libssl1.1 \
        openssl && \
    # We clean the apt cache at the end of each apt command so that the caches
    # don't get stored in each layer.
    apt-get clean && rm -rf /var/lib/apt/lists/

RUN ./miniconda/bin/conda update -y certifi
RUN ./miniconda/bin/conda update -y cryptography
