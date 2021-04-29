# FtpEasyTransfer

FtpEasyTransfer is a simple .NET 5 worker service, which can sync files or directories between two FTP servers and/or a local machine.

Usage is straightforward, simply configure the "TransferOptions" section of appsettings.json with as many different types of transfers as you'd like, then run the application and it will automatically determine how to sync based on your provided settings.

## Example appsettings.json

```json
{
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft": "Warning",
            "Microsoft.Hosting.Lifetime": "Information"
        }
    },
    "Serilog": {
        "Using": [ "Serilog.Sinks.File" ],
        "MinimumLevel": "Warning",
        "WriteTo:File": {
            "Name": "File",
            "Args": {
                "path": "C:/poller/logs/log-.txt",
                "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                "rollingInterval": "Day"
            }
        }
    },
    "PollFrequency": 900000,
    "TransferOptions": [
        {
            "LocalPath": "C:/FtpEasyTransfer",
            "ChangeExtensions": [
                {
                    "Source": "jpeg",
                    "Target": "jpg"
                },
                {
                    "Source": "xls",
                    "Target": "xlsx"
                }
            ],
            "Source": {
                "Server": "your.ftp.server",
                "Port": 0,
                "User": "username",
                "Password": "password",
                "RemotePath": "/download/",
                "FileTypesToDownload": [ "mkv", "avi" ],
                "OverwriteExisting": false,
                "DeleteOnceTransferred": true
            },
            "Destination": {
                "Server": "another.ftp.server",
                "Port": 0,
                "User": "username",
                "Password": "password",
                "RemotePath": "/your/remote/path/",
                "OverwriteExisting": true,
                "DeleteOnceTransferred": false
            }
        }
    ]
}
```
### Serilog - Optional
The Serilog section can be configured as per [Serilog.Settings.Configuration](https://github.com/serilog/serilog-settings-configuration)

### Poll Frequency - Mandatory
The time in milliseconds between the end of the last task and the start of the next

### Transfer Options - Mandatory - array
Transfer Options contains an array of settings options - the service will work through these in order from top to bottom.

Example:
```json
"TransferOptions": [
        {
            "LocalPath": "C:/FtpEasyTransfer",
            "ChangeExtensions": [
                {
                    "Source": "jpeg",
                    "Target": "jpg"
                },
                {
                    "Source": "xls",
                    "Target": "xlsx"
                }
            ],
            "Source": {
                "Server": "your.ftp.server",
                "Port": 0,
                "User": "username",
                "Password": "password",
                "RemotePath": "/download/",
                "FileTypesToDownload": [ "mkv", "avi" ],
                "OverwriteExisting": false,
                "DeleteOnceTransferred": true
            },
            "Destination": {
                "Server": "another.ftp.server",
                "Port": 0,
                "User": "username",
                "Password": "password",
                "RemotePath": "/your/remote/path/",
                "OverwriteExisting": true,
                "DeleteOnceTransferred": false
            }
        }
    ]
```

#### Local Path - Mandatory - string (file or directory path)
A local path must be specified for each transfer, regardless of the type of transfer being performed - even when syncing one FTP server to another, the directory must be specified as a temporary location whilst transferring files across. Use only `/` path separators, even in Windows paths.

Examples:
```json
"LocalPath": "C:/FtpEasyTransfer"
```
```json
"LocalPath": "C:/FtpEasyTransfer/file.txt"
```

#### ChangeExtensions - Optional - array
Contains a list of ChangeExtension objects - each object must contain a source file extension and target file extension. Extensions are not case sensitive, do not include the ```.``` or any wildcard symbols.

Examples:
```json
"ChangeExtensions": [
    {
        "Source": "txt",
        "Target": "text"
    }
]
```
```json
"ChangeExtensions": [
    {
        "Source": "jpg",
        "Target": "jpeg"
    },
    {
        "Source": "mpeg",
        "Target": "mpg"
    }
]
```

#### Source/Destination - Mandatory (At least one) - object
Each TransferOptions object can contain either one or both of Source/Destination. These objects are configured identically.

##### Server - Mandatory - string
IP/host address of FTP server
```json
"Server": "ftp.someserver.com"
```

##### Port - Optional - int
Port of FTP server. Defaults to 21 if not defined.
```json
"Port": 21
```

##### User - Mandatory - string
Username for FTP server. Use "anonymous" if no user
```json
"User": "username"
```

##### Password - Mandatory - string
Password for FTP server. Use "" if anonymous
```json
"Password": "password123"
```

##### RemotePath - Mandatory - string
Remote path to either file or directory to be downloaded/uploaded. If directory, use trailing ```/```
```json
"RemotePath": "/server/file.txt"
```
```json
"RemotePath": "/server/directory_to_sync/"
```

##### OverwriteExisting - Optional - bool
If ```true```, will overwrite existing files when downloading/uploading. If in Source, will overwrite local files with files from server. If in Destination, will overwrite files on server with local files.

If ```false```, Source will ignore any local files matching files on server. Destination will ignore any remote files matching locally.

Default: ```false```
```json
"OverwriteExisting": true
```

##### DeleteOnceTransferred - Optional - bool
If ```true```, will delete any files from ```Source```/```Desintation``` if they have been transferred successfully. Only applies to files actually transferred, not any pre-existing files.

Default: ```false```
```json
"DeleteOnceTransferred": true
```

##### FolderSyncMode - Optional - enum
Can be set to either ```"Update"``` or ```"Mirror"```

```Mirror```: Dangerous - Uploads/downloads all the missing files to update the server/local filesystem. Deletes the extra files to ensure that the target is an exact mirror of the source/destination.

```Update```: Safer method - Uploads/downloads all the missing files to update the server/local filesystem.

Default: ```"Update"```
```json
"FolderSyncMode": "Mirror"
```
