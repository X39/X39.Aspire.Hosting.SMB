using X39.Aspire.Hosting.SMB;
var builder = DistributedApplication.CreateBuilder(args);

var smbNoFolder = builder.AddSmbShare("smb-no-folder");
var smbWithFolder = builder.AddSmbShare("smb-with-folder") 
    .WithFolder("SharedFolder");

builder.Build()
    .Run();
