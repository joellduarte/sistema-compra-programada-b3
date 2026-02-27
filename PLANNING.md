# Planejamento - Sistema de Compra Programada de Ações (Top Five)

## Visão Geral
Desafio técnico Itaú Corretora: implementação de um sistema de compra programada de ações
com arquitetura Clean Architecture + DDD, utilizando .NET 8, MySQL, Apache Kafka e REST API.

## Plano de Execução em 10 Etapas

| # | Etapa | Status | Regras de Negócio |
|---|-------|--------|-------------------|
| 1 | Setup Solution Clean Architecture + Docker Compose | [x] Concluída | - |
| 2 | Entidades de Domínio, Enums, Value Objects e Interfaces de Repositório | [x] Concluída | RN-001 (CPF), RN-043 (Preço Médio) |
| 3 | EF Core com MySQL: DbContext, Fluent API Mappings e Migration Inicial | [x] Concluída | - |
| 4 | Parser COTAHIST + Serviço de Cotações + Upload via Stream | [x] Concluída | RN-055 a RN-062 (COTAHIST) |
| 5 | API do Cliente: Adesão, Saída, Alterar Valor, Carteira e Rentabilidade | [x] Concluída | RN-001 a RN-013, RN-063 a RN-070 |
| 6 | Admin API: CRUD Cesta Top Five + Histórico de Recomendações | [x] Concluída | RN-014 a RN-019 |
| 7 | Motor de Compra Programada (core do sistema) | [ ] Pendente | RN-023 a RN-042, RN-044 a RN-048 |
| 8 | Kafka Messaging: Producer IR dedo-duro + IR sobre vendas | [ ] Pendente | RN-049 a RN-054 |
| 9 | Motor de Rebalanceamento | [ ] Pendente | RN-044 a RN-048 |
| 10 | Testes (cobertura >= 70%) + README + Swagger polish | [ ] Pendente | - |

## Stack Tecnológica
- **Backend**: .NET 8.0 (C#)
- **Banco de Dados**: MySQL 8.0 (via Docker, porta 3307)
- **Mensageria**: Apache Kafka (Confluent)
- **ORM**: Entity Framework Core (Pomelo MySQL)
- **Testes**: xUnit + Moq
- **Documentação API**: Swagger/OpenAPI

## Arquitetura
```
CompraProgramada.Domain        → Entidades, Value Objects, Enums, Interfaces
CompraProgramada.Application   → DTOs, Services, Interfaces de Serviço
CompraProgramada.Infrastructure→ EF Core, Repositories, Parsers, Kafka
CompraProgramada.API           → Controllers, Middleware, DI, Program.cs
CompraProgramada.UnitTests     → Testes unitários (Domain + Service)
```

## Progresso de Testes
- **60 testes passando** (0 falhas)
  - 11 testes do Parser COTAHIST
  - 10 testes de domínio (Cliente)
  - 8 testes de domínio (Custodia)
  - 13 testes de domínio (CestaRecomendacao)
  - 11 testes de serviço (ClienteService)
  - 7 testes de serviço (CestaService)
