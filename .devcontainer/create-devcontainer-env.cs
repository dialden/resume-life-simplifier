#!/usr/bin/env -S dotnet run

var directory = Path.Combine(Directory.GetCurrentDirectory(), ".devcontainer");
if (!Directory.Exists(directory))
{
    throw new DirectoryNotFoundException("Run this script from the repository root.");
}

var path = Path.Combine(directory, "devcontainer.env");
using var file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
Console.WriteLine("Devcontainer environment file is ready. Existing contents are preserved.");
