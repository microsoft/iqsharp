import json
import datetime

from io import StringIO
from typing import List, Optional
from pathlib import Path

import click

try:
    from ruamel.yaml import YAML
except ImportError:
    from ruamel_yaml import YAML

yaml = YAML()
yaml.indent(mapping=2, sequence=2)

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
    for magic in magics:
        magic_doc = format_as_document(magic, uid_base)
        with open(output_dir / f"{magic['Name'].replace('%', '')}.md", 'w', encoding='utf8') as f:
            f.write(magic_doc)

def format_as_section(name : str, content : Optional[str]) -> str:
    content = content.strip() if content else None
    return f"""

## {name}

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

def format_as_document(magic, uid_base : str) -> str:
    # NB: this function supports both the old and new Documentation format.
    #     See https://github.com/microsoft/jupyter-core/pull/49.
    magic_name = magic['Name'].strip()
    metadata = {
        'title': f"{magic_name} (magic command)",
        'uid': f"{uid_base}.{magic_name.replace('%', '')}",
        'ms.date': datetime.date.today().isoformat(),
        'ms.topic': 'article'
    }
    header = f"# `{magic_name}`"
    doc = magic['Documentation']

    summary = format_as_section('Summary', doc.get('Summary', ""))
    description = format_as_section(
        'Description',
        doc.get('Description', doc.get('Full', ''))
    )
    remarks = format_as_section('Remarks', doc.get('Remarks', ""))
    examples = "\n".join(
        format_as_section("Example", example)
        for example in doc.get('Examples', [])
    )
    see_also = format_as_section(
        "See Also",
        "\n".join(
            f"- [{description}]({target})"
            for description, target in doc.get('SeeAlso', [])
        )
    )

    # Convert the metadata header to YAML.
    metadata_as_yaml = StringIO()
    yaml.dump(metadata, metadata_as_yaml)

    return cleanup_markdown(f"""
---
{metadata_as_yaml.getvalue().rstrip()}
---
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
    """)

if __name__ == "__main__":
    main()
