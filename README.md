# FtpEasyTransfer

FtpEasyTransfer is a simple .NET 5 worker service, which can sync files or directories between two FTP servers and/or a local machine.

Usage is straightforward, simply configure the "TransferOptions" section of appsettings.json with as many different types of transfers as you'd like, then run the application and it will automatically determine how to sync based on your provided settings.
