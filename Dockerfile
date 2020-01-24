FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/runtime:3.1

#RUN groupadd --gid 3000 odbc \
#  && useradd --uid 1000 --gid odbc --shell /bin/bash --create-home odbc

# Install app
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "sqlaudit-runner.dll"]