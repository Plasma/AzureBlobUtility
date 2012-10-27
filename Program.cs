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

			// Download?
			if (options.Download) {
				var downloads = options.Sources.ToList();
				_log.Info(string.Format("Downloading {0} file(s)", downloads.Count));

				foreach (var download in downloads) {
					var blob = container.GetBlobReference(download);
					blob.FetchAttributes();

					_log.Info(string.Format("Found Blob: {0}", blob.Uri));

					// Does this file exist locally?
					var localFilename = Path.GetFileName(blob.Name);
					if (localFilename == null)
						throw new ArgumentException(string.Format("Could not resolve Blob name: {0}", blob.Uri));

					// Prepend Directory if provided
					var saveDirectory = options.Directory;
					if (!string.IsNullOrEmpty(saveDirectory))
						localFilename = Path.Combine(saveDirectory, localFilename);

					// Ignore existing files if required
					if (!options.Force && File.Exists(localFilename)) {
						_log.Warn(string.Format("Local file {0} already exists; skipping download", localFilename));
						continue;
					}

					// Create directory if required
					if (!string.IsNullOrEmpty(saveDirectory) && !Directory.Exists(saveDirectory))
						Directory.CreateDirectory(saveDirectory);

					// Download Blob
					blob.DownloadToFile(localFilename);
					_log.Info(string.Format("Saved Blob: {0} ({1} bytes)", localFilename, blob.Attributes.Properties.Length));
				}

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
