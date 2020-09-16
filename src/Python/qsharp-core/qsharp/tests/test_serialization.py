
#!/bin/env python
# -*- coding: utf-8 -*-
##
# test_serialization.py: Checks correctness of JSON serialization.
##
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
##

## IMPORTS ##

import unittest
import json
import numpy as np
from qsharp.serialization import map_tuples, unmap_tuples

class TestSerialization(unittest.TestCase):
    def test_map_shallow_tuple(self):
        self.assertEqual(
            map_tuples((42, 'foo')),
            {'@type': 'tuple', 'Item1': 42, 'Item2': 'foo'}
        )

    def test_map_deep_tuple(self):
        actual = {
            'foo': [1, 3.14, (42, 'baz')],
            'bar': {'a': ('a', 'a'), 'b': ()}
        }
        expected = {
            'foo': [1, 3.14, {'@type': 'tuple', 'Item1': 42, 'Item2': 'baz'}],
            'bar': {
                'a': {'@type': 'tuple', 'Item1': 'a', 'Item2': 'a'},
                'b': {'@type': 'tuple'}
            }
        }
        self.assertEqual(
            map_tuples(actual), expected
        )


    def test_map_numpy_types(self):
        self.assertEqual(
            map_tuples(np.array([1, 2, 3], dtype=np.int32)),
            [1, 2, 3]
        )
        self.assertEqual(
            map_tuples(np.array([1., 2., 3.], dtype=np.single)),
            [1., 2., 3.]
        )


    def test_map_ndarray(self):
        self.assertEqual(
            map_tuples(np.array([1, 2, 3])),
            [1, 2, 3]
        )
        
        tuples = [(0, 'Zero'), (1, 'One')]
        tuples_array = np.empty(len(tuples), dtype=object)
        tuples_array[:] = tuples
        self.assertEqual(
            map_tuples(tuples_array),
            [
                {'@type': 'tuple', 'Item1': 0, 'Item2': 'Zero'},
                {'@type': 'tuple', 'Item1': 1, 'Item2': 'One'}
            ]
        )

    def test_roundtrip_shallow_tuple(self):
        actual = ('a', 3.14, False)
        self.assertEqual(
            unmap_tuples(map_tuples(actual)), actual
        )

    def test_roundtrip_dict(self):
        actual = {'a': 'b', 'c': ('d', 'e')}
        self.assertEqual(
            unmap_tuples(map_tuples(actual)), actual
        )

    def test_roundtrip_deep_tuple(self):
        actual = ('a', ('b', 'c'))
        self.assertEqual(
            unmap_tuples(map_tuples(actual)), actual
        )

    def test_roundtrip_very_deep_tuple(self):
        actual = {
            'a': {
                'b': (
                    {
                        'c': ('d', ['e', ('f', 'g', 12, False)])
                    },
                    ['h', {'g': ('i', 'j')}]
                )
            }
        }
        self.assertEqual(
            unmap_tuples(map_tuples(actual)), actual
        )

if __name__ == "__main__":
    unittest.main()
