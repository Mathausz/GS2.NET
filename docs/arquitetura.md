# Arquitetura — Nexus Verde

Veja os diagramas (Mermaid) no [README](../README.md): visão em camadas, modelo de dados e fluxo principal.

## Dependências entre projetos
`Api → Infrastructure → Application → Domain` (a Api também referencia Application/Domain).
`Domain` e `Application` **não** dependem de EF Core, ASP.NET nem do Oracle: as regras são testáveis em isolamento.

## Pipeline HTTP (ordem importa)
1. `UseForwardedHeaders` — IP real do cliente atrás de proxy/PaaS
2. `CorrelationIdMiddleware` — gera/propaga `X-Correlation-Id`
3. `UseExceptionHandler` — `GlobalExceptionHandler` → `ProblemDetails`
4. `UseSerilogRequestLogging`
5. Swagger / CORS (se configurado)
6. `UseAuthentication` (JWT)
7. `UseRateLimiter` — 429 antes de qualquer trabalho caro
8. `UseAuthorization` (políticas por perfil)
9. `IdempotencyMiddleware` — só em endpoints marcados com `RequireIdempotency()`, depois de autenticar (chave isolada por usuário)
10. Endpoints (`MapGroup` por recurso, `ValidationFilter<T>`)

## Códigos HTTP
| Código | Quando |
|---|---|
| 200 / 201 / 204 | Sucesso / criado / sem conteúdo |
| 400 | Validação (DataAnnotations), JSON inválido, `Idempotency-Key` ausente, `pageSize` > 50 |
| 401 | Token ausente, inválido ou expirado; login incorreto |
| 403 | Perfil sem permissão, ou recurso de outro usuário/órgão |
| 404 | Recurso inexistente |
| 409 | E-mail já cadastrado; solicitação já em atendimento; violação de integridade |
| 422 | Regra de negócio violada; `Idempotency-Key` reutilizada com outro corpo |
| 429 | Rate limit excedido (`Retry-After`) |
| 500 | Erro inesperado (detalhe oculto fora de Development; use o `correlationId`) |
