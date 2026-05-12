namespace _01_34_SysProg;

public class SearchService(string rootPath)
{
    private readonly string _rootPath = rootPath;

    public string? FindFile(string fileName)
    {
        var dirs = new Queue<string>();
        dirs.Enqueue(_rootPath);

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
                Console.WriteLine($"No access to directory: {currDir}\n{ex.Message}");
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine($"Directory not found: {currDir}\n{ex.Message}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"IO error in directory: {currDir}\n{ex.Message}");
            }
        }

        return null;
    }
}