#!/bin/env python
# -*- coding: utf-8 -*-
##
# iqsharp.py: Client for the IQ# Jupyter kernel.
##
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
##

## IMPORTS ##

from contextlib import contextmanager
import subprocess
import time
import http.client
import atexit
import json
import sys
import urllib.parse
import os
import jupyter_client

from functools import partial
from io import StringIO
from collections import defaultdict
from typing import List, Dict, Callable, Any, Optional
from pathlib import Path
from distutils.version import LooseVersion

from qsharp.serialization import map_tuples, unmap_tuples

try:
    from IPython.display import display
    display_raw = partial(display, raw=True)
except:
    def display_raw(content):
        pass

## VERSION REPORTING ##

try:
    from qsharp.version import _user_agent_extra
except ImportError:
    _user_agent_extra = ""

## LOGGING ##

import logging
logger = logging.getLogger(__name__)

DEFAULT_TIMEOUT=120

## CLASSES ##

class IQSharpError(RuntimeError):
    """
    Represents a Q# error passed by the IQ# kernel to the Python host.
    """
    def __init__(self, iqsharp_errors : List[str]):
        self.iqsharp_errors = iqsharp_errors
        error_msg = StringIO()
        error_msg.write("The Q# kernel raised the following errors:\n")
        error_msg.writelines([
            "    " + msg for msg in iqsharp_errors
        ])
        super().__init__(error_msg.getvalue())

class AlreadyExecutingError(IOError):
    """
    Raised when the IQ# client is already executing a command and cannot safely
    process an additional command.
    """
    pass

class IQSharpClient(object):
    kernel_manager = None
    kernel_client = None
    _busy : bool = False

class IQSharpClient(object):
    kernel_manager = None
    kernel_client = None
    _busy : bool = False

    display_data_callback: Optional[Callable[[Any], bool]] = None

    def __init__(self, kernel_name: str = 'iqsharp'):
        self.kernel_manager = jupyter_client.KernelManager(kernel_name=kernel_name)

    ## Server Lifecycle ##

    def start(self):
        logger.info("Starting IQ# kernel...")
        # Pass along all environment variables except the user agent,
        # as we'll override that to mark this as a Python session.
        env = os.environ.copy()
        env["IQSHARP_USER_AGENT"] = f"qsharp.py{_user_agent_extra}"
        self.kernel_manager.start_kernel(env=env)
        self.kernel_client = self.kernel_manager.client()
        atexit.register(self.stop)

    def stop(self):
        # Don't use logger here. If we're running inside pytest, the handle to the
        # log output file may have already been closed.
        try:
            self.kernel_manager.shutdown_kernel()
        except:
            pass

    def is_ready(self):
        try:
            result = self.component_versions(_timeout_=6)
            logger.info(f"Q# version\n{result}")
        except Exception as ex:
            logger.info('Exception while checking if IQ# is ready.', exc_info=ex)
            return
        return True

    def check_status(self):
        if not self.kernel_manager.is_alive():
            logger.debug("IQ# kernel is not running. Restarting.")
            self.start()

    ## Public Interface ##

    @property
    def busy(self) -> bool:
        return self._busy

    def compile(self, body):
        return self._execute(body)

    def get_available_operations(self) -> List[str]:
        return self._execute('%who', raise_on_stderr=False)

    def get_operation_metadata(self, name : str) -> Dict[str, Any]:
        return self._execute(f"?{name}")

    def get_workspace_operations(self) -> List[str]:
        return self._execute("%workspace")

    def reload(self) -> None:
        return self._execute(f"%workspace reload", raise_on_stderr=True)

    def get_config(self) -> Dict[str, object]:
        raw = self._execute(f"%config", raise_on_stderr=True)
        config_settings = {}
        for row in raw["rows"]:
            config_settings[row['Key']] = row['Value']
        return config_settings

    def set_config(self, name : str, value : object) -> None:
        from numbers import Number
        if isinstance(value, bool):
            self._execute(f"%config {name}={'true' if value else 'false'}", raise_on_stderr=True)
        elif isinstance(value, Number):
            self._execute(f"%config {name}={value}", raise_on_stderr=True)
        else:
            self._execute(f"%config {name}='{value}'", raise_on_stderr=True)

    def save_config(self) -> None:
        self._execute(f"%config --save", raise_on_stderr=True)

    def add_package(self, name : str) -> None:
        return self._execute(f"%package {name}", raise_on_stderr=True)

    def get_packages(self) -> List[str]:
        return self._execute("%package", raise_on_stderr=False)

    def add_project(self, path : str) -> None:
        return self._execute(f"%project {path}", raise_on_stderr=True)

    def get_projects(self) -> List[str]:
        return self._execute("%project", raise_on_stderr=False)

    def simulate(self, op, **kwargs) -> Any:
        kwargs.setdefault('_timeout_', None)
        return self._execute_callable_magic('simulate', op, **kwargs)

    def simulate_sparse(self, op, **kwargs) -> Any:
        kwargs.setdefault('_timeout_', None)
        return self._execute_callable_magic('simulate_sparse', op, **kwargs)

    def toffoli_simulate(self, op, **kwargs) -> Any:
        kwargs.setdefault('_timeout_', None)
        return self._execute_callable_magic('toffoli', op, **kwargs)

    def estimate(self, op, **kwargs) -> Dict[str, int]:
        kwargs.setdefault('_timeout_', None)
        raw_counts = self._execute_callable_magic('estimate', op, **kwargs)
        # Note that raw_counts will have the form:
        # [
        #     {"Metric": "<name>", "Sum": "<value>"},
        #     ...
        # ]
        # We thus need to convert it into a dict. As we do so, we convert counts
        # to ints, since they get turned to floats by JSON serialization.
        counts = {}
        for row in raw_counts:
            counts[row["Metric"]] = int(row["Sum"])
        return counts

    def trace(self, op, **kwargs) -> Any:
        return self._execute_callable_magic('trace', op, _quiet_ = True, **kwargs)

    def compile_to_qir(self, op, output: str) -> None:
        return self._execute_callable_magic('qir', op, output=output)

    def component_versions(self, **kwargs) -> Dict[str, LooseVersion]:
        """
        Returns a dictionary from components of the IQ# kernel to their
        versions.
        """
        versions = {}
        def capture(msg):
            # We expect a display_data with the version table.
            if msg["msg_type"] == "display_data":
                data = unmap_tuples(json.loads(self._get_qsharp_data(msg["content"])))
                for component, version in data["rows"]:
                    versions[component] = LooseVersion(version)
        self._execute("%version", display_data_handler=capture, _quiet_=True, **kwargs)
        return versions

    @contextmanager
    def capture_diagnostics(self, passthrough: bool) -> List[Any]:
        captured_data = []
        def callback(msg):
            msg_data = (
                # Check both the old and new MIME types used by the IQ#
                # kernel.
                json.loads(msg['content']['data'].get('application/json', "null")) or
                json.loads(msg['content']['data'].get('application/x-qsharp-data', "null"))
            )
            if msg_data is not None:
                captured_data.append(msg_data)
                return passthrough
            else:
                # No JSON found found, so just fall back.
                return True

        old_callback = self.display_data_callback
        self.display_data_callback = callback
        try:
            yield captured_data
        finally:
            self.display_data_callback = old_callback

    ## Experimental Methods ##
    # These methods expose experimental functionality that may be removed without
    # warning. To communicate to users that these are not reliable, we mark
    # these methods as private, and will re-export them in the
    # qsharp.experimental submodule.

    def _simulate_noise(self, op, **kwargs) -> Any:
        kwargs.setdefault('_timeout_', None)
        return self._execute_callable_magic('experimental.simulate_noise', op, **kwargs)

    def _get_noise_model(self) -> str:
        return self._execute(f'%experimental.noise_model')

    def _get_noise_model_by_name(self, name : str) -> None:
        return self._execute(f'%experimental.noise_model --get-by-name {name}')

    def _set_noise_model(self, json_data : str) -> None:
        # We assume json_data is already serialized, so that we skip the support
        # provided by _execute_magic and call directly.
        return self._execute(f'%experimental.noise_model {json_data}')

    def _set_noise_model_by_name(self, name : str) -> None:
        return self._execute(f'%experimental.noise_model --load-by-name {name}')


    ## Internal-Use Methods ##

    @staticmethod
    def _get_qsharp_data(message_content):
        if "application/x-qsharp-data" in message_content["data"]:
            # Current versions of IQ# use application/x-qsharp-data
            # for the JSON-encoded data in the execution result.
            return message_content["data"]["application/x-qsharp-data"]
        if "application/json" in message_content["data"]:
            # For back-compat with older versions of IQ# <= 0.17.2105.144881
            # that used the application/json MIME type for the JSON data.
            return message_content["data"]["application/json"]
        return None

    def _execute_magic(self, magic : str, raise_on_stderr : bool = False, _quiet_ : bool = False, **kwargs) -> Any:
        _timeout_ = kwargs.pop('_timeout_', DEFAULT_TIMEOUT)
        return self._execute(
            f'%{magic} {json.dumps(map_tuples(kwargs))}',
            raise_on_stderr=raise_on_stderr, _quiet_=_quiet_, _timeout_=_timeout_
        )

    def _execute_callable_magic(self, magic : str, op,
            raise_on_stderr : bool = False,
            _quiet_ : bool = False,
            **kwargs
    ) -> Any:
        return self._execute_magic(
            f"{magic} {op._name}",
            raise_on_stderr=raise_on_stderr,
            _quiet_=_quiet_,
            **kwargs
        )

    def _handle_message(self, msg, handlers=None, error_callback=None, fallback_hook=None):
        if handlers is None:
            handlers = {}
        if fallback_hook is None:
            fallback_hook = self.kernel_client._output_hook_default
        msg_type = msg['msg_type']
        if msg_type in handlers:
            handlers[msg_type](msg)
        else:
            if error_callback is not None and msg['msg_type'] == 'stream' and msg['content']['name'] == 'stderr':
                error_callback(msg['content']['text'])
            else:
                fallback_hook(msg)

    def _execute(self, input, return_full_result=False, raise_on_stderr : bool = False, output_hook=None, display_data_handler=None, _timeout_=DEFAULT_TIMEOUT, _quiet_ : bool = False, **kwargs):
        logger.debug(f"sending:\n{input}")
        logger.debug(f"timeout: {_timeout_}")

        # make sure the server is still running:
        try:
            self.check_status()
        except:
            raise IQSharpError(["IQ# is not running."])

        results = []
        errors = []

        def log_error(msg):
            errors.append(msg)

        # Set up handlers for various kinds of messages, making sure to
        # fallback through to output_hook as appropriate, so that the IPython
        # package can send display data through to Jupyter clients.
        handlers = {
            'execute_result': (lambda msg: results.append(msg)),
            'render_execution_path':  (lambda msg: results.append(msg)),
            'display_data': display_data_handler if display_data_handler is not None else lambda msg: ...
        }

        # Pass display data through to IPython if we're not in quiet mode.
        if not _quiet_:
            handlers['display_data'] = (
                lambda msg: display_raw(msg['content']['data'])
            )

        # Finish setting up handlers by allowing the display_data_callback
        # to intercept display data first, only sending messages through to
        # other handlers if it returns True.
        if self.display_data_callback is not None:
            inner_handler = handlers['display_data']

            def filter_display_data(msg):
                if self.display_data_callback(msg):
                    return inner_handler(msg)

            handlers['display_data'] = filter_display_data

        _output_hook = partial(
            self._handle_message,
            error_callback=log_error if raise_on_stderr else None,
            fallback_hook=output_hook,
            handlers=handlers
        )

        try:
            if self._busy:
                # Trying to execute while already executing can corrupt the
                # ordering of messages internally to ZeroMQ
                # (see https://github.com/Microsoft/QuantumLibraries/issues/69),
                # so we need to throw early rather than letting the problem
                # propagate to a Jupyter protocol error.
                raise AlreadyExecutingError("Cannot execute through the IQ# client while another execution is completing.")
            self._busy = True
            reply = self.kernel_client.execute_interactive(input, timeout=_timeout_, output_hook=_output_hook, **kwargs)
        finally:
            self._busy = False

        logger.debug(f"received:\n{reply}")

        # There should be either zero or one execute_result messages.
        if errors:
            raise IQSharpError(errors)
        if results:
            assert len(results) == 1
            content = results[0]['content']
            if 'executionPath' in content:
                obj = content['executionPath']
            else:
                qsharp_data = self._get_qsharp_data(content)
                if qsharp_data:
                    obj = unmap_tuples(json.loads(qsharp_data))
                else:
                    obj = None
            return (obj, content) if return_full_result else obj
        else:
            return None
