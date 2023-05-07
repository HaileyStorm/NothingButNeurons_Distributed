using NothingButNeurons.Shared.DataClasses.Neurons;
using Proto;

namespace NothingButNeurons.Brain.Neurons
{
    /// <summary>
    /// Represents an output neuron in the neural network.
    /// </summary>
    internal class OutputNeuron : NeuronBase
    {
        // TODO: many of these prameters will ALWAYS use defaults for OutputNeurons. Update accordingly.
        /// <summary>
        /// Initializes a new instance of the <see cref="OutputNeuron"/> class.
        /// </summary>
        public OutputNeuron(PID debugServerPID, int synapseCt, NeuronData neuronData) : base(debugServerPID, synapseCt, neuronData)
        {
            //Debug.WriteLine($"Enabling Output Neuron {address.RegionPart}/{address.NeuronPart}");
            _behavior.Become(Enabled);
        }

        /// <summary>
        /// Handles messages received by the output neuron.
        /// </summary>
        protected override bool ReceiveMessage(IContext context)
        {
            // Process base class messages first
            bool processed = base.ReceiveMessage(context);
            if (processed)
            {
                return true;
            }
            Debug.WriteLine($"!!! Output neuron received unhandled message: {context.Message.GetType()}");
            // Handle OutputNeuron-specific messages
            switch (context.Message)
            {
                // TODO: OutputNeuron specific messages here (set processed true if handled!)
                default:
                    break;
            }

            return processed;
        }

        #region Lifecycle Event Handlers
        protected override void ProcessRestartingMessage(IContext context, Restarting msg)
        {
        }

        protected override void ProcessStartedMessage(IContext context, Started msg)
        {
        }

        protected override void ProcessStoppingMessage(IContext context, Stopping msg)
        {
        }

        protected override void ProcessStoppedMessage(IContext context, Stopped msg)
        {
        }
        #endregion
    }

}
