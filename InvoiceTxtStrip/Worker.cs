using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InvoiceTxtStrip
{
    public class Worker : BackgroundService
    {
        private readonly string watchPath;
        private readonly string destinationPath;
        private readonly ILogger<Worker> _logger;
        public Worker(ILogger<Worker> logger)
        {
            watchPath = @"\\Replenphonedc2\HDrive\OfficeShare\SalesBoard\Invoicing\TextFiles";
            destinationPath = @"\\Replenphonedc2\HDrive\OfficeShare\SalesBoard\Invoicing\RawData";
            _logger = logger;
        }
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Watch(watchPath);
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private void Watch(string path)
        {
            //initialize
            FileSystemWatcher watcher = new FileSystemWatcher
            {

                //assign paramater path
                Path = path,

                //don't watch subdirectories
                IncludeSubdirectories = false
            };

            //file created event
            watcher.Created += FileSystemWatcher_Created;

            //filters
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size | NotifyFilters.Attributes;

            //only look for csv
            watcher.Filter = "*.txt";

            // Begin watching.
            watcher.EnableRaisingEvents = true;

        }

        private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            Thread.Sleep(3000);
            while (!IsFileLocked(e.FullPath))
            {
                ReadWriteStream(e.FullPath, e.Name);
                break;
            }
        }

        private bool IsFileLocked(string filePath)
        {
            try
            {
                using FileStream originalFileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                originalFileStream.Close();
            }
            catch (Exception)
            {
                return true;
            }
            return false;
        }

        private void ReadWriteStream(string path, string fileName)
        {
            using FileStream originalFileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using FileStream destinationFileStream = new FileStream(destinationPath + @"\" + fileName, FileMode.Create, FileAccess.Write);
            using StreamReader streamReader = new StreamReader(originalFileStream);
            using StreamWriter streamWriter = new StreamWriter(destinationFileStream);

            try
            {
                string currentLine = streamReader.ReadLine();
                //skip the first seven lines
                for (int i = 0; i < 7; i++)
                {
                    currentLine = streamReader.ReadLine();
                }
                //begin writing to \RawData
                while (currentLine != null)
                {

                    streamWriter.WriteLine(currentLine);
                    currentLine = streamReader.ReadLine();

                }
                streamReader.Close();
                streamWriter.Close();
                File.Delete(path);

            }
            catch (Exception e)
            {
                _logger.LogError($"failed to process {fileName} {Environment.NewLine} error {e}");
            }
            finally
            {
                destinationFileStream.Close();
                originalFileStream.Close();

            }
        }
    }
}
