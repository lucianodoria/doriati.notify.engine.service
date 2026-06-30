# BUILD
# O que faz: Baixa (faz o pull) da imagem oficial da Microsoft que contém o SDK do .NET 9 e dá o apelido de build para essa etapa. 
# O SDK é pesado porque vem com compiladores, ferramentas de CLI e gerenciadores de pacotes necessários para transformar código em binário.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# O que faz: Cria uma pasta chamada src dentro do container e entra nela. 
# A partir de agora, qualquer comando executado ou arquivo copiado vai acontecer dentro desse diretório /src.
WORKDIR /src

# O que faz: Copia o arquivo da sua Solution (.sln) que está no seu computador (ou no repositório do GitLab) 
# para a raiz da pasta atual do container (./, que significa /src).
COPY Doriati.Notify.Engine.Service.sln ./

# O que faz: Copia apenas os arquivos .csproj de cada camada do seu projeto para suas respectivas pastas correspondentes dentro do container.
# Por que fazer isso antes do código? O Docker trabalha com um sistema de camadas de cache. 
# Copiando apenas os .csproj primeiro, o Docker percebe que as dependências raramente mudam. 
# Se você alterar uma linha de código em um controlador e der push, o Docker pula as linhas acima e não precisa baixar todos os pacotes da internet de novo,
# economizando tempo no pipeline.
COPY src/Api/*.csproj src/Api/
COPY src/Application/*.csproj src/Application/
COPY src/Domain/*.csproj src/Domain/
COPY src/Infrastructure/*.csproj src/Infrastructure/

# Força o .NET a usar clientes HTTP mais estáveis e isola o timeout de rede
ENV DOTNET_HTTP_USES_SOCKETS_HTTP_HANDLER=1
ENV NUGET_HTTP_CACHE_MODE=direct

# O que faz: Executa (RUN) o comando do .NET para ler o arquivo da Solution, verificar quais pacotes do NuGet (bibliotecas externas) 
# o seu projeto precisa e baixá-los da internet. 
# A flag --disable-parallel avisa o .NET para baixar projeto por projeto em fila, evitando travar a rede do GitLab.
# AJUSTE 1: Restaurar a Solution inteira de forma sequencial (evita o timeout no GitLab)
# Comando cirúrgico sem o caminho do arquivo fixo
RUN dotnet restore Doriati.Notify.Engine.Service.sln --disable-parallel --no-cache

# O que faz: Agora sim! Copia todo o restante dos arquivos do seu projeto (classes .cs, configurações, etc.) do seu computador/GitLab para dentro do container (/src).
# Os arquivos .csproj copiados antes são apenas subscritos pelos novos sem problemas.
COPY . .

# O que faz: Compila o projeto da API em modo de produção (-c Release), junta todas as DLLs e arquivos necessários para o sistema funcionar
# e descarrega tudo dentro de uma nova pasta chamada /app/publish. 
# A flag --no-restore avisa: "Não baixe nada da internet de novo, use o que o comando dotnet restore já baixou ali atrás".
# AJUSTE 2: Adicionar --no-restore para aproveitar o cache do comando de cima
RUN dotnet publish src/Api/Doriati.Notify.Engine.Api.csproj -c Release -o /app/publish --no-restore

# RUNTIME
# O que faz: Inicia a imagem final baseada no ASP.NET 9 Runtime rodando sobre o Alpine Linux. 
# O Runtime não sabe compilar código, ele apenas sabe executar o que já está compilado. 
# O Alpine é uma distribuição Linux cirúrgica (pesa cerca de 5MB a 10MB), o que torna seu container final extremamente leve e seguro contra vulnerabilidades.
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine

# O que faz: Cria e entra na pasta app dentro desse novo container Alpine.
WORKDIR /app

# O que faz: Este é o coração do multi-stage build. O Docker vai lá no primeiro estágio (que apelidamos de build), entra na pasta /app/publish dele,
# pega os arquivos que foram compilados e os copia para dentro da pasta atual (., que é /app) deste novo container Alpine.
COPY --from=build /app/publish .

# O que faz: Cria uma Variável de Ambiente (ENV) informando ao .NET que o sistema está rodando oficialmente em ambiente de Produção.
# Isso faz com que sua API carregue o arquivo appsettings.Production.json automaticamente.
ENV ASPNETCORE_ENVIRONMENT=Production

# O que faz: Define o comando definitivo que será executado assim que o container for iniciado na sua VPS.
# Ele roda o comando de terminal dotnet Doriati.Notify.Engine.Api.dll, inicializando o Kestrel (servidor web interno do .NET)
# e deixando seu microsserviço de pé, escutando a fila e a API.
ENTRYPOINT ["dotnet", "Doriati.Notify.Engine.Api.dll"]
