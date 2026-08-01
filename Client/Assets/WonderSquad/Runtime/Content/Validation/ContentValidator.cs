using System;
using System.Collections.Generic;
using WonderSquad.Content.Definitions;

namespace WonderSquad.Content.Validation
{
    public static class ContentValidator
    {
        public static IReadOnlyList<ContentValidationIssue> Validate(
            IEnumerable<IContentDescriptor> descriptors)
        {
            if (descriptors == null)
            {
                throw new ArgumentNullException(nameof(descriptors));
            }

            var issues = new List<ContentValidationIssue>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var validDescriptors = new List<IContentDescriptor>();

            foreach (var descriptor in descriptors)
            {
                if (descriptor == null)
                {
                    issues.Add(new ContentValidationIssue(
                        "CONTENT_NULL",
                        "Content collection contains a null descriptor."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(descriptor.StableId))
                {
                    issues.Add(new ContentValidationIssue(
                        "CONTENT_ID_EMPTY",
                        "Content descriptor has an empty stable ID."));
                    continue;
                }

                if (!IsStableIdFormatValid(descriptor.StableId))
                {
                    issues.Add(new ContentValidationIssue(
                        "CONTENT_ID_FORMAT",
                        $"Stable ID '{descriptor.StableId}' must use lower-case dot-separated tokens."));
                }

                if (!ids.Add(descriptor.StableId))
                {
                    issues.Add(new ContentValidationIssue(
                        "CONTENT_ID_DUPLICATE",
                        $"Stable ID '{descriptor.StableId}' is duplicated."));
                }
                else
                {
                    validDescriptors.Add(descriptor);
                }

                if (descriptor.SchemaVersion <= 0)
                {
                    issues.Add(new ContentValidationIssue(
                        "CONTENT_SCHEMA_INVALID",
                        $"Stable ID '{descriptor.StableId}' has an invalid schema version."));
                }
            }

            foreach (var descriptor in validDescriptors)
            {
                if (descriptor.ReferencedIds == null)
                {
                    continue;
                }

                foreach (var referencedId in descriptor.ReferencedIds)
                {
                    if (string.IsNullOrWhiteSpace(referencedId) ||
                        !ids.Contains(referencedId))
                    {
                        issues.Add(new ContentValidationIssue(
                            "CONTENT_REFERENCE_MISSING",
                            $"Stable ID '{descriptor.StableId}' references missing content " +
                            $"'{referencedId ?? "<null>"}'."));
                    }
                }
            }

            return issues;
        }

        public static bool IsStableIdFormatValid(string stableId)
        {
            if (string.IsNullOrWhiteSpace(stableId) ||
                stableId.StartsWith(".", StringComparison.Ordinal) ||
                stableId.EndsWith(".", StringComparison.Ordinal))
            {
                return false;
            }

            var previousWasDot = false;
            foreach (var character in stableId)
            {
                if (character == '.')
                {
                    if (previousWasDot)
                    {
                        return false;
                    }

                    previousWasDot = true;
                    continue;
                }

                previousWasDot = false;
                if (!(character >= 'a' && character <= 'z') &&
                    !(character >= '0' && character <= '9') &&
                    character != '_')
                {
                    return false;
                }
            }

            return true;
        }
    }
}
