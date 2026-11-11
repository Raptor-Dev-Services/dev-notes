# Práctica - OSINT con Sherlock sobre una cuenta propia o inventada

## Objetivo

Instalar y usar Sherlock para auditar la huella digital de un nombre de usuario, documentando dónde aparece y guardando la evidencia.

**Regla de esta práctica:** solo se busca un nombre de usuario propio (tuyo) o uno inventado para el ejercicio. Nunca el de un compañero, un familiar o cualquier tercero sin su consentimiento explícito.

## 1. Instala Sherlock

```bash
pipx install sherlock-project
```

## 2. Búsqueda básica

Elige un nombre de usuario propio o inventa uno para la práctica (por ejemplo, `laboratorio_curso_2025`):

```bash
sherlock laboratorio_curso_2025
```

Observa la lista de resultados: cada línea verde (encontrado) indica una plataforma donde ese nombre de usuario existe.

## 3. Guarda la evidencia

```bash
sherlock laboratorio_curso_2025 -o resultado_laboratorio.txt
```

## 4. Filtra a plataformas específicas

```bash
sherlock laboratorio_curso_2025 --site GitHub --site Reddit
```

## 5. Búsqueda de varios usuarios

Sherlock recibe los usuarios como argumentos (no tiene opción para leerlos de un archivo). Usa dos o tres nombres propios o inventados:

```bash
sherlock laboratorio_curso_2025 laboratorio_curso_2025_b -fo resultados_practica
```

Revisa la carpeta `resultados_practica` generada.

## 6. Documenta tu hallazgo

Redacta un párrafo corto: ¿en cuántas plataformas apareció el nombre de usuario que probaste? ¿Hubo algún resultado que no esperabas? ¿Qué harías si encontraras una cuenta tuya olvidada que ya no usas?

## Checklist de la práctica

- [ ] Sherlock instalado y ejecutándose con el comando `sherlock`.
- [ ] Búsqueda realizada solo sobre una cuenta propia o inventada.
- [ ] Resultado guardado en un archivo de salida.
- [ ] Reflexión escrita sobre lo encontrado.
