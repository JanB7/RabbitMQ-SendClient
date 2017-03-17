CREATE TABLE [dbo].[DataControl]
(
	[Id] INT NOT NULL PRIMARY KEY, 
    [Message] TEXT NOT NULL, 
    [TimeStamp] TIMESTAMP NOT NULL, 
    [DeviceIndex] INT NOT NULL, 
    [Channel] TEXT NOT NULL, 
    [Exchange] TEXT NOT NULL, 
    [ServerAddress] TEXT NOT NULL, 
    [UserName] TEXT NULL
)
