from notebook.tests.selenium.utils import Notebook
from notebook.tests.selenium.quick_selenium import quick_driver

nb = Notebook.new_notebook(quick_driver(), kernel_name='kernel-iqsharp')
nb.add_and_execute_cell(content='%version')

print(nb.get_cell_output())

nb.browser.quit()
