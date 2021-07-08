#!/bin/env python
# -*- coding: utf-8 -*-
##
# ipython_magic.py: Integration into the IPython notebook environment.
##
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
##

# NB: This should ONLY be imported from an IPython session.

import qsharp as qs
from IPython.display import display
from IPython.core.magic import (register_line_magic, register_cell_magic,
                                register_line_cell_magic, needs_local_scope)


def register_magics():
    @register_cell_magic
    @needs_local_scope
    def qsharp(magic_args, cell, local_ns=None):
        """Compiles a Q# snippet, exposing its operations and functions to
           the current local scope."""
        callables = qs.compile(cell)
        if isinstance(callables, qs.QSharpCallable):
            local_ns[callables._name] = callables
        else:
            for qs_callable in callables:
                local_ns[qs_callable._name] = qs_callable

def register_experimental_magics():
    import qsharp.experimental as exp

    @register_line_magic
    def noise_model(line):
        args = line.split(' ')
        if args[0] == '--set-by-name':
            exp.set_noise_model_by_name(args[1])
