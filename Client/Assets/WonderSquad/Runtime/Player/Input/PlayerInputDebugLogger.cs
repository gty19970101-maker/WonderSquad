using UnityEngine;
using WonderSquad.Core.Logging;

namespace WonderSquad.Player.Input
{
    /// <summary>
    /// Reports input changes for the prototype Sandbox without polling input.
    /// </summary>
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerInputDebugLogger : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Input reader whose values are reported in the Unity Console.")]
        private PlayerInputReader inputReader;

        private void Awake()
        {
            if (inputReader == null)
            {
                inputReader = GetComponent<PlayerInputReader>();
            }
        }

        private void OnEnable()
        {
            if (inputReader == null)
            {
                ProjectLog.Error(
                    "PlayerInputDebugLogger requires a PlayerInputReader.");
                enabled = false;
                return;
            }

            inputReader.MoveInputChanged += OnMoveInputChanged;
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.MoveInputChanged -= OnMoveInputChanged;
            }
        }

        private static void OnMoveInputChanged(Vector2 moveInput)
        {
            ProjectLog.Debug(
                $"Player Move Input: ({moveInput.x:0.00}, {moveInput.y:0.00}), " +
                $"Magnitude: {moveInput.magnitude:0.00}");
        }
    }
}
