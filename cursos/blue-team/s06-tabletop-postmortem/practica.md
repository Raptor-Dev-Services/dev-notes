# Práctica - S06 - Ejercicio de mesa: ransomware en Distribuidora Ejemplo

**Defensa y respuesta a incidentes**
**Duración estimada:** 2 horas 30 minutos (preparación 20 min, ejercicio 80 min, debrief y evaluación 50 min)

---

## Objetivo

Conducir y jugar un ejercicio de mesa completo, con roles, siete injects con reloj, una hoja de decisión por inject y un debrief inmediato. Las hojas que llenes son la materia prima de la tarea: el informe posterior y el postmortem.

La regla del curso: si no lo viste fallar, no lo probaste. Aquí lo que falla es el plan: tiene un contacto que ya no trabaja ahí y un paso que choca con lo que aprendiste de evidencia. El ejercicio está diseñado para que **lo descubran ustedes**.

---

## Caso hipotético

**Todo lo que sigue es un caso hipotético.** Distribuidora Ejemplo, sus personas, sus sistemas y sus atacantes no existen. Cualquier parecido con una empresa real es casualidad. Nadie ejecuta ningún comando durante el ejercicio: **se discute**.

---

## Materiales

- Esta hoja (el facilitador usa las partes 2 y 3; los jugadores solo la parte 1 y las hojas de la parte 4).
- Un reloj visible (un cronómetro proyectado).
- Una hoja de decisión impresa o digital **por inject** (parte 4).
- Una hoja de línea de tiempo para el anotador (parte 4).
- Tarjetas de rol (parte 1).

---

## Parte 1 - Lo que ve el equipo que juega

### La empresa

- Distribuidora Ejemplo: 45 personas, una oficina y una bodega. Vende al mayoreo.
- Un servidor de archivos con las carpetas de Finanzas, Clientes, Pedidos y Nóminas.
- Un sistema de pedidos y facturación en otro servidor, con adjuntos en el servidor de archivos.
- 20 computadoras de escritorio y portátiles.
- Un NAS con los respaldos nocturnos. El servidor de archivos lo tiene **siempre montado** como una unidad.
- Acceso remoto por VPN para el proveedor de TI y para la persona de soporte.
- Una persona de soporte interna y un proveedor de TI externo de medio tiempo.
- Un plan de respuesta a incidentes en un PDF de hace dos años (el extracto está en la parte 3).

### Los roles (una tarjeta por persona)

| Rol | Qué hace en el ejercicio |
|---|---|
| Mando del incidente | Coordina; decide quién hace qué; mantiene el reloj del equipo |
| Líder técnico (soporte interno) | Contiene, investiga, restaura |
| Proveedor de TI (externo) | Apoya al líder técnico; tiene acceso a la VPN |
| Comunicaciones | Mensajes a personal, clientes y proveedores |
| Legal y cumplimiento (asesor externo) | Obligaciones de aviso, contratos, datos personales |
| Dirección general | Decide gastos y si la empresa opera |
| Operaciones y bodega | Entregas, pedidos, modo manual |
| Finanzas y contabilidad | Nómina, facturación, impacto económico |

Equipos de 7 u 8: sobran roles, se combinan. Además hay un **facilitador** (que no juega) y un **anotador**, y un **observador** si hay más gente.

---

## Parte 2 - Guía del facilitador

*(Los jugadores no leen esta parte.)*

### Objetivos del ejercicio y criterios de evaluación (escritos antes)

| Obj. | Qué se quiere validar | Se ve cumplido cuando |
|---|---|---|
| O1 | Roles claros | En 5 minutos de reloj real hay una persona nombrada al mando y las decisiones se asignan por rol |
| O2 | Contención sin destruir evidencia | Deciden aislar antes de apagar, o saben por qué apagar pierde datos de memoria, y dicen qué se conserva |
| O3 | Comunicación ordenada | Definen quién habla con dirección, con el personal y con los clientes, y qué se dice y qué no |
| O4 | Recuperación por prioridades | Restauran por importancia al negocio y definen un criterio de "recuperado" |
| O5 | El plan es utilizable | Encuentran el plan y los contactos; **detectan** lo que está desactualizado o contradictorio |

### Reloj real y reloj de escenario

| # | Reloj de escenario | Reloj real (inicio) | Duración |
|---|---|---|---|
| Preámbulo | - | 0:00 | 5 min |
| Inject 1 | T+0 (viernes 17:40) | 0:05 | 10 min |
| Inject 2 | T+15 | 0:15 | 12 min |
| Inject 3 | T+30 | 0:27 | 10 min |
| Inject 4 | T+45 | 0:37 | 10 min |
| Inject 5 | T+60 | 0:47 | 12 min |
| Inject 6 | T+75 | 0:59 | 10 min |
| Inject 7 | T+90 (lunes 7:00) | 1:09 | 10 min |
| Debrief | - | 1:19 | 15 min |

Si un inject se pasa de tiempo, se corta: "lo anotamos como pendiente". **Mantén el reloj**: decidir con tiempo escaso es parte de lo que se prueba.

### Preámbulo (léelo tal cual)

> Es viernes, 17:40. Distribuidora Ejemplo cierra la semana. Este ejercicio es una discusión: nadie ejecuta comandos. Hablen desde su rol, digan qué harían, quién lo haría y con qué información. No hay respuestas correctas únicas ni se califica a las personas: se prueba el plan. Cada vez que yo lea una novedad, el reloj de escenario avanza. Si no tienen un dato, díganlo: es un hallazgo.

### La verdad fundamental (solo para el facilitador)

| Hecho | Detalle |
|---|---|
| Acceso inicial | Miércoles 03:12: alguien inicia sesión en la VPN con la credencial de la cuenta `soporte`, filtrada en la brecha de un tercero. La VPN no pide segundo factor |
| Qué hicieron | Miércoles y jueves: reconocimiento de la red. Jueves 22:00: copian unos 18 GB de las carpetas Clientes y Nóminas hacia el exterior |
| El cifrado | Viernes 17:30: se activa el cifrado en el servidor de archivos y en 6 de 20 equipos |
| Respaldos | El NAS estaba montado: los atacantes borraron los respaldos de los últimos 9 días. El más reciente sano es de hace **nueve días** |
| Datos personales | Las carpetas copiadas incluyen nombres, teléfonos y domicilios de clientes, y datos de nómina del personal |
| Mensaje de los atacantes | Llega en el inject 5: dicen que se llevaron datos y fijan un plazo de 72 horas |
| El plan | Contacto del proveedor desactualizado; el paso 3 dice "apagar los equipos afectados", que choca con conservar memoria |

### Inject 1 - T+0, viernes 17:40

**Dirigido a:** Finanzas y contabilidad.

**Léelo:**

> Una persona de contabilidad no puede abrir sus hojas de cálculo. Los archivos aparecen con otro nombre y la extensión `.locked`. Hay un archivo de texto en el escritorio con un mensaje que dice que los datos están cifrados. Ya llamó a soporte.

**Preguntas:**

1. ¿A quién avisa soporte, y por qué medio?
2. ¿Quién queda al mando del incidente y cómo lo saben todos?
3. ¿Qué le piden a contabilidad que haga y que **no** haga con esa computadora?
4. ¿Dónde está el plan y quién lo abre?

**Si preguntan:** "El plan está en un PDF en el servidor de archivos." (Que está cifrado: tienen que decidir cómo acceder.)

**Qué observar:** O1 (¿se nombró al mando?) y O5 (¿encontraron el plan sin el servidor de archivos?).

### Inject 2 - T+15

**Dirigido a:** Líder técnico y proveedor de TI.

**Léelo:**

> Soporte revisa: 6 de las 20 computadoras muestran el mismo mensaje, y el servidor de archivos tiene las carpetas de Finanzas, Clientes y Pedidos cifradas. El servidor de archivos sigue encendido. La unidad del NAS aparece montada en el servidor.

**Preguntas:**

1. ¿Qué hacen con las computadoras afectadas? ¿Y con el servidor de archivos?
2. ¿Qué dice el plan? (**Entregar el extracto del plan, parte 3.**)
3. ¿Qué pasa con el NAS en este momento? ¿Qué riesgo corre cada minuto montado?
4. Si apagan los equipos, ¿qué información pierden? ¿Quién lo sabe?

**Si preguntan:** "El NAS está accesible desde el servidor de archivos. Aún no saben si fue tocado."

**Trampa deliberada (verlo fallar):** el plan dice "apagar los equipos afectados". Si el equipo **apaga**, anota la hora de la decisión: el inject 3 les preguntará por la evidencia. Si el equipo **aísla** (desconecta de la red y mantiene encendido), pregúntales cómo lo harían y qué conservarían antes de cualquier cambio.

**Qué observar:** O2 y O5 (¿notaron la contradicción del plan?).

### Inject 3 - T+30

**Dirigido a:** Líder técnico y Mando.

**Léelo:**

> El proveedor de TI revisa la bitácora de la VPN. Hay una sesión de la cuenta `soporte` el miércoles a las 03:12 desde una dirección que nadie reconoce, que duró varias horas. La cuenta de `soporte` es la que usa la persona de soporte interna. Dirección pregunta cómo entraron.

**Preguntas:**

1. ¿Qué otras bitácoras revisan para ver hasta dónde llegó? (VPN, servidor de archivos, sistema de pedidos...)
2. Si habían apagado equipos en el inject 2: ¿qué datos tienen para contestar a dirección y qué perdieron?
3. ¿Qué hacen con la cuenta `soporte` ahora mismo?
4. ¿Qué técnicas buscarían para saber si hay más persistencia? (Cacería, sesión 5.)

**Si preguntan:** "La VPN conserva 30 días de bitácora. El servidor de archivos conserva 7 días y se sobrescribe." (Hay que decidir de inmediato qué copiar.)

**Qué observar:** O2 (¿conservan bitácoras antes de que se sobrescriban?) y O1.

### Inject 4 - T+45

**Dirigido a:** Comunicaciones y Dirección general.

**Léelo:**

> Dirección general pregunta si el lunes se opera. Una persona de bodega escribe al grupo de WhatsApp de la empresa: "¿es verdad que nos hackearon?" Un cliente grande pregunta por mensaje si sus entregas de la semana siguiente están en riesgo.

**Preguntas:**

1. ¿Qué se le dice al personal y por cuál medio? ¿Qué **no** se dice?
2. ¿Qué se contesta al cliente, y quién lo contesta?
3. ¿Quién decide si el lunes se opera, con qué información y a qué hora?
4. ¿Qué canal usan para comunicarse si el correo y el chat dependen de sistemas afectados?

**Si preguntan:** "El correo está en un servicio externo y funciona. El grupo de WhatsApp es personal, no de la empresa."

**Qué observar:** O3.

### Inject 5 - T+60

**Dirigido a:** Legal y cumplimiento, Dirección general.

**Léelo:**

> Llega un correo a la cuenta de la dirección: los atacantes dicen que **también se llevaron datos** de clientes y de nómina, adjuntan los nombres de 20 archivos como prueba y dan un plazo de 72 horas para contactarlos.

**Preguntas:**

1. ¿Qué datos personales podrían estar involucrados y qué obligaciones de aviso surgen? ¿Quién las determina?
2. ¿Quién debe saber esto hoy y quién no?
3. ¿Quién decide qué se contesta a los atacantes, si se contesta, y con quién se consulta antes de decidir?
4. ¿Se avisa a una autoridad o a un tercero (aseguradora, proveedor, asesor)? ¿A cuál y quién lo hace?

**Si preguntan:** "No hay seguro cibernético. Hay un asesor legal externo con contrato general."

**Nota para el facilitador:** no marques una respuesta "correcta" sobre pagar o no pagar. Observa **cómo deciden**: quién decide, con qué asesoría y con qué información. Las obligaciones concretas de aviso las revisa el asesor legal (el curso de legislación de la academia las cubre).

**Qué observar:** O3 y O1 (¿quién decide?).

### Inject 6 - T+75

**Dirigido a:** Líder técnico y Operaciones.

**Léelo:**

> El proveedor revisa el NAS. Los respaldos de los últimos nueve días fueron borrados. El más reciente sano es de hace nueve días. No hay copia fuera de la oficina.

**Preguntas:**

1. ¿Qué se pierde si restauran desde ese respaldo? ¿Cómo se recuperan los pedidos de nueve días?
2. ¿En qué orden restauran? ¿Qué es lo primero que la empresa necesita para facturar y entregar?
3. ¿Restauran sobre los mismos equipos? ¿Qué hay que verificar **antes** de reconectar algo a la red?
4. ¿Cuándo dicen "recuperado"? Definan el criterio.

**Si preguntan:** "El sistema de pedidos tiene su base en otro servidor que no fue cifrado, pero sus adjuntos viven en el servidor de archivos."

**Qué observar:** O4.

### Inject 7 - T+90, lunes 7:00

**Dirigido a:** Dirección general y Mando.

**Léelo:**

> Es lunes, 7:00. La bodega abre en una hora y hay entregas programadas. El servidor de archivos está reconstruido hasta el 60 %. Todavía no se sabe si la cuenta de acceso remoto sigue comprometida.

**Preguntas:**

1. ¿Abre la empresa? ¿En qué modo (manual, parcial, completo)? ¿Quién lo decide?
2. ¿Qué condiciones mínimas se piden antes de reconectar la VPN?
3. ¿Qué se le dice al personal que llega a las 8:00?
4. ¿Cómo se sabe que el atacante ya no tiene acceso?

**Qué observar:** O4, O3 y O1.

### Cierre del escenario

Detén el reloj. Di: "Fin del ejercicio. No hay más injects". Pasa de inmediato al debrief: lo que se dice en caliente se pierde si no se anota.

---

## Parte 3 - Extracto del plan (ficticio)

**Para entregar a los jugadores en el inject 2.** Caso hipotético.

> **PLAN DE RESPUESTA A INCIDENTES - Distribuidora Ejemplo (versión de hace dos años)**
>
> **Contactos.** Responsable de seguridad: *(persona que ya no trabaja en la empresa)*. Proveedor de TI: *(teléfono vigente)*. Asesor legal: *(contacto vigente)*.
>
> **Pasos.**
> 1. Avisar al responsable de seguridad.
> 2. Registrar la hora y qué se observó.
> 3. **Apagar los equipos afectados.**
> 4. Llamar al proveedor de TI.
> 5. Restaurar desde el respaldo más reciente.
> 6. Avisar a la dirección general.

**Lo que los jugadores deberían detectar (O5):** el contacto está desactualizado; el paso 3 destruye memoria y puede borrar evidencia; el paso 5 no verifica que el respaldo esté sano ni que la brecha esté cerrada; no hay pasos de comunicación externa ni de aviso legal.

---

## Parte 4 - Hojas

### Hoja de decisión (una por inject, la llena el anotador con el equipo)

| Campo | Se escribe |
|---|---|
| Inject n.º | |
| Reloj de escenario | T+__ |
| Reloj real | __:__ |
| Decisión tomada | |
| Quién decide (rol) | |
| Información que faltó | |
| Riesgo de la decisión | |
| Pendiente | |

### Hoja de línea de tiempo del anotador

| Reloj real | Reloj de escenario | Quién | Qué se dijo o decidió | Objetivo (O1-O5) |
|---|---|---|---|---|
| | | | | |

### Formulario del debrief (cada participante)

1. ¿En qué áreas se desempeñó bien el equipo?
2. ¿En qué áreas necesita más capacitación?
3. ¿Qué partes del plan hay que actualizar o crear?
4. ¿Qué información necesitó y no tuvo?
5. ¿Qué cambiaría el equipo antes del próximo incidente?

---

## Parte 5 - Debrief y evaluación

### Debrief inmediato (15 minutos)

Sin pausa. El facilitador hace las tres primeras preguntas del formulario en voz alta; el anotador registra cada respuesta con quién la dio **por rol**, no por nombre.

### Evaluación contra los objetivos (20 minutos)

Facilitador, anotador y observador completan, **juntos**, con evidencia de las hojas:

| Obj. | Evidencia (de las hojas) | Lograda / Parcial / No lograda | Observación | Recomendación |
|---|---|---|---|---|
| O1 | | | | |
| O2 | | | | |
| O3 | | | | |
| O4 | | | | |
| O5 | | | | |

### Verlo fallar: qué descubrieron

Cada equipo contesta por escrito, con evidencia de su hoja:

1. ¿Descubrieron que el paso 3 del plan choca con conservar evidencia? ¿En qué inject y cuánto tardaron?
2. ¿Quién ocupó el puesto de mando y quién tomó realmente las decisiones? ¿Coinciden?
3. ¿Qué información faltó que el plan debería haber tenido a la mano?
4. ¿Qué decisión se tomó más rápido de lo que debía y por qué?

### Entrega

El anotador entrega al terminar: las **siete hojas de decisión**, la **línea de tiempo**, la **evaluación** y las respuestas de "verlo fallar". Son la entrada de la tarea.

---

## Notas para quien conduce

- **Lee los injects tal como están.** Improvisar las respuestas de la verdad fundamental provoca contradicciones.
- **No resuelvas.** Pregunta: "¿quién decide?", "¿con qué información?", "¿y si no contesta?".
- **Cuida el reloj.** Si el equipo se atora, anota el pendiente y avanza.
- **Si todo sale bien,** sube la presión con una pregunta ("¿y si el proveedor no contesta?"); un ejercicio sin fallas no ejercita nada.
- **Quien juega no se evalúa como persona.** Las observaciones hablan del plan y del proceso.

---

## Checklist de salida

- [ ] Los siete injects se leyeron con su reloj de escenario.
- [ ] Hay una hoja de decisión por inject.
- [ ] La evaluación por objetivo está llena, con evidencia.
- [ ] Contestaron las cuatro preguntas de "verlo fallar".
- [ ] El anotador guardó todas las hojas.
