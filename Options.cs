using CommandLine;
using CommandLine.Text;

namespace BlobUtility
{
	public class Options : CommandLineOptionsBase
	{
		[Option("k", "key", HelpText = "Blob storage Access Key.", Required = true)]
		public string Key { get; set; }

		[Option("a", "account", HelpText = "Blob storage Account Name.", Required = true)]
		public string Account { get; set; }

		[Option("c", "container", HelpText = "Blob storage Container Name.", Required = true)]
		public string Container { get; set; }

		[OptionArray("s", "source", HelpText = "Specifies the local files/directories to upload.")]
		public string[] Sources { get; set; }

		[Option("d", "destination", DefaultValue = "", HelpText = "Specifies the destination filename/directory to upload to.")]
		public string Directory { get; set; }

		[Option("f", "force", DefaultValue = false, HelpText = "Force overwrite of any existing blobs.")]
		public bool Force { get; set; }

		[Option(null, "download", DefaultValue = false, HelpText = "Download the specified source file instead of uploading.")]
		public bool Download { get; set; }

		[Option(null, "brief", DefaultValue = false, HelpText = "Show minimal log information.")]
		public bool Brief { get; set; }

		[Option(null, "getDefaultServiceVersion", DefaultValue = false, HelpText = "Display the current default Service (API) Version for the storage service.")]
		public bool GetApiVersion { get; set; }

		[Option(null, "setDefaultServiceVersion", DefaultValue = null, HelpText = "Change the default Service (API) Version for the storage service.")]
		public string SetApiVersion { get; set; }

		[HelpOption]
		public string GetUsage()
		{
			return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
		}
	}
}