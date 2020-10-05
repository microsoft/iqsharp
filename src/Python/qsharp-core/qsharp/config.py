#!/bin/env python
# -*- coding: utf-8 -*-
##
# config.py: Provides access to IQ# configuration settings.
##
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
##

from typing import Any

## CLASSES ##

class Config(object):
    """
    Provides dictionary-like access to IQ# configuration settings.
    """

    def __init__(self, client):
        self._client = client

    def __getitem__(self, name : str) -> Any:
        """
        Returns the value of the specified IQ# configuration option.
        Options can be set by calls to qsharp.set_config() or by loading
        a file previously created by qsharp.save_config().
        See https://docs.microsoft.com/qsharp/api/iqsharp-magic/config for the list
        of supported IQ# configuration setting names and values.
        """
        return self._client.get_config()[name]

    def __setitem__(self, name : str, value : object) -> None:
        """
        Sets a specified IQ# configuration option with the specified value.
        See https://docs.microsoft.com/qsharp/api/iqsharp-magic/config for the list
        of supported IQ# configuration setting names and values.
        """
        self._client.set_config(name, value)

    def save(self) -> None:
        """
        Saves all current IQ# configuration options to a file named .iqsharp-config.json
        in the current working directory. This file is automatically loaded
        by the IQ# kernel at initialization time.
        See https://docs.microsoft.com/qsharp/api/iqsharp-magic/config for the list
        of supported IQ# configuration setting names and values.
        """
        self._client.save_config()
