using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using XOutput.Logging;
using XOutput.UI.Windows;

namespace XOutput.Tools
{
    /// <summary>
    /// Bundles the diagnostics results, the application log and the settings file
    /// into a timestamped zip that users can attach to bug reports.
    /// </summary>
    public static class DiagnosticsExporter
    {
        private static readonly ILogger logger = LoggerFactory.GetLogger(typeof(DiagnosticsExporter));

        /// <summary>
        /// Writes "XOutput-diagnostics-&lt;timestamp&gt;.zip" into <paramref name="outputDirectory"/>
        /// containing report.txt (version + per-device diagnostics), XOutput.log and
        /// settings.json (both best-effort if present).
        /// </summary>
        /// <param name="viewModel">Diagnostics page view model to read results from</param>
        /// <param name="outputDirectory">Directory the zip is written to</param>
        /// <returns>Full path of the created zip</returns>
        public static string Export(DiagnosticsViewModel viewModel, string outputDirectory)
        {
            if (viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }
            Directory.CreateDirectory(outputDirectory);
            string zipPath = Path.Combine(outputDirectory,
                "XOutput-diagnostics-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".zip");

            StringBuilder report = new StringBuilder();
            report.AppendLine("XOutput diagnostics report");
            report.AppendLine("Version:   " + UpdateChecker.Version.AppVersion);
            report.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            report.AppendLine();
            foreach (var item in viewModel.Model.Diagnostics)
            {
                report.AppendLine("## " + (item.ViewModel.Model.Source ?? "Unknown"));
                foreach (var result in item.ViewModel.Model.Results)
                {
                    report.AppendLine("  - " + result.Type + ": " + result.Value + " [" + result.State + "]");
                }
                report.AppendLine();
            }

            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                AddEntry(zip, "report.txt", report.ToString());
                AddFileIfExists(zip, "XOutput.log");
                AddFileIfExists(zip, "settings.json");
            }
            logger.Info("Diagnostics report written to " + zipPath);
            return zipPath;
        }

        private static void AddEntry(ZipArchive zip, string name, string content)
        {
            ZipArchiveEntry entry = zip.CreateEntry(name);
            using (var writer = new StreamWriter(entry.Open(), Encoding.UTF8))
            {
                writer.Write(content);
            }
        }

        private static void AddFileIfExists(ZipArchive zip, string fileName)
        {
            string fullPath = Path.Combine(Environment.CurrentDirectory, fileName);
            if (File.Exists(fullPath))
            {
                zip.CreateEntryFromFile(fullPath, fileName);
            }
        }
    }
}
