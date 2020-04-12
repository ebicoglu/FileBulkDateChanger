using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileBulkDateChanger
{
    internal class Program
    {
        public static string SearchPattern = "*.dll";
        private const int MinValidYear = 2000;
        private static readonly DateTime NewDate = new DateTime(2000, 1, 1);

        private static void Main(string[] args)
        {
            if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
            {
                ScanAndFix(args[0]);
            }
            else
            {
                foreach (var driveInfo in DriveInfo.GetDrives())
                {
                    var path = driveInfo.RootDirectory.FullName;
                    Console.WriteLine("Scanning " + path);
                    ScanAndFix(path);
                    Console.WriteLine(new string('-', 30) + Environment.NewLine + "Completed.");
                }
            }

            Console.ReadKey();
        }

        private static void ScanAndFix(string path)
        {
            Console.WriteLine("Finding invalid modified date DLLs in " + path);

            var files = FindInvalidModifiedDateDlls(path);

            if (files == null || !files.Any())
            {
                Console.WriteLine("No invalid files found.");
            }
            else
            {
                Console.WriteLine(files.Count + " invalid modified date DLL(s) found.");

                for (var i = 0; i < files.Count(); i++)
                {
                    var file = files[i];

                    Console.WriteLine((i + 1) + " / " + files.Count + " | " + file);

                    SetLastWriteTime(file);
                }

                Console.WriteLine("Completed.");
            }

        }

        private static void SetLastWriteTime(string file)
        {
            try
            {
                File.SetLastWriteTime(file, NewDate);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot set date for " + file + "! Error: " + ex.Message);
            }
        }

        private static bool IsInvalidDate(string file)
        {
            return File.GetLastWriteTime(file).Year < MinValidYear;
        }

        private static List<string> FindInvalidModifiedDateDlls(string path)
        {
            var allDlls = FindAccessableFiles(path, SearchPattern, true);
            var invalidFiles = new List<string>();

            foreach (var dll in allDlls)
            {
                if (IsInvalidDate(dll))
                {
                    invalidFiles.Add(dll);
                }
            }

            return invalidFiles;
        }

        private static IEnumerable<string> FindAccessableFiles(string path, string filePattern, bool recurse)
        {
            if (File.Exists(path))
            {
                yield return path;
                yield break;
            }

            if (!Directory.Exists(path))
            {
                yield break;
            }

            var topDirectory = new DirectoryInfo(path);

            IEnumerator<FileInfo> files;

            try
            {
                files = topDirectory.EnumerateFiles(filePattern).GetEnumerator();
            }
            catch (Exception)
            {
                files = null;
            }

            while (true)
            {
                FileInfo file = null;

                try
                {
                    if (files != null && files.MoveNext())
                    {
                        file = files.Current;
                    }
                    else
                    {
                        break;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }
                catch (PathTooLongException)
                {
                    continue;
                }

                if (file != null)
                {
                    yield return file.FullName;
                }
            }

            if (!recurse)
            {
                yield break;
            }

            IEnumerator<DirectoryInfo> dirs;

            try
            {
                dirs = topDirectory.EnumerateDirectories("*").GetEnumerator();
            }
            catch (Exception)
            {
                dirs = null;
            }

            while (true)
            {
                DirectoryInfo dir = null;

                try
                {
                    if (dirs != null && dirs.MoveNext())
                    {
                        dir = dirs.Current;
                    }
                    else
                    {
                        break;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }
                catch (PathTooLongException)
                {
                    continue;
                }

                if (dir == null)
                {
                    continue;
                }

                foreach (var subpath in FindAccessableFiles(dir.FullName, filePattern, recurse))
                {
                    yield return subpath;
                }
            }
        }
    }
}
