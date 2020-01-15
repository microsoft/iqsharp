// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#nullable enable

using System;
using System.Linq;
using System.Numerics;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    public class JupyterSimulator : QuantumSimulator
    {
        private readonly IChannel Channel;
        public JupyterSimulator(IChannel channel,
                                bool throwOnReleasingQubitsNotInZeroState = true,
                                uint? randomNumberGeneratorSeed = null,
                                bool disableBorrowing = false)
        : base(throwOnReleasingQubitsNotInZeroState, randomNumberGeneratorSeed, disableBorrowing)
        {
            Channel = channel;
            // Default to setting the channel to be the handler for Message.
            DisableLogToConsole();
            OnLog += Channel.Stdout;
        }

        protected override QVoid Dump<T>(T target, IQArray<Qubit>? qubits = null)
        {
            // Check if we're supposed to be writing to a file. In that case,
            // we call the superclass version.
            if (!(target is QVoid) && target.ToString().Length != 0)
            {
                return base.Dump(target, qubits);
            }

            // Otherwise, we know the target is () (represented in C# by QVoid),
            // so we need to display to Jupyter. Let's make a dumper that prints
            // out to the display channel.
            var dumper = new JupyterDisplayDumper(this, Channel);
            dumper.Dump(qubits);


            
            // Finally, return () back to the caller.
            return QVoid.Instance;
        }


        public class JupyterDisplayDumper : StateDumper
        {
            private readonly IChannel Channel;
            private long _count = -1;
            private Complex[]? _data = null;

            public JupyterDisplayDumper(QuantumSimulator sim, IChannel channel) : base(sim)
            {
                Channel = channel;
            }

            public override bool Callback(uint idx, double real, double img)
            {
                if (_data == null) throw new Exception("Expected data buffer to be initialized before callback, but it was null.");
                _data[idx] = new Complex(real, img);
                return true;
            }

            public override bool Dump(IQArray<Qubit>? qubits = null)
            {
                _count = qubits == null
                         ? this.Simulator.QubitManager.GetAllocatedQubitsCount()
                         : qubits.Length;
                _data = new Complex[1 << ((int)_count)];
                var result = base.Dump(qubits);

                // At this point, _data should be filled with the full state
                // vector, so let's display it, counting on the right display
                // encoder to be there to pack it into a table.
                Channel.Display(new StateVector
                {
                    // We cast here as we don't support a large enough number
                    // of qubits to saturate an int.
                    NQubits = (int)_count,
                    Amplitudes = _data
                });

                // Clean up the state vector buffer.
                _data = null;

                return result;
            }
        }
    }
}
