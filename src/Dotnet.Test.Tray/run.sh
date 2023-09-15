#!/bin/bash

deviceId=$(ios-deploy -c | grep -oE 'Found ([0-9A-Za-z\-]+)' | sed 's/Found //g' | tr -d \'\r\n\')

echo "Sending to device $deviceId"

dotnet build -t:Run -f net7.0-ios -c:Release -p:_DeviceName=$deviceId

