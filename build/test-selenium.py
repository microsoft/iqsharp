import os
import pytest
import time

from notebook.notebookapp import list_running_servers
from notebook.tests.selenium.utils import Notebook
from selenium.common.exceptions import JavascriptException
from selenium.webdriver import Firefox

# workaround for missing os.uname attribute on some Windows systems
if not hasattr(os, "uname"):
    os.uname = lambda: ["Windows"]

def setup_module():
    '''
    Wait for notebook server to start
    '''
    remaining_time = 180
    iteration_time = 5
    print("Waiting for notebook server to start...")
    while not list(list_running_servers()) and remaining_time > 0:
        time.sleep(iteration_time)
        remaining_time -= iteration_time

    if not list(list_running_servers()):
        raise Exception(f"Notebook server did not start in {remaining_time} seconds")

def create_notebook():
    '''
    Returns a new IQ# notebook in a Firefox web driver
    '''
    server = list(list_running_servers())[0]
    driver = Firefox()
    driver.get('{url}?token={token}'.format(**server))
    return Notebook.new_notebook(driver, kernel_name='kernel-iqsharp')
    
def test_cell_execution():
    '''
    Check that %version executes and outputs an expected result
    '''
    nb = create_notebook()
    nb.edit_cell(index=0, content='%version')
    nb.execute_cell(cell_or_index=0)
    outputs = nb.wait_for_cell_output(index=0, timeout=120)

    assert len(outputs) > 0
    assert "iqsharp" in outputs[0].text, outputs[0].text
    
    nb.browser.quit()

def test_javascript_loaded():
    '''
    Check that the expected IQ# JavaScript modules are properly loaded
    '''
    nb = create_notebook()

    with pytest.raises(JavascriptException):
        nb.browser.execute_script("require('fake/module')")

    nb.browser.execute_script("require('codemirror/addon/mode/simple')")
    nb.browser.execute_script("require('telemetry')")
    nb.browser.execute_script("require('visualizer')")
    nb.browser.execute_script("require('ExecutionPathVisualizer/index')")
    
    nb.browser.quit()
   