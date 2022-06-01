#!/bin/env python
# -*- coding: utf-8 -*-
##
# utils.py: Utilities internal to the qsharp package.
##
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
##

from dataclasses import dataclass
import logging
import warnings
logger = logging.getLogger(__name__)
from typing import Callable

## INTERNAL FUNCTIONS ##

def log_messages(data, action : Callable[[str], None] = logger.error):
    msgs = data['messages']
    for msg in msgs:
        action(msg)

@dataclass
class ImportFailure:
    cause: ImportError

_qutip_cache = None
def try_import_qutip(warn=False, optional=False):
    global _qutip_cache
    if _qutip_cache is None:
        try:
            import qutip
            _qutip_cache = qutip
        except ImportError as ex:
            _qutip_cache = ImportFailure(ex)

    if warn and isinstance(_qutip_cache, ImportFailure):
        warnings.warn(f"Failed to import QuTiP with error: {_qutip_cache.cause.msg}", source=_qutip_cache.cause)

    if optional and isinstance(_qutip_cache, ImportFailure):
        # suppress failure when optional
        return None

    return _qutip_cache
