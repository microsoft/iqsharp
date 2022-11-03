# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

import unittest
import jupyter_kernel_test

import sys

class MyKernelTests(jupyter_kernel_test.KernelTests):
    # Required --------------------------------------

    # The name identifying an installed kernel to run the tests against
    kernel_name = "iqsharp"

    # language_info.name in a kernel_info_reply should match this
    language_name = "qsharp"

    def test_iqsharp_metadata_is_correct(self):
        """
        Some clients, e.g. nteract, require that metadata on displayable data
        is convertable to dict[str, dict[str, Any]]; we test that this is the
        case here.
        """

        # NB: There is a race condition in jupyter_kernel_test itself on 3.10
        #     that can cause this test to false positive. Since these tests are
        #     not intended to test Python integration, but Jupyter adherence
        #     itself, the Python version is insignificant to this test and can
        #     be safely locked down to 3.9. See tests on the qsharp-core package
        #     for unit and integration testing of Q# + Python interop.
        if sys.version_info.minor >= 10:
            print("This test is not yet supported on Python 3.10.")
            raise unittest.SkipTest

        self.flush_channels()
        reply, output_msgs = self.execute_helper("%version", timeout=30)
        self.assertEqual(output_msgs[0]['header']['msg_type'], 'display_data')
        self.assert_(isinstance(output_msgs[0]['content']['metadata'], dict))
        for mime_type, contents in output_msgs[0]['content']['metadata'].items():
            self.assert_(isinstance(mime_type, str))
            self.assert_(isinstance(contents, dict))
            self.assertEqual(contents, {})

if __name__ == '__main__':
    unittest.main()
