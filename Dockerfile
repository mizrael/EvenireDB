FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /src
COPY ./ ./
RUN cd ./src/EvenireDB.Server && dotnet restore && dotnet publish -c Release -o /publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 as runtime
ARG SERVER_PORT=80
COPY --from=build-env /publish .
ENTRYPOINT ["dotnet", "EvenireDB.Server.dll"]
EXPOSE $SERVER_PORT
