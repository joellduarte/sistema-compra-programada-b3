# Sistema de Compra Programada de Ações (Top Five)

Sistema completo de compra programada de ações para a Itaú Corretora, implementando 70 regras de negócio com arquitetura Clean Architecture + DDD.

## Arquitetura

```
┌─────────────────────────────────────────────────────────────┐
│                    CompraProgramada.API                      │
│              (Controllers, Swagger, Program.cs)              │
├─────────────────────────────────────────────────────────────┤
│                CompraProgramada.Application                  │
│               (Services, DTOs, Interfaces)                   │
├─────────────────────────────────────────────────────────────┤
│                  CompraProgramada.Domain                     │
│        (Entities, Value Objects, Enums, Interfaces)          │
├─────────────────────────────────────────────────────────────┤
│              CompraProgramada.Infrastructure                 │
│         (EF Core, Repositories, Kafka, Parsers)              │
└──────────┬──────────────────┬───────────────────┬───────────┘
           │                  │                   │
       MySQL 8.0         Apache Kafka        COTAHIST B3
      (porta 3307)     (ir-dedo-duro,       (parser .TXT)
                         ir-venda)
```

**Padrões utilizados:** Clean Architecture, DDD (Domain-Driven Design), Repository Pattern, Unit of Work, Value Objects (CPF).

## Stack Tecnológica

| Tecnologia | Versão | Uso |
|---|---|---|
| .NET | 8.0 | Backend / Web API |
| MySQL | 8.0 | Banco de dados relacional |
| Apache Kafka | Confluent 7.5 | Mensageria (eventos de IR) |
| Entity Framework Core | 8.0 | ORM (Pomelo MySQL) |
| Confluent.Kafka | 2.3.0 | Producer Kafka com idempotência |
| xUnit + Moq | - | Testes unitários |
| Swagger/OpenAPI | Swashbuckle 6.6 | Documentação interativa da API |
| Docker Compose | - | Infraestrutura local |

## Pré-requisitos

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- Arquivo COTAHIST da B3 (`.TXT`) — disponível em [dados históricos da B3](https://www.b3.com.br/pt_br/market-data-e-indices/servicos-de-dados/market-data/historico/mercado-a-vista/cotacoes-historicas/)

## Como Executar

### 1. Subir a infraestrutura

```bash
docker compose up -d
```

Isso inicia:
- **MySQL 8.0** na porta `3307` (user: `compra_user`, pass: `compra_pass`)
- **Kafka + Zookeeper** na porta `9092`
- **kafka-init** cria os tópicos `ir-dedo-duro` e `ir-venda` automaticamente

Aguarde ~15 segundos para o Kafka ficar saudável.

### 2. Rodar a API

```bash
cd src/CompraProgramada.API
dotnet run
```

A API sobe em `http://localhost:5265` com Swagger na página inicial.

> As migrations do EF Core são aplicadas automaticamente no startup.

### 3. Rodar os testes

```bash
dotnet test
```

> **113 testes unitários** passando com cobertura de domínio, serviços e regras de negócio.

## Guia de Teste Rápido

Siga esta ordem no Swagger (`http://localhost:5265`) para ver o sistema funcionando de ponta a ponta:

### Passo 1 — Importar Cotações

**`POST /api/Cotacoes/importar`** — Faça upload do arquivo COTAHIST (.TXT) da B3.

### Passo 2 — Cadastrar um Cliente

**`POST /api/Clientes/adesao`**
```json
{
  "nome": "João Silva",
  "cpf": "52998224725",
  "email": "joao@email.com",
  "valorMensal": 3000
}
```

### Passo 3 — Criar a Cesta Top Five

**`POST /api/Admin/cestas`**
```json
{
  "nome": "Top Five Março 2026",
  "itens": [
    { "ticker": "PETR4", "percentual": 30 },
    { "ticker": "VALE3", "percentual": 25 },
    { "ticker": "ITUB4", "percentual": 20 },
    { "ticker": "BBDC4", "percentual": 15 },
    { "ticker": "ABEV3", "percentual": 10 }
  ]
}
```

> A cesta deve ter **exatamente 5 ativos** e a soma dos percentuais deve ser **100%**.

### Passo 4 — Executar Compra Programada

**`POST /api/Compras/executar?dataReferencia=2026-03-05`**

O motor executa o ciclo completo:
- Consolida 1/3 do aporte mensal de todos os clientes
- Compra em lote padrão (≥100) e fracionário (1-99)
- Distribui proporcionalmente para cada cliente (resíduos ficam na conta master)
- Registra IR dedo-duro (0,005%) no Kafka

### Passo 5 — Verificar Carteira

**`GET /api/Clientes/1/carteira`**

Veja a posição de cada ativo com quantidade, preço médio e valor atual.

### Passo 6 — Rentabilidade Detalhada

**`GET /api/Clientes/1/rentabilidade`**

Retorna histórico de aportes (parcelas 1/3, 2/3, 3/3) e evolução da carteira ao longo do tempo.

### Passo 7 — Consultar Resíduos na Conta Master

**`GET /api/Admin/conta-master/custodia`**

Veja as ações que ficaram na conta master após a distribuição (resíduos de arredondamento).

### Passo 8 (Opcional) — Consultar Datas de Compra

**`GET /api/Compras/datas/2026/3`**

Retorna os dias 5, 15 e 25 ajustados para dia útil.

### Passo 9 (Opcional) — Rebalancear por Desvio

**`POST /api/Rebalanceamento/desvio?limiar=5`**

Verifica se algum ativo desviou mais de 5pp do alvo e rebalanceia vendendo sobre-alocados e comprando sub-alocados.

## Endpoints da API

| Método | Endpoint | Descrição | Regras |
|---|---|---|---|
| `GET` | `/api/Health` | Health check | - |
| `POST` | `/api/Cotacoes/importar` | Upload COTAHIST | RN-055 a RN-062 |
| `GET` | `/api/Cotacoes/{ticker}/preco` | Preço de fechamento | - |
| `GET` | `/api/Cotacoes/data/{data}` | Cotações por data | - |
| `GET` | `/api/Cotacoes/ultimo-pregao` | Último pregão importado | - |
| `POST` | `/api/Clientes/adesao` | Adesão ao produto | RN-001 a RN-006 |
| `POST` | `/api/Clientes/{id}/saida` | Saída do produto | RN-007 a RN-009 |
| `PUT` | `/api/Clientes/{id}/valor-mensal` | Alterar aporte mensal | RN-011 a RN-013 |
| `GET` | `/api/Clientes/{id}/carteira` | Consultar carteira | RN-063 a RN-070 |
| `GET` | `/api/Clientes/{id}/rentabilidade` | Rentabilidade detalhada | RN-063 a RN-070 |
| `POST` | `/api/Admin/cestas` | Criar cesta Top Five (+ rebalanceamento auto) | RN-014 a RN-019 |
| `GET` | `/api/Admin/cestas/ativa` | Cesta ativa | - |
| `GET` | `/api/Admin/cestas/{id}` | Cesta por ID | - |
| `GET` | `/api/Admin/cestas/historico` | Histórico de cestas | RN-019 |
| `GET` | `/api/Admin/conta-master/custodia` | Custódia master (resíduos) | RN-039/040 |
| `POST` | `/api/Compras/executar` | Executar compra programada | RN-020 a RN-044 |
| `GET` | `/api/Compras/datas/{ano}/{mes}` | Datas de compra do mês | RN-020 |
| `POST` | `/api/Rebalanceamento/mudanca-cesta` | Rebalancear por mudança | RN-045 a RN-049 |
| `POST` | `/api/Rebalanceamento/desvio` | Rebalancear por desvio | RN-050 a RN-052 |

## Regras de Negócio Implementadas

O sistema implementa **70 regras de negócio** (RN-001 a RN-070) organizadas em:

- **RN-001 a RN-013**: Gestão de clientes (adesão, saída, alteração de valor, CPF válido)
- **RN-014 a RN-019**: Cesta Top Five (5 ativos, soma 100%, histórico)
- **RN-020 a RN-044**: Motor de Compra Programada (consolidação, lote/fracionário, distribuição proporcional, preço médio)
- **RN-045 a RN-052**: Rebalanceamento (mudança de cesta, desvio de proporção)
- **RN-053 a RN-062**: Apuração fiscal e Kafka (IR dedo-duro 0,005%, IR vendas >R$20k = 20%)
- **RN-063 a RN-070**: Consulta de carteira e rentabilidade

## Testes

O projeto possui **113 testes unitários** cobrindo:

| Categoria | Testes | Cobertura |
|---|---|---|
| Parser COTAHIST | 11 | Layout posicional, filtros TIPREG/TPMERC, preços /100 |
| Domínio: Cliente | 10 | CPF, adesão, saída, valor mensal |
| Domínio: Custódia | 8 | Preço médio, adicionar/remover ações |
| Domínio: Cesta | 13 | 5 ativos, soma 100%, ativação/desativação |
| Domínio: EventoIR | 8 | Dedo-duro, IR vendas, isenção R$20k |
| Serviço: Cliente | 13 | CRUD, validações, carteira, rentabilidade |
| Serviço: Cesta | 11 | Criar, ativar, histórico, custódia master, RN-019 |
| Serviço: Motor de Compra | 22 | Consolidação, lote/fracionário, distribuição, resíduos |
| Serviço: EventoIR | 8 | Kafka OK/falha, reprocessamento, resiliência |
| Serviço: Rebalanceamento | 9 | Mudança de cesta, desvio, apuração fiscal |

```bash
dotnet test --verbosity normal
```

## Decisões Técnicas

- **Kafka com resiliência**: Eventos de IR são salvos no banco primeiro, publicados no Kafka depois. Se o broker falhar, o evento fica marcado como "não publicado" para reprocessamento posterior.
- **Distribuição com TRUNCAR**: Quantidades fracionárias são truncadas (não arredondadas). Resíduos permanecem na conta master.
- **Preço médio**: Recalculado apenas em operações de compra. Vendas não alteram o preço médio.
- **Lote padrão vs fracionário**: Compras ≥100 ações vão para o mercado à vista; 1-99 ações vão para o fracionário (sufixo F no ticker).
- **RN-019 automático**: Ao criar uma nova cesta, o rebalanceamento por mudança de cesta é disparado automaticamente para todos os clientes ativos.
- **Migrations automáticas**: O EF Core aplica migrations no startup para facilitar demonstração.

## Estrutura do Projeto

```
sistema-compra-programada-b3/
├── docker-compose.yml
├── src/
│   ├── CompraProgramada.API/           # Controllers, Program.cs, Swagger
│   ├── CompraProgramada.Application/   # Services, DTOs, Interfaces
│   ├── CompraProgramada.Domain/        # Entities, Value Objects, Enums
│   └── CompraProgramada.Infrastructure/# EF Core, Repositories, Kafka, Parsers
├── tests/
│   ├── CompraProgramada.UnitTests/     # 113 testes unitários
│   └── CompraProgramada.IntegrationTests/
└── PLANNING.md
```
