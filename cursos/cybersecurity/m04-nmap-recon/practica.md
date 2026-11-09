# Práctica - Escaneo de tu laboratorio con Nmap

## Objetivo

Descubrir tu máquina víctima en la red del laboratorio, identificar sus puertos abiertos, los servicios que corren y guardar la evidencia en un reporte.

**Recordatorio:** todos los comandos de esta práctica apuntan a tu propia máquina víctima de laboratorio (módulo 3), nunca a una IP fuera de tu control.

## 1. Descubre qué está activo en tu red de laboratorio

```bash
nmap -sn <RANGO_DE_TU_RED_DE_LABORATORIO>/24
```

Anota la IP de tu máquina víctima a partir del resultado.

## 2. Escaneo rápido de puertos comunes

```bash
nmap -F <IP_DE_TU_VICTIMA>
```

Identifica qué puertos aparecen abiertos (por ejemplo, 22 si configuraste SSH en el módulo 3).

## 3. Detecta la versión del servicio

```bash
nmap -sV <IP_DE_TU_VICTIMA>
```

Anota el nombre y versión exacta del servicio SSH que reporta Nmap.

## 4. Escaneo completo de puertos

```bash
nmap -p- <IP_DE_TU_VICTIMA>
```

Este escaneo tarda más porque revisa los 65535 puertos. Compara el resultado contra el escaneo rápido del paso 2: ¿apareció algún puerto adicional?

## 5. Escaneo agresivo con guardado de evidencia

```bash
sudo nmap -sS -p- -T4 -A -oN escaneo_final.txt <IP_DE_TU_VICTIMA>
```

Revisa el archivo `escaneo_final.txt` generado: debe incluir puertos, versiones de servicio y, si Nmap pudo determinarlo, el sistema operativo.

## 6. Prueba el motor de scripts (NSE)

```bash
nmap -sV --script vuln <IP_DE_TU_VICTIMA>
```

Documenta qué scripts corrieron y si reportaron algún hallazgo sobre el servicio SSH de tu máquina víctima.

## 7. Redacta un resumen de tres líneas

En tu reporte, anota: qué puertos encontraste abiertos, qué servicio y versión corre en cada uno, y si el escaneo `vuln` reportó algo relevante.

## Checklist de la práctica

- [ ] Identificaste la IP de tu máquina víctima con `nmap -sn`.
- [ ] Documentaste los puertos abiertos y su versión de servicio.
- [ ] Guardaste la evidencia del escaneo completo en un archivo (`-oN`).
- [ ] Corriste el motor de scripts `vuln` y registraste el resultado.
- [ ] Todo lo anterior se ejecutó contra tu propia máquina víctima, no contra un tercero.
