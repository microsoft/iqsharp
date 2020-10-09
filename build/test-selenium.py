import os
import pytest
import time

from notebook.notebookapp import list_running_servers
from notebook.tests.selenium.utils import Notebook
from notebook.tests.selenium.quick_selenium import quick_driver

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
    
def test_cell_execution():
    '''
    Check that %version executes and outputs a result
    '''
    nb = Notebook.new_notebook(quick_driver(), kernel_name='kernel-iqsharp')
    nb.edit_cell(index=0, content='%version')
    nb.execute_cell(cell_or_index=0)
    nb.wait_for_cell_output(index=0, timeout=120)

    assert len(nb.get_cell_output(index=0)) > 0
    
    nb.browser.quit()
