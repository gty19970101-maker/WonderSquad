using NUnit.Framework;
using WonderSquad.Editor;

namespace WonderSquad.Tests.EditMode
{
    public sealed class P0ProjectValidatorTests
    {
        [Test]
        public void ProjectSkeleton_HasRequiredScenesAndInputConfiguration()
        {
            var errors = P0ProjectValidator.CollectValidationErrors();

            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }
    }
}
