# Roadmap — SaaS Multi-Tenant

**Prerequisito:** [Roadmap Arquitectura](roadmap-arquitectura.md) completado.
**Objetivo:** construir un producto SaaS multi-tenant completo con el back-template como base.
**Duración estimada:** 8-12 semanas (el roadmap más largo — es el objetivo final del repo).

---

## Fase 1 — Fundamentos de multi-tenancy (semana 1)

> Entender el modelo antes de implementar nada. Una decisión equivocada aquí es muy costosa.

- [ ] [Multi-Tenancy Logic](../04-backend/24-multi-tenancy-logic.md) — modelo conceptual, estrategias de aislamiento (shared DB, schema separado, DB separada)
- [ ] [Tenant Branch Logic](../04-backend/25-tenant-branch-logic.md) — lógica de branches dentro de un tenant
- [ ] [Tenant Subdomain Routing](../03-arquitectura/10-tenant-subdomain-routing.md) — middleware que resuelve tenant desde `alfacorp.miapp.com`

**Decisión clave:** elegir estrategia de aislamiento. El back-template usa shared DB con `tenant_id` en cada tabla.

---

## Fase 2 — Capa de datos multi-tenant (semana 1-2)

> El corazón técnico: que las queries nunca filtren datos de otro tenant.

- [ ] [EF Core Multi-Tenancy](../04-backend/22-ef-core-multi-tenancy.md) — Global Query Filters, ITenantContextAccessor
- [ ] [Dapper](../04-backend/23-dapper.md) — tenant context en queries Dapper, TenantParams helper
- [ ] [Migrations Multi-Tenant](../05-bases-de-datos/07-migrations-multi-tenant.md) — estrategias de migration por tenant
- [ ] [Row Level Security](../05-bases-de-datos/08-row-level-security.md) — RLS en PostgreSQL como segunda línea de defensa

**Al terminar esta fase:** ninguna query puede acceder a datos de otro tenant, verificado a nivel de base de datos.

---

## Fase 3 — Identidad y acceso (semana 2-3)

> Quién puede hacer qué, dentro de cada tenant.

- [ ] [JWT](../04-backend/21-jwt.md) — claims `tenant_id` y `branch_id` en el token
- [ ] [RBAC](../04-backend/27-rbac.md) — roles por tenant (flat/jerárquico), permisos granulares
- [ ] [Keycloak](../03-arquitectura/08-keycloak.md) — delegación de identidad a un IdP externo (opcional)
- [ ] [SSO / OIDC](../04-backend/39-sso-oidc.md) — flujos OIDC por tenant, federación con Google/AD
- [ ] [2FA / MFA](../04-backend/38-2fa-mfa.md) — TOTP, códigos de respaldo, QR code
- [ ] [Session Management](../04-backend/40-session-management.md) — refresh tokens, revocación por dispositivo

**Al terminar esta fase:** sistema de identidad completo con roles por tenant, SSO y MFA.

---

## Fase 4 — Ciclo de vida del tenant (semana 3-4)

> Cómo entra un nuevo cliente, cómo invita a su equipo.

- [ ] [Tenant Onboarding](../04-backend/26-tenant-onboarding.md) — flujo completo de registro: tenant → admin → configuración inicial
- [ ] [User Invitation](../04-backend/28-user-invitation.md) — invitación por email, token de un solo uso, límite de seats
- [ ] [Tenant Config](../04-backend/30-tenant-config.md) — configuración personalizada por tenant (features, branding, límites)
- [ ] [Super Admin](../04-backend/43-super-admin.md) — panel cross-tenant para el equipo del SaaS (impersonate, debug)

**Al terminar esta fase:** un nuevo tenant puede registrarse, configurar su cuenta e invitar a su equipo sin intervención manual.

---

## Fase 5 — Monetización y planes (semana 4-5)

> El modelo de negocio implementado como código.

- [ ] [Planes y Límites](../04-backend/31-planes-limites.md) — planes de suscripción, feature flags por plan, enforcement de límites
- [ ] [Stripe Billing](../04-backend/32-stripe-billing.md) — Customer, Subscription, webhooks de pago, portal del cliente
- [ ] [Rate Limiting Tenant](../04-backend/33-rate-limiting-tenant.md) — rate limiting diferenciado por plan

**Al terminar esta fase:** los tenants pagan, tienen límites según su plan y el SaaS tiene ingresos recurrentes.

---

## Fase 6 — Integraciones externas (semana 5-6)

> El tenant conecta el SaaS con sus propias herramientas.

- [ ] [API Keys](../04-backend/34-api-keys.md) — generación, hash, scopes, rotación, revocación
- [ ] [Webhooks Salientes](../04-backend/36-webhooks-salientes.md) — webhooks outbound con reintentos, firmas HMAC
- [ ] [Emails Transaccionales](../04-backend/37-emails-transaccionales.md) — emails por tenant vía SendGrid/SES con templates

**Al terminar esta fase:** el SaaS puede integrarse con cualquier herramienta externa del tenant via API Keys y webhooks.

---

## Fase 7 — Tiempo real y background (semana 6-7)

> Notificaciones en vivo y procesamiento asíncrono aislado por tenant.

- [ ] [SignalR Realtime](../04-backend/42-signalr-realtime.md) — grupos por tenant/branch/usuario, reconexión, fallback
- [ ] [Background Jobs Multi-Tenant](../04-backend/41-background-jobs-multitenant.md) — Hangfire con TenantContext en jobs
- [ ] [Audit Trail](../04-backend/29-audit-trail.md) — registro inmutable de acciones por tenant

**Al terminar esta fase:** notificaciones en tiempo real y tareas en background completamente aisladas por tenant.

---

## Fase 8 — Seguridad, privacidad y confiabilidad (semana 7-8)

> Lo que convierte un MVP en un producto enterprise-ready.

- [ ] [Idempotency Keys](../04-backend/45-idempotency-keys.md) — prevención de operaciones duplicadas en pagos y acciones críticas
- [ ] [Tenant Leakage Tests](../04-backend/35-tenant-leakage-tests.md) — tests que garantizan el aislamiento entre tenants
- [ ] [GDPR / Data Export](../04-backend/44-gdpr-data-export.md) — exportación completa de datos del tenant bajo petición
- [ ] [File Storage Tenant](../10-cloud/09-file-storage-tenant.md) — S3 por tenant, presigned URLs, lifecycle

**Al terminar esta fase:** el SaaS cumple con GDPR, previene operaciones duplicadas y tiene tests de aislamiento automatizados.

---

## Resumen: las piezas del SaaS Multi-Tenant

```
Tenant resolve      → SubdomainMiddleware → ITenantContextAccessor
                                                    ↓
Data isolation      → Global Query Filters (EF Core) + TenantParams (Dapper) + RLS (PostgreSQL)
                                                    ↓
Identity            → JWT (tenant_id + branch_id) → RBAC → SSO/OIDC → 2FA
                                                    ↓
Lifecycle           → Onboarding → Invitation → Config → Super Admin
                                                    ↓
Monetization        → Plans → Limits → Stripe → Rate Limiting per plan
                                                    ↓
Integrations        → API Keys → Webhooks → Emails
                                                    ↓
Realtime            → SignalR groups → Hangfire with TenantContext → Audit Trail
                                                    ↓
Trust               → Idempotency → Leakage Tests → GDPR → File Storage
```

---

*Duración total estimada: 8-12 semanas — Nivel objetivo: senior / tech lead SaaS*
