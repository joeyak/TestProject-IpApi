dotnet build .\IpWorker -c Release
"geoip", "rdap", "reversedns", "ping" | ForEach-Object {
    Start-Process -FilePath ".\IpWorker\IpWorker\bin\Release\netcoreapp3.1\IpWorker.exe" -Args "--Service=$_" -WorkingDirectory ".\IpWorker\IpWorker\bin\Release\netcoreapp3.1\"
}
