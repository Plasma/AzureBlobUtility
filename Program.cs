using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using log4net;
using log4net.Filter;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace BlobUtility
{
	class Program
	{
		const string DownloadPolicyName = "readonly-private";
		static readonly ILog Log = LogManager.GetLogger(typeof (Program));

		static int Main(string[] args)
		{
			// Parse Options
			var options = new Options();
			if (!CommandLineParser.Default.ParseArguments(args, options))
				return 1;

			// Configure Logging
			ConfigureLogging(options.Brief);

			// Fetch References
			var credentials = new StorageCredentials(options.Account, options.Key);
			var account = new CloudStorageAccount(credentials, true);
			var client = account.CreateCloudBlobClient();
			var container = client.GetContainerReference(options.Container);

			// Fetch API Version?
			if (options.GetApiVersion) {
				var properties = client.GetServiceProperties();
				Log.Info("Service Properties:");
				Log.Info(string.Format("Default Service (API) Version: {0}", properties.DefaultServiceVersion));
				return 0;
			}

			// Set API Version?
			if (!string.IsNullOrEmpty(options.SetApiVersion)) {
				var properties = client.GetServiceProperties();
				var version = options.SetApiVersion == "reset" ? null : options.SetApiVersion;
				Log.Info(string.Format("Updating API Version from {0} to {1}", properties.DefaultServiceVersion, version));
				properties.DefaultServiceVersion = version;
				client.SetServiceProperties(properties);
				Log.Info("Updated Ok");
				return 0;
			}

			// Source is Required
			if (options.Sources == null) {
				Console.WriteLine(options.GetUsage());
				return 0;
			}

			// Scan for Links?
			if (options.Links) {
				// Assign Container Permission
				Log.Info(string.Format("Applying protected download policy '{0}' on container '{1}'", DownloadPolicyName, container.Name));
				AssignDownloadPolicyContainerPermissions(container);

				var sources = options.Sources.ToList();
				Log.Info(string.Format("Fetching links for {0} file(s)", sources.Count));

				// Header
				foreach (var file in sources) {
					var blob = container.GetBlockBlobReference(file);
					blob.FetchAttributes();

					// Generate Link
					var downloadUrl = GetDownloadUrlFor(blob);

					// Print Result
					Log.Info(string.Empty);
					Log.Info(string.Format("Blob: {0}", blob.Uri));
					Log.Info(string.Format("Url: {0}", downloadUrl));
				}

				return 0;
			}

			// Download?
			if (options.Download) {
				var downloads = options.Sources.ToList();
				Log.Info(string.Format("Downloading {0} file(s)", downloads.Count));

				foreach (var download in downloads) {
					var blob = container.GetBlockBlobReference(download);
					blob.FetchAttributes();

					Log.Info(string.Format("Found Blob: {0}", blob.Uri));

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
						Log.Warn(string.Format("Local file {0} already exists; skipping download", localFilename));
						continue;
					}

					// Create directory if required
					if (!string.IsNullOrEmpty(saveDirectory) && !Directory.Exists(saveDirectory))
						Directory.CreateDirectory(saveDirectory);

					// Download Blob
					blob.DownloadToFile(localFilename, FileMode.OpenOrCreate);
				    Log.Info(string.Format("Saved Blob: {0}", localFilename));
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
			Log.Info(string.Format("Uploading {0} file(s)", files.Count));
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

				var blob = container.GetBlockBlobReference(uploadPath);

				// Blob existance check?
				if (!options.Force) {
					try {
						// Fetch Attributes (and checks to see if Blob exists)
						blob.FetchAttributes();

						// If this succeeded, our Blob already exists
						throw new ArgumentException(string.Format("Blob already exists: {0}", uploadPath));
					} catch (StorageException) {
						// Ignored - Blob does not exist
					}
				}

				Log.Info(string.Format("Uploading {0} to {1}", fileInfo, uploadPath));

			    if (!string.IsNullOrWhiteSpace(options.ContentType))
			    {
			        blob.Properties.ContentType = options.ContentType;
			    }

				blob.UploadFromFile(fileInfo.FullName, FileMode.OpenOrCreate);
			}

			Log.Info("Finished uploading");
			return 0;
		}

		static BlobRequestOptions CreateBlobRequestOptionsWithMaxTimeout()
		{
			// There appears to be a maximum value we can supply for a requested timeout
		    return new BlobRequestOptions {ServerTimeout = TimeSpan.FromHours(6)};
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

		/// <summary>
		/// Return a secret, persistent Download Url for a given Blob
		/// </summary>
		static string GetDownloadUrlFor(ICloudBlob blob)
		{
		    var signature = blob.GetSharedAccessSignature(new SharedAccessBlobPolicy
		    {
		        SharedAccessExpiryTime = new DateTime(2050, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
		    }, DownloadPolicyName);

			return string.Format("{0}{1}", blob.Uri.ToString().Replace(" ", "%20"), signature);
		}

		/// <summary>
		/// Assign permissions for a download to occur for our readonly permission policy
		/// </summary>
		static void AssignDownloadPolicyContainerPermissions(CloudBlobContainer container)
		{
			// Secure storage. We will provide read tokens for access to blob data on a per-blob basis.
			var containerPermissions = new BlobContainerPermissions();
			containerPermissions.PublicAccess = BlobContainerPublicAccessType.Off;
		    containerPermissions.SharedAccessPolicies.Add(DownloadPolicyName, new SharedAccessBlobPolicy
		    {
		        Permissions = SharedAccessBlobPermissions.Read
		    });

			container.SetPermissions(containerPermissions);
		}
	}
}
