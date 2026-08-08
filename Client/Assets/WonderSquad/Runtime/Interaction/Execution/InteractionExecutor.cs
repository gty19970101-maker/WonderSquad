using System;
using UnityEngine;
using WonderSquad.Core.Contracts.Interaction;
using WonderSquad.Core.Contracts.Player;
using WonderSquad.Core.Identifiers;
using WonderSquad.Core.Logging;
using WonderSquad.Interaction.Detection;

namespace WonderSquad.Interaction.Execution
{
    /// <summary>
    /// Converts primary-interaction input into a validated local request
    /// without discovering targets or changing presentation state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractionExecutor : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Component implementing the device-independent interaction input source.")]
        private MonoBehaviour interactionInputSourceComponent;

        [SerializeField]
        [Tooltip("Detector that owns the current local interaction focus.")]
        private InteractionDetector interactionDetector;

        [SerializeField]
        [Tooltip("Component providing the stable local player identity.")]
        private MonoBehaviour playerIdentitySourceComponent;

        [SerializeField]
        [Tooltip("Current authority-side origin used to revalidate the request.")]
        private Transform interactionOrigin;

        [SerializeField]
        [Tooltip("Replaceable request port used to apply the validated request.")]
        private MonoBehaviour requestPortComponent;

        [SerializeField]
        [Tooltip("Allows local execution without changing input or detection state.")]
        private bool shouldAllowExecution = true;

        private readonly InteractionExecutionValidator validator =
            new InteractionExecutionValidator();
        private readonly InteractionRequestIdGenerator requestIdGenerator =
            new InteractionRequestIdGenerator();

        private IInteractionInputSource interactionInputSource;
        private IPlayerIdentitySource playerIdentitySource;
        private IInteractionRequestPort requestPort;
        private InteractionResult lastResult;
        private bool isExecuting;

        public event Action<InteractionResult> InteractionResolved;

        public InteractionResult LastResult => lastResult;

        public bool IsExecuting => isExecuting;

        public bool ShouldAllowExecution => shouldAllowExecution;

        public bool HasValidConfiguration =>
            interactionInputSourceComponent is IInteractionInputSource &&
            interactionDetector != null &&
            playerIdentitySourceComponent is IPlayerIdentitySource &&
            interactionOrigin != null &&
            requestPortComponent is IInteractionRequestPort;

        /// <summary>
        /// Supplies explicit local dependencies before the executor is
        /// enabled. Intended for Prefab composition and isolated tests.
        /// </summary>
        public bool Configure(
            MonoBehaviour inputSource,
            InteractionDetector detector,
            MonoBehaviour identitySource,
            Transform authorityOrigin,
            MonoBehaviour localRequestPort,
            bool shouldAllow)
        {
            if (isActiveAndEnabled)
            {
                ProjectLog.Error(
                    "InteractionExecutor cannot be reconfigured while enabled.");
                return false;
            }

            interactionInputSourceComponent = inputSource;
            interactionDetector = detector;
            playerIdentitySourceComponent = identitySource;
            interactionOrigin = authorityOrigin;
            requestPortComponent = localRequestPort;
            shouldAllowExecution = shouldAllow;
            return HasValidConfiguration;
        }

        private void OnEnable()
        {
            if (!TryResolveDependencies())
            {
                ProjectLog.Error(
                    "InteractionExecutor requires explicit input, detector, identity, origin, and request port references.");
                enabled = false;
                return;
            }

            interactionInputSource.InteractionPressed +=
                OnInteractionPressed;
        }

        private void OnDisable()
        {
            if (interactionInputSource != null)
            {
                interactionInputSource.InteractionPressed -=
                    OnInteractionPressed;
            }

            isExecuting = false;
        }

        /// <summary>
        /// Executes one request against the detector's current stable target.
        /// It never discovers or selects another target.
        /// </summary>
        public InteractionResult ExecuteCurrentTarget()
        {
            var currentTarget =
                interactionDetector == null
                    ? null
                    : interactionDetector.CurrentTarget;
            var targetId =
                IsAlive(currentTarget)
                    ? currentTarget.TargetId
                    : default;
            var request = CreateRequest(targetId);
            return Execute(request, currentTarget);
        }

        /// <summary>
        /// Revalidates and executes one explicit request against one already
        /// selected target. The caller remains responsible for target
        /// discovery or authority-side ID resolution.
        /// </summary>
        public InteractionResult Execute(
            in InteractionRequest request,
            IInteractable currentTarget)
        {
            if (!shouldAllowExecution ||
                !isActiveAndEnabled)
            {
                return Resolve(
                    InteractionResult.Create(
                        request,
                        InteractionResultCode.Cancelled));
            }

            if (isExecuting)
            {
                return Resolve(
                    InteractionResult.Create(
                        request,
                        InteractionResultCode.Busy));
            }

            var executableTarget =
                ResolveExecutableTarget(currentTarget);
            var validationCode = validator.Validate(
                request,
                currentTarget,
                executableTarget,
                interactionOrigin,
                interactionDetector.Settings);
            if (validationCode != InteractionResultCode.Success)
            {
                return Resolve(
                    InteractionResult.Create(
                        request,
                        validationCode));
            }

            if (requestPort == null || !requestPort.IsAvailable)
            {
                return Resolve(
                    InteractionResult.Create(
                        request,
                        InteractionResultCode.Cancelled));
            }

            isExecuting = true;
            try
            {
                return Resolve(
                    requestPort.Execute(
                        request,
                        executableTarget));
            }
            finally
            {
                isExecuting = false;
            }
        }

        private void OnInteractionPressed()
        {
            ExecuteCurrentTarget();
        }

        private InteractionRequest CreateRequest(
            InteractionTargetId targetId)
        {
            var playerId =
                playerIdentitySource == null
                    ? default(PlayerId)
                    : playerIdentitySource.PlayerId;
            var context = new InteractionContext(
                playerId,
                requestIdGenerator.CreateNext(),
                Time.unscaledTimeAsDouble,
                interactionOrigin == null
                    ? Vector3.zero
                    : interactionOrigin.position,
                interactionOrigin == null
                    ? Vector3.zero
                    : interactionOrigin.forward);
            return new InteractionRequest(context, targetId);
        }

        private bool TryResolveDependencies()
        {
            interactionInputSource =
                interactionInputSourceComponent as IInteractionInputSource;
            playerIdentitySource =
                playerIdentitySourceComponent as IPlayerIdentitySource;
            requestPort =
                requestPortComponent as IInteractionRequestPort;

            return
                HasValidConfiguration &&
                playerIdentitySource.HasValidIdentity;
        }

        private static IExecutableInteraction ResolveExecutableTarget(
            IInteractable detectionTarget)
        {
            if (!IsAlive(detectionTarget))
            {
                return null;
            }

            if (detectionTarget is IExecutableInteraction executableTarget)
            {
                return executableTarget;
            }

            return detectionTarget is Component component
                ? component.GetComponentInParent<IExecutableInteraction>()
                : null;
        }

        private InteractionResult Resolve(InteractionResult result)
        {
            lastResult = result;
            InteractionResolved?.Invoke(result);
            return result;
        }

        private static bool IsAlive(IInteractable target)
        {
            if (target == null)
            {
                return false;
            }

            return
                !(target is UnityEngine.Object unityObject) ||
                unityObject != null;
        }
    }
}
