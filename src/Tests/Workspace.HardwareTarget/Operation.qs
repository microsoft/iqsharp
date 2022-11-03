namespace Tests.qss {

    open Microsoft.Quantum.Intrinsic;

    operation ValidEntryPoint() : Result {
        use q = Qubit();
        H(q);
        return M(q);
    }
    
    operation ClassicalControl() : Result {
        use q = Qubit[2];
        H(q[0]);
        if (M(q[0])== One) {
            X(q[1]);
        }
        return M(q[1]);
    }
}