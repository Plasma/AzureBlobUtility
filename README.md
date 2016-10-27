AzureBlobUtility
================

Simple command line script to upload, download and manage blobs in Windows Azure Blob Storage.

Example:
=
```
BlobUtility.exe -k AccessKey -a AccountName -c ContainerName -s UploadThisFile.txt -d SomeFolder/SaveAsThis.txt -t text/plain
```

You may also change the Default Service (API) Version using this utility:
```
BlobUtility.exe -k AccessKey -a AccountName -c ContainerName --setDefaultServiceVersion 2012-02-12
```

You may want to leverage this tool for other national cloud, for example China Azure. Please specify "--env" to "China Azure" or "Mooncake"
```
BlobUtility.exe -k AccessKey -a AccountName -c ContainerName -e "China Azure" --setDefaultServiceVersion 2012-02-12
```

See http://msdn.microsoft.com/en-us/library/windowsazure/dd894041 for a list of API Versions.

Usage:
=
```
BlobUtility 1.0.0
Copyright © Andrew Armstrong 2012

  -k, --key                     Required. Blob storage Access Key.

  -a, --account                 Required. Blob storage Account Name.

  -c, --container               Required. Blob storage Container Name.

  -s, --source                  Specifies the local files/directories to
                                upload.

  -d, --destination             Specifies the destination filename/directory to
                                upload to.

  -f, --force                   Force overwrite of any existing blobs.

  -t, --contenttype             Specifies the file content type.

  --contentencoding             Specifies the file content encoding.

  --contentlanguage             Specifies the file content language.

  --cachecontrol                Specifies the file cache control.

  --brief                       Show minimal backup progress log information.

  --getDefaultServiceVersion    Display the current default Service (API)
                                Version for the storage service.

  --setDefaultServiceVersion    Change the default Service (API) Version for
                                the storage service.

  --help                        Display this help screen.
```
