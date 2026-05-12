# 13 · Checklists de verificación

## 13.1 Antes de hacer commit

- ¿Hay secretos, contraseñas, API keys reales en el diff?

- ¿Hay console.log, Console.WriteLine, debugger o código de prueba?

- ¿Pasa el lint y el format?

- ¿Compila localmente?

- ¿Pasan los tests que tocan el área modificada?

- ¿El mensaje de commit sigue conventional commits?

## 13.2 Antes de hacer merge a main

- ¿El PR fue revisado por al menos una persona?

- ¿Pasaron todos los checks de CI?

- ¿Hay tests para el código nuevo?

- ¿La documentación está actualizada?

- ¿Las variables de entorno nuevas están documentadas?

- ¿Las migraciones de BD son reversibles?

- ¿Hay cambios breaking? Si sí, ¿se documentaron y se incrementó MAJOR?

## 13.3 Antes de desplegar a producción

- ¿Se probó en staging con datos reales (no producción) primero?

- ¿Las migraciones se probaron en staging?

- ¿Se agregaron las variables de entorno nuevas a producción?

- ¿Los secretos nuevos están en Key Vault / Secrets Manager?

- ¿Hay un plan de rollback documentado?

- ¿La ventana de deploy es la correcta (no viernes 5 PM)?

- ¿Está alguien on-call por si algo falla?

## 13.4 Diagnóstico rápido cuando algo falla en producción

- Health checks: /health responde 200 en cada servicio?

- Logs: en Seq buscar errores en los últimos 15 minutos.

- Métricas: latencia p95, tasa de error, uso de CPU/memoria.

- Conexiones BD: ¿hay pool agotado? ¿queries lentas?

- Servicios externos: ¿Stripe está caído? ¿el SMTP responde?

- Última versión desplegada: ¿este bug existía antes? ¿coincide con un deploy reciente?



> Fuente: *Learning DevSecOps* (Mark Rendell) — Ch.5 Security Gates and Deployment Checklists

---

*Rogelio Arriaga Gonzalez*
