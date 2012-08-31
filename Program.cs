using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

			// Fetch API Version?
			if (options.GetApiVersion) {
				var properties = client.GetServiceProperties();
				_log.Info("Service Properties:");
				_log.Info(string.Format("Default Service (API) Version: {0}", properties.DefaultServiceVersion));
				return 0;
			}

			// Set API Version?
			if (!string.IsNullOrEmpty(options.SetApiVersion)) {
				var properties = client.GetServiceProperties();
				var version = options.SetApiVersion == "reset" ? null : options.SetApiVersion;
				_log.Info(string.Format("Updating API Version from {0} to {1}", properties.DefaultServiceVersion, version));
				properties.DefaultServiceVersion = version;
				client.SetServiceProperties(properties);
				_log.Info("Updated Ok");
				return 0;
			}

			// Source is Required
			if (options.Sources == null) {
				Console.WriteLine(options.GetUsage());
				return 0;
			}

			// Resolve Sources
			var files = new List<FileInfo>();
			foreach (var file in options.Sources) {
				if (Path.IsPathRooted(file))
					files.Add(new FileInfo(file));
				else
					files.AddRange(Directory.GetFiles(Environment.CurrentDirectory, file).Select(x => new FileInfo(x)));
			}

			// Perform Upload
			_log.Info(string.Format("Uploading {0} file(s)", files.Count));
			foreach(var fileInfo in files) {
				// Calculate Paths
				var directory = fileInfo.Directory;
				if (directory == null)
					throw new ArgumentException(string.Format("Invalid directory for file: {0}", fileInfo.FullName));

				var uploadPath = Path.Combine(options.Directory, fileInfo.Name).Replace("\\", "/");

				// If our upload Directory has an extension, then we mean to write out this file with that exact path
				if (Path.HasExtension(options.Directory)) {
					if (files.Count > 1)
						throw new ArgumentException("Cannot specify an exact filename to upload to (-d) with multiple files");

					uploadPath = options.Directory;
				}

				var blob = container.GetBlobReference(uploadPath);

				// Blob existance check?
				if (!options.Force) {
					try {
						// Fetch Attributes (and checks to see if Blob exists)
						blob.FetchAttributes();

						// If this succeeded, our Blob already exists
						throw new ArgumentException(string.Format("Blob already exists: {0}", uploadPath));
					} catch (StorageClientException) {
						// Ignored - Blob does not exist
					}
				}

				_log.Info(string.Format("Uploading {0} to {1}", fileInfo, uploadPath));
				blob.UploadFile(fileInfo.FullName);
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
