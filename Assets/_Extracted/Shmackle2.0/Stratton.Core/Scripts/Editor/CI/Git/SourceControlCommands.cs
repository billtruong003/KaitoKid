namespace Stratton.CI.Editor
{
    public static class SourceControlCommands
    {
        public static string GetCurrentBranch()
        {
            switch (BuildEditorSettings.Instance.SourceControlType)
            {
                case SourceControlType.Git:
                    return GitCommands.GetCurrentBranch();
                default:
                    return string.Empty;
            }
        }

        public static string GetCurrentRevision()
        {
            switch (BuildEditorSettings.Instance.SourceControlType)
            {
                case SourceControlType.Git:
                    return GitCommands.GetCurrentRevision();
                default:
                    return string.Empty;
            }
        }
    }
}