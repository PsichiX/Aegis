#!/bin/bash

MSBUILD="MSBuild.exe" # /c/WINDOWS/Microsoft.NET/Framework/v4.0.30319/MSBuild.exe
MODE=-1
MSBUILDMODE="build"
BUILDMSG="Build"

for arg in ${@}; do
	if [ ${MODE} = -1 ]; then
		if [ ${arg} = "-msb" ]; then
			MODE=0
		elif [ ${arg} = "-r" ]; then
			MSBUILDMODE="rebuild"
			BUILDMSG="Rebuild"
		elif [ ${arg} = "-c" ]; then
			MSBUILDMODE="clean"
			BUILDMSG="Clean"
		fi
	elif [ ${MODE} = 0 ]; then
		MODE=-1
		MSBUILD=${arg}
	fi
done

echo "${BUILDMSG}: Debug"
${MSBUILD} ./Server/AegisServer/AegisServer.sln /target:${MSBUILDMODE} /property:Configuration=Debug
${MSBUILD} ./Eye/AegisEye/AegisEye.sln /target:${MSBUILDMODE} /property:Configuration=Debug
${MSBUILD} ./ControllerAPI/DotNetControllerAPI/DotNetControllerAPI.sln /target:${MSBUILDMODE} /property:Configuration=Debug
${MSBUILD} ./Controllers/LocalViewer/LocalViewer.sln /target:${MSBUILDMODE} /property:Configuration=Debug

echo "${BUILDMSG}: Release"
${MSBUILD} ./Server/AegisServer/AegisServer.sln /target:${MSBUILDMODE} /property:Configuration=Release
${MSBUILD} ./Eye/AegisEye/AegisEye.sln /target:${MSBUILDMODE} /property:Configuration=Release
${MSBUILD} ./ControllerAPI/DotNetControllerAPI/DotNetControllerAPI.sln /target:${MSBUILDMODE} /property:Configuration=Release
${MSBUILD} ./Controllers/LocalViewer/LocalViewer.sln /target:${MSBUILDMODE} /property:Configuration=Release

rm -r ./bin/
if [ ${BUILDMSG} = "Build" ] || [ ${BUILDMSG} = "Rebuild" ]; then
	mkdir -p ./bin/
	mkdir -p ./bin/debug/
	mkdir -p ./bin/debug/server/
	mkdir -p ./bin/debug/eye/
	mkdir -p ./bin/debug/controller-api/dotnet/
	mkdir -p ./bin/debug/local-viewer/
	mkdir -p ./bin/release/
	mkdir -p ./bin/release/server/
	mkdir -p ./bin/release/eye/
	mkdir -p ./bin/release/controller-api/dotnet/
	mkdir -p ./bin/release/local-viewer/

	cp ./Server/AegisServer/AegisServer/bin/Debug/*.* ./bin/debug/server/
	cp ./Server/AegisServer/AegisServer/bin/Release/*.* ./bin/release/server/
	cp ./Eye/AegisEye/AegisEye/bin/Debug/*.* ./bin/debug/eye/
	cp ./Eye/AegisEye/AegisEye/bin/Release/*.* ./bin/release/eye/
	cp ./ControllerAPI/DotNetControllerAPI/DotNetControllerAPI/bin/Debug/*.* ./bin/debug/controller-api/dotnet/
	cp ./ControllerAPI/DotNetControllerAPI/DotNetControllerAPI/bin/Release/*.* ./bin/release/controller-api/dotnet/
	cp ./Controllers/LocalViewer/LocalViewer/bin/Debug/*.* ./bin/debug/local-viewer/
	cp ./Controllers/LocalViewer/LocalViewer/bin/Release/*.* ./bin/release/local-viewer/
	
	rm -r ./bin/release/server/*.pdb
	rm -r ./bin/release/eye/*.pdb
	rm -r ./bin/release/controller-api/dotnet/*.pdb
	rm -r ./bin/release/local-viewer/*.pdb
	rm -r ./bin/release/server/*.exe.config
	rm -r ./bin/release/eye/*.exe.config
	rm -r ./bin/release/local-viewer/*.exe.config
	
	cp -r ./Server/AegisServer/AegisServer/working/* ./bin/debug/server/
	cp -r ./Server/AegisServer/AegisServer/working/* ./bin/release/server/
	cp -r ./Eye/AegisEye/AegisEye/working/* ./bin/debug/eye/
	cp -r ./Eye/AegisEye/AegisEye/working/* ./bin/release/eye/
	
	rm ./bin/debug/server/settings.json
	rm ./bin/release/server/settings.json

	cp -r ./templates/*.sh ./bin/debug/
	cp -r ./templates/*.sh ./bin/release/
fi

echo "${BUILDMSG}: DONE!"
