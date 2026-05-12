## 1.1 Variables de entorno en Vite

Vite tiene un sistema simple pero estricto. Solo expone al cliente las variables con prefijo VITE\_. Todas las demás están disponibles solo en build-time del lado de Node, nunca llegan al navegador.

Archivos .env reconocidos por Vite

| **Archivo** | **Cuándo se carga** |
|----|----|
| **.env** | Siempre. Variables compartidas por todos los modos. |
| **.env.local** | Siempre, salvo en CI. Para overrides personales (NUNCA al repo). |
| **.env.development** | Solo en modo dev (npm run dev). |
| **.env.production** | Solo en modo build (npm run build). |
| **.env.development.local** | Override local de development. |
| **.env.production.local** | Override local de production. |

Orden de precedencia (de menor a mayor): .env → .env.{mode} → .env.local → .env.{mode}.local. Los archivos .local NUNCA se comitean.

Ejemplo real de .env para frontend SaaS

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>.env</em></td>
</tr>
<tr>
<td><p># .env (compartido, va al repo)</p>
<p>VITE_APP_NAME=TaskFlow</p>
<p>VITE_DEFAULT_LOCALE=es-MX</p>
<p># .env.development (va al repo)</p>
<p>VITE_API_BASE_URL=http://localhost:5000/api</p>
<p>VITE_WS_URL=ws://localhost:5000/hubs</p>
<p>VITE_STRIPE_PUBLIC_KEY=pk_test_51AbCd...</p>
<p>VITE_ENABLE_DEVTOOLS=true</p>
<p># .env.production (va al repo, valores reales no sensibles)</p>
<p>VITE_API_BASE_URL=https://api.taskflow.com/api</p>
<p>VITE_WS_URL=wss://api.taskflow.com/hubs</p>
<p>VITE_ENABLE_DEVTOOLS=false</p>
<p># .env.local (NUNCA al repo, .gitignore obligatorio)</p>
<p>VITE_API_BASE_URL=http://192.168.1.50:5000/api</p></td>
</tr>
</tbody>
</table>

Cómo leerlas en código React

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>src/config/env.js</em></td>
</tr>
<tr>
<td><p>// src/config/env.js — centralizar el acceso, NO usar import.meta.env disperso</p>
<p>export const config = {</p>
<p>appName: import.meta.env.VITE_APP_NAME ?? 'App',</p>
<p>apiBaseUrl: import.meta.env.VITE_API_BASE_URL,</p>
<p>wsUrl: import.meta.env.VITE_WS_URL,</p>
<p>stripeKey: import.meta.env.VITE_STRIPE_PUBLIC_KEY,</p>
<p>defaultLocale: import.meta.env.VITE_DEFAULT_LOCALE ?? 'en-US',</p>
<p>enableDevtools: import.meta.env.VITE_ENABLE_DEVTOOLS === 'true',</p>
<p>isDev: import.meta.env.DEV,</p>
<p>isProd: import.meta.env.PROD,</p>
<p>mode: import.meta.env.MODE,</p>
<p>};</p>
<p>if (!config.apiBaseUrl) {</p>
<p>throw new Error('VITE_API_BASE_URL es requerido');</p>
<p>}</p></td>
</tr>
</tbody>
</table>

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><p><strong>Las variables Vite son público</strong></p>
<p>TODO lo que pongas con prefijo VITE_ se incluye en el bundle final que llega al navegador. Cualquier usuario puede verlo. JAMÁS pongas API keys secretas, contraseñas o connection strings con prefijo VITE_.</p></td>
</tr>
</tbody>
</table>



> Fuente: *Full Stack React, TypeScript, and Node* (David Choi) — Ch.3 Creating React Apps with Vite

---

*Rogelio Arriaga Gonzalez*
