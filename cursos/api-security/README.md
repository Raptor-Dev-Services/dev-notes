# Seguridad de APIs: atacar y defender

Curso de **Raptor Dev Services Academy**. Las clases como presentación, el quiz y el avance están en [academy.raptorcloud.dev/api-security/](https://academy.raptorcloud.dev/api-security/); aquí está todo el material en Markdown, y las lecturas son notas de este mismo repositorio. Los libros se citan por capítulo: cada quien consigue su copia.

**Prerrequisito:** HTTP y una API hecha por ustedes. Recomendable: la sesión 3 de Titulación 2026 o la unidad "JWT y autenticación" del curso [Backend con .NET](../dotnet-backend/README.md).
**Objetivo:** probar una API como la probaría quien la revisa (superficie, autenticación, autorización, entradas, lógica de negocio, GraphQL) y corregir cada hallazgo en el código con una prueba que lo demuestre.
**Formato:** 6 módulos de 2 horas (12 h), con clase, quiz, práctica, tarea y glosario.
**Para quién:** desarrolladores de backend que quieren encontrar sus fallas antes que otros.

> **Alcance del curso: metodología y defensa.** Cada clase de falla del OWASP API Security Top 10 de 2023 se explica (qué es y por qué ocurre), se detecta, se corrige en C# y ASP.NET Core y se **verifica** con una prueba que se ve fallar al quitar la corrección. No hay procedimientos de explotación paso a paso, cargas de ataque ni herramientas contra sistemas ajenos. Todo se prueba con **código propio** (la API de referencia de este repositorio) o con aplicaciones hechas para practicar, citando su guía oficial. Sin autorización por escrito no se prueba nada que no sea tuyo.

> **Laboratorio.** La API de referencia (datos ficticios, .NET 10) corre con `dotnet test`. Las prácticas opcionales con crAPI (OWASP) y con *Damn Vulnerable GraphQL Application* van en una máquina virtual propia y aislada; pueden compartir la red solo anfitrión `192.168.56.0/24` de los cursos `/blue-team/` y `/pentesting-with-kali/`. Esas prácticas **no se ejecutaron** al escribir el curso: se remiten a la guía oficial de cada proyecto.

**Traslape con otros cursos.** Este curso no repite `/web-security/` (el Top 10 de aplicaciones web) ni la unidad de JWT de `/dotnet-backend/` (cómo se emite un token): aquí se **audita** cómo se valida.

---

## Módulo 1. Superficie e inventario (2 h)

> La API que nadie documentó es la que nadie protegió.

**Material:** [clase](m01-surface-inventory/slides.md), [práctica](m01-surface-inventory/practica.md), [tarea](m01-surface-inventory/tarea.md), [glosario](m01-surface-inventory/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** cómo funciona la seguridad de una API, el OWASP API Security Top 10 de 2023 como mapa, API9 (inventario) y la superficie vista desde la defensa.
- [ ] **Lectura:** [`04-backend/15-openapi.md`](../../04-backend/15-openapi.md).
- [ ] **Práctica:** el inventario de rutas calculado del código y comparado con el contrato; verlo fallar con una ruta olvidada.
- [ ] **Tarea:** inventario de una API propia, con su prueba y la ficha de dos entornos.
- **Libros:** *Hacking APIs* (Ball, 2022), caps. 0 a 3 y 6. *Pentesting APIs* (Harley, 2024), caps. 1 a 3. *Web Application Security* (Hoffman, 2.ª ed.), caps. 2 a 5. *Attack Surface Management* (Eddings y Kaufmann, 2025), caps. 1, 2, 4 y 5.

**Al terminar puedes:** levantar el inventario de una API y escribir la prueba que falla cuando aparece una ruta sin documentar.

---

## Módulo 2. Autenticación, JWT y OAuth (2 h)

> Un JWT firmado con una llave débil es una contraseña escrita en el token.

**Material:** [clase](m02-authn-jwt-oauth/slides.md), [práctica](m02-authn-jwt-oauth/practica.md), [tarea](m02-authn-jwt-oauth/tarea.md), [glosario](m02-authn-jwt-oauth/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** API2, qué garantiza un JWT y qué no, la validación estricta (RFC 8725), OAuth 2.0 y OpenID Connect, y un login endurecido.
- [ ] **Lectura:** [`04-backend/21-jwt.md`](../../04-backend/21-jwt.md), [`03-arquitectura/08-keycloak.md`](../../03-arquitectura/08-keycloak.md).
- [ ] **Práctica:** un token malo por cada control de validación, vistos fallar al quitar el control; la llave de 256 bits validada al arrancar.
- [ ] **Tarea:** auditoría de autenticación de una API, con dos pruebas nuevas.
- **Libros:** *Pentesting APIs*, cap. 4. *The JWT Handbook* (Peyrott, 2018), secciones "JSON Web Tokens in Detail" y "JSON Web Signatures". *Keycloak* (Thorgersen y Silva, 2.ª ed.), caps. 3 a 5. *Hacking APIs*, cap. 8.

**Al terminar puedes:** auditar cómo su API emite y valida tokens y probar cada control.

---

## Módulo 3. Autorización rota: BOLA, BOPLA y BFLA (2 h)

> Estar autenticado no significa poder ver ese registro.

**Material:** [clase](m03-bola-bfla-bopla/slides.md), [práctica](m03-bola-bfla-bopla/practica.md), [tarea](m03-bola-bfla-bopla/tarea.md), [glosario](m03-bola-bfla-bopla/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** API1, API3 y API5: objeto, propiedad y función; la matriz de autorización como prueba.
- [ ] **Lectura:** [`04-backend/27-rbac.md`](../../04-backend/27-rbac.md), [`04-backend/35-tenant-leakage-tests.md`](../../04-backend/35-tenant-leakage-tests.md).
- [ ] **Práctica:** cuatro "verlo fallar" con los interruptores del laboratorio y una ruta nueva con sus filas de matriz.
- [ ] **Tarea:** la matriz de autorización de una API propia (15 filas o más).
- **Libros:** *Hacking APIs*, caps. 10 y 11. *Pentesting APIs*, cap. 4. *Web Application Security*, caps. 15 y 33. *Bug Bounty Bootcamp* (Li, 2021), caps. 10 y 17.

**Al terminar puedes:** probar cada endpoint con dos usuarios distintos y demostrar que no se cruzan.

---

## Módulo 4. Inyección, errores y límites (2 h)

> Un mensaje de error detallado le cuenta al atacante cómo está hecha su API.

**Material:** [clase](m04-injection-errors-limits/slides.md), [práctica](m04-injection-errors-limits/practica.md), [tarea](m04-injection-errors-limits/tarea.md), [glosario](m04-injection-errors-limits/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** validación de entrada, inyección y consultas parametrizadas, errores con ProblemDetails (RFC 9457) y API4: topes, tamaño de cuerpo y límites de tasa.
- [ ] **Lectura:** [`04-backend/08-problem-details.md`](../../04-backend/08-problem-details.md), [`04-backend/12-rate-limiting.md`](../../04-backend/12-rate-limiting.md), [`04-backend/06-validation.md`](../../04-backend/06-validation.md).
- [ ] **Práctica:** la consulta concatenada, el error que filtra y la paginación sin tope, vistos fallar.
- [ ] **Tarea:** entradas, errores y límites de una API propia, con sus pruebas.
- **Libros:** *Pentesting APIs*, caps. 5 a 7. *Web Application Security*, caps. 13, 14, 31 y 32. *Building Secure and Reliable Systems* (Adkins et al., 2020), cap. 10.

**Al terminar puedes:** demostrar que su API no filtra información al fallar y aguanta un abuso de peticiones.

---

## Módulo 5. Lógica de negocio, destinos y exposición de datos; GraphQL (2 h)

> Ningún escáner encuentra que el descuento se puede aplicar dos veces.

**Material:** [clase](m05-logic-ssrf-exposure-graphql/slides.md), [práctica](m05-logic-ssrf-exposure-graphql/practica.md), [tarea](m05-logic-ssrf-exposure-graphql/tarea.md), [glosario](m05-logic-ssrf-exposure-graphql/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** API6 (flujos sensibles), API7 (SSRF), API10 (consumo inseguro de APIs) y la superficie de GraphQL desde la defensa.
- [ ] **Lectura:** [`04-backend/45-idempotency-keys.md`](../../04-backend/45-idempotency-keys.md), [`12-seguridad/09-graphql-seguridad.md`](../../12-seguridad/09-graphql-seguridad.md) (nota nueva).
- [ ] **Práctica:** el cupón repetible, la lista de destinos y las 20 peticiones simultáneas; crAPI opcional.
- [ ] **Tarea:** flujos de negocio y destinos de una API propia, con sus pruebas.
- **Libros:** *Pentesting APIs*, caps. 8 y 9. *Web Application Security*, caps. 18 y 36. *Black Hat GraphQL* (Aleks y Farhi, 2023), caps. 3 a 7. *Bug Bounty Bootcamp*, caps. 12, 13, 17 y 24.

**Al terminar puedes:** pensar como alguien que abusa de las reglas, no solo del código, y cerrarlo con una prueba.

---

## Módulo 6. Corregir, probar y reportar (2 h)

> Un hallazgo cerrado sin prueba vuelve en el siguiente merge.

**Material:** [clase](m06-fix-test-report/slides.md), [práctica](m06-fix-test-report/practica.md), [tarea](m06-fix-test-report/tarea.md), [glosario](m06-fix-test-report/glosario.md). El quiz está en la plataforma.

- [ ] **Clase:** API8 (configuración), cada clase del Top 10 con su control y su prueba, modelado de amenazas continuo, pruebas de seguridad en CI y el informe.
- [ ] **Lectura:** [`12-seguridad/10-pruebas-de-seguridad-de-apis.md`](../../12-seguridad/10-pruebas-de-seguridad-de-apis.md) (nota nueva), [`04-backend/testing/03-integration-tests.md`](../../04-backend/testing/03-integration-tests.md).
- [ ] **Práctica:** la suite completa, una mutación vista caer y el informe de un hallazgo.
- [ ] **Tarea (proyecto final):** el informe completo de la API propia.
- **Libros:** *Pentesting APIs*, cap. 10. *Web Application Security*, caps. 20 a 22, 24 y 25. *Threat Modeling* (Tarandach y Coles, 2020), caps. 1, 2 y 5.

**Al terminar puedes:** cerrar una vulnerabilidad de forma que no regrese.

---

## Proyecto final

La prueba de seguridad completa de una API propia: inventario, hallazgos priorizados, correcciones con su prueba vista fallar y el informe.

## Código de referencia

En [`referencia/`](referencia/) está la API de práctica con sus pruebas, completa y compilable con .NET 10: el código de las clases sale de ahí. Para comprobarla: `dotnet test --filter "Categoria=Seguridad"` dentro de esa carpeta. Los interruptores `Lab:*` reintroducen a propósito una falla clásica para ver fallar su prueba; existen solo en esta aplicación y arrancan apagados. Úsenla para comparar después de intentarlo, no antes.

## Libros base

| Libro | Autor | Año | Para qué |
|---|---|---|---|
| Pentesting APIs | Maurício Harley | 2024 | Columna del curso |
| Hacking APIs | Corey J. Ball | 2022 | Anatomía y fallas de las APIs |
| Web Application Security (2.ª ed.) | Andrew Hoffman | 2023 | Ataque y defensa web |
| Black Hat GraphQL | Nick Aleks y Dolev Farhi | 2023 | GraphQL |
| The JWT Handbook | Sebastián E. Peyrott | 2018 | Tokens |
| Keycloak (2.ª ed.) | Stian Thorgersen y Pedro Igor Silva | 2023 | OAuth y OpenID Connect |
| Threat Modeling | Izar Tarandach y Matthew J. Coles | 2020 | Modelado continuo |
| Bug Bounty Bootcamp | Vickie Li | 2021 | Lectura de apoyo |

## Lecturas de profundización

Del resto de la biblioteca de ciberseguridad, estos libros sirven a una o más unidades; se citan por capítulo de su índice.

| Libro | Capítulos | Módulos |
|---|---|---|
| *Attack Surface Management* (Eddings y Kaufmann, 2025) | 1, 2, 4 a 8 | 1 |
| *Defensive Security Handbook* (Berlin et al., 2024) | 2, 15, 18, 19, 22 | 1, 2, 6 |
| *Building Secure and Reliable Systems* (Adkins et al., 2020) | 5, 10, 12 a 14 | 3, 4, 6 |
| *Zero Trust Networks* (Rais et al., 2024) | 4, 6, 7 | 2, 3 |
| *Identity-Native Infrastructure Access Management* (Kontsevoy et al., 2023) | 4, 5 | 2, 3 |
| *Security and Microservice Architecture on AWS* (Raje, 2021) | 2, 6, 7 | 2, 3 |
| *Serious Cryptography* (Aumasson) | 6 a 8 | 2 |
| *Practical Cybersecurity Architecture* (Moyle y Kelley, 2020) | 6 | 6 |
| *Software Supply Chain Security* (Crossley, 2024) | 4 a 6 | 6 |
| *Executive's Cybersecurity Program Handbook* (Brown, 2023) | 11, 12 | 6 |
| *97 Things Every Application Security Professional Should Know* (Kaur y Wang, 2024) | 29, 31 | 6 |
| *The Vulnerability Researcher's Handbook* (Strout, 2023) | 4, 8 | 6 |
| *Practical Cloud Security* (Dotson, 2023) | 4, 5 | 2, 6 |
| *Learning Kali Linux* (Messier, 2024) | 8, 13 | 6 |
| *The Ultimate Kali Linux Book* (Singh, 2022) | 15, 16 | 6 |

Los demás libros de ciberseguridad de la biblioteca (malware, forense, ICS, nube de AWS, ingeniería social y similares) se revisaron y no sirven a este curso. Los *CIS Benchmarks* (Docker, Kubernetes y Ubuntu 24.04) son PDF cuyo índice no se pudo leer al escribirlo, así que no se citan por sección; para endurecer el contenedor donde corre una API, véase el curso `/devops-essentials/`.

---

*Rogelio Arriaga Gonzalez*
