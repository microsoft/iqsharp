# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

import unittest
import jupyter_kernel_test

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
        self.flush_channels()
        reply, output_msgs = self.execute_helper("%version")
        self.assertEqual(output_msgs[0]['header']['msg_type'], 'display_data')
        self.assert_(isinstance(output_msgs[0]['content']['metadata'], dict))
        for mime_type, contents in output_msgs[0]['content']['metadata'].items():
            self.assert_(isinstance(mime_type, str))
            self.assert_(isinstance(contents, dict))
            self.assertEqual(contents, {})

if __name__ == '__main__':
    unittest.main()
