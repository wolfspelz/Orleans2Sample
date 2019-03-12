FROM mcr.microsoft.com/dotnet/core/runtime:2.2
WORKDIR /app
#RUN apt-get update && apt-get install -y netcat
ENV ClusterId=Docker
ENTRYPOINT ["dotnet", "_Wolfspelz.OrleansSample.SiloHost.dll"]
#ENTRYPOINT ["netcat", "-l", "-p", "2000"]
COPY bin/Release/netcoreapp2.2/publish/ /app
