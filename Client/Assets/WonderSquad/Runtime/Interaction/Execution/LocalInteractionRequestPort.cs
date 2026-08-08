using UnityEngine;
using WonderSquad.Core.Contracts.Interaction;
using WonderSquad.Core.Identifiers;

namespace WonderSquad.Interaction.Execution
{
    /// <summary>
    /// Applies one validated request locally. A future host-authority adapter
    /// replaces this boundary without changing the interaction contract.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocalInteractionRequestPort :
        MonoBehaviour,
        IInteractionRequestPort
    {
        private PlayerId cachedPlayerId;
        private RequestId cachedRequestId;
        private InteractionResult cachedResult;
        private bool hasCachedResult;

        public bool IsAvailable => isActiveAndEnabled;

        public InteractionResult Execute(
            in InteractionRequest request,
            IExecutableInteraction target)
        {
            if (hasCachedResult &&
                cachedPlayerId == request.Context.PlayerId &&
                cachedRequestId == request.Context.RequestId)
            {
                return cachedResult;
            }

            InteractionResult result;
            if (!IsAvailable)
            {
                result = InteractionResult.Create(
                    request,
                    InteractionResultCode.Cancelled);
            }
            else if (!request.IsValid)
            {
                result = InteractionResult.Create(
                    request,
                    InteractionResultCode.Unknown);
            }
            else if (!IsAlive(target) ||
                     target.TargetId != request.TargetId)
            {
                result = InteractionResult.Create(
                    request,
                    InteractionResultCode.TargetInvalid);
            }
            else if (!target.IsExecutionAvailable)
            {
                result = InteractionResult.Create(
                    request,
                    InteractionResultCode.Busy);
            }
            else
            {
                result = NormalizeResult(
                    request,
                    target.Execute(request.Context));
            }

            Cache(request, result);
            return result;
        }

        private static InteractionResult NormalizeResult(
            in InteractionRequest request,
            InteractionResult result)
        {
            if (result.RequestId != request.Context.RequestId ||
                result.TargetId != request.TargetId)
            {
                return InteractionResult.Create(
                    request,
                    InteractionResultCode.Unknown);
            }

            return result;
        }

        private void Cache(
            in InteractionRequest request,
            InteractionResult result)
        {
            cachedPlayerId = request.Context.PlayerId;
            cachedRequestId = request.Context.RequestId;
            cachedResult = result;
            hasCachedResult = true;
        }

        private static bool IsAlive(IExecutableInteraction target)
        {
            if (target == null)
            {
                return false;
            }

            return
                !(target is Object unityObject) ||
                unityObject != null;
        }
    }
}
