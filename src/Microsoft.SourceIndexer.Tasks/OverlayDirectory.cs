using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.SourceIndexer.Tasks
{
    /// <summary>
    /// Overlays an extracted directory at <see cref="InputDirectory"/> onto a cloned git repo at <see cref="OutputDirectory"/>
    /// </summary>
    public class OverlayDirectory : Task
    {
        [Required]
        public string InputDirectory { get; set; }

        [Required]
        public string OutputDirectory { get; set; }

        [Output]
        public string[] SolutionFiles { get; set; }

        public override bool Execute()
        {
            try
            {
                ExecuteCore();
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex, true);
            }
            return !Log.HasLoggedErrors;
        }

        private void ExecuteCore()
        {
            int fileCount = 0;
            int skippedFileCount = 0;
            var solutions = new List<string>();
            InputDirectory = Path.GetFullPath(InputDirectory).TrimEnd('/', '\\');
            foreach(var file in Directory.EnumerateFiles(InputDirectory, "*.*", SearchOption.AllDirectories))
            {
                if (file.EndsWith("wpftmp.csproj", StringComparison.OrdinalIgnoreCase))
                {
                    skippedFileCount++;
                    continue;
                }

                fileCount++;
                var relativePath = file.Substring(InputDirectory.Length).TrimStart('/', '\\');
                string destFileName = Path.Combine(OutputDirectory, relativePath);
                if (File.Exists(destFileName))
                {
                    File.Delete(destFileName);
                }

                if (file.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
                {
                    solutions.Add(destFileName);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destFileName));
                File.Move(file, destFileName);
            }

            SolutionFiles = solutions.ToArray();

            Log.LogMessage($"{solutions.Count} solution files. Moved {fileCount} files. Skipped {skippedFileCount} files.");
        }
    }
}
