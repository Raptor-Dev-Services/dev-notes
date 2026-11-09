# Práctica - OSINT con Sherlock sobre una cuenta propia o inventada

## Objetivo

Instalar y usar Sherlock para auditar la huella digital de un nombre de usuario, documentando dónde aparece y guardando la evidencia.

**Regla de esta práctica:** solo se busca un nombre de usuario propio (tuyo) o uno inventado para el ejercicio. Nunca el de un compañero, un familiar o cualquier tercero sin su consentimiento explícito.

## 1. Instala Sherlock

```bash
mkdir -p ~/HackingTools && cd ~/HackingTools
git clone https://github.com/sherlock-project/sherlock.git
cd sherlock
pip install -r requirements.txt
```

## 2. Búsqueda básica

Elige un nombre de usuario propio o inventa uno para la práctica (por ejemplo, `laboratorio_curso_2025`):

```bash
python3 sherlock laboratorio_curso_2025
```

Observa la lista de resultados: cada línea verde (encontrado) indica una plataforma donde ese nombre de usuario existe.

## 3. Guarda la evidencia

```bash
python3 sherlock laboratorio_curso_2025 -o resultado_laboratorio.txt
```

## 4. Filtra a plataformas específicas

```bash
python3 sherlock laboratorio_curso_2025 --site GitHub --site Reddit
```

## 5. Búsqueda masiva con una lista

Crea un archivo con dos o tres nombres de usuario propios o inventados:

```bash
echo "laboratorio_curso_2025" > usuarios_practica.txt
echo "laboratorio_curso_2025_b" >> usuarios_practica.txt

python3 sherlock -L usuarios_practica.txt --fo resultados_practica
```

Revisa la carpeta `resultados_practica` generada.

## 6. Documenta tu hallazgo

Redacta un párrafo corto: ¿en cuántas plataformas apareció el nombre de usuario que probaste? ¿Hubo algún resultado que no esperabas? ¿Qué harías si encontraras una cuenta tuya olvidada que ya no usas?

## Checklist de la práctica

- [ ] Sherlock instalado y ejecutándose desde su propia carpeta.
- [ ] Búsqueda realizada solo sobre una cuenta propia o inventada.
- [ ] Resultado guardado en un archivo de salida.
- [ ] Reflexión escrita sobre lo encontrado.
