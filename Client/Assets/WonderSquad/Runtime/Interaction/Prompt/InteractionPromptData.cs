using System;
using WonderSquad.Core.Contracts.Interaction;

namespace WonderSquad.Interaction.Prompt
{
    public enum InteractionPromptDeviceKind
    {
        Unknown = 0,
        KeyboardMouse = 1,
        Gamepad = 2
    }

    /// <summary>
    /// Describes immutable local presentation data for one detected target.
    /// It carries no execution callback or authoritative gameplay state.
    /// </summary>
    public readonly struct InteractionPromptData :
        IEquatable<InteractionPromptData>
    {
        private readonly string promptId;
        private readonly string actionTextKey;
        private readonly string actionText;
        private readonly string bindingDisplayText;

        public InteractionPromptData(
            InteractionTargetId targetId,
            string promptId,
            string actionTextKey,
            string actionText,
            InteractionPromptDeviceKind deviceKind,
            string bindingDisplayText,
            bool isVisible)
        {
            TargetId = targetId;
            this.promptId = promptId ?? string.Empty;
            this.actionTextKey =
                actionTextKey ?? string.Empty;
            this.actionText = actionText ?? string.Empty;
            DeviceKind = deviceKind;
            this.bindingDisplayText =
                bindingDisplayText ?? string.Empty;
            IsVisible = isVisible;
        }

        public InteractionTargetId TargetId { get; }

        public string PromptId =>
            promptId ?? string.Empty;

        public string ActionTextKey =>
            actionTextKey ?? string.Empty;

        public string ActionText =>
            actionText ?? string.Empty;

        public InteractionPromptDeviceKind DeviceKind { get; }

        public string BindingDisplayText =>
            bindingDisplayText ?? string.Empty;

        public bool IsVisible { get; }

        public bool HasValidContent =>
            TargetId.IsValid &&
            !string.IsNullOrWhiteSpace(PromptId) &&
            !string.IsNullOrWhiteSpace(ActionTextKey) &&
            !string.IsNullOrWhiteSpace(ActionText) &&
            DeviceKind != InteractionPromptDeviceKind.Unknown &&
            !string.IsNullOrWhiteSpace(BindingDisplayText);

        public bool IsDisplayable =>
            IsVisible &&
            HasValidContent;

        public static InteractionPromptData CreateHidden(
            InteractionPromptDeviceKind deviceKind,
            string bindingDisplayText)
        {
            return new InteractionPromptData(
                default,
                string.Empty,
                string.Empty,
                string.Empty,
                deviceKind,
                bindingDisplayText,
                false);
        }

        public bool Equals(InteractionPromptData other)
        {
            return
                TargetId == other.TargetId &&
                string.Equals(
                    PromptId,
                    other.PromptId,
                    StringComparison.Ordinal) &&
                string.Equals(
                    ActionTextKey,
                    other.ActionTextKey,
                    StringComparison.Ordinal) &&
                string.Equals(
                    ActionText,
                    other.ActionText,
                    StringComparison.Ordinal) &&
                DeviceKind == other.DeviceKind &&
                string.Equals(
                    BindingDisplayText,
                    other.BindingDisplayText,
                    StringComparison.Ordinal) &&
                IsVisible == other.IsVisible;
        }

        public override bool Equals(object other)
        {
            return
                other is InteractionPromptData promptData &&
                Equals(promptData);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = TargetId.GetHashCode();
                hashCode =
                    (hashCode * 397) ^
                    StringComparer.Ordinal.GetHashCode(PromptId);
                hashCode =
                    (hashCode * 397) ^
                    StringComparer.Ordinal.GetHashCode(ActionTextKey);
                hashCode =
                    (hashCode * 397) ^
                    StringComparer.Ordinal.GetHashCode(ActionText);
                hashCode =
                    (hashCode * 397) ^
                    (int)DeviceKind;
                hashCode =
                    (hashCode * 397) ^
                    StringComparer.Ordinal.GetHashCode(
                        BindingDisplayText);
                hashCode =
                    (hashCode * 397) ^
                    IsVisible.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(
            InteractionPromptData left,
            InteractionPromptData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            InteractionPromptData left,
            InteractionPromptData right)
        {
            return !left.Equals(right);
        }
    }
}
