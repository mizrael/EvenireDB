namespace EvenireDB.Utils;

internal static class FileUtils
{
    public static async ValueTask<bool> TryDeleteDirectoryAsync(
        string path,
        int maxAttempts = 3,
        double delayInSeconds = 0.5,
        CancellationToken cancellationToken = default)
    {
        int currAttempt = 0;
        var delay = TimeSpan.FromSeconds(delayInSeconds);
        while (currAttempt++ < maxAttempts)
        {
            if (!Directory.Exists(path))
                return true;

            try
            {
                Directory.Delete(path, true);
            }
            catch
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        return !Directory.Exists(path);
    }

    public static async ValueTask<bool> TryDeleteFileAsync(
        string path,
        int maxAttempts = 3,
        double delayInSeconds = 0.5,
        CancellationToken cancellationToken = default)
    {
        int currAttempt = 0;
        var delay = TimeSpan.FromSeconds(delayInSeconds);
        while (currAttempt++ < maxAttempts)
        {
            if (!File.Exists(path))
                return true;

            try
            {
                File.Delete(path);
            }
            catch
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        return !File.Exists(path);
    }
}