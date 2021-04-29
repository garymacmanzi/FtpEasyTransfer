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
                "DeleteOnceDownloaded": true
            },
            "Destination": {
                "Server": "another.ftp.server",
                "Port": 0,
                "User": "username",
                "Password": "password",
                "RemotePath": "/your/remote/path/",
                "OverwriteExisting": true,
                "DeleteOnceUploaded": false
            }
        }
    ]
}
```
### Serilog - Optional
The Serilog section can be configured as per [Serilog.Settings.Configuration](https://github.com/serilog/serilog-settings-configuration)

### Poll Frequency - Mandatory
The time in milliseconds between the end of the last task and the start of the next

### Transfer Options - Mandatory
Transfer Options contains an array of settings options - the service will work through these in order from top to bottom.

#### Local Path - Mandatory - string (file or directory path)
A local path must be specified for each transfer, regardless of the type of transfer being performed - even when syncing one FTP server to another, the directory must be specified as a temporary location whilst transferring files across. Use only `/` path separators, even in Windows paths.

Example:
```json
"LocalPath": "C:/FtpEasyTransfer"
```
or
```json
"LocalPath": "C:/FtpEasyTransfer/file.txt"
```

#### ChangeExtensions - Optional - array
Contains a list of ChangeExtension objects - each object must contain a source file extension and target file extension. Extensions are not case sensitive, do not include the ```.``` or any wildcard symbols.
