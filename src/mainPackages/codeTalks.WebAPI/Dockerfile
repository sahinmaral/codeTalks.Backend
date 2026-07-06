FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/corePackages/Core.Application/*.csproj src/corePackages/Core.Application/
COPY src/corePackages/Core.CrossCuttingConcerns/*.csproj src/corePackages/Core.CrossCuttingConcerns/
COPY src/corePackages/Core.Mailing/*.csproj src/corePackages/Core.Mailing/
COPY src/corePackages/Core.Persistence/*.csproj src/corePackages/Core.Persistence/
COPY src/corePackages/Core.Security/*.csproj src/corePackages/Core.Security/

COPY src/mainPackages/codeTalks.Application/*.csproj src/mainPackages/codeTalks.Application/
COPY src/mainPackages/codeTalks.Domain/*.csproj src/mainPackages/codeTalks.Domain/
COPY src/mainPackages/codeTalks.Infrastructure/*.csproj src/mainPackages/codeTalks.Infrastructure/
COPY src/mainPackages/codeTalks.Persistence/*.csproj src/mainPackages/codeTalks.Persistence/
COPY src/mainPackages/codeTalks.Presentation/*.csproj src/mainPackages/codeTalks.Presentation/
COPY src/mainPackages/codeTalks.WebAPI/*.csproj src/mainPackages/codeTalks.WebAPI/

RUN dotnet restore src/mainPackages/codeTalks.WebAPI/codeTalks.WebAPI.csproj

COPY . .
RUN dotnet publish src/mainPackages/codeTalks.WebAPI/codeTalks.WebAPI.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
EXPOSE 8081

ENTRYPOINT ["dotnet", "codeTalks.WebAPI.dll"]