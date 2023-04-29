using Proto;

namespace NothingButNeurons.Brain.Regions.Neurons
{
    /// <summary>
    /// Represents an interior neuron in the neural network, used within hidden layers.
    /// </summary>
    internal class InteriorNeuron : NeuronBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InteriorNeuron"/> class.
        /// </summary>
        public InteriorNeuron(PID debugServerPID, NeuronAddress address, int synapseCt, AccumulationFunction accumulationFunction = AccumulationFunction.Sum, double preActivationThreshold = 0d, ActivationFunction activationFunction = ActivationFunction.TanH, double activationParameterA = 0d, double activationParameterB = 0d, double activationThreshold = 0d, ResetFunction resetFunction = ResetFunction.Zero) : base(debugServerPID, address, synapseCt, accumulationFunction, preActivationThreshold, activationFunction, activationParameterA, activationParameterB, activationThreshold, resetFunction)
        {
        }

        /// <summary>
        /// Handles messages received by the interior neuron.
        /// </summary>
        protected override bool ReceiveMessage(IContext context)
        {
            // Process base class messages first
            bool processed = base.ReceiveMessage(context);
            if (processed)
            {
                return true;
            }

            // Handle InteriorNeuron-specific messages
            switch (context.Message)
            {
                // TODO: InteriorNeuron specific messages here (set processed true if handled!)
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
