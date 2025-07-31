@echo off
echo Building solution...
dotnet build -c Release

echo Checking build output location...
if exist "bin\Release\net9.0-windows\RebornRotations.dll" (
    echo Found DLL in bin\Release\net9.0-windows\
    echo Copying to bin\Release\...
    if not exist "bin\Release" mkdir "bin\Release"
    copy "bin\Release\net9.0-windows\RebornRotations.dll" "bin\Release\RebornRotations.dll"
) else if exist "C:\bin\Release\net9.0-windows\RebornRotations.dll" (
    echo Found DLL in C:\bin\Release\net9.0-windows\
    echo Copying to bin\Release\...
    if not exist "bin\Release" mkdir "bin\Release"
    copy "C:\bin\Release\net9.0-windows\RebornRotations.dll" "bin\Release\RebornRotations.dll"
) else (
    echo ERROR: Could not find RebornRotations.dll in expected locations
)

echo Deploy complete!
pause