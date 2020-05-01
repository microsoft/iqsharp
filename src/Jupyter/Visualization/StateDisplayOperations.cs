// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#nullable enable

using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators;
using Newtonsoft.Json;

namespace Microsoft.Quantum.IQSharp.Jupyter
{

    /// <summary>
    ///     A state dumper that encodes dumped states into Jupyter-display
    ///     objects.
    /// </summary>
    public class JupyterDisplayDumper : QuantumSimulator.StateDumper
    {
        private readonly IChannel Channel;
        private long _count = -1;
        private Complex[]? _data = null;

        /// <summary>
        ///     Whether small amplitudes should be truncated when dumping
        ///     states.
        /// </summary>
        public bool TruncateSmallAmplitudes { get; set; } = false;

        /// <summary>
        ///     The threshold for truncating measurement probabilities when
        ///     dumping states. Computational basis states whose measurement
        ///     probabilities (i.e: squared magnitudes) are below this threshold
        ///     are subject to truncation when
        ///     <see cref="Microsoft.Quantum.IQSharp.Kernel.JupyterDisplayDumper.TruncateSmallAmplitudes" />
        ///     is <c>true</c>.
        /// </summary>
        public double TruncationThreshold { get; set; } = 1e-10;

        /// <summary>
        ///     The labeling convention to be used when labeling computational
        ///     basis states.
        /// </summary>
        public BasisStateLabelingConvention BasisStateLabelingConvention { get; set; } = BasisStateLabelingConvention.Bitstring;


        /// <summary>
        ///     Sets the properties of this display dumper from a given
        ///     configuration source.
        /// </summary>
        public JupyterDisplayDumper Configure(IConfigurationSource configurationSource)
        {
            configurationSource
                .ApplyConfiguration<bool>("dump.truncateSmallAmplitudes", value => TruncateSmallAmplitudes = value)
                .ApplyConfiguration<double>("dump.truncationThreshold", value => TruncationThreshold = value)
                .ApplyConfiguration<BasisStateLabelingConvention>(
                    "dump.basisStateLabelingConvention", value => BasisStateLabelingConvention = value
                );
            return this;
        }

        /// <summary>
        ///     Constructs a new display dumper for a given simulator, using a
        ///     given Jupyter display channel to output dumped states.
        /// </summary>
        public JupyterDisplayDumper(QuantumSimulator sim, IChannel channel) : base(sim)
        {
            Channel = channel;
        }

        /// <summary>
        ///     Used by the simulator to provide states when dumping.
        ///     Not intended to be called directly.
        /// </summary>
        public override bool Callback(uint idx, double real, double img)
        {
            if (_data == null) throw new Exception("Expected data buffer to be initialized before callback, but it was null.");
            _data[idx] = new Complex(real, img);
            return true;
        }

        /// <summary>
        ///     Dumps the state of a register of qubits as a Jupyter displayable
        ///     object.
        /// </summary>
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
            Channel.Display(new DisplayableState
            {
                // We cast here as we don't support a large enough number
                // of qubits to saturate an int.
                QubitIds = qubits?.Select(q => q.Id) ?? Simulator.QubitIds.Select(q => (int)q),
                NQubits = (int)_count,
                Amplitudes = _data
            });

            // Clean up the state vector buffer.
            _data = null;

            return result;
        }

        internal static QVoid DumpToChannel(QuantumSimulator sim, IChannel channel, IConfigurationSource configurationSource, IQArray<Qubit>? qubits = null)
        {
            new JupyterDisplayDumper(sim, channel)
                .Configure(configurationSource)
                .Dump(qubits);
            return QVoid.Instance;
        }
    }

    /// <summary>
    ///     An implementation of <see cref="Microsoft.Quantum.Diagnostics.DumpMachine{__T__}" />
    ///     that dumps the state of its target machine to a Jupyter-displayable
    ///     object.
    /// </summary>
    public class JupyterDumpMachine<T> : Microsoft.Quantum.Diagnostics.DumpMachine<T>
    {
        private QuantumSimulator Simulator { get; }
        internal IConfigurationSource? ConfigurationSource = null;
        internal IChannel? Channel = null;

        /// <inheritdoc />
        public JupyterDumpMachine(QuantumSimulator m) : base(m)
        {
            this.Simulator = m;
        }

        /// <inheritdoc />
        public override Func<T, QVoid> Body => (location) =>
        {
            if (location == null) { throw new ArgumentNullException(nameof(location)); }
            // Check if we're supposed to be writing to a file. In that case,
            // we call the superclass version.
            if (!(location is QVoid) && location.ToString().Length != 0)
            {
                Console.Out.WriteLine("Falling back to base DumpMachine.");
                return base.Body.Invoke(location);
            }

            Debug.Assert(
                Channel != null,
                "No Jupyter display channel was provided when this operation was registered. " +
                "This is an internal error, and should never occur."
            );
            Debug.Assert(
                ConfigurationSource != null,
                "No configuration source was provided when this operation was registered. " +
                "This is an internal error, and should never occur."
            );
            return JupyterDisplayDumper.DumpToChannel(Simulator, Channel, ConfigurationSource);
        };
    }

    /// <summary>
    ///     An implementation of <see cref="Microsoft.Quantum.Diagnostics.DumpRegister{__T__}" />
    ///     that dumps the state of its target machine to a Jupyter-displayable
    ///     object.
    /// </summary>
    public class JupyterDumpRegister<T> : Microsoft.Quantum.Diagnostics.DumpRegister<T>
    {
        private QuantumSimulator Simulator { get; }
        internal IConfigurationSource? ConfigurationSource = null;
        internal IChannel? Channel = null;

        /// <inheritdoc />
        public JupyterDumpRegister(QuantumSimulator m) : base(m)
        {
            this.Simulator = m;
        }

        /// <inheritdoc />
        public override Func<(T, IQArray<Qubit>), QVoid> Body => (input) =>
        {
            var (location, qubits) = input;
            if (location == null) { throw new ArgumentNullException(nameof(location)); }
            // Check if we're supposed to be writing to a file. In that case,
            // we call the superclass version.
            if (!(location is QVoid) && location.ToString().Length != 0)
            {
                Console.Out.WriteLine("Falling back to base DumpRegister.");
                return base.Body.Invoke(input);
            }

            Debug.Assert(
                Channel != null,
                "No Jupyter display channel was provided when this operation was registered." +
                "This is an internal error, and should never occur."
            );
            Debug.Assert(
                ConfigurationSource != null,
                "No configuration source was provided when this operation was registered." +
                "This is an internal error, and should never occur."
            );
            return JupyterDisplayDumper.DumpToChannel(Simulator, Channel, ConfigurationSource, qubits);
        };
    }
}
