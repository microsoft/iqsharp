#!/bin/env python
# -*- coding: utf-8 -*-
##
# projects.py: Abstraction to represent the list of projects.
##
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
##

from typing import Iterable, Tuple

## LOGGING ##

import logging
logger = logging.getLogger(__name__)


## CLASSES ##

class Projects(object):
    """
    Represents the list of projects loaded into the current Q# session, and
    allows for adding references to additional .csproj files.
    """

    def __init__(self, client):
        self._client = client

    def __iter__(self) -> Iterable[str]:
        for project in self._client.get_projects():
            yield project

    def __repr__(self) -> str:
        return repr(list(self))
    def __str__(self) -> str:
        return str(list(self))

    def add(self, project_path : str) -> None:
        """
        Adds a reference to the given Q# project to be loaded
        into the current IQ# session.
        :param project_path: Path to the .csproj to be added. May be an absolute
        path or a path relative to the current workspace root folder.
        """
        logger.info(f"Loading project: {project_path}")
        loaded_projects = self._client.add_project(project_path)
        logger.info("Loading complete: " + ';'.join(str(p) for p in loaded_projects))
