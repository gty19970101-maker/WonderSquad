using UnityEngine;

namespace WonderSquad.Bootstrap.Configuration
{
    public enum ProjectEnvironment
    {
        Development = 0,
        Test = 1,
        Release = 2
    }

    [CreateAssetMenu(
        fileName = "ProjectConfiguration",
        menuName = "Wonder Squad/Configuration/Project Configuration")]
    public sealed class ProjectConfiguration : ScriptableObject
    {
        [SerializeField]
        private string configurationId = "project.default";

        [SerializeField]
        private ProjectEnvironment environment = ProjectEnvironment.Development;

        [SerializeField]
        private bool diagnosticsEnabled = true;

        public string ConfigurationId => configurationId;

        public ProjectEnvironment Environment => environment;

        public bool DiagnosticsEnabled => diagnosticsEnabled;
    }
}

