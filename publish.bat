@echo off
echo 正在發布 MCP Database Server...
dotnet publish -c Release -r win-x64 --self-contained false
echo.
echo 發布完成！
echo 可執行檔案位置: bin\Release\net8.0\win-x64\publish\DbMcpServer.exe
echo.
pause
