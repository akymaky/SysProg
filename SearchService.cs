namespace _01_34_SysProg;

public class SearchService(string rootPath)
{
    public string? FindFile(string fileName)
    {
        var dirs = new Queue<string>();
        dirs.Enqueue(rootPath);

        while (dirs.Count > 0)
        {
            var currDir =  dirs.Dequeue();

            try
            {
                foreach (var file in Directory.EnumerateFiles(currDir))
                {
                    if (string.Equals(Path.GetFileName(file), fileName, StringComparison.OrdinalIgnoreCase))
                    {
                        return file;
                    }
                }

                foreach (var subDir in Directory.EnumerateDirectories(currDir))
                {
                    dirs.Enqueue(subDir);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Error($"No access to directory: {currDir}\n{ex.Message}");
            }
            catch (DirectoryNotFoundException ex)
            {
                Logger.Error($"Directory not found: {currDir}\n{ex.Message}");
            }
            catch (IOException ex)
            {
                Logger.Error($"IO error in directory: {currDir}\n{ex.Message}");
            }
        }

        return null;
    }
}