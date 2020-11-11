dotnet build .\IpWorker -c Release
if ($LASTEXITCODE -ne 0) {
    exit
}
"geoip", "rdap", "reversedns", "ping", "ipapi", "weather" | ForEach-Object {
    Start-Process -FilePath ".\IpWorker\IpWorker\bin\Release\netcoreapp3.1\IpWorker.exe" -Args "--Service=$_" -WorkingDirectory ".\IpWorker\IpWorker\bin\Release\netcoreapp3.1\"
}
