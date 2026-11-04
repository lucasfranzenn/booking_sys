# Booking Sys

Sistema de reserva de horários para uma sala, com interface em React e API em ASP.NET Core. O usuário escolhe uma data, consulta os horários disponíveis e confirma a reserva, que é persistida em PostgreSQL.

## Funcionalidades

- Consulta de disponibilidade por data.
- Horários padrão às **09:00, 10:00 e 11:00**.
- Confirmação de reserva e atualização da disponibilidade na interface.
- Tratamento de resposta de conflito quando um horário já está reservado.
- Restrição de unicidade de data e horário no banco de dados.

## Tecnologias

| Camada | Tecnologias |
| --- | --- |
| Frontend | React 19, TypeScript 7, Vite 8 |
| Backend | C#, ASP.NET Core, .NET 10 |
| Persistência | PostgreSQL, Entity Framework Core 10, Npgsql |
| Testes | xUnit, Vitest, Testing Library e jsdom |
| Análise estática | ESLint |

## Estrutura

```text
booking-sys/
├── API/
│   ├── Controllers/       # Rotas de consulta e reserva
│   ├── Data/              # Contexto do Entity Framework
│   ├── Entities/          # Modelo de reserva
│   ├── Migrations/        # Criação da tabela e índice único
│   ├── Properties/        # Perfis e portas de execução
│   ├── Views/             # Views existentes, não utilizadas pela interface React
│   ├── API.csproj
│   └── Program.cs
├── API.Tests/             # Testes com banco PostgreSQL real
└── frontend/
    ├── src/Pages/         # Tela de reserva
    ├── src/test/          # Testes da interface
    └── package.json
```

## Pré-requisitos

- SDK do **.NET 10**.
- **Node.js 22.12 ou superior na linha 22**, ou **Node.js 24**, com npm, compatíveis com Vite e Vitest registrados no lockfile.
- PostgreSQL em execução e um usuário com permissão para criar tabelas no banco da aplicação.
- Ferramenta `dotnet-ef` da linha 10 para aplicar as migrations.

Os exemplos abaixo usam **PowerShell**. Execute os comandos a partir da raiz do projeto, salvo quando indicado.

## Executar localmente

### 1. Preparar o banco de dados

No PostgreSQL, crie o banco por meio do pgAdmin, do psql ou de outro cliente SQL:

```sql
CREATE DATABASE booking_sys;
```

No terminal que será usado para iniciar a API, configure a conexão. Substitua o usuário e a senha pelos valores do seu ambiente:

```powershell
$env:ConnectionStrings__ReservationsContext = "Host=localhost;Port=5432;Database=booking_sys;Username=postgres;Password=SUA_SENHA"
```

Essa variável substitui `ConnectionStrings:ReservationsContext` de `API/appsettings.json` e vale para a sessão atual do terminal. Ao abrir outro terminal para a API ou para migrations, defina-a novamente.

### 2. Restaurar dependências e aplicar migrations

```powershell
dotnet restore .\API\API.csproj
dotnet tool install --global dotnet-ef --version "10.*"
dotnet ef database update --project .\API\API.csproj
```

Se `dotnet-ef` já estiver instalado, confira a versão com `dotnet ef --version`. Para atualizar para a linha 10, use `dotnet tool update --global dotnet-ef --version "10.*"`.

A migration cria a tabela `Reservations` e o índice único sobre `Date` e `Time`. A API não aplica migrations automaticamente na inicialização.

### 3. Iniciar a API

No mesmo terminal em que foi definida a conexão:

```powershell
dotnet run --project .\API\API.csproj --launch-profile http
```

A API fica disponível em `http://localhost:5215`. O perfil `http` corresponde à URL utilizada pelo frontend.

O projeto também possui um perfil `https`, na porta `7126`. Para usá-lo com a interface, ajuste a constante `API_URL` em `frontend/src/Pages/BookingPage.tsx` e configure o certificado de desenvolvimento. O middleware de redirecionamento HTTPS está presente; no perfil somente HTTP, pode aparecer um aviso de que a porta HTTPS não foi determinada.

### 4. Iniciar o frontend

Em outro terminal, a partir da raiz do projeto:

```powershell
cd .\frontend
npm ci
npm run dev -- --port 5173 --strictPort
```

Acesse **http://localhost:5173**, escolha uma data e selecione um horário disponível para reservar.

A política CORS da API permite a origem exata `http://localhost:5173`. Se alterar a porta ou o endereço do frontend, atualize essa política em `API/Program.cs`. A URL da API é definida diretamente em `BookingPage.tsx`; não há configuração por variável `VITE_*` implementada.

## API HTTP

As rotas não possuem prefixo `/api`.

| Método | Rota | Comportamento |
| --- | --- | --- |
| `GET` | `/times?date=YYYY-MM-DD` | Retorna os horários disponíveis para a data |
| `POST` | `/booking` | Reserva um horário existente e disponível |

### Consultar horários

```powershell
Invoke-RestMethod -Uri "http://localhost:5215/times?date=2099-02-04"
```

Exemplo de resposta para uma data sem reservas, sem garantia de ordenação:

```json
["09:00:00", "10:00:00", "11:00:00"]
```

**A consulta também pode gravar no banco:** quando não existe nenhum horário para a data, o endpoint cria os três horários padrão. Por isso, consulte a data antes de tentar reservá-la. Horários reservados são excluídos da resposta; quando não há disponibilidade, o retorno é `[]`.

### Reservar um horário

```powershell
$body = @{ date = "2099-02-04"; time = "10:00:00" } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:5215/booking" -Method Post -ContentType "application/json" -Body $body
```

Exemplo de corpo da resposta `201 Created` (o identificador varia):

```json
{
  "id": 2,
  "date": "2099-02-04",
  "time": "10:00:00",
  "isBooked": true
}
```

| Status | Significado |
| --- | --- |
| `201 Created` | Reserva confirmada |
| `404 Not Found` | Não existe horário cadastrado para a combinação de data e hora |
| `409 Conflict` | O horário consultado já está reservado |
| `400 Bad Request` | Falha de validação ou conversão dos dados recebidos |

A resposta de criação inclui `Location: /booking/{id}`, mas não existe uma rota de consulta por identificador implementada.

## Testes

### Backend

Os testes usam **PostgreSQL real**. Crie um banco exclusivo para eles:

```sql
CREATE DATABASE test_booking_sys;
```

Na raiz do projeto:

```powershell
$env:test_connString = "Host=localhost;Port=5432;Database=test_booking_sys;Username=postgres;Password=SUA_SENHA"
dotnet test .\API.Tests\API.Tests.csproj
```

O nome do banco deve começar com `test_`; caso contrário, os testes são cancelados. A variável `test_connString` é obrigatória: a entrada `TestReservationsContext` do arquivo de configuração não é lida pelos testes atuais.

Os testes aplicam as migrations, gravam dados e removem os registros utilizados ao final. Use um banco exclusivo para evitar interferência em outros dados. A cobertura existente verifica a restrição contra duplicidade de data e horário, a criação dos três horários padrão e a confirmação de uma reserva disponível.

### Frontend

Dentro de `frontend`:

```powershell
npm run test -- --run
```

Para manter o executor em modo interativo:

```powershell
npm test
```

O teste existente simula uma resposta `409` da API e verifica a mensagem de conflito e a atualização dos horários. Ele utiliza chamadas HTTP simuladas e não depende da API em execução.

## Build e análise estática

Na raiz do projeto:

```powershell
dotnet build .\API\API.csproj
```

Dentro de `frontend`:

```powershell
npm run lint
npm run build
```

O build do frontend gera os arquivos em `frontend/dist`. `npm run preview` permite visualizar esse build localmente, mas sua origem precisa estar autorizada no CORS para acessar a API.

## Escopo atual

- O modelo representa uma agenda única; não há cadastro de salas ou usuários.
- Não há autenticação, cancelamento, edição ou listagem geral de reservas nas rotas atuais.
- Não há validação de datas passadas implementada.
- A configuração de OpenAPI está comentada em `API/Program.cs`; não há Swagger UI configurado.
- O índice único impede linhas duplicadas para a mesma data e hora. A confirmação da reserva ainda não possui controle explícito de concorrência para duas requisições simultâneas sobre a mesma linha.
