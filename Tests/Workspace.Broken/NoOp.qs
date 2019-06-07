// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Tests.qss {
    
    open Microsoft.Quantum.Intrinsic;
    
    
    operation NoOp () : Unit {
        //[ERROR]: Can't return anything here:
        return 5;
    }
}
