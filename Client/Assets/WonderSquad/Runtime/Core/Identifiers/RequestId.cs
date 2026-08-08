using System;

namespace WonderSquad.Core.Identifiers
{
    /// <summary>
    /// Identifies one gameplay request within a player session.
    /// Zero is reserved for an invalid request.
    /// </summary>
    public readonly struct RequestId :
        IEquatable<RequestId>,
        IComparable<RequestId>
    {
        public RequestId(uint value)
        {
            Value = value;
        }

        public uint Value { get; }

        public bool IsValid => Value != 0U;

        public int CompareTo(RequestId other)
        {
            return Value.CompareTo(other.Value);
        }

        public bool Equals(RequestId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object other)
        {
            return other is RequestId requestId && Equals(requestId);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static bool operator ==(RequestId left, RequestId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RequestId left, RequestId right)
        {
            return !left.Equals(right);
        }
    }
}
