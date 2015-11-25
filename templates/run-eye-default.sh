#!/bin/bash

TOKEN="test"
EYEPORT="8081"
MODE=-1

for arg in ${@}; do
	if [ ${MODE} = -1 ]; then
		if [ ${arg} = "-t" ] || [ ${arg} = "--token" ]; then
			MODE=0
		elif [ ${arg} = "-ep" ] || [ ${arg} = "--eye-port" ]; then
			MODE=1
		fi
	elif [ ${MODE} = 0 ]; then
		MODE=-1
		TOKEN=${arg}
	elif [ ${MODE} = 1 ]; then
		MODE=-1
		EYEPORT=${arg}
	fi
done

cd ./eye/
./AegisEye.exe -t ${TOKEN} -ra "127.0.0.1" -rp ${EYEPORT} -is leap -i 250
