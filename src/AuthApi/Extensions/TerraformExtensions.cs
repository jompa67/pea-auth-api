using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AuthApi.Extensions;

public static class TerraformExtensions
{
    /// <summary>
    /// Releases a Terraform state lock using the provided lock ID.
    /// </summary>
    /// <param name="lockId">The ID of the lock to release</param>
    /// <param name="logger">Optional logger for recording the process</param>
    /// <returns>True if the lock was successfully released, false otherwise</returns>
    public static bool ReleaseStateLock(string lockId, ILogger? logger = null)
    {
        if (string.IsNullOrEmpty(lockId))
        {
            logger?.LogWarning("Cannot release Terraform state lock: Lock ID is null or empty");
            return false;
        }

        try
        {
            logger?.LogInformation("Attempting to release Terraform state lock with ID: {LockId}", lockId);

            var processInfo = new ProcessStartInfo
            {
                FileName = "terraform",
                Arguments = $"force-unlock -force {lockId}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                logger?.LogError("Failed to start Terraform process for releasing lock");
                return false;
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                logger?.LogInformation("Successfully released Terraform state lock: {Output}", output.Trim());
                return true;
            }
            else
            {
                logger?.LogError("Failed to release Terraform state lock. Error: {Error}", error.Trim());
                return false;
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Exception occurred while trying to release Terraform state lock");
            return false;
        }
    }

    /// <summary>
    /// Runs a Terraform command with lock disabled
    /// </summary>
    /// <param name="command">The Terraform command to run (e.g., "apply", "plan")</param>
    /// <param name="additionalArgs">Additional arguments for the command</param>
    /// <param name="logger">Optional logger for recording the process</param>
    /// <returns>The exit code of the Terraform process</returns>
    public static async Task<int> RunWithoutLockAsync(string command, string additionalArgs = "", ILogger? logger = null)
    {
        try
        {
            logger?.LogInformation("Running Terraform {Command} with lock disabled", command);

            var args = $"{command} -lock=false {additionalArgs}".Trim();
            var processInfo = new ProcessStartInfo
            {
                FileName = "terraform",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                logger?.LogError("Failed to start Terraform process");
                return -1;
            }

            // Create tasks to read the output and error streams asynchronously
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            // Wait for the process to complete
            await process.WaitForExitAsync();

            // Get the output and error content
            var output = await outputTask;
            var error = await errorTask;

            if (!string.IsNullOrEmpty(output))
            {
                logger?.LogInformation("Terraform output: {Output}", output);
            }

            if (!string.IsNullOrEmpty(error))
            {
                logger?.LogWarning("Terraform errors: {Error}", error);
            }

            logger?.LogInformation("Terraform process completed with exit code: {ExitCode}", process.ExitCode);
            return process.ExitCode;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Exception occurred while running Terraform command");
            return -1;
        }
    }

    /// <summary>
    /// Extracts the lock ID from a Terraform error message
    /// </summary>
    /// <param name="errorMessage">The error message containing the lock ID</param>
    /// <returns>The extracted lock ID or null if not found</returns>
    public static string ExtractLockId(string errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
            return null;

        // The lock ID in Terraform error messages typically follows the format:
        // "Lock Info: ID: [lock-id-here]"
        const string idPrefix = "Lock Info: ID: ";
        var idIndex = errorMessage.IndexOf(idPrefix);
        if (idIndex < 0)
            return null;

        idIndex += idPrefix.Length;
        var endIndex = errorMessage.IndexOf('\n', idIndex);
        if (endIndex < 0)
            endIndex = errorMessage.Length;

        return errorMessage.Substring(idIndex, endIndex - idIndex).Trim();
    }
}
