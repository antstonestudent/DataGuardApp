using System;
using System.IO;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
       // Change string pathways, i tested them on my visual under basic ones but it refused so i've just put in space filler
    {
        string filePath = "C:\\Users\\Example\\Downloads\\testfile.exe";
        string directory = "C:\\Users\\Example\\Documents";

        Console.WriteLine("Checking file integrity...\n");
        Console.WriteLine(GetFileSource(filePath));
        Console.WriteLine(GetFileOrigin(filePath));
        CheckForNewerVersion(filePath, directory);
    }

    static string GetFileSource(string filePath)
    {
        string zonePath = filePath + ":Zone.Identifier";
        if (!File.Exists(filePath)) return "File does not exist.";
        if (File.Exists(zonePath))
        {
            foreach (string line in File.ReadAllLines(zonePath))
            {
                if (line.Contains("ZoneId=3")) return "Untrusted (Internet Download)";
                if (line.Contains("ZoneId=2")) return "Trusted (Local Intranet)";
            }
        }
        return "Local File (Not Downloaded)";
    }

    static string GetFileOrigin(string filePath)
    {
        string zonePath = filePath + ":Zone.Identifier";
        if (!File.Exists(filePath)) return "File does not exist.";
        if (File.Exists(zonePath))
        {
            string content = File.ReadAllText(zonePath);
            Match match = Regex.Match(content, "ReferrerUrl=(.*)");
            if (match.Success) return "File Origin: " + match.Groups[1].Value;
        }
        return "Origin Unknown";
    }

    static void CheckForNewerVersion(string filePath, string directory)
    {
        if (!File.Exists(filePath) || !Directory.Exists(directory))
        {
            Console.WriteLine("Invalid file or directory.");
            return;
        }

        FileInfo originalFile = new FileInfo(filePath);
        DateTime latestModification = originalFile.LastWriteTime;
        string latestFile = filePath;

        foreach (string file in Directory.GetFiles(directory, originalFile.Name, SearchOption.AllDirectories))
        {
            FileInfo fileInfo = new FileInfo(file);
            if (fileInfo.LastWriteTime > latestModification)
            {
                latestModification = fileInfo.LastWriteTime;
                latestFile = file;
            }
        }

        Console.WriteLine(latestFile != filePath ? $"Newer version: {latestFile}" : "No newer version found.");
    }
}
 
