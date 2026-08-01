using UnityEngine.InputSystem;

namespace WonderSquad.Player.Input
{
    /// <summary>
    /// Validates the minimum contract required by the move input reader.
    /// </summary>
    public static class PlayerInputConfigurationValidator
    {
        private const string ExpectedControlType = "Vector2";

        public static bool TryValidateMoveAction(
            InputActionReference moveActionReference,
            out string error)
        {
            if (moveActionReference == null)
            {
                error = "Move Input Action Reference is missing.";
                return false;
            }

            var moveAction = moveActionReference.action;
            if (moveAction == null)
            {
                error = "Move Input Action Reference does not resolve to an action.";
                return false;
            }

            if (moveAction.type != InputActionType.Value)
            {
                error = "Move Input Action must use the Value action type.";
                return false;
            }

            if (moveAction.expectedControlType != ExpectedControlType)
            {
                error = $"Move Input Action must expect {ExpectedControlType}.";
                return false;
            }

            if (moveAction.bindings.Count == 0)
            {
                error = "Move Input Action must contain at least one binding.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
