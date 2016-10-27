using CommandLine;
using CommandLine.Text;

namespace BlobUtility
{
	public class Options
	{
		[Option('k', "key", HelpText = "Blob storage Access Key.", Required = true)]
		public string Key { get; set; }

		[Option('a', "account", HelpText = "Blob storage Account Name.", Required = true)]
		public string Account { get; set; }

		[Option('c', "container", HelpText = "Blob storage Container Name.", Required = true)]
		public string Container { get; set; }

		[OptionArray('s', "source", HelpText = "Specifies the local files/directories to upload.")]
		public string[] Sources { get; set; }

		[Option('d', "destination", DefaultValue = "", HelpText = "Specifies the destination filename/directory to upload to.")]
		public string Directory { get; set; }

		[Option('f', "force", DefaultValue = false, HelpText = "Force overwrite of any existing blobs.")]
		public bool Force { get; set; }

		[Option("download", DefaultValue = false, HelpText = "Download the specified source file instead of uploading.")]
		public bool Download { get; set; }

		[Option('l', "links", DefaultValue = false, HelpText = "Generate Download Links for matching files instead of uploading.")]
		public bool Links { get; set; }

		[Option("brief", DefaultValue = false, HelpText = "Show minimal log information.")]
		public bool Brief { get; set; }

		[Option("getDefaultServiceVersion", DefaultValue = false, HelpText = "Display the current default Service (API) Version for the storage service.")]
		public bool GetApiVersion { get; set; }

		[Option("setDefaultServiceVersion", DefaultValue = null, HelpText = "Change the default Service (API) Version for the storage service.")]
		public string SetApiVersion { get; set; }
        
		[Option('t', "contenttype", DefaultValue = null, HelpText = "Specifies the file Content-Type")]
		public string ContentType { get; set; }

		[Option("contentencoding", DefaultValue = null, HelpText = "Specifies the file Content-Encoding")]
		public string ContentEncoding { get; set; }

		[Option("contentlanguage", DefaultValue = null, HelpText = "Specifies the file Content-Language")]
		public string ContentLanguage { get; set; }

		[Option("cachecontrol", DefaultValue = null, HelpText = "Specifies the file Cache-Control")]
		public string CacheControl { get; set; }

        [Option('e', "env", HelpText = "Specify Cloud Environment. Default is Global Azure. Set to \"China Azure\" for Mooncake", Required = false)]
        public string Environment { get; set; }

        [HelpOption]
		public string GetUsage()
		{
			return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
		}
	}
}