#!/bin/env python
# -*- coding: utf-8 -*-
##
# __init__.py: Root module for the qsharp package.
##
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
##

"""

"""

## IMPORTS ##

import sys
import os
import platform
import shutil
import subprocess
from typing import List, Dict, Union
from collections import defaultdict
from distutils.version import LooseVersion

from qsharp.clients import _start_client
from qsharp.clients.iqsharp import IQSharpError
from qsharp.loader import QSharpCallable, QSharpModuleFinder
from qsharp.packages import Packages
from qsharp.types import Result, Pauli
try:
    from qsharp.version import __version__
except:
    __version__ = "<unknown>"

## LOGGING ##

import logging
logger = logging.getLogger(__name__)


## EXPORTS ##

__all__ = [
    'setup',
    'compile', 'reload',
    'get_available_operations', 'get_available_operations_by_namespace',
    'get_workspace_operations',
    'packages',
    'IQSharpError',
    'Result', 'Pauli'
]

## FUNCTIONS ##

def compile(code : str) -> Union[QSharpCallable, List[QSharpCallable]]:
    """
    Given a string containing Q# source code, compiles it into the current
    workspace and returns one or more Q# callable objects that can be used to
    invoke the new code.

    :param code: A string containing Q# source code to be compiled.
    :returns: A list of callables compiled from `code`, or a callable if exactly
        one callable is found.
    """
    ops = [
        QSharpCallable(op, "snippets")
        for op in client.compile(code)
    ]
    if len(ops) == 1:
        return ops[0]
    else:
        return ops

def _download_iqsharp(system: str, exe: str, path: str) -> None:
    # this should be replaced with a safe downloading from the blob store
    # that also takes the version into consideration.
    shutil.copy(f"../../drops/blobs/{system}/{exe}", path)


def _register_kernel(path: str, exe: str) -> None:
    cmd = os.path.join(path, exe)
    args =  [
        cmd,
        'install',
        '--user',
        '--path-to-tool', cmd
    ]

    logger.debug(f"Executing {args}.")
    subprocess.run(args)
    

def setup() -> int:
    path = os.path.realpath(__file__)
    path = os.path.dirname(path)
    logger.info(f"Installing IQ# in {path}")

    # copy from local folder... this needs to be changed to 
    system = platform.system()
    if (system == 'Windows'):
        (system, exe) = ('win10-x64', 'Microsoft.Quantum.IQSharp.exe')
    elif (system == 'Darwin'):
        (system, exe) = ('osx-x64', 'Microsoft.Quantum.IQSharp')
    elif (platform == 'Linux'):
        (system, exe) = ('linux-x64', 'Microsoft.Quantum.IQSharp')
    else: 
        raise IQSharpError([f"IQ# is not supported on this platform ({platform})."])

    logger.debug(f"Identified source file: {exe}.")
    _download_iqsharp(system, exe, path)
    _register_kernel(path, exe)

    logger.info(f"IQ# kernel installed successfully.")

    return 0
    

def reload() -> None:
    """
    Reloads the current IQ# workspace, recompiling source files in the
    workspace.

    If the workspace fails to compile (e.g., because of a missing package),
    Q# compilation errors are raised as an exception.
    """
    client.reload()

def get_available_operations() -> List[str]:
    """
    Returns a list containing the names of all operations and functions defined
    in the current workspace, and that have been compiled dynamically from
    snippets.
    """
    return client.get_available_operations()

def get_workspace_operations() -> List[str]:
    """
    Returns a list containing the names of all operations and functions defined
    in the current workspace, excluding dynamically compiled snippets.
    """
    return client.get_workspace_operations()

def get_available_operations_by_namespace() -> Dict[str, List[str]]:
    """
    Returns a dictionary from namespaces to lists of operations and functions
    defined in the current workspace that are members of each namespace.
    """
    ops = get_available_operations()
    by_ns = defaultdict(list)

    for qualified_name in ops:
        idx_last_dot = qualified_name.rfind(".")
        ns_name = qualified_name[:idx_last_dot]
        op_name = qualified_name[idx_last_dot + 1:]

        by_ns[ns_name].append(op_name)

    return dict(by_ns.items())

def component_versions() -> Dict[str, LooseVersion]:
    """
    Returns a dictionary from components of the IQ# kernel to their
    versions.
    """
    versions = client.component_versions()
    # Add in the qsharp Python package itself.
    versions["qsharp"] = LooseVersion(__version__)
    return versions


## STARTUP ##

# TODO: Change the client to start on first call and not when the module is loaded.
client = _start_client()
packages = Packages(client)

# Make sure that we're last on the meta_path so that actual modules are loaded
# first.
sys.meta_path.append(QSharpModuleFinder())
