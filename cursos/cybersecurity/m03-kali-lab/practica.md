# Práctica - Preparar tu laboratorio Kali

## Objetivo

Tener un laboratorio funcional, aislado de tu red real, con Kali actualizado y las herramientas que usarás en los módulos 4 a 6.

## 0. Antes de empezar

Esta práctica se hace completa dentro de un hipervisor (VirtualBox, VMware, Hyper-V...) en tu propio equipo. No se conecta a ningún sistema fuera de tu laboratorio.

## 1. Crea la red aislada

En tu hipervisor, crea una red interna o "solo anfitrión" dedicada al laboratorio. Verifica que ninguna de tus máquinas virtuales de laboratorio use el adaptador en modo puente (bridge) hacia tu red doméstica.

## 2. Instala o importa Kali Linux

Descarga la imagen oficial de Kali Linux desde kali.org/get-kali e impórtala o instálala en tu hipervisor, conectada únicamente a la red aislada que creaste.

## 3. Actualiza el sistema

```bash
sudo apt update
sudo apt upgrade -y
sudo apt autoremove -y
```

## 4. Instala los metapaquetes necesarios

```bash
sudo apt install -y \
  kali-tools-information-gathering \
  kali-tools-passwords \
  kali-tools-web \
  seclists
```

## 5. Prepara los diccionarios

```bash
cd /usr/share/wordlists/
sudo gunzip rockyou.txt.gz
ln -s /usr/share/wordlists/rockyou.txt ~/rockyou.txt
```

Verifica que el archivo existe y no está vacío:

```bash
wc -l ~/rockyou.txt
```

## 6. Crea tu máquina víctima

Crea una segunda máquina virtual (por ejemplo, Ubuntu Server) en la **misma red aislada**, con el servicio SSH habilitado:

```bash
sudo apt update && sudo apt install -y openssh-server
sudo systemctl enable --now ssh
```

Anota su dirección IP interna con `ip a`.

## 7. Confirma conectividad entre Kali y tu víctima

Desde Kali, verifica que alcanzas tu máquina víctima por la red interna:

```bash
ping -c 3 <IP_DE_TU_VICTIMA>
```

## 8. Toma un snapshot de ambas máquinas

Antes de pasar al siguiente módulo, toma una instantánea de Kali y de tu máquina víctima. Es el punto al que regresarás si algo se rompe.

## Checklist de la práctica

- [ ] Red aislada creada, sin modo puente hacia tu red real.
- [ ] Kali actualizado y con los metapaquetes instalados.
- [ ] `rockyou.txt` descomprimido y accesible.
- [ ] Máquina víctima con SSH activo, en la misma red aislada.
- [ ] Snapshot tomado de ambas máquinas.
