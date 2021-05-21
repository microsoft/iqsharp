import os
import pytest
import time
import sys

from notebook.notebookapp import list_running_servers
from notebook.tests.selenium.test_interrupt import interrupt_from_menu
from notebook.tests.selenium.utils import Notebook, wait_for_selector
from selenium.common.exceptions import JavascriptException
from selenium.webdriver import Firefox
from selenium.webdriver.support.ui import WebDriverWait

def setup_module():
    '''
    Wait for notebook server to start
    '''
    # workaround for missing os.uname attribute on some Windows systems
    if not hasattr(os, "uname"):
        os.uname = lambda: ["Windows"]

    total_wait_time = 180
    remaining_time = total_wait_time
    iteration_time = 5
    print("Waiting for notebook server to start...")
    while not list(list_running_servers()) and remaining_time > 0:
        time.sleep(iteration_time)
        remaining_time -= iteration_time

    if not list(list_running_servers()):
        raise Exception(f"Notebook server did not start in {total_wait_time} seconds")

def create_notebook():
    '''
    Returns a new IQ# notebook in a Firefox web driver
    '''
    driver = None
    max_retries = 5
    for _ in range(max_retries):
        try:
            driver = Firefox()
            break
        except:
            print(f"Exception creating Firefox driver, retrying. Exception info: {sys.exc_info()}")

    if not driver:
        raise Exception(f"Failed to create Firefox driver in {max_retries} tries")

    server = list(list_running_servers())[0]
    driver.get('{url}?token={token}'.format(**server))
    return Notebook.new_notebook(driver, kernel_name='kernel-iqsharp')

def get_sample_operation():
    '''
    Returns a sample Q# operation to be entered into a Jupyter Notebook cell
    '''    
    # Jupyter Notebook automatically adds a closing brace when typing
    # an open brace into a code cell. To work around this, we omit the
    # closing braces from the string that we enter.
    return '''
        operation DoNothing() : Unit {
            use q = Qubit();
            H(q);
            H(q);
    '''
    
def test_kernel_startup():
    '''
    Check that basic functionality works
    '''
    nb = create_notebook()

    # Check that %version executes and outputs an expected result
    nb.add_and_execute_cell(index=0, content='%version')
    outputs = nb.wait_for_cell_output(index=0, timeout=120)

    assert len(outputs) > 0
    assert "iqsharp" in outputs[0].text, outputs[0].text

    # Check that the expected IQ# JavaScript modules are properly loaded
    with pytest.raises(JavascriptException):
        nb.browser.execute_script("require('fake/module')")

    nb.browser.execute_script("require('codemirror/addon/mode/simple')")
    nb.browser.execute_script("require('plotting')")
    nb.browser.execute_script("require('telemetry')")
    nb.browser.execute_script("require('visualizer')")
    nb.browser.execute_script("require('ExecutionPathVisualizer/index')")
    
    nb.browser.quit()

def test_trace_magic():
    '''
    Check that the IQ# %trace command works correctly
    '''
    nb = create_notebook()

    cell_index = 0
    nb.add_and_execute_cell(index=cell_index, content=get_sample_operation())
    outputs = nb.wait_for_cell_output(index=cell_index, timeout=120)
    assert len(outputs) > 0
    assert "DoNothing" == outputs[0].text, outputs[0].text

    cell_index = 1
    nb.add_and_execute_cell(index=cell_index, content='%trace DoNothing')
    outputs = nb.wait_for_cell_output(index=cell_index, timeout=120)
    assert len(outputs) > 0

    # Verify expected text output
    expected_trace = '|0\u27E9 q0 H H' # \u27E9 is mathematical right angle bracket
    WebDriverWait(nb.browser, 60).until(
        lambda b: expected_trace == nb.get_cell_output(index=cell_index)[0].text
    )
    outputs = nb.get_cell_output(index=cell_index)
    assert expected_trace == outputs[0].text, outputs[0].text

    nb.browser.quit()

def test_debug_magic():
    '''
    Check that the IQ# %debug command works correctly
    '''
    nb = create_notebook()

    cell_index = 0
    nb.add_and_execute_cell(index=cell_index, content=get_sample_operation())
    outputs = nb.wait_for_cell_output(index=cell_index, timeout=120)
    assert len(outputs) > 0
    assert "DoNothing" == outputs[0].text, outputs[0].text

    def validate_debug_outputs(index, expected_trace):
        WebDriverWait(nb.browser, 60).until(
            lambda b: len(nb.get_cell_output(index=index)) >= 4 and \
                      expected_trace == nb.get_cell_output(index=index)[2].text
        )
        outputs = nb.get_cell_output(index=index)
        assert len(outputs) >= 4
        assert 'Starting debug session' in outputs[0].text, outputs[0].text
        assert 'Debug controls' in outputs[1].text, outputs[1].text
        assert expected_trace == outputs[2].text, outputs[2].text
        assert 'Finished debug session' in outputs[3].text, outputs[3].text

    debug_button_selector = ".iqsharp-debug-toolbar .btn"

    def wait_for_debug_button():
        wait_for_selector(nb.browser, debug_button_selector, single=True)

    def click_debug_button():
        nb.browser.find_element_by_css_selector(debug_button_selector).click()

    cell_index = 1

    # Run %debug and interrupt kernel without clicking "Next step"
    nb.add_and_execute_cell(index=cell_index, content='%debug DoNothing')
    wait_for_debug_button()
    interrupt_from_menu(nb)

    validate_debug_outputs(index=cell_index, expected_trace='')

    nb.browser.quit()
