using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using log4net;
using log4net.Filter;

namespace BlobUtility
{
	class Program
	{
		static readonly ILog _log = LogManager.GetLogger(typeof (Program));

		static int Main(string[] args)
		{
			// Parse Options
			var options = new Options();
			if (!CommandLineParser.Default.ParseArguments(args, options))
				return 1;

			// Configure Logging
			ConfigureLogging(options.Brief);

			// Fetch References
			var credentials = new StorageCredentialsAccountAndKey(options.Account, options.Key);
			var account = new CloudStorageAccount(credentials, true);
			var client = account.CreateCloudBlobClient();
			var container = client.GetContainerReference(options.Container);

			// Resolve Files
			var files = new List<string>();
			var workingDirectoryUri = new Uri(string.Format("{0}/", Environment.CurrentDirectory), UriKind.Absolute);
			foreach(var file in options.Files)
				files.AddRange(Directory.GetFiles(Environment.CurrentDirectory, file));

			// Perform Upload
			_log.Info(string.Format("Uploading {0} file(s)", files.Count));
			foreach(var file in files) {
				var fileUri = new Uri(file, UriKind.Absolute);
				var relativePath = workingDirectoryUri.MakeRelativeUri(fileUri);
				var uploadPath = Path.Combine(options.Directory ?? string.Empty, relativePath.ToString()).Replace("\\", "/");
				var blob = container.GetBlobReference(uploadPath);

				_log.Info(string.Format("Uploading {0} to {1}", relativePath, uploadPath));
				blob.UploadFile(file);
			}

			_log.Info("Finished uploading");
			return 0;
		}

		static void ConfigureLogging(bool verbose)
		{
			var consoleAppender = new log4net.Appender.ConsoleAppender();
			consoleAppender.Layout = new log4net.Layout.PatternLayout("[%date{yyyy-MM-dd HH:mm:ss}] %-5p %c{1} - %m%n");

			if (!verbose) {
				var filter = new LoggerMatchFilter();
				filter.AcceptOnMatch = true;
				filter.LoggerToMatch = typeof(Program).ToString();
				consoleAppender.AddFilter(filter);

				consoleAppender.AddFilter(new DenyAllFilter());
			}

			log4net.Config.BasicConfigurator.Configure(consoleAppender);
		}
	}
}
