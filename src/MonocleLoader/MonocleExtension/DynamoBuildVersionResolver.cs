using System;
using System.Collections.Generic;

namespace MonocleExtension
{
    internal static class DynamoBuildVersionResolver
    {
        internal static IEnumerable<string> GetCandidates(Version dynamoVersion)
        {
            if (dynamoVersion == null) throw new ArgumentNullException(nameof(dynamoVersion));

            for (var minor = dynamoVersion.Minor; minor >= 0; minor--)
            {
                yield return $"{dynamoVersion.Major}.{minor}";
            }
        }
    }
}
