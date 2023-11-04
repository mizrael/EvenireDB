FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /src
COPY ./ ./
RUN cd ./src/EvenireDB.Server && dotnet restore && dotnet publish -c Release -o /publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 as runtime
ARG HTTP_PORT=16281
ARG GRPC_PORT=16282
COPY --from=build-env /publish .
ENTRYPOINT ["dotnet", "EvenireDB.Server.dll"]
EXPOSE $HTTP_PORT $GRPC_PORT
