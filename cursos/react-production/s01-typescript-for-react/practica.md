# Práctica - S01 - TypeScript para React

**React en producción con TypeScript**
**Duración:** 80 minutos, por parejas, en su máquina.

---

## Objetivo

Comprobar con tus propios ojos tres cosas:

1. Que un componente lleno de `any` **no se entera** cuando el servidor cambia un campo.
2. Que el mismo componente, tipado con el contrato de la API, **rompe el build** en la línea exacta.
3. Que el compilador no basta: cuando el servidor se sale del contrato, hace falta **validar en la frontera** y leer el error.

Al final quitas los `any` de un módulo y encuentras un error que estaba escondido.

> **Ejemplo ilustrativo.** La *Mesa de ayuda*, sus tickets y la persona "Ana Ejemplo" no existen. El servidor corre solo en tu máquina (`127.0.0.1`).

---

## Antes de empezar

Necesitas Node 22.22 o superior (a noviembre de 2026, `react-router` 8.5.1 pide `>=22.22.0`).

```bash
node -v
# v22.22.1 (o superior)
cp -r referencia/mesa-de-ayuda ~/mesa-de-ayuda   # la carpeta referencia/ está junto a las unidades
cd ~/mesa-de-ayuda
npm ci
```

`npm ci` instala **exactamente** lo que dice `package-lock.json`. Verifica la base:

```bash
npm run typecheck
npm run lint
npm test
```

Los tres deben terminar sin errores. Si no, **no sigas**: lo que falle aquí no es de la práctica.

En otra terminal (la llamaremos **A**) arranca el servidor de laboratorio y déjalo corriendo:

```bash
npm run api
# Mesa de ayuda (laboratorio) en http://127.0.0.1:3100
```

---

## Paso 1 - Mirar el contrato real

El servidor pide sesión. Entra con `curl`, guarda la cookie y pide los tickets:

```bash
curl -s -c cookies.txt -H 'content-type: application/json' \
  -d '{"email":"ana@example.com","password":"clave-de-ejemplo"}' \
  http://127.0.0.1:3100/api/login
# {"usuario":{"id":1,"nombre":"Ana Ejemplo","email":"ana@example.com"}}

curl -s -b cookies.txt http://127.0.0.1:3100/api/tickets
```

La respuesta trae, por cada ticket, `id`, `titulo`, **`prioridad`**, `estado` y `creadoEn`. Anota el nombre del campo de prioridad: lo vas a necesitar.

> El correo y la contraseña son de ejemplo y solo existen en este servidor de laboratorio.

---

## Paso 2 - Verlo NO fallar

Crea `src/practica/lista-any.tsx` con el código de alguien con prisa. **Nota el error a propósito:** usa `urgencia`, no `prioridad`.

```tsx
export async function cargarTickets(): Promise<any> {
  const respuesta = await fetch('/api/tickets');
  return respuesta.json();
}

export function ListaDeTickets({ tickets }: { tickets: any }) {
  return (
    <ul>
      {tickets.datos.map((t: any) => (
        <li key={t.id}>
          {t.titulo} ({t.urgencia})
        </li>
      ))}
    </ul>
  );
}
```

Compila y revisa el código de salida:

```bash
npx tsc --noEmit
echo $?
```

Resultado esperado: **no imprime nada y el código es `0`**. TypeScript no vio nada raro: con `any`, `t.urgencia` es "lo que sea".

Ahora pasa el lint:

```bash
npx eslint src/practica/lista-any.tsx
```

```console
  1:48  error  Unexpected any. Specify a different type  @typescript-eslint/no-explicit-any
  6:56  error  Unexpected any. Specify a different type  @typescript-eslint/no-explicit-any
  9:30  error  Unexpected any. Specify a different type  @typescript-eslint/no-explicit-any

✖ 3 problems (3 errors, 0 warnings)
```

**Responde por escrito** (2 líneas): el lint protestó por los `any`, ¿protestó por el campo equivocado? ¿Qué vería la persona que usa esta pantalla?

---

## Paso 3 - Tipar con el contrato

El contrato ya está en `src/shared/api/contract.ts` (lo escribió Zod; los tipos salen de él con `z.infer`). Léelo completo antes de seguir: son 40 líneas.

Crea `src/practica/lista-tipada.tsx`. Sigue con `urgencia`, **a propósito**:

```tsx
import { paginaDeTicketsSchema, type PaginaDeTickets, type Ticket } from '../shared/api/contract';

export async function cargarTickets(): Promise<PaginaDeTickets> {
  const respuesta = await fetch('/api/tickets');
  const datos: unknown = await respuesta.json();
  return paginaDeTicketsSchema.parse(datos);
}

export function ListaDeTickets({ tickets }: { tickets: Ticket[] }) {
  return (
    <ul>
      {tickets.map((t) => (
        <li key={t.id}>
          {t.titulo} ({t.urgencia})
        </li>
      ))}
    </ul>
  );
}
```

```bash
npx tsc --noEmit
```

```console
src/practica/lista-tipada.tsx(14,26): error TS2339: Property 'urgencia' does not exist on type '{ id: number; titulo: string; prioridad: "baja" | "media" | "alta"; estado: "abierto" | "cerrado"; creadoEn: string; }'.
```

Ahí está: el compilador te señala **el archivo, la línea, la columna** y te muestra los campos que sí existen. Corrige `t.urgencia` por `t.prioridad` y compila otra vez: debe quedar en cero errores.

Tres preguntas para la pareja:

1. ¿Por qué `cargarTickets` recibe `unknown` y no `any`?
2. ¿Qué hace `paginaDeTicketsSchema.parse` que el tipo `PaginaDeTickets` no puede hacer?
3. ¿Cuántas veces está escrito el tipo `Ticket` en el proyecto? (Pista: busca `interface Ticket`.)

---

## Paso 4 - Verlo fallar: el contrato cambia

Imagina que el equipo de backend decide renombrar `prioridad` a `urgencia`. Lo hacemos **solo en el contrato** del front:

```bash
sed -i "s/  prioridad: prioridadSchema,/  urgencia: prioridadSchema,/" src/shared/api/contract.ts
grep -n urgencia src/shared/api/contract.ts
# 11:  urgencia: prioridadSchema,
# 25:  urgencia: prioridadSchema,
npx tsc --noEmit
```

Salida real (TypeScript 6.0.3):

```console
src/features/tickets/ticket-form.tsx(11,34): error TS2353: Object literal may only specify known properties, and 'prioridad' does not exist in type 'AsyncDefaultValues<{ titulo: string; urgencia: "baja" | "media" | "alta"; }> | { titulo?: string | undefined; urgencia?: "baja" | "media" | "alta" | undefined; }'.
src/features/tickets/ticket-form.tsx(15,18): error TS2345: Argument of type 'TFieldValues' is not assignable to parameter of type '{ titulo: string; urgencia: "baja" | "media" | "alta"; }'.
src/features/tickets/ticket-form.tsx(21,71): error TS2345: Argument of type '"titulo" | "prioridad"' is not assignable to parameter of type '"urgencia" | "titulo" | ErrorNamespacePath'.
src/features/tickets/ticket-form.tsx(38,43): error TS2345: Argument of type '"prioridad"' is not assignable to parameter of type '"urgencia" | "titulo"'.
src/features/tickets/ticket-form.tsx(43,15): error TS2339: Property 'prioridad' does not exist on type 'FieldErrors<{ titulo: string; urgencia: "baja" | "media" | "alta"; }>'.
src/features/tickets/tickets-page.tsx(23,45): error TS2339: Property 'prioridad' does not exist on type '{ id: number; titulo: string; prioridad... }'.
src/practica/lista-tipada.tsx(14,26): error TS2339: Property 'prioridad' does not exist on type '{ ... urgencia ... }'.
```

(Los mensajes largos se recortaron con `...`; los tuyos mostrarán el texto completo. Si tienes también `resumen.ts` del paso 6, verás más líneas.)

Haz esto en tu cuaderno:

- Cuenta los errores y anota **los archivos** que toca el cambio.
- Distingue los errores del **formulario** (`react-hook-form` pide las claves del esquema) de los de **lectura** (`t.prioridad`).
- Esa lista **es** el trabajo que tendrías que hacer para soportar el cambio. Sin tipos, lo habrías descubierto en producción.

**Deshaz el cambio** antes de seguir:

```bash
sed -i "s/  urgencia: prioridadSchema,/  prioridad: prioridadSchema,/" src/shared/api/contract.ts
npx tsc --noEmit && echo "restaurado"
```

---

## Paso 5 - Verlo fallar en ejecución: el servidor se sale del contrato

El compilador solo ve **tu** código. Ahora el que cambia es el servidor: el parámetro `?contrato=roto` hace que devuelva `urgencia` en lugar de `prioridad`.

```bash
curl -s -b cookies.txt 'http://127.0.0.1:3100/api/tickets?contrato=roto'
```

Fíjate en que el campo cambió de nombre. Y `npx tsc --noEmit` sigue en `0`: nadie tocó el código.

Corre la verificación del contrato contra el servidor real:

```bash
npm run contrato
# /api/tickets: 3 tickets, contrato cumplido

npx tsx scripts/verificar-contrato.ts '/api/tickets?contrato=roto'
echo $?
```

```console
/api/tickets?contrato=roto -> datos.0.prioridad: Invalid option: expected one of "baja"|"media"|"alta"
/api/tickets?contrato=roto -> datos.1.prioridad: Invalid option: expected one of "baja"|"media"|"alta"
/api/tickets?contrato=roto -> datos.2.prioridad: Invalid option: expected one of "baja"|"media"|"alta"
1
```

El script (`scripts/verificar-contrato.ts`) hace lo mismo que `pedir()` en la aplicación: entra, pide, y pasa la respuesta por el esquema. La diferencia es que **lo puedes correr en el pipeline** contra un entorno de pruebas del backend y enterarte del cambio antes que los usuarios.

Responde: ¿qué candado atrapó el cambio del paso 4 y cuál el del paso 5? ¿Por qué hacen falta los dos?

---

## Paso 6 - Quitar los `any` de un módulo

Crea `src/practica/resumen.ts` tal como lo dejó alguien con prisa:

```ts
export function resumir(tickets: any) {
  const porPrioridad: any = {};
  for (const t of tickets.datos) {
    porPrioridad[t.prioridad] = (porPrioridad[t.prioridad] || 0) + 1;
  }
  return porPrioridad;
}

export function masUrgente(tickets: any) {
  return tickets.datos.find((t: any) => t.prioridad === 'urgente');
}
```

Mide el punto de partida:

```bash
npx tsc --noEmit; echo "tsc: $?"
npx eslint src/practica/resumen.ts
```

```console
tsc: 0
   1:34  error  Unexpected any. Specify a different type  @typescript-eslint/no-explicit-any
   2:23  error  Unexpected any. Specify a different type  @typescript-eslint/no-explicit-any
   9:37  error  Unexpected any. Specify a different type  @typescript-eslint/no-explicit-any
  10:33  error  Unexpected any. Specify a different type  @typescript-eslint/no-explicit-any

✖ 4 problems (4 errors, 0 warnings)
```

Reescríbelo con el contrato. **Antes de leer la solución**, intenta tipar tú: parámetros con `PaginaDeTickets`, resultado de `resumir` como `Record<Prioridad, number>`.

<details>
<summary>Solución, para comparar</summary>

```ts
import type { PaginaDeTickets, Prioridad, Ticket } from '../shared/api/contract';

export function resumir(tickets: PaginaDeTickets): Record<Prioridad, number> {
  const resumen: Record<Prioridad, number> = { baja: 0, media: 0, alta: 0 };
  for (const t of tickets.datos) {
    resumen[t.prioridad] += 1;
  }
  return resumen;
}

export function masUrgente(tickets: PaginaDeTickets): Ticket | undefined {
  return tickets.datos.find((t) => t.prioridad === 'urgente');
}
```

</details>

Compila:

```console
src/practica/resumen.ts(12,36): error TS2367: This comparison appears to be unintentional because the types '"baja" | "media" | "alta"' and '"urgente"' have no overlap.
```

**Un error que ya existía.** La prioridad `'urgente'` no existe en el contrato: la función `masUrgente` **nunca encontraba nada**, y con `any` nadie lo notó. Corrige a `'alta'` y deja `tsc` y `eslint` limpios:

```bash
npx tsc --noEmit && npx eslint src/practica/resumen.ts && echo "limpio"
```

Y una prueba que lo confirme (`src/practica/resumen.test.ts`, usa la que viene en la referencia o escríbela tú):

```bash
npx vitest run src/practica
#  Test Files  1 passed (1)
#       Tests  2 passed (2)
```

> **Nota sobre el `any` que sí se queda.** Hay lugares donde un `any` es razonable (un dato que viene de una librería sin tipos, por ejemplo). Si dejas uno, comenta por qué. Un `any` documentado es una decisión; uno sin explicar es un descuido.

---

## Paso 7 - Que el tipo se cuide solo: agregar una prioridad

Agrega `'critica'` al enum de prioridades en `contract.ts`:

```bash
sed -i "s/z.enum(\['baja', 'media', 'alta'\])/z.enum(['baja', 'media', 'alta', 'critica'])/" src/shared/api/contract.ts
npx tsc --noEmit
```

Salida real:

```console
src/ejemplos/s01-compatibilidad.ts(6,7): error TS2322: Type 'true' is not assignable to type 'false'.
src/ejemplos/s01-compatibilidad.ts(7,7): error TS2322: Type 'true' is not assignable to type 'false'.
src/ejemplos/s01-tipos.tsx(95,3): error TS1360: Type '{ baja: string; media: string; alta: string; }' does not satisfy the expected type 'Record<"baja" | "media" | "alta" | "critica", string>'.
  Property 'critica' is missing in type '{ baja: string; media: string; alta: string; }' but required in type 'Record<"baja" | "media" | "alta" | "critica", string>'.
src/practica/resumen.ts(4,9): error TS2741: Property 'critica' is missing in type '{ baja: number; media: number; alta: number; }' but required in type 'Record<"baja" | "media" | "alta" | "critica", number>'.
```

Tres mecanismos distintos acaban de avisarte:

| Archivo | Mecanismo | Qué te dijo |
|---|---|---|
| `s01-compatibilidad.ts` | Igualdad de tipos con el contrato OpenAPI | "Tu esquema y la especificación ya no describen lo mismo" |
| `s01-tipos.tsx` | `satisfies Record<Prioridad, string>` | "Falta la etiqueta de la nueva prioridad" |
| `resumen.ts` | `Record<Prioridad, number>` | "Falta inicializar el contador de la nueva prioridad" |

Revierte (`sed` inverso) y confirma que vuelve a compilar.

---

## Paso 8 - (extra) Generar los tipos desde OpenAPI

El proyecto trae `contract/openapi.yaml`, una especificación OpenAPI 3.1 de la API. Genera los tipos:

```bash
npx openapi-typescript contract/openapi.yaml -o src/shared/api/schema.d.ts
```

Compara `components['schemas']['Ticket']` con el tipo `Ticket` de Zod. ¿Dicen lo mismo? ¿Qué pasa si renombras una propiedad en el YAML y vuelves a generar? Abre `src/ejemplos/s01-compatibilidad.ts` y ejecuta `npx tsc --noEmit`.

> `openapi-typescript` no es una dependencia de producción: genera un archivo `.d.ts` que se versiona. Si no la tienes instalada, `npx` la baja (versión 7.13.0 a noviembre de 2026).

---

## Evidencia a entregar

Un archivo `evidencia-s01.md` con:

1. La salida de `tsc` del paso 2 (código de salida `0`) y del paso 3 (el error `TS2339`).
2. La lista de errores del paso 4 y los archivos que tocaba el cambio.
3. La salida del paso 5 con el código de salida `1`.
4. El módulo `resumen.ts` antes y después, con el error `TS2367` que apareció.
5. Las respuestas de las preguntas de los pasos 2, 3 y 5.

---

## Si algo no sale

| Síntoma | Causa probable | Qué hacer |
|---|---|---|
| `npm ci` avisa `EBADENGINE`, o Vite y Vitest no arrancan | Node anterior a 22.12 | Instala Node 22.22 o superior |
| `npm install` termina con `ERESOLVE` al subir TypeScript | `typescript-eslint` 8.72.2 declara `typescript <6.1.0` | Usa la 6.0.3 que fija el proyecto |
| `curl` responde `{"codigo":"sin-sesion",...}` | Falta `-b cookies.txt` o expiró (1 h) | Repite el login del paso 1 |
| `npm run contrato` dice `ECONNREFUSED` | El servidor no corre | Terminal A: `npm run api` |
| `sed` no cambia nada | La línea ya cambió | Revisa con `grep -n prioridad src/shared/api/contract.ts` |
| Los errores del paso 4 salen más cortos | El `tsc` recorta mensajes largos con `...` | Es normal; la columna y el archivo son lo que importa |

Al terminar, detén el servidor de la terminal A con `Ctrl-C`.
