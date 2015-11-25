#!/bin/bash

TOKEN="test"
EYEPORT="8081"
CONPORT="8082"
MODE=-1

for arg in ${@}; do
	if [ ${MODE} = -1 ]; then
		if [ ${arg} = "-t" ] || [ ${arg} = "--token" ]; then
			MODE=0
		elif [ ${arg} = "-ep" ] || [ ${arg} = "--eye-port" ]; then
			MODE=1
		elif [ ${arg} = "-cp" ] || [ ${arg} = "--controller-port" ]; then
			MODE=2
		fi
	elif [ ${MODE} = 0 ]; then
		MODE=-1
		TOKEN=${arg}
	elif [ ${MODE} = 1 ]; then
		MODE=-1
		EYEPORT=${arg}
	elif [ ${MODE} = 2 ]; then
		MODE=-1
		CONPORT=${arg}
	fi
done

cd ./server/
./AegisServer.exe -t ${TOKEN} -ep ${EYEPORT} -cp ${CONPORT} -aa yes -s no -tr 20
