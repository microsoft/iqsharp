FROM mcr.microsoft.com/quantum/iqsharp-base:latest

USER root
RUN pip install click ruamel.yaml
WORKDIR /workdir
RUN chown -R ${USER} /workdir

USER ${USER}
ARG EXTRA_PACKAGES=
COPY build_docs.py /workdir
RUN python build_docs.py \
        /workdir/drops/docs/iqsharp-magic \
        microsoft.quantum.iqsharp.magic-ref \
        ${EXTRA_PACKAGES}
