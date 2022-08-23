FROM mcr.microsoft.com/windows/servercore:ltsc2022 AS buildbase

RUN powershell (New-Object System.Net.WebClient).DownloadFile('https://repo.anaconda.com/miniconda/Miniconda3-py39_4.12.0-Windows-x86_64.exe', 'Miniconda3.exe') && \
    powershell (Get-FileHash .\Miniconda3.exe).Hash -eq '1ACBC2E8277DDD54A5F724896C7EDEE112D068529588D944702966C867E7E9CC' && \
    powershell Unblock-File Miniconda3.exe && \
    Miniconda3.exe /InstallationType=JustMe /RegisterPython=1 /S /D=C:\Miniconda3

FROM mcr.microsoft.com/powershell:lts-windowsservercore-ltsc2022

COPY --from=buildbase C:/Miniconda3 C:/Miniconda3

USER ContainerAdministrator

RUN C:\Miniconda3\Library\bin\conda init && \
    C:\Miniconda3\Library\bin\conda clean -afy

CMD ["cmd"]
