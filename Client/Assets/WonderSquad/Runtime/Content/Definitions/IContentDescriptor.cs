using System.Collections.Generic;

namespace WonderSquad.Content.Definitions
{
    public interface IContentDescriptor
    {
        string StableId { get; }

        int SchemaVersion { get; }

        IReadOnlyList<string> ReferencedIds { get; }
    }
}
