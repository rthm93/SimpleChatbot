FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Chatbot/Chatbot.csproj Chatbot/
RUN dotnet restore Chatbot/Chatbot.csproj

COPY . .
RUN dotnet publish Chatbot/Chatbot.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8888
EXPOSE 8888

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Chatbot.dll"]
