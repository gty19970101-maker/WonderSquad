using System;

namespace WonderSquad.Core.Identifiers
{
    /// <summary>
    /// Identifies a player without relying on a Unity object or scene name.
    /// Zero is reserved for an invalid identity.
    /// </summary>
    public readonly struct PlayerId :
        IEquatable<PlayerId>,
        IComparable<PlayerId>
    {
        public PlayerId(ulong value)
        {
            Value = value;
        }

        public ulong Value { get; }

        public bool IsValid => Value != 0UL;

        public int CompareTo(PlayerId other)
        {
            return Value.CompareTo(other.Value);
        }

        public bool Equals(PlayerId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object other)
        {
            return other is PlayerId playerId && Equals(playerId);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static bool operator ==(PlayerId left, PlayerId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PlayerId left, PlayerId right)
        {
            return !left.Equals(right);
        }
    }
}
