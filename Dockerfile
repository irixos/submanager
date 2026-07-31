FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SubManager.ApiClient/SubManager.ApiClient.csproj SubManager.ApiClient/
COPY SubManager.Client/SubManager.Client.csproj SubManager.Client/
COPY SubManager.Api/SubManager.Api.csproj SubManager.Api/

RUN dotnet restore SubManager.Api/SubManager.Api.csproj

COPY SubManager.ApiClient/ SubManager.ApiClient/
COPY SubManager.Client/ SubManager.Client/
COPY SubManager.Api/ SubManager.Api/

FROM build AS client-publish
RUN dotnet publish SubManager.Client/SubManager.Client.csproj \
	-c Release \
	-o /client \
	--no-restore

FROM build AS api-publish
RUN dotnet publish SubManager.Api/SubManager.Api.csproj \
	-c Release \
	-o /api \
	--no-restore \
	/p:UseAppHost=false

FROM api-publish AS migration-bundle
RUN dotnet tool install dotnet-ef \
	--tool-path /tools \
	--version 10.0.8
RUN /tools/dotnet-ef migrations bundle \
	--project SubManager.Api/SubManager.Api.csproj \
	--configuration Release \
	--no-build \
	--output /efbundle

FROM build AS licenses
COPY nuget-license-projects.json nuget-license-overrides.json ./
COPY ThirdPartyLicenses/MIT.txt /licenses/MIT.txt
RUN dotnet tool install nuget-license \
	--tool-path /tools \
	--version 4.0.15
RUN /tools/nuget-license \
	--json-input nuget-license-projects.json \
	--include-transitive \
	--exclude-publish-false \
	--ignored-packages "Microsoft.AspNetCore.Components.Analyzers;Microsoft.AspNetCore.Components.WebAssembly.DevServer;Microsoft.EntityFrameworkCore.Analyzers;Microsoft.NET.ILLink.Tasks;Microsoft.NET.Sdk.WebAssembly.Pack" \
	--override-package-information nuget-license-overrides.json \
	--output Markdown \
	--file-output /licenses/THIRD-PARTY-NOTICES.md
RUN find /root/.nuget/packages/microsoft.data.sqlclient.sni.runtime \
	-name LICENSE.txt \
	-exec cp {} /licenses/Microsoft.Data.SqlClient.SNI.runtime.txt \; \
	-quit && \
	test -s /licenses/Microsoft.Data.SqlClient.SNI.runtime.txt
RUN mkdir /licenses/UpstreamNotices && \
	cd /root/.nuget/packages && \
	find . -type f -iname '*notice*' \
		-exec cp --parents {} /licenses/UpstreamNotices/ \;

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=api-publish /api .
COPY --from=client-publish /client/wwwroot/ ./wwwroot/
COPY --from=migration-bundle /efbundle ./efbundle
COPY --from=licenses /licenses/ ./ThirdPartyLicenses/

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "SubManager.Api.dll"]
