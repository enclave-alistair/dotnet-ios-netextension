#!/bin/bash

deviceId=$(ios-deploy -c | grep -oE 'Found ([0-9A-Za-z\-]+)' | sed 's/Found //g' | tr -d \'\r\n\')

echo "Sending to device $deviceId"

ios-deploy --id $deviceId --bundle bin/Release/net7.0-ios/ios-arm64/Dotnet.Test.Tray.app