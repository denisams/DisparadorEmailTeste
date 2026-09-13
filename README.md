# DisparadorEmailTeste

Disparador de email assíncrono, projetado para volumetria alta e com controle explícito de
taxa de envio (emails por segundo).

## Arquitetura

```
Cliente HTTP → Api (publica) → RabbitMQ (fila durável) → Worker (consome e envia) → SMTP
```

- **DisparadorEmailTeste.Api** — recebe um pedido de disparo em lote (`POST /api/disparos`) e
  publica um email por destinatário na fila `envio-de-emails`. Responde `202 Accepted` assim
  que os itens são enfileirados — o envio em si é assíncrono.
- **DisparadorEmailTeste.Worker** — consome a fila, aguarda uma permissão do limitador de taxa
  antes de cada envio e entrega via SMTP (MailKit). Falhas transitórias (timeout, servidor fora
  do ar) são reprocessadas automaticamente (retry com backoff exponencial); falhas permanentes
  (endereço rejeitado) e falhas que esgotaram as tentativas vão para a fila morta
  `envio-de-emails.morta`, sem bloquear o restante do lote.
- **DisparadorEmailTeste.Dominio** — entidades e contratos (`Email`, `IServicoDeEnvioDeEmail`,
  `IPublicadorDeEmails`, `ILimitadorDeTaxaDeEnvio`), sem dependência de infraestrutura.
- **DisparadorEmailTeste.Infraestrutura** — implementações concretas: RabbitMQ, SMTP e o
  limitador de taxa.

O uso de fila persistente (RabbitMQ) em vez de uma fila em memória permite escalar
horizontalmente (subir múltiplas instâncias do Worker) e sobreviver a reinícios sem perder
mensagens em trânsito.

## Configurando a volumetria de envio

O botão principal é `LimiteDeEnvio:EmailsPorSegundo`, configurado no `appsettings.json` do
Worker ou via variável de ambiente `LimiteDeEnvio__EmailsPorSegundo`. Ele controla um token
bucket: no máximo N emails são disparados por segundo, e o restante da fila aguarda de forma
assíncrona (sem consumir CPU) até haver permissão — aplicando backpressure natural sobre o
RabbitMQ.

Outros ajustes de throughput:

- `RabbitMq:PrefetchCount` — quantas mensagens o Worker mantém em processamento simultâneo.
- Rodar múltiplas instâncias do Worker (`docker compose up --scale worker=3`) — o RabbitMQ
  distribui as mensagens entre elas; ajuste `EmailsPorSegundo` em cada instância pensando no
  limite total desejado por provedor de SMTP.

## Rodando localmente

Pré-requisitos: Docker e Docker Compose.

```bash
docker compose up --build
```

Isso sobe:

- **RabbitMQ** — `localhost:5672` (AMQP) e `localhost:15672` (painel de administração,
  usuário/senha `guest`/`guest`).
- **MailHog** — servidor SMTP falso para testes locais, em `localhost:1025`; veja os emails
  recebidos em `http://localhost:8025`.
- **Api** — `http://localhost:8080`, com Swagger UI em `http://localhost:8080/swagger` (também abre automaticamente ao rodar a Api localmente com `dotnet run`, fora do Docker).
- **Worker** — consumindo a fila em segundo plano.

### Disparando um lote de teste

```bash
curl -X POST http://localhost:8080/api/disparos \
  -H "Content-Type: application/json" \
  -d '{
        "assunto": "Teste de disparo",
        "corpoHtml": "<p>Olá!</p>",
        "destinatarios": ["a@exemplo.com", "b@exemplo.com"]
      }'
```

Acompanhe a entrega em `http://localhost:8025` (MailHog) e o estado da fila em
`http://localhost:15672`.

## Testes

```bash
dotnet test
```

Cobrem validação da entidade `Email`, o comportamento do limitador de taxa (token bucket) e a
política de retentativa (falha transitória reprocessa, falha permanente não).

## Fila morta (dead-letter)

Mensagens que falharam permanentemente ou esgotaram as tentativas de retry ficam em
`envio-de-emails.morta`, disponíveis para inspeção manual no painel do RabbitMQ — nada é
descartado silenciosamente.
