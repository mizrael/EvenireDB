FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /src
COPY ./ ./
RUN cd ./src/EvenireDB.Server && dotnet restore && dotnet publish -c Release -o /publish

FROM mcr.microsoft.com/dotnet/aspnet:7.0 as runtime
COPY --from=build-env /publish .
ENTRYPOINT ["dotnet", "EvenireDB.Server.dll"]