using System.Collections.Generic;
using UnityEngine;
using WonderSquad.Core.Configuration;

namespace WonderSquad.Content.Definitions
{
    public abstract class ContentDefinition : ScriptableObject, IContentDescriptor
    {
        [SerializeField]
        private string stableId = string.Empty;

        [SerializeField]
        private int schemaVersion = ProjectConstants.ContentSchemaVersion;

        [SerializeField]
        private string[] referencedIds = new string[0];

        public string StableId => stableId;

        public int SchemaVersion => schemaVersion;

        public IReadOnlyList<string> ReferencedIds => referencedIds;
    }
}
