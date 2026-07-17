using System;
using System.Diagnostics;
using Stratton.Core;

namespace Stratton.CI.Editor
{
    public static class GitCommands
    {
        public static string GetCurrentBranch()
        {
            try
            {
                Process p = new Process();
                p.StartInfo.FileName = "git";
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.Arguments = "rev-parse --abbrev-ref HEAD";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.Start();
                return p.StandardOutput.ReadToEnd().Trim();
            }
            catch (Exception e)
            {
                Log.Error(BaseLogChannel.Core, $"An exception occured while invoking a Git command: {e.Message}");
                return string.Empty;
            }
        }

        public static string GetCurrentRevision()
        {
            try
            {
                Process p = new Process();
                p.StartInfo.FileName = "git";
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.Arguments = "rev-parse HEAD";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.Start();
                return p.StandardOutput.ReadToEnd().Trim().Substring(0, 10);
            }
            catch (Exception e)
            {
                Log.Error(BaseLogChannel.Core, $"An exception occured while invoking a Git command: {e.Message}");
                return string.Empty;
            }
        }
    }
}