using CommandLine;
using CommandLine.Text;

namespace BlobUtility
{
	public class Options : CommandLineOptionsBase
	{
		[Option("k", "key", HelpText = "Blob storage Access Key", Required = true)]
		public string Key { get; set; }

		[Option("a", "account", HelpText = "Blob storage Account Name", Required = true)]
		public string Account { get; set; }

		[Option("c", "container", HelpText = "Blob storage Container Name", Required = true)]
		public string Container { get; set; }

		[Option("d", "directory", HelpText = "Destination directory to upload to")]
		public string Directory { get; set; }

		[OptionArray("f", "files", HelpText = "Specifies the local filenames to upload", Required = true)]
		public string[] Files { get; set; }

		[Option(null, "brief", DefaultValue = false, HelpText = "Show minimal backup progress log information.")]
		public bool Brief { get; set; }

		[HelpOption]
		public string GetUsage()
		{
			return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
		}
	}
}