@echo off
SET MSINAME="WinLLDPService-installer.msi"
echo [x] Applying paths..
if not exist "paths.bat" goto error

call paths.bat

if exist %MSINAME% del %MSINAME%

echo.
echo [x] Generating WIX Object..
candle -ext WiXNetFxExtension -ext WixUtilExtension installer.wxs || goto error

echo.
echo [x] Generating MSI file..
light -ext WixUIExtension -ext WiXNetFxExtension -out %MSINAME% installer.wixobj || goto error

del *.wixobj
del *.wixpdb

if not exist %MSINAME% goto error

echo.
echo All done. Press any key.

pause > nul

exit /b 0

:error
echo.
echo !! WiX Compile failed.
pause > nul
exit /b 1