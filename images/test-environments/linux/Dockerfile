ARG CONDA_VER=latest
ARG UBUNTU_VER=20.04
ARG OS_TYPE=x86_64
ARG PY_VER=3.7

FROM ubuntu:latest

RUN apt-get update && apt-get install -yq curl wget jq

ARG CONDA_VER
ARG OS_TYPE
# Install miniconda to /miniconda
RUN curl -LO "http://repo.continuum.io/miniconda/Miniconda3-${CONDA_VER}-Linux-${OS_TYPE}.sh" && \
    bash Miniconda3-${CONDA_VER}-Linux-${OS_TYPE}.sh -p /miniconda -b && \
    rm Miniconda3-${CONDA_VER}-Linux-${OS_TYPE}.sh
ENV PATH=/miniconda/bin:${PATH}
RUN conda update -y conda

ARG PY_VER
# Set Python version for conda
RUN conda install -c anaconda -y python=${PY_VER}

ARG UBUNTU_VER
# Download the Microsoft repository GPG keys
RUN wget -q "https://packages.microsoft.com/config/ubuntu/${UBUNTU_VER}/packages-microsoft-prod.deb"
# Register the Microsoft repository GPG keys
RUN dpkg -i packages-microsoft-prod.deb

# Update the list of packages after we added packages.microsoft.com
RUN apt-get update

# Install powershell and git
RUN apt-get install -y powershell
RUN apt-get install -y git