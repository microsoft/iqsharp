from typing import Dict
import markdown

from ..azure import AzureResult

class ResourceEstimatorBatchResult(AzureResult):
    MAX_DEFAULT_ITEMS_IN_TABLE = 6

    def __init__(self, data) -> None:
        # super().__init__(data)
        self._data = data

        self.result_items = [ResourceEstimatorResult(result) for result in self._data]

    def data(self):
        return self._data

    def _repr_html_(self):
        num_items = len(self._data)
        if num_items > self.MAX_DEFAULT_ITEMS_IN_TABLE:
            html = f"<p><b>Info:</b> <i>The overview table is cut off after {self.MAX_DEFAULT_ITEMS_IN_TABLE} items.  If you want to see all items, suffix the result variable with <code>[0:{num_items - 1}]</code></i></p>"
            return html + batch_result_html_table(self, range(self.MAX_DEFAULT_ITEMS_IN_TABLE))
        else:
            return batch_result_html_table(self, range(len(self._data)))
    
    def plot(self):
        import matplotlib.pyplot as plt

        num_items = len(self._data)
        [xs, ys] = zip(*[(self._data[i]['physicalCounts']['runtime'], self._data[i]['physicalCounts']['physicalQubits']) for i in range(num_items)])

        plt.ylabel('Physical qubits')
        plt.xlabel('Runtime')
        plt.loglog()
        plt.scatter(x=xs, y=ys)
        for i in range(num_items):
            plt.annotate(f"{i}", (xs[i], ys[i]))

        nsec = 1
        usec = 1e3 * nsec
        msec = 1e3 * usec
        sec = 1e3 * msec
        min = 60 * sec
        hour = 60 * min
        day = 24 * hour
        week = 7 * day
        month = 31 * day
        year = 365 * month
        decade = 10 * year
        century = 10 * decade

        time_units = [nsec, usec, msec, sec, min, hour, day, week, month, year, decade, century]
        time_labels = ["1 ns", "1 µs", "1 ms", "1 s", "1 min", "1 hour", "1 day", "1 week", "1 month", "1 year", "1 decade", "1 century"]

        cutoff = next((i for i, x in enumerate(time_units) if x > max(xs)), len(time_units) - 1) + 1

        plt.xticks(time_units[0:cutoff], time_labels[0:cutoff], rotation=90)
        plt.show()

    def __getitem__(self, key):
        if isinstance(key, slice):
            from IPython.display import display, HTML
            display(HTML(batch_result_html_table(self, range(len(self._data))[key])))
        else:
            return self.result_items[key]

class ResourceEstimatorResult(AzureResult):
    def __init__(self, data: Dict):
        super().__init__(data)
        self._data = data
        self.summary = ResourceEstimatorResultSummary(data)

    def data(self):
        return self._data

    def _repr_html_(self):
        html = ""

        md = markdown.Markdown(extensions=['mdx_math'])
        for group in self.data()['reportData']['groups']:
            html += f"""
                <details {"open" if group['alwaysVisible'] else ""}>
                    <summary style="display:list-item">
                        <strong>{group['title']}</strong>
                    </summary>
                    <table>"""
            for entry in group['entries']:
                val = self.data()
                for key in entry['path'].split("/"):
                    val = val[key]
                explanation = md.convert(entry["explanation"])
                html += f"""
                    <tr>
                        <td style="font-weight: bold; vertical-align: top; white-space: nowrap">{entry['label']}</td>
                        <td style="vertical-align: top; white-space: nowrap">{val}</td>
                        <td style="text-align: left">
                            <strong>{entry["description"]}</strong>
                            <hr style="margin-top: 2px; margin-bottom: 0px; border-top: solid 1px black" />
                            {explanation}
                        </td>
                    </tr>
                """
            html += "</table></details>"

        html += f"<details><summary style=\"display:list-item\"><strong>Assumptions</strong></summary><ul>"
        for assumption in self.data()['reportData']['assumptions']:
            html += f"<li>{md.convert(assumption)}</li>"
        html += "</ul></details>"

        return html

class ResourceEstimatorResultSummary(AzureResult):
    def __init__(self, data):
        super().__init__(data)
        self._data = data

    def data(self):
        return self._data

    def _repr_html_(self):
        html = """
            <style>
                .aqre-tooltip {
                    position: relative;
                    border-bottom: 1px dotted black;
                }

                .aqre-tooltip .aqre-tooltiptext {
                    font-weight: normal;
                    visibility: hidden;
                    width: 600px;
                    background-color: #e0e0e0;
                    color: black;
                    text-align: center;
                    border-radius: 6px;
                    padding: 5px 5px;
                    position: absolute;
                    z-index: 1;
                    top: 150%;
                    left: 50%;
                    margin-left: -200px;
                    border: solid 1px black;
                }

                .aqre-tooltip .aqre-tooltiptext::after {
                    content: "";
                    position: absolute;
                    bottom: 100%;
                    left: 50%;
                    margin-left: -5px;
                    border-width: 5px;
                    border-style: solid;
                    border-color: transparent transparent black transparent;
                }

                .aqre-tooltip:hover .aqre-tooltiptext {
                    visibility: visible;
                }
            </style>"""

        md = markdown.Markdown(extensions=['mdx_math'])
        for group in self.data()['reportData']['groups']:
            html += f"""
                <details {"open" if group['alwaysVisible'] else ""}>
                    <summary style="display:list-item">
                        <strong>{group['title']}</strong>
                    </summary>
                    <table>"""
            for entry in group['entries']:
                val = self.data()
                for key in entry['path'].split("/"):
                    val = val[key]
                explanation = md.convert(entry["explanation"])
                html += f"""
                    <tr class="aqre-tooltip">
                        <td style="font-weight: bold"><span class="aqre-tooltiptext">{explanation}</span>{entry['label']}</td>
                        <td>{val}</td>
                        <td style="text-align: left">{entry["description"]}</td>
                    </tr>
                """
            html += "</table></details>"

        html += f"<details><summary style=\'display:list-item\'><strong>Assumptions</strong></summary><ul>"
        for assumption in self.data()['reportData']['assumptions']:
            html += f"<li>{md.convert(assumption)}</li>"
        html += "</ul></details>"

        return html

def batch_result_html_table(result, indices):
    html = ""

    md = markdown.Markdown(extensions=['mdx_math'])

    item_headers = "".join(f"<th>{i}</th>" for i in indices)

    for group_index, group in enumerate(result._data[0]['reportData']['groups']):
        html += f"""
            <details {"open" if group['alwaysVisible'] else ""}>
                <summary style="display:list-item">
                    <strong>{group['title']}</strong>
                </summary>
                <table>
                    <thead><tr><th>Item</th>{item_headers}</tr></thead>"""

        visited_entries = set()

        for entry in [entry for index in indices for entry in result._data[index]['reportData']['groups'][group_index]['entries']]:
            label = entry['label']
            if label in visited_entries:
                continue
            visited_entries.add(label)

            html += f"""
                <tr>
                    <td style="font-weight: bold; vertical-align: top; white-space: nowrap">{label}</td>
            """

            for index in indices:
                val = result._data[index]
                for key in entry['path'].split("/"):
                    if key in val:
                        val = val[key]
                    else:
                        val = "N/A"
                        break
                html += f"""
                        <td style="vertical-align: top; white-space: nowrap">{val}</td>
                """

            html += """
                </tr>
            """
        html += "</table></details>"

    html += f"<details><summary style=\"display:list-item\"><strong>Assumptions</strong></summary><ul>"
    for assumption in result._data[0]['reportData']['assumptions']:
        html += f"<li>{md.convert(assumption)}</li>"
    html += "</ul></details>"

    return html
