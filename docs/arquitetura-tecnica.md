# Doriati Notify Engine (Telegram) — Documento Técnico

## 1. Objetivo

Este repositório implementa um **microsserviço de notificações genérico para Telegram**, com duas portas de entrada:

- **Síncrona (HTTP)**: Minimal API `POST /api/v1/notifications/telegram` protegida por autenticação **Machine-to-Machine (M2M)** via `X-Client-Id` + `X-Client-Secret`.
- **Assíncrona (RabbitMQ)**: consumidor de fila que recebe um payload JSON compatível com o mesmo contrato usado pela API HTTP.

O serviço é “genérico” no sentido de que **`BotToken` e `ChatId` são enviados pela aplicação origem** (ex.: GCD/BWEB), permitindo múltiplos bots/destinos sem reconfiguração do serviço.

---

## 2. Arquitetura: Clean Architecture + Vertical Slice + Minimal API

### 2.1. Princípios aplicados

- **Clean Architecture**
  - `Application` não depende de `Infrastructure`.
  - `Infrastructure` depende de `Application`.
  - `Api` depende de `Application` e `Infrastructure` (composição/DI).
- **Vertical Slice**
  - Os endpoints HTTP são organizados por *feature*.
  - Cada feature expõe handlers (métodos estáticos) com foco em um caso de uso.
- **Minimal API**
  - Sem MVC Controllers.
  - `Program.cs` mínimo: bootstrap + DI + middlewares + mapeamento de endpoints.

---

## 3. Estrutura de pastas

```
src/
  Api/
    Program.cs
    Extensions/
      DependencyInjection.cs
      MiddlewareExtensions.cs
      EndpointExtensions.cs
      AllowedClientsEndpointFilter.cs
    Endpoints/
      TelegramNotifications/
        TelegramNotificationEndpoints.cs
        CreateTelegramNotification.cs
    Services/
      RabbitMqTelegramHostedService.cs
    appsettings*.json

  Application/
    Abstractions/
      ITelegramSender.cs
    TelegramNotifications/
      Commands/
        SendTelegramNotification.cs
      Dtos/
        TelegramNotificationRequest.cs

  Domain/
    (reservado para entidades/invariantes puras)

  Infrastructure/
    Telegram/
      TelegramSender.cs
    Messaging/
      RabbitMq/
        RabbitMqOptions.cs
        RabbitMqTelegramConsumer.cs
```

---

## 4. Designer Partners (padrões e práticas de arquitetura)

Nesta seção, “designer partners” significa **padrões de design/arquitetura e práticas** que guiaram a organização do código e a forma de evoluir o serviço.

### 4.1. Clean Architecture (camadas e dependências)

- O que é: organização em camadas onde regras de negócio/casos de uso ficam isolados de detalhes técnicos.
- Como usamos neste projeto:
  - `Application` não referencia `Infrastructure`.
  - `Infrastructure` implementa portas definidas em `Application` (ex.: `ITelegramSender`).
  - `Api` apenas compõe o sistema (DI + endpoints + middlewares).

Referências:
- `Api -> Application/Infrastructure`: [Doriati.Notify.Engine.Api.csproj](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Api/Doriati.Notify.Engine.Api.csproj)
- `Infrastructure -> Application`: [Doriati.Notify.Engine.Infrastructure.csproj](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Infrastructure/Doriati.Notify.Engine.Infrastructure.csproj)
- `Application -> Domain`: [Doriati.Notify.Engine.Application.csproj](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Application/Doriati.Notify.Engine.Application.csproj)

### 4.2. Vertical Slice (feature-first)

- O que é: organizar o código por *feature* / “fatia vertical” (entrada → caso de uso → saída), em vez de por tipo técnico (controllers/services/etc.).
- Como usamos neste projeto:
  - Endpoints HTTP estão agrupados na feature `TelegramNotifications`.
  - O handler do endpoint é testável isoladamente (método estático) e chama o caso de uso.

Referências:
- Feature: [Endpoints/TelegramNotifications](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Api/Endpoints/TelegramNotifications)
- Caso de uso: [SendTelegramNotification.cs](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Application/TelegramNotifications/Commands/SendTelegramNotification.cs)

### 4.3. Ports & Adapters (Hexagonal) via interface mínima

- O que é: definir “portas” (interfaces) na camada de aplicação e implementar “adapters” na infraestrutura.
- Como usamos neste projeto:
  - Porta: `ITelegramSender` em `Application`.
  - Adapter: `TelegramSender` em `Infrastructure`.
  - Benefício: facilita testes (mock) e evolução para múltiplos canais (WhatsApp/Email/SMS) sem alterar o caso de uso.

Referências:
- Porta: [ITelegramSender.cs](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Application/Abstractions/ITelegramSender.cs)
- Adapter: [TelegramSender.cs](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Infrastructure/Telegram/TelegramSender.cs)

### 4.4. Minimal API + Route Grouping (MapGroup)

- O que é: construir API HTTP com endpoints enxutos e composáveis, sem Controllers MVC.
- Como usamos neste projeto:
  - Agrupamento de rotas em `/api/v1/notifications` com `MapGroup`.
  - Mapeamento do endpoint `POST /telegram` fora do `Program.cs`.

Referências:
- [TelegramNotificationEndpoints.cs](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Api/Endpoints/TelegramNotifications/TelegramNotificationEndpoints.cs)

### 4.5. Endpoint Filter (M2M por Client Id/Secret)

- O que é: um filtro por endpoint (pipeline) para validar/rejeitar a chamada antes de chegar no handler.
- Como usamos neste projeto:
  - O filtro valida `X-Client-Id` e `X-Client-Secret` contra `AllowedClients`.
  - A comparação de secret usa hash + `FixedTimeEquals` (reduz risco de timing attack).

Referências:
- [AllowedClientsEndpointFilter.cs](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Api/Extensions/AllowedClientsEndpointFilter.cs)

### 4.6. Options Pattern (config tipada)

- O que é: bind de configuração para classes tipadas via `IOptions<T>`.
- Como usamos neste projeto:
  - `RabbitMqOptions` representa a seção `RabbitMq` (host, user, vhost, queue).
  - O consumer recebe `IOptions<RabbitMqOptions>`.

Referências:
- [RabbitMqOptions.cs](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Infrastructure/Messaging/RabbitMq/RabbitMqOptions.cs)
- DI: [DependencyInjection.cs](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Api/Extensions/DependencyInjection.cs)

### 4.7. BackgroundService + consumo resiliente (processo contínuo)

- O que é: padrão de processamento contínuo em background dentro do Host do ASP.NET.
- Como usamos neste projeto:
  - `RabbitMqTelegramHostedService` mantém o consumo de fila ativo.
  - Se o RabbitMQ estiver indisponível, o serviço não derruba o host HTTP: ele loga e tenta reconectar.

Referências:
- [RabbitMqTelegramHostedService.cs](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Api/Services/RabbitMqTelegramHostedService.cs)

### 4.8. Mensageria com ACK manual (at-least-once controlado)

- O que é: o consumidor controla explicitamente o ACK/NACK, evitando perda silenciosa por auto-ack.
- Como usamos neste projeto:
  - Sucesso: `BasicAckAsync`.
  - Erro: `BasicNackAsync(requeue: false)` (DLQ depende do broker).

Referências:
- [RabbitMqTelegramConsumer.cs](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Infrastructure/Messaging/RabbitMq/RabbitMqTelegramConsumer.cs)

---

## 5. Partners técnicos (dependências e integrações)

Nesta seção, “partners” inclui **bibliotecas NuGet**, **frameworks** e **serviços externos** integrados.

### 5.1. ASP.NET Core (.NET 9) / Minimal API

- O que é: framework web do .NET para expor endpoints HTTP.
- Como usamos:
  - Endpoint `POST /api/v1/notifications/telegram` mapeado por feature com `MapGroup`.
  - Endpoint filter para M2M (validação de headers).
  - DI nativo para injeção de dependências.

Arquivos principais:
- [Program.cs](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Api/Program.cs)
- [TelegramNotificationEndpoints.cs](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Api/Endpoints/TelegramNotifications/TelegramNotificationEndpoints.cs)

### 5.2. Telegram.Bot (NuGet)

- O que é: SDK oficial/comum de mercado para a **Telegram Bot API**.
- Como usamos:
  - Instanciamos `TelegramBotClient` **dinamicamente** por request usando `BotToken` recebido no payload.
  - Enviamos mensagem com `SendMessage(...)` e `ParseMode` selecionado dinamicamente.

Implementação:
- [TelegramSender.cs](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Infrastructure/Telegram/TelegramSender.cs)

### 5.3. RabbitMQ.Client (NuGet)

- O que é: cliente oficial para integração com **RabbitMQ** via AMQP.
- Como usamos:
  - `QueueDeclareAsync` (fila definida por configuração).
  - `BasicQosAsync(prefetchCount: 1)` para processar 1 mensagem por vez.
  - `BasicConsumeAsync(autoAck: false)` com:
    - `BasicAckAsync` em sucesso.
    - `BasicNackAsync(requeue: false)` em falha (DLQ dependerá de DLX configurada no broker).

Implementação:
- [RabbitMqTelegramConsumer.cs](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Infrastructure/Messaging/RabbitMq/RabbitMqTelegramConsumer.cs)

### 5.4. Microsoft.Extensions.Options (NuGet)

- O que é: pacote base para o **Options Pattern** (binding de config tipada).
- Como usamos:
  - Binding de `RabbitMqOptions` a partir da seção `RabbitMq` do `appsettings.*` / variáveis de ambiente.

Implementação:
- [RabbitMqOptions.cs](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Infrastructure/Messaging/RabbitMq/RabbitMqOptions.cs)
- Registro em DI: [DependencyInjection.cs](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Api/Extensions/DependencyInjection.cs)

### 5.5. Microsoft.Extensions.Logging.Abstractions (NuGet)

- O que é: abstrações de logging (`ILogger<T>`) desacopladas de um provider específico.
- Como usamos:
  - `ILogger<T>` em consumer, hosted service e filtro de autenticação.
  - `LogInformation/LogWarning/LogError` para observabilidade operacional.

### 5.6. Serviços externos (infra)

- **Telegram Bot API**
  - Serviço externo para envio de mensagens.
  - Dependência operacional: disponibilidade de rede + token válido.
- **RabbitMQ Broker**
  - Serviço externo para mensageria assíncrona.
  - Dependência operacional: host/porta/credenciais/vhost configurados.

---

## 6. Clean Code e SOLID (destaque)

Esta seção explica **o que foi aplicado** no projeto e **como você pode aprender/replicar**.

### 6.1. Clean Code: o que foi aplicado aqui

- **Nomes explícitos e orientados à intenção**
  - Feature: `TelegramNotifications`, caso de uso: `SendTelegramNotification`, adapter: `TelegramSender`.
- **Separação de responsabilidades**
  - Endpoints só recebem HTTP e delegam (sem regra de negócio).
  - Caso de uso centraliza validação e orquestração.
  - Infra somente integra com Telegram/RabbitMQ.
- **Duplicação reduzida**
  - Tanto o fluxo HTTP quanto o RabbitMQ usam o mesmo caso de uso `SendTelegramNotification`.
- **Código previsível e testável**
  - Handlers e caso de uso são métodos estáticos e dependem apenas de parâmetros (fácil de unit-test).

Referências rápidas:
- Endpoint sem lógica de negócio: [TelegramNotificationEndpoints.cs](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Api/Endpoints/TelegramNotifications/TelegramNotificationEndpoints.cs)
- Caso de uso: [SendTelegramNotification.cs](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Application/TelegramNotifications/Commands/SendTelegramNotification.cs)

### 6.2. SOLID: onde aparece no projeto

- **S — Single Responsibility Principle (SRP)**
  - `AllowedClientsEndpointFilter`: só autentica/autoriza.
  - `CreateTelegramNotificationHandler`: só trata HTTP e chama o caso de uso.
  - `SendTelegramNotification`: só executa o caso de uso.
  - `TelegramSender`: só fala com o Telegram.
  - `RabbitMqTelegramConsumer`: só cuida do consumo/ack e chama o caso de uso.
- **O — Open/Closed Principle (OCP)**
  - Para adicionar outro canal (ex.: WhatsApp), você cria outro `ISender`/feature sem alterar o caso de uso existente do Telegram.
- **L — Liskov Substitution Principle (LSP)**
  - Qualquer implementação de `ITelegramSender` pode substituir `TelegramSender` sem quebrar o caso de uso.
- **I — Interface Segregation Principle (ISP)**
  - `ITelegramSender` é uma interface pequena e específica (não “gigante”).
- **D — Dependency Inversion Principle (DIP)**
  - `Application` depende de abstrações (`ITelegramSender`), não de `TelegramBotClient`.
  - `Infrastructure` implementa a abstração e é plugada via DI.

Referências:
- Interface (abstração): [ITelegramSender.cs](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Application/Abstractions/ITelegramSender.cs)
- Implementação (infra): [TelegramSender.cs](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Infrastructure/Telegram/TelegramSender.cs)

### 6.3. Como estudar/aprender com este projeto (sugestão prática)

- Copie o padrão de pasta de uma feature existente:
  - `Api/Endpoints/<Feature>`
  - `Application/<Feature>/Commands|Queries|Dtos`
  - `Infrastructure/<Integration>`
- Quando precisar de um detalhe técnico novo (HTTP client, DB, cache):
  - crie uma **interface em Application**,
  - implemente em **Infrastructure**,
  - injete via **Api/Extensions/DependencyInjection.cs**.

---

## 7. Contratos de dados

### 7.1. TelegramNotificationRequest (DTO)

Arquivo:
- [TelegramNotificationRequest.cs](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Application/TelegramNotifications/Dtos/TelegramNotificationRequest.cs)

Campos:
- `BotToken` (string) — token do bot (vem do sistema origem).
- `ChatId` (string) — destino (vem do sistema origem).
- `Message` (string) — texto.
- `ParseMode` (string, default `MarkdownV2`) — modo de parse do Telegram.

O mesmo contrato é usado em:
- HTTP body (`POST /api/v1/notifications/telegram`)
- Payload da fila RabbitMQ (JSON)

---

## 8. Fluxos principais

### 8.1. Fluxo síncrono (HTTP)

1. Cliente chama `POST /api/v1/notifications/telegram` enviando JSON do `TelegramNotificationRequest`.
2. `AllowedClientsEndpointFilter` valida:
   - `X-Client-Id`
   - `X-Client-Secret`
   contra `AllowedClients` do `appsettings`.
3. Handler chama o caso de uso `SendTelegramNotification.HandleAsync(...)`.
4. Caso de uso valida campos e delega o envio para `ITelegramSender`.
5. `Infrastructure.TelegramSender` envia via Telegram Bot API.
6. Retorno:
   - `401` se headers inválidos/ausentes.
   - `200` se envio ok.
   - `500` se ocorrer exceção no envio (ex.: token inválido, erro de rede).

Código:
- Filtro: [AllowedClientsEndpointFilter.cs](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Api/Extensions/AllowedClientsEndpointFilter.cs)
- Endpoint: [TelegramNotificationEndpoints.cs](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Api/Endpoints/TelegramNotifications/TelegramNotificationEndpoints.cs)
- Handler: [CreateTelegramNotification.cs](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Api/Endpoints/TelegramNotifications/CreateTelegramNotification.cs)
- Use case: [SendTelegramNotification.cs](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Application/TelegramNotifications/Commands/SendTelegramNotification.cs)

### 8.2. Fluxo assíncrono (RabbitMQ)

1. `RabbitMqTelegramHostedService` inicia e tenta conectar no RabbitMQ.
2. `RabbitMqTelegramConsumer` declara a fila configurada e inicia o consumo (ack manual).
3. Ao receber mensagem:
   - desserializa JSON em `TelegramNotificationRequest`
   - executa `SendTelegramNotification.HandleAsync(...)`
4. Resultado:
   - sucesso: `BasicAckAsync`
   - falha: `BasicNackAsync(requeue: false)` (DLQ depende da infra do broker)

Resiliência de host:
- Se a conexão com RabbitMQ falhar no start, o hosted service registra erro e tenta novamente (a API HTTP continua rodando).

Código:
- Hosted service: [RabbitMqTelegramHostedService.cs](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Api/Services/RabbitMqTelegramHostedService.cs)
- Consumer: [RabbitMqTelegramConsumer.cs](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Infrastructure/Messaging/RabbitMq/RabbitMqTelegramConsumer.cs)

---

## 9. Segurança (M2M por AllowedClients)

### 7.1. Headers exigidos

- `X-Client-Id`: identificador do client (ex.: `GCD_APP`)
- `X-Client-Secret`: segredo configurado para o client

### 7.2. Configuração

Seção:

```json
"AllowedClients": {
  "GCD_APP": "secret_gcd_ficticio_123",
  "BWEB_APP": "secret_bweb_ficticio_456"
}
```

Arquivos:
- [appsettings.json](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Api/appsettings.json)
- [appsettings.Development.json](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/src/Api/appsettings.Development.json)

### 7.3. Observação importante

Esse modelo é propositalmente simples (API Key/Secret). Para produção:
- Prefira configurar secrets via **variáveis de ambiente** (já suportado pelo binder: `AllowedClients__GCD_APP=...`).
- Se necessário, evoluir para OAuth2 Client Credentials com um IdP (ex.: Keycloak/Auth0/Azure AD).

---

## 10. Configuração do RabbitMQ

Seção `RabbitMq` (Options Pattern):

```json
"RabbitMq": {
  "HostName": "localhost",
  "UserName": "guest",
  "Password": "guest",
  "Port": 5672,
  "VirtualHost": "/",
  "Queue": "queue.applications.notifications.telegram"
}
```

Binding:
- `Api` registra `services.Configure<RabbitMqOptions>(...)`
- `Infrastructure` consome via `IOptions<RabbitMqOptions>`

---

## 11. Execução e deploy

### 9.1. Local (dotnet)

- Build:
  - `dotnet build Doriati.Notify.Engine.Service.sln`
- Run:
  - `dotnet run --project src/Api/Doriati.Notify.Engine.Api.csproj`

### 9.2. Docker

- O build/publish é feito a partir do projeto `Api`.
- Runtime usa `mcr.microsoft.com/dotnet/aspnet:9.0-alpine`.

Arquivos:
- [Dockerfile](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/Dockerfile)
- [docker-compose.yml](file:///g:/Projetos/GITLAB/doriati.notify.engine.service/docker-compose.yml)

---

## 12. Pontos de extensão (roadmap natural)

- **Healthchecks**: adicionar endpoints `/health` (liveness/readiness) e checar RabbitMQ/Telegram (opcional).
- **Retry/DLQ**: padronizar política de retry e DLQ (hoje o `requeue:false` delega DLQ à configuração do broker).
- **Observabilidade**: OpenTelemetry (traces/metrics/logs) + correlação por request/message.
- **Multi-providers**: adicionar novos canais (WhatsApp/Email/SMS) replicando o mesmo padrão de feature (Vertical Slice) e abstrações em `Application`.
