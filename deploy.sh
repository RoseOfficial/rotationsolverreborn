#!/bin/bash
echo "Building solution..."
dotnet build -c Release

echo "Copying RebornRotations.dll to correct location..."
if [ -f "bin/Release/net9.0-windows/RebornRotations.dll" ]; then
    echo "Found DLL in bin/Release/net9.0-windows/"
    cp "bin/Release/net9.0-windows/RebornRotations.dll" "bin/Release/RebornRotations.dll"
    echo "Copy successful!"
else
    echo "ERROR: Could not find RebornRotations.dll in expected location"
fi

echo "Deploy complete!"