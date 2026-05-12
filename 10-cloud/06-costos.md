# 06 · Optimización de costos cloud

> Fuente: *AWS Certified Solutions Architect – Associate Guide* Ch.18 (Reserved Instances, Billing and cost management, AWS Organizations)

## Problema que resuelve

AWS cobra por uso — sin gestión activa los costos crecen con el tiempo por recursos olvidados, sobredimensionamiento y falta de estrategias de ahorro. Este documento cubre las palancas principales para controlar el gasto en una startup SaaS.

## Modelos de precio en EC2

| Modelo | Ahorro vs On-Demand | Compromiso | Caso de uso |
|--------|---------------------|------------|-------------|
| On-Demand | referencia (0%) | ninguno | desarrollo, cargas impredecibles |
| Reserved Instances (1 año) | ~40% | 1 año | producción estable |
| Reserved Instances (3 años) | ~60% | 3 años | infraestructura base establecida |
| Savings Plans (Compute) | ~66% | 1-3 años, más flexible que RI | reemplazo moderno de RI |
| Spot Instances | ~70-90% | pueden interrumpirse | batch jobs, CI runners, workers no críticos |

### Savings Plans vs Reserved Instances

Savings Plans son más flexibles: el compromiso es un gasto mínimo por hora (ej. $0.10/hr), y aplica automáticamente a cualquier instancia EC2, Fargate o Lambda que uses — sin especificar el tipo de instancia o región de antemano.

```
Recomendación para startup:
- Workloads de producción estables → Savings Plans Compute (1 año)
- CI runners y ambientes de dev → On-Demand o Spot
- Nunca Reserved Instances en development
```

## Rightsizing — dimensionamiento correcto

El error más común es elegir instancias grandes "por si acaso". AWS Cost Explorer tiene recomendaciones de rightsizing basadas en uso real:

```bash
# ver recomendaciones de rightsizing con AWS CLI
aws ce get-rightsizing-recommendation \
  --service EC2 \
  --configuration RecommendationTarget=SAME_INSTANCE_FAMILY
```

Proceso:
1. Desplegar con instancia sobredimensionada
2. Monitorear CPU, memoria y red por 2 semanas
3. Hacer rightsizing a la instancia mínima que no supere 70% de CPU en picos
4. Revisar trimestralmente

## Herramientas de billing

### AWS Cost Explorer

Visualiza el gasto histórico por servicio, cuenta y etiqueta.

```bash
# costo del mes actual por servicio
aws ce get-cost-and-usage \
  --time-period Start=2026-05-01,End=2026-05-31 \
  --granularity MONTHLY \
  --metrics BlendedCost \
  --group-by Type=DIMENSION,Key=SERVICE
```

### AWS Budgets

Alerta cuando el gasto supera un umbral:

```bash
# crear budget de $200/mes con alerta al 80%
aws budgets create-budget \
  --account-id 123456789012 \
  --budget '{
    "BudgetName": "gtm-monthly-budget",
    "BudgetLimit": {"Amount": "200", "Unit": "USD"},
    "TimeUnit": "MONTHLY",
    "BudgetType": "COST"
  }' \
  --notifications-with-subscribers '[{
    "Notification": {
      "NotificationType": "ACTUAL",
      "ComparisonOperator": "GREATER_THAN",
      "Threshold": 80
    },
    "Subscribers": [{"SubscriptionType": "EMAIL", "Address": "rogelio@raptordev.io"}]
  }]'
```

### AWS Trusted Advisor

Revisa automáticamente la cuenta y da recomendaciones de costo, seguridad y rendimiento. Categorías de costo:

- Instancias EC2 infrautilizadas (CPU < 10% sostenido)
- Load balancers sin instancias asociadas
- Elastic IPs sin asignar
- Snapshots de EBS muy antiguos

## Etiquetado (tagging) para control de costos

Sin tags es imposible saber qué proyecto o ambiente genera cada costo.

```bash
# etiquetar recursos al crearlos
aws ec2 create-tags \
  --resources i-1234567890 \
  --tags Key=Project,Value=GTM Key=Environment,Value=production Key=Owner,Value=raptor-dev
```

Convención de tags recomendada:

| Tag | Valores ejemplo |
|-----|-----------------|
| `Project` | GTM, front-template, client-a |
| `Environment` | production, staging, development |
| `Owner` | raptor-dev, client-a |
| `CostCenter` | engineering, ops |

Activar Cost Allocation Tags en Billing Console para que aparezcan en Cost Explorer.

## Recursos que generan costo oculto

Recursos que se olvidan y siguen cobrando:

| Recurso | Costo mensual aprox. | Acción |
|---------|---------------------|--------|
| Elastic IP sin asignar | $3.65 | liberar si no se usa |
| NAT Gateway sin tráfico | $32 + transferencia | eliminar si no hay VPC privada activa |
| RDS detenida (después de 7 días) | se reinicia automáticamente | hacer snapshot y eliminar en dev |
| EBS snapshot acumulados | $0.05/GB/mes | lifecycle policy para eliminar los viejos |
| Load balancer sin targets | $16 | eliminar si el servicio se bajó |

## Estrategia para startup SaaS

```
Mes 1-6  (MVP):
  - Todo On-Demand
  - t3.micro para dev/staging
  - t3.small para producción inicial
  - Monitorear con Cost Explorer semanalmente

Mes 6-12 (producto validado):
  - Savings Plans Compute 1 año para producción
  - Rightsizing basado en métricas reales
  - Spot Instances para CI runners
  - Budget alert al 80% del gasto esperado

Año 2+:
  - Multi-account con AWS Organizations (prod / staging / dev separados)
  - Savings Plans 3 años para servicios estables
  - Reserved Instances para RDS (db.t3.medium+)
```

## AWS Organizations — cuentas separadas por ambiente

Separar producción y desarrollo en cuentas AWS distintas da:
- billing separado por ambiente
- blast radius reducido (un error en dev no puede afectar prod)
- políticas de seguridad distintas por cuenta

```bash
# crear cuenta hija para staging
aws organizations create-account \
  --email staging@raptordev.io \
  --account-name "GTM Staging"
```

## Cuándo usar / no usar

| Usar | No usar |
|------|---------|
| Savings Plans desde que la infraestructura de producción está estabilizada (>3 meses) | Reserved Instances antes de conocer el patrón de uso real |
| Spot Instances para CI runners y batch processing | Spot Instances para bases de datos o servicios que no toleran interrupciones |
| Budget alerts desde el día 1 | esperar la factura de fin de mes para revisar gastos |
| Rightsizing cada trimestre | sobredimensionar "por si acaso" de forma permanente |


---

*Rogelio Arriaga Gonzalez*
