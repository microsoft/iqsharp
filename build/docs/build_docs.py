#!/bin/env python
# -*- coding: utf-8 -*-
##
# build_docs.py: Builds documentation for IQ# magic commands as
#     DocFX-compatible Markdown and YAML files.
##
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
##

"""
Builds documentation for IQ# magic commands as
DocFX-compatible Markdown and YAML files.

Note that this command requires the IQ# kernel and the qsharp package to
be installed, as well as Python 3.7 or later, click, and ruamel.yaml.
"""

import json
import datetime
import dataclasses

from io import StringIO
from typing import List, Optional, Dict
from pathlib import Path

import click

try:
    from ruamel.yaml import YAML
except ImportError:
    from ruamel_yaml import YAML

yaml = YAML()
yaml.indent(mapping=2, sequence=2)

@dataclasses.dataclass
class MagicReferenceDocument:
    content: str
    name: str
    safe_name: str
    uid: str
    summary: str

@click.command()
@click.argument("OUTPUT_DIR")
@click.argument("UID_BASE")
@click.option("--package", "-p", multiple=True)
def main(output_dir : str, uid_base : str, package : List[str]):
    output_dir = Path(output_dir)
    # Make the output directory if it doesn't already exist.
    output_dir.mkdir(parents=True, exist_ok=True)

    import qsharp

    print("Adding packages...")
    for package_name in package:
        qsharp.packages.add(package_name)
    
    print("Generating Markdown files...")
    magics = qsharp.client._execute(r"%lsmagic")
    all_magics = {}
    for magic in magics:
        magic_doc = format_as_document(magic, uid_base)
        all_magics[magic_doc.name] = magic_doc
        with open(output_dir / f"{magic_doc.safe_name}.md", 'w', encoding='utf8') as f:
            f.write(magic_doc.content)

    toc_content = format_toc(all_magics)
    with open(output_dir / "toc.yml", 'w', encoding='utf8') as f:
        f.write(toc_content)
    
    index_content = format_index(all_magics, uid_base)
    with open(output_dir / "index.md", 'w', encoding='utf8') as f:
        f.write(index_content)

def format_as_section(name : str, content : Optional[str], heading_level : Optional[int] = 2) -> str:
    content = content.strip() if content else None
    return f"""

{"#" * heading_level} {name}

{content}

""" if content else ""

def _cleanup_markdown(content : str):
    # Ensure that there is exactly one trailing newline, and that each line
    # is free of trailing whitespace (with an exception for exactly two
    # trailing spaces). 
    # We also want to make sure that there are not two or more blank lines in
    # a row.
    prev_blank = False
    for line in content.split("\n"):
        cleaned_line = line.rstrip() if line != line.rstrip() + "  " else line
        this_blank = cleaned_line == ""

        if not (prev_blank and this_blank):
            yield cleaned_line

        prev_blank = this_blank

def cleanup_markdown(content : str):
    return "\n".join(
        _cleanup_markdown(content)
    ).strip() + "\n"

def as_yaml_header(metadata) -> str:
    # Convert the metadata header to YAML.
    metadata_as_yaml = StringIO()
    yaml.dump(metadata, metadata_as_yaml)

    return f"---\n{metadata_as_yaml.getvalue().rstrip()}\n---"""

def format_as_document(magic, uid_base : str) -> MagicReferenceDocument:
    # NB: this function supports both the old and new Documentation format.
    #     See https://github.com/microsoft/jupyter-core/pull/49.
    magic_name = magic['Name'].strip()
    safe_name = magic_name.replace('%', '')
    uid = f"{uid_base}.{safe_name}"
    doc = magic['Documentation']
    raw_summary = doc.get('Summary', "")
    metadata = {
        'title': f"{magic_name} (magic command)",
        'description': raw_summary.strip(),
        'author': 'rmshaffer',
        'uid': uid,
        'ms.author': 'ryansha',
        'ms.date': datetime.date.today().strftime("%m/%d/%Y"),
        'ms.topic': 'article'
    }
    
    header = f"# `{magic_name}`"

    summary = format_as_section('Summary', raw_summary)
    description = format_as_section(
        'Description',
        doc.get('Description', doc.get('Full', ''))
    )
    remarks = format_as_section('Remarks', doc.get('Remarks', ""))

    raw_examples = doc.get('Examples', [])
    examples = format_as_section(f'Examples for `{magic_name}`', "\n".join(
        format_as_section(f"Example {i+1}", example, heading_level=3)
        for i, example in enumerate(raw_examples)
    )) if raw_examples else ""

    raw_see_also = doc.get('SeeAlso', [])
    see_also = format_as_section(
        "See Also",
        "\n".join(
            f"- [{description}]({target})"
            for description, target in raw_see_also
        )
    ) if raw_see_also else ""

    return MagicReferenceDocument(
        content=cleanup_markdown(f"""
{as_yaml_header(metadata)}

<!--
    NB: This file has been automatically generated from {magic.get("AssemblyName", "<unknown>")}.dll,
        please do not manually edit it.

    [DEBUG] JSON source:
        {json.dumps(magic)}
-->

{header}
{summary}
{description}
{remarks}
{examples}
{see_also}
        """),
        name=magic_name, safe_name=safe_name,
        uid=uid,
        summary=raw_summary
    )

def format_toc(all_magics : Dict[str, MagicReferenceDocument]) -> str:
    toc_content = [
        {
            'href': f"{doc.safe_name}.md",
            'name': f"{doc.name} magic command"
        }
        for magic_name, doc in sorted(all_magics.items(), key=lambda item: item[0])
    ]
    
    as_yaml = StringIO()
    yaml.dump(toc_content, as_yaml)

    return as_yaml.getvalue()


def format_index(all_magics : Dict[str, MagicReferenceDocument], uid_base : str) -> str:
    index_content = "\n".join(
        f"| [`{magic_name}`](xref:{doc.uid}) | {doc.summary} |"
        for magic_name, doc in sorted(all_magics.items(), key=lambda item: item[0])
    )
    metadata = {
        'title': "IQ# Magic Commands",
        'description': "Lists the magic commands available in the IQ# Jupyter kernel.",
        'author': 'rmshaffer',
        'uid': f"{uid_base}.index",
        'ms.author': 'ryansha',
        'ms.date': datetime.date.today().strftime("%m/%d/%Y"),
        'ms.topic': 'article'
    }
    return cleanup_markdown(f"""
{as_yaml_header(metadata)}
# IQ# Magic Commands                                 
| Magic Command | Summary |
|---------------|---------|
{index_content}
    """)

if __name__ == "__main__":
    main()
