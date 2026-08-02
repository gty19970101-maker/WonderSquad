using System;

namespace WonderSquad.Core.Contracts.Interaction
{
    /// <summary>
    /// Identifies an interaction target without relying on scene names or
    /// Unity object references.
    /// </summary>
    public readonly struct InteractionTargetId :
        IEquatable<InteractionTargetId>,
        IComparable<InteractionTargetId>
    {
        private readonly string value;

        public InteractionTargetId(string value)
        {
            this.value = value ?? string.Empty;
        }

        public string Value => value ?? string.Empty;

        public bool IsValid => !string.IsNullOrWhiteSpace(value);

        public int CompareTo(InteractionTargetId other)
        {
            return string.CompareOrdinal(Value, other.Value);
        }

        public bool Equals(InteractionTargetId other)
        {
            return string.Equals(
                Value,
                other.Value,
                StringComparison.Ordinal);
        }

        public override bool Equals(object other)
        {
            return
                other is InteractionTargetId targetId &&
                Equals(targetId);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(
            InteractionTargetId left,
            InteractionTargetId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            InteractionTargetId left,
            InteractionTargetId right)
        {
            return !left.Equals(right);
        }
    }
}
