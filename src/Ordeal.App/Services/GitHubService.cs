using System;
using System.Diagnostics;

namespace Ordeal.App.Services;

public static class GitHubService
{
    public static bool IsCliAvailable()
    {
        try
        {
            var startInfo =
                CreateCliProcessStartInfo();

            startInfo.ArgumentList.Add(
                "--version");

            using var process =
                Process.Start(startInfo);

            if (process == null)
                return false;

            process.StandardOutput.ReadToEnd();
            process.StandardError.ReadToEnd();

            process.WaitForExit();

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsConnected()
    {
        try
        {
            if (!IsCliAvailable())
                return false;

            var startInfo =
                CreateCliProcessStartInfo();

            startInfo.ArgumentList.Add("auth");
            startInfo.ArgumentList.Add("status");

            using var process =
                Process.Start(startInfo);

            if (process == null)
                return false;

            process.StandardOutput.ReadToEnd();
            process.StandardError.ReadToEnd();

            process.WaitForExit();

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public static bool RepositoryExists(string repositoryName)
    {
        if (string.IsNullOrWhiteSpace(repositoryName))
            return false;

        if (!IsConnected())
            return false;

        var startInfo = CreateCliProcessStartInfo();

        startInfo.ArgumentList.Add("repo");
        startInfo.ArgumentList.Add("view");
        startInfo.ArgumentList.Add(repositoryName);
        startInfo.ArgumentList.Add("--json");
        startInfo.ArgumentList.Add("name");

        try
        {
            using var process = Process.Start(startInfo);

            if (process == null)
                return false;

            var output = process.StandardOutput.ReadToEnd();
            process.StandardError.ReadToEnd();

            process.WaitForExit();

            return process.ExitCode == 0 &&
                   !string.IsNullOrWhiteSpace(output);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"Could not check GitHub repository: {ex.Message}");

            return false;
        }
    }

    public static string? GetUsername()
    {
        try
        {
            if (!IsConnected())
                return null;

            var startInfo =
                CreateCliProcessStartInfo();

            startInfo.ArgumentList.Add("api");
            startInfo.ArgumentList.Add("user");
            startInfo.ArgumentList.Add("--jq");
            startInfo.ArgumentList.Add(".login");

            using var process =
                Process.Start(startInfo);

            if (process == null)
                return null;

            var output =
                process.StandardOutput
                    .ReadToEnd()
                    .Trim();

            process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
                return null;

            return string.IsNullOrWhiteSpace(output)
                ? null
                : output;
        }
        catch
        {
            return null;
        }
    }

    public static void StartLogin(
        Action? onFinished = null)
    {
        var startInfo =
            new ProcessStartInfo
            {
                FileName =
                    "x-terminal-emulator",

                UseShellExecute =
                    false
            };

        startInfo.ArgumentList.Add("-e");
        startInfo.ArgumentList.Add("gh");
        startInfo.ArgumentList.Add("auth");
        startInfo.ArgumentList.Add("login");

        try
        {
            var process =
                Process.Start(startInfo);

            if (process == null)
                return;

            if (onFinished == null)
                return;

            process.EnableRaisingEvents = true;

            process.Exited += (_, _) =>
            {
                onFinished();
                process.Dispose();
            };
        }
        catch
        {
            // Login could not be started.
        }
    }

    private static ProcessStartInfo
        CreateCliProcessStartInfo()
    {
        return new ProcessStartInfo
        {
            FileName =
                "gh",

            UseShellExecute =
                false,

            RedirectStandardOutput =
                true,

            RedirectStandardError =
                true,

            CreateNoWindow =
                true
        };
    }
}
