#!/bin/env python
# -*- coding: utf-8 -*-
##
# test_loader.py: Tests that the loader correctly handles namespaces and
#     callables.
##
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
##

## IMPORTS ##

import json
import numpy as np
import os
import pytest
import qsharp
import qsharp.clients.mock
from .utils import set_environment_variables

print ( qsharp.component_versions() )

old_client = qsharp.client

## SETUP ##

def setup_module():
    # Override with the mock client
    qsharp.client = qsharp.clients.mock.MockClient()

def teardown_module():
    qsharp.client = old_client

## TESTS ##

def test_can_import():
    import A.B
    import A.B

def test_can_import_sub_ns():
    import A

def test_import_dir_is_correct():
    import A
    assert dir(A) == ["B"]

    import A.B
    assert dir(A.B) == ["C", "D"]
