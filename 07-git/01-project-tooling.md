# 12 · Archivos de proyecto y tooling de equipo

## 12.1 .gitignore

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>.gitignore</em></td>
</tr>
<tr>
<td><p># .NET</p>
<p>bin/</p>
<p>obj/</p>
<p>*.user</p>
<p>*.suo</p>
<p>.vs/</p>
<p>[Tt]est[Rr]esult*/</p>
<p># Node / React</p>
<p>node_modules/</p>
<p>dist/</p>
<p>build/</p>
<p>.vite/</p>
<p>coverage/</p>
<p># Variables de entorno</p>
<p>.env</p>
<p>.env.local</p>
<p>.env.*.local</p>
<p># IDE</p>
<p>.idea/</p>
<p>.vscode/*</p>
<p>!.vscode/settings.json</p>
<p>!.vscode/extensions.json</p>
<p># OS</p>
<p>.DS_Store</p>
<p>Thumbs.db</p>
<p># Secretos</p>
<p>*.pfx</p>
<p>*.pem</p>
<p>*.key</p>
<p>secrets.json</p></td>
</tr>
</tbody>
</table>

## 12.2 .dockerignore

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>.dockerignore</em></td>
</tr>
<tr>
<td><p>**/.git</p>
<p>**/.gitignore</p>
<p>**/.vs</p>
<p>**/.vscode</p>
<p>**/.idea</p>
<p>**/bin</p>
<p>**/obj</p>
<p>**/node_modules</p>
<p>**/dist</p>
<p>**/build</p>
<p>**/.env</p>
<p>**/.env.*</p>
<p>**/coverage</p>
<p>**/*.md</p>
<p>**/Dockerfile*</p>
<p>**/docker-compose*</p>
<p>**/.dockerignore</p>
<p>**/README.md</p>
<p>**/LICENSE</p></td>
</tr>
</tbody>
</table>

## 12.3 .editorconfig

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>.editorconfig</em></td>
</tr>
<tr>
<td><p>root = true</p>
<p>[*]</p>
<p>indent_style = space</p>
<p>indent_size = 4</p>
<p>end_of_line = lf</p>
<p>charset = utf-8</p>
<p>trim_trailing_whitespace = true</p>
<p>insert_final_newline = true</p>
<p>[*.{js,jsx,ts,tsx,json,yml,yaml,html,css}]</p>
<p>indent_size = 2</p>
<p>[*.cs]</p>
<p>indent_size = 4</p>
<p>csharp_new_line_before_open_brace = all</p>
<p>csharp_indent_case_contents = true</p></td>
</tr>
</tbody>
</table>

## 12.4 Husky + lint-staged

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>bash</em></td>
</tr>
<tr>
<td><p># Frontend (Node):</p>
<p># npm install --save-dev husky lint-staged</p>
<p># npx husky init</p>
<p># package.json</p>
<p>{</p>
<p>"scripts": {</p>
<p>"prepare": "husky",</p>
<p>"lint": "eslint .",</p>
<p>"format": "prettier --write ."</p>
<p>},</p>
<p>"lint-staged": {</p>
<p>"*.{js,jsx,ts,tsx}": ["eslint --fix", "prettier --write"],</p>
<p>"*.{json,css,md}": ["prettier --write"]</p>
<p>}</p>
<p>}</p>
<p># .husky/pre-commit</p>
<p>npx lint-staged</p></td>
</tr>
</tbody>
</table>

## 12.5 Dependabot

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>.github/dependabot.yml</em></td>
</tr>
<tr>
<td><p>version: 2</p>
<p>updates:</p>
<p>- package-ecosystem: "npm"</p>
<p>directory: "/frontend"</p>
<p>schedule: { interval: "weekly" }</p>
<p>open-pull-requests-limit: 10</p>
<p>- package-ecosystem: "nuget"</p>
<p>directory: "/backend"</p>
<p>schedule: { interval: "weekly" }</p>
<p>- package-ecosystem: "docker"</p>
<p>directory: "/"</p>
<p>schedule: { interval: "weekly" }</p>
<p>- package-ecosystem: "github-actions"</p>
<p>directory: "/"</p>
<p>schedule: { interval: "monthly" }</p></td>
</tr>
</tbody>
</table>



---

*Rogelio Arriaga Gonzalez*
