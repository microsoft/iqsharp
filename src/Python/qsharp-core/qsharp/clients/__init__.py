#!/bin/env python
# -*- coding: utf-8 -*-
##
# __init__.py: Logic for launching and configuring Q# clients.
##
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
##


import os
import sys
import time
import logging
import jupyter_client
from distutils.util import strtobool

class IQSharpNotInstalledError(Exception):
    pass

class IQSharpNotAvailableError(Exception):
    pass

def _start_client():
    logger = logging.getLogger(__name__)

    client_name =  os.getenv("QSHARP_PY_CLIENT", "iqsharp")

    if client_name == "iqsharp":
        # Allow users to override what kernel is used, making it easier to
        # test kernels side-by-side.
        kernel_name =  os.getenv("QSHARP_PY_IQSHARP_KERNEL_NAME", "iqsharp")
        import qsharp.clients.iqsharp
        client = qsharp.clients.iqsharp.IQSharpClient(kernel_name=kernel_name)
    elif client_name == "mock":
        import qsharp.clients.mock
        client = qsharp.clients.mock.MockClient()

    try:
        client.start()
    except jupyter_client.kernelspec.NoSuchKernel as ex:
        message = "IQ# is not installed." + \
            "\nPlease follow the instructions at https://aka.ms/qdk-install/python."
        print(message)
        raise IQSharpNotInstalledError(message)

    # Check if the server is up and running:
    server_ready = False
    for idx_attempt in range(20):
        try:
            server_ready = client.is_ready()
            if server_ready:
                break
            if idx_attempt == 0:
                print("Preparing Q# environment...")
            else:
                print(".", end='', flush=True)
            time.sleep(1)
        except Exception as ex:
            logger.debug('Exception while checking Q# environment.', exc_info=ex)
            print("!", end='', flush=True)
            time.sleep(1)
    if not server_ready:
        message = "Q# environment was not available in allocated time." + \
            "\nPlease check the instructions at https://aka.ms/qdk-install/python."
        print(message)
        raise IQSharpNotAvailableError(message)

    return client
