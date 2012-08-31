AzureBlobUtility
================

Simple command line script to upload blobs to Azure Blob Storage.

Example:
=
```
BlobUtility.exe -k AccessKey -a AccountName -c ContainerName -s UploadThisFile.txt -d SomeFolder/SaveAsThis.txt
```

Usage:
=
```
BlobUtility 1.0.0
Copyright © Andrew Armstrong 2012

  -k, --key            Required. Blob storage Access Key.

  -a, --account        Required. Blob storage Account Name.

  -c, --container      Required. Blob storage Container Name.

  -s, --source         Required. Specifies the local files/directories to
                       upload.

  -d, --destination    Specifies the destination filename/directory to upload
                       to.

  -f, --force          Force overwrite of any existing blobs.

  --brief              Show minimal backup progress log information.

  --help               Display this help screen.
```