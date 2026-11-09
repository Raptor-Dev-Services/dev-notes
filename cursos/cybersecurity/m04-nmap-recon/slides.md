<!-- _class: lead -->

# Módulo 4 - Reconocimiento con Nmap

### Ciberseguridad

**Rogelio Arriaga - Raptor Dev Services**

---

## Qué es Nmap

**Network Mapper** es la herramienta estándar de la industria para descubrir hosts activos en una red y los servicios que corren en ellos. Viene preinstalada en Kali.

**Regla del módulo:** todos los ejemplos apuntan a tu máquina víctima de laboratorio. Nunca a una IP que no controlas.

Fuente: Nmap Reference Guide, nmap.org/book/man.html.

---

<!-- _class: lead -->

# Parte 1
## Descubrir hosts activos

---

## Ping scan: quién está prendido

```bash
nmap -sn 192.0.2.0/24
```

No escanea puertos: solo hace un barrido (ICMP/ARP) para ver qué direcciones responden. Ideal antes de escanear puertos en una red completa.

> Sustituye `192.0.2.0/24` por el rango real de tu red de laboratorio.

---

## Omitir el ping cuando hay firewall

```bash
nmap -Pn 192.0.2.10
```

Trata al host como activo aunque no responda al ping; útil cuando un firewall bloquea ICMP pero el host sí tiene puertos abiertos.

---

<!-- _class: lead -->

# Parte 2
## Técnicas de escaneo de puertos

---

## SYN Scan vs. TCP Connect Scan

| Opción | Nombre | Cómo funciona |
|---|---|---|
| `-sS` | SYN Stealth | Envía solo SYN; si responde SYN/ACK, el puerto está abierto. Más rápido, requiere privilegios |
| `-sT` | TCP Connect | Completa el handshake de 3 vías. Más lento y ruidoso, no requiere privilegios |

---

## Controlar qué y cuántos puertos

```bash
nmap <IP_DEL_LABORATORIO>              # los 1000 puertos TCP más comunes
nmap -F <IP_DEL_LABORATORIO>           # escaneo rápido, 100 puertos
nmap -p- <IP_DEL_LABORATORIO>          # los 65535 puertos (tarda)
nmap -p 22,80,443 <IP_DEL_LABORATORIO> # puertos específicos
```

---

## Detección de versión y sistema operativo

```bash
nmap -sV <IP_DEL_LABORATORIO>   # versión exacta del servicio detectado
nmap -O <IP_DEL_LABORATORIO>    # sistema operativo (requiere privilegios)
nmap -A <IP_DEL_LABORATORIO>    # agresivo: -sV -O --traceroute --script=default
```

`-A` es lento y ruidoso, pero el más informativo: úsalo cuando ya sabes que tienes autorización y tiempo.

---

<!-- _class: lead -->

# Parte 3
## El motor de scripts (NSE) y guardar resultados

---

## Nmap Scripting Engine (NSE)

```bash
nmap -sV --script vuln <IP_DEL_LABORATORIO>
```

`--script vuln` ejecuta scripts que buscan vulnerabilidades conocidas en los servicios detectados. Otras categorías útiles: `default`, `safe`, `auth`.

**Nota:** correr `vuln` contra un servicio real sin autorización escrita es exactamente el delito descrito en el módulo 1.

---

## Guardar la salida para tu reporte

```bash
nmap -sV -oN escaneo_log.txt <IP_DEL_LABORATORIO>   # texto legible
nmap -sV -oX escaneo_xml.xml <IP_DEL_LABORATORIO>   # XML, para importar a otras herramientas
```

Siempre guarda la salida: es la evidencia de lo que encontraste, para tu reporte de laboratorio.

---

## El escaneo completo de laboratorio

```bash
sudo nmap -sS -p- -T4 -A -oN escaneo_final.txt <IP_DE_TU_VICTIMA>
```

- `-sS`: SYN scan. `-p-`: todos los puertos. `-T4`: velocidad agresiva.
- `-A`: detección de SO, versión y scripts por defecto. `-oN`: guarda el resultado.

Este es el comando de cierre del módulo: corre contra tu propia máquina víctima de laboratorio.

---

<!-- _class: lead -->

# Cierre

## Lo que te llevas de este módulo

- Descubrir hosts activos antes de escanear puertos uno por uno
- Elegir el tipo de escaneo (SYN vs. Connect) según privilegios y sigilo
- Detectar versión, SO y vulnerabilidades, siempre dentro de tu laboratorio

**Siguiente módulo:** OSINT con Sherlock.
