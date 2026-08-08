using WonderSquad.Core.Identifiers;

namespace WonderSquad.Interaction.Execution
{
    /// <summary>
    /// Generates non-zero request IDs for one local player session.
    /// </summary>
    public sealed class InteractionRequestIdGenerator
    {
        private uint nextValue;

        public InteractionRequestIdGenerator(uint firstValue = 1U)
        {
            nextValue = firstValue == 0U ? 1U : firstValue;
        }

        public RequestId CreateNext()
        {
            var requestId = new RequestId(nextValue);
            nextValue++;
            if (nextValue == 0U)
            {
                nextValue = 1U;
            }

            return requestId;
        }
    }
}
