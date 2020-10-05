#!/bin/env python
# -*- coding: utf-8 -*-
##
# serialization.py: Utilities for mapping C# values to and from JSON.
##
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
##

try:
    import numpy as np
except ImportError:
    np = None
except Exception as ex:
    print("Failed to import numpy. Please check your numpy installation.")
    raise

# Tuples are json encoded differently in C#, this makes sure they are in the right format.
def map_tuples(obj):
    """
    Given a Python object to be serialized, converts any tuples to dictionaries
    of a form expected by the Q# backend.
    """
    if isinstance(obj, tuple):
        result = {
            '@type': 'tuple'
        }
        
        # For tuples of more than 7 items, the .NET type is ValueTuple<T1,T2,T3,T4,T5,T6,T7,TRest>.
        # Items beyond Item7 must be nested inside a key called "Rest".
        max_tuple_length = 7
        for i in range(min(len(obj), max_tuple_length)):
            result[f"Item{i+1}"] = map_tuples(obj[i])
        if len(obj) > max_tuple_length:
            result["Rest"] = map_tuples(obj[max_tuple_length:])
        return result

    elif isinstance(obj, list) or (np and isinstance(obj, np.ndarray)):
        result = []
        for i in obj:
            result.append(map_tuples(i))
        return result

    elif isinstance(obj, dict):
        result = {}
        for i in obj:
            result[i] = map_tuples(obj[i])
        return result

    elif np and isinstance(obj, np.generic):
        return obj.item()

    else:
        return obj

def unmap_tuples(obj):
    """
    Given a Python object deserialized from JSON, converts any dictionaries that
    represent tuples back to Python tuples. Dictionaries are considered to be
    tuples if they either contain a key `@type` with the value `tuple`, or if
    they have a key `item1`.
    """
    if isinstance(obj, dict):
        # Does this dict represent a tuple?
        if obj.get('@type', None) in ('tuple', '@tuple') or 'Item1' in obj:
            values = []
            while True:
                item = f"Item{len(values) + 1}"
                if item in obj:
                    values.append(unmap_tuples(obj[item]))
                else:
                    break
            return tuple(values)
        # Since this is a plain dict, unmap its values and we're good.
        return {
            key: unmap_tuples(value)
            for key, value in obj.items()
        }

    elif isinstance(obj, list):
        return [unmap_tuples(value) for value in obj]

    else:
        return obj
