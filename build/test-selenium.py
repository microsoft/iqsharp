import os
import pytest
import time

from notebook.notebookapp import list_running_servers
from notebook.tests.selenium.test_interrupt import interrupt_from_menu
from notebook.tests.selenium.utils import Notebook, wait_for_selector
from selenium.common.exceptions import JavascriptException
from selenium.webdriver import Firefox

def setup_module():
    '''
    Wait for notebook server to start
    '''
    # workaround for missing os.uname attribute on some Windows systems
    if not hasattr(os, "uname"):
        os.uname = lambda: ["Windows"]

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

def get_sample_operation():
    '''
    Returns a sample Q# operation to be entered into a Jupyter Notebook cell
    '''    
    # Jupyter Notebook automatically adds a closing brace when typing
    # an open brace into a code cell. To work around this, we omit the
    # closing braces from the string that we enter.
    return '''
        operation DoNothing() : Unit {
            using (q = Qubit()) {
                H(q);
                H(q);
    '''
    
def test_cell_execution():
    '''
    Check that %version executes and outputs an expected result
    '''
    nb = create_notebook()

    nb.add_and_execute_cell(index=0, content='%version')
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
    nb.browser.execute_script("require('plotting')")
    nb.browser.execute_script("require('telemetry')")
    nb.browser.execute_script("require('visualizer')")
    nb.browser.execute_script("require('ExecutionPathVisualizer/index')")
    
    nb.browser.quit()

def test_debug_magic():
    '''
    Check that the IQ# %debug command works correctly
    '''
    nb = create_notebook()

    nb.add_and_execute_cell(index=0, content=get_sample_operation())
    outputs = nb.wait_for_cell_output(index=0, timeout=120)
    assert len(outputs) > 0
    assert "DoNothing" == outputs[0].text, outputs[0].text

    def validate_outputs(outputs, expected_trace):
        assert len(outputs) > 0
        assert 'Starting debug session' in outputs[0].text, outputs[0].text
        assert 'Debug controls' in outputs[1].text, outputs[1].text
        assert expected_trace == outputs[2].text, outputs[2].text
        assert 'Finished debug session' in outputs[3].text, outputs[3].text

    debug_button_selector = ".iqsharp-debug-toolbar .btn"

    def wait_for_debug_button():
        wait_for_selector(nb.browser, debug_button_selector, single=True)

    def click_debug_button():
        nb.browser.find_element_by_css_selector(debug_button_selector).click()

    #
    # Run %debug and interrupt kernel without clicking "Next step"
    #
    nb.add_and_execute_cell(index=1, content='%debug DoNothing')
    wait_for_debug_button()
    interrupt_from_menu(nb)

    outputs = nb.wait_for_cell_output(index=1, timeout=120)
    validate_outputs(outputs, expected_trace='')

    #
    # Run %debug and click the "Next step" button before interrupting
    #
    nb.clear_cell_output(index=1)
    nb.add_and_execute_cell(index=1, content='%debug DoNothing')
    wait_for_debug_button()
    click_debug_button()
    interrupt_from_menu(nb)

    outputs = nb.wait_for_cell_output(index=1, timeout=120)
    validate_outputs(outputs, expected_trace='|0\u27E9 q0 H') # \u27E9 is mathematical right angle bracket
    
    #
    # Run %debug and click the "Next step" button twice,
    # which should finish running the operation
    #
    nb.clear_cell_output(index=1)
    nb.add_and_execute_cell(index=1, content='%debug DoNothing')
    wait_for_debug_button()
    click_debug_button()
    click_debug_button()

    outputs = nb.wait_for_cell_output(index=1, timeout=120)
    validate_outputs(outputs, expected_trace='|0\u27E9 q0 H H') # \u27E9 is mathematical right angle bracket

    nb.browser.quit()

def test_trace_magic():
    '''
    Check that the IQ# %trace command works correctly
    '''
    nb = create_notebook()

    nb.add_and_execute_cell(index=0, content=get_sample_operation())
    outputs = nb.wait_for_cell_output(index=0, timeout=120)
    assert len(outputs) > 0
    assert "DoNothing" == outputs[0].text, outputs[0].text

    nb.add_and_execute_cell(index=1, content='%trace DoNothing')
    outputs = nb.wait_for_cell_output(index=1, timeout=120)
    assert len(outputs) > 0

    # Verify expected text output
    assert '|0\u27E9 q0 H H' == outputs[0].text, outputs[0].text # \u27E9 is mathematical right angle bracket

    nb.browser.quit()
