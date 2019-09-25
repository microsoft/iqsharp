# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import unittest
import jupyter_kernel_test

class MyKernelTests(jupyter_kernel_test.KernelTests):
    # Required --------------------------------------

    # The name identifying an installed kernel to run the tests against
    kernel_name = "iqsharp"

    # language_info.name in a kernel_info_reply should match this
    language_name = "qsharp"

if __name__ == '__main__':
    unittest.main()
