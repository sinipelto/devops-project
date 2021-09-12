#!/bin/bash

[ -z $1 ] && echo "USAGE: $0 d(ocker) | g(ateway) | h(ttpserv) | b(oth)" && exit 1

rm -rf /app
mkdir -p /app

cp -rT /vagrant /app

cd /app

if [[ $1 == "d" ]]; then

	docker-compose down

	docker stop $(docker ps -a -q)
	docker kill $(docker ps -a -q)
	docker rm $(docker ps -a -q)

	docker-compose build --no-cache && \
	docker-compose up

else

	dotnet restore
	dotnet clean
	dotnet build

	if [[ $1 == "g" ]]; then

		dotnet run --project src/ApiGateway/ApiGateway.csproj

	elif [[ $1 == "h" ]]; then

		dotnet run --project src/HttpServ/HttpServ.csproj

	elif [[ $1 == "b" ]]; then

		trap "trap - SIGTERM && kill -- -$$" SIGINT SIGTERM EXIT

		dotnet run --project src/HttpServ/HttpServ.csproj > /dev/null &
		dotnet run --project src/ApiGateway/ApiGateway.csproj

	fi
fi
