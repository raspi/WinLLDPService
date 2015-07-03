@echo off
echo [x] Applying paths..
call paths.bat

echo.
echo [x] Generating WIX Object..
candle -ext WiXNetFxExtension -ext WixUtilExtension installer.wxs || goto error

echo.
echo [x] Generating MSI file..
light -ext WixUIExtension -ext WiXNetFxExtension -out WinLLDPService-installer.msi installer.wixobj || goto error

del *.wixobj
del *.wixpdb

echo.
echo All done. Press any key.

pause > nul

exit /b 0

:error
echo.
echo !! WiX Compile failed.
pause > nul
exit /b 1