# Glosario - S02 - Usuarios, permisos y paquetes

> Definiciones de los términos técnicos de la sesión 2.
> Ordenado alfabéticamente.

---

## A

### ACL
**Categoría:** Permisos
**Definición:** Lista de control de acceso: permisos adicionales para usuarios o grupos concretos sobre un archivo o carpeta, más allá de dueño, grupo y otros. Una ACL *por defecto* en una carpeta se aplica a todo lo que se cree dentro. Se administran con `setfacl` y `getfacl`, del paquete `acl`.
**En el curso:** `setfacl -d -m g:equipo:rwX /srv/equipo` hace que el grupo pueda escribir aunque quien crea el archivo traiga `umask 022`. El `+` en `ls -l` avisa que hay una ACL.

---

### apt
**Categoría:** Paquetes
**Definición:** Gestor de paquetes de alto nivel de Debian y Ubuntu. Conoce los repositorios, resuelve dependencias y decide qué versión instalar; para instalar de verdad llama a `dpkg`. `apt update` refresca el catálogo y `apt upgrade` actualiza lo instalado.
**En el curso:** `apt policy` dice qué versión está instalada, cuál se instalaría y de qué repositorio sale.

---

## B

### Bit setgid
**Categoría:** Permisos
**Definición:** En un directorio, hace que todo lo que se cree dentro herede el grupo del directorio en lugar del grupo principal de quien lo crea. Se ve como una `s` en la posición de ejecución del grupo y se activa con `chmod g+s` o con un `2` al frente del octal. En un ejecutable, hace que corra con el grupo del archivo.
**En el curso:** `/srv/equipo` en `2770`: sin el bit, los archivos de `beto` nacen con grupo `beto` y `ana` no puede editarlos.

---

### Bit setuid
**Categoría:** Permisos
**Definición:** En un ejecutable, hace que corra con los permisos de su dueño y no con los de quien lo ejecuta. Se ve como una `s` en la ejecución del dueño. `passwd` y `sudo` lo necesitan; un setuid de root que no viene de un paquete del sistema es una alarma.
**En el curso:** `find / -xdev -perm -4000 -type f` lista los setuid de la VM; esa lista se guarda para compararla después.

---

## C

### cloud-init
**Categoría:** Nube
**Definición:** Programa que configura una VM de nube en su primer arranque: crea el usuario inicial, instala su llave SSH, ajusta red y nombre. En las imágenes de Ubuntu suele dejar en `/etc/sudoers.d/` un archivo que da sudo sin contraseña al usuario inicial.
**En el curso:** Revisar ese archivo es el primer paso de la tarea de auditoría de sudo.

---

## D

### dpkg
**Categoría:** Paquetes
**Definición:** Herramienta de bajo nivel que instala, quita y registra paquetes `.deb` ya descargados. Sabe qué archivos instaló cada paquete: `dpkg -L` los lista y `dpkg -S` dice qué paquete instaló un archivo. En `dpkg -l`, `ii` es instalado y `rc` es quitado con configuración pendiente.
**En el curso:** `dpkg -S /usr/sbin/nologin` responde `login`; si un archivo no es de ningún paquete, alguien lo puso a mano.

---

## G

### GID
**Categoría:** Usuarios
**Definición:** Identificador numérico de un grupo. El kernel trabaja con números, no con nombres: `/etc/group` traduce entre ambos. Cada usuario tiene un grupo principal (campo 4 de `/etc/passwd`) y puede estar en otros, llamados suplementarios.
**En el curso:** `id ana` muestra su grupo principal `ana` y el suplementario `equipo`.

---

### Grupo propio
**Categoría:** Usuarios
**Definición:** Convención de Ubuntu y Debian de crear, para cada persona, un grupo con su mismo nombre del que solo ella es miembro. Con `USERGROUPS_ENAB yes`, `pam_umask` le da `umask 002` en lugar de `022`, porque dar escritura a un grupo de una sola persona no expone nada.
**En el curso:** Es la razón de que, en la carpeta con setgid, lo que crea `beto` quede escribible para el grupo `equipo`.

---

## M

### Mínimo privilegio
**Categoría:** Seguridad
**Definición:** Principio de dar a cada usuario, proceso o servicio solo los permisos que necesita para su función, y ninguno más. Limita lo que un atacante puede hacer si compromete esa pieza.
**En el curso:** El lema de la sesión: empieza en el sistema operativo, con el usuario que corre cada cosa, y se demuestra con un intento que falla.

---

## N

### nologin
**Categoría:** Usuarios
**Definición:** Programa (`/usr/sbin/nologin`) que se usa como shell de cuentas que no deben iniciar sesión. Imprime `This account is currently not available.` y termina con error. No impide que root o alguien con sudo ejecute comandos como esa cuenta.
**En el curso:** Shell de `api-lab`: `su - api-lab` no entra, `sudo -u api-lab bash -c 'id'` sí corre.

---

### NOPASSWD
**Categoría:** sudo
**Definición:** Etiqueta de una regla de sudoers que permite ejecutar los comandos de esa regla sin pedir contraseña. Razonable para un comando exacto que se automatiza; peligrosa en `NOPASSWD:ALL`, que convierte una llave SSH robada en root inmediato.
**En el curso:** La regla de `beto` usa `NOPASSWD` para un solo comando; la de cloud-init suele usarla para todo.

---

### Notación octal
**Categoría:** Permisos
**Definición:** Forma de escribir permisos con un dígito por terna: lectura vale 4, escritura 2 y ejecución 1, y se suman. `640` es `rw-r-----`. Un cuarto dígito al frente codifica los bits especiales: 4 setuid, 2 setgid, 1 sticky. Fija las tres ternas de golpe; la forma simbólica (`g+w`, `o=`) cambia solo lo que nombra.
**En el curso:** `chmod 2770 /srv/equipo` y `chmod 640` para un archivo de configuración con secretos.

---

## P

### Permisos rwx
**Categoría:** Permisos
**Definición:** Tres derechos para dueño, grupo y otros. En un archivo: leer, cambiar el contenido, ejecutarlo. En un directorio: listar los nombres, crear o borrar lo que hay dentro, y atravesarlo para llegar a lo que contiene. Linux aplica solo una terna: la del dueño si eres el dueño, si no la del grupo, si no la de otros.
**En el curso:** `intruso` no lee un archivo `rw-r--r--` porque la carpeta no le da `x`.

---

## R

### Repositorio
**Categoría:** Paquetes
**Definición:** Servidor con paquetes y sus índices, firmados con una llave. En Ubuntu 24.04 se declaran en formato deb822 en `/etc/apt/sources.list.d/ubuntu.sources`, con `Types`, `URIs`, `Suites`, `Components` y `Signed-By`, que indica la llave para verificarlo.
**En el curso:** Un repositorio de terceros va en su propio archivo y con su propia llave.

---

### root
**Categoría:** Usuarios
**Definición:** La cuenta con UID 0. El kernel no le revisa permisos de archivos: puede leer, escribir y cambiar dueño de todo. En Ubuntu no tiene contraseña y no se entra con ella; se usa a través de sudo.
**En el curso:** Correr una API como root hace que cualquier fallo de la API alcance todo el sistema.

---

## S

### shadow
**Categoría:** Usuarios
**Definición:** El archivo `/etc/shadow`, con los hashes de contraseña y sus fechas de cambio y caducidad. Solo lo leen root y el grupo `shadow` (`640`). Su contenido no se copia ni se muestra; `passwd -S` resume el estado de una cuenta sin enseñar el hash.
**En el curso:** `passwd -S api-lab` muestra `L`: cuenta bloqueada, sin contraseña.

---

### Sticky bit
**Categoría:** Permisos
**Definición:** En un directorio donde muchos pueden escribir, impide que alguien borre o renombre archivos que no son suyos. Se ve como una `t` al final de los permisos y se activa con `chmod +t`. Es lo que hace segura la carpeta `/tmp`.
**En el curso:** Con `+t` en `/srv/equipo`, `ana` recibe `Operation not permitted` al borrar un archivo de `beto`.

---

### sudo
**Categoría:** sudo
**Definición:** Programa que permite a un usuario ejecutar comandos como otro, normalmente root, según las reglas de sudoers. Pide la contraseña de quien lo usa, no la de root, y registra cada comando en el journal. `sudo -l -U <cuenta>` dice qué puede hacer cada quien.
**En el curso:** `journalctl _COMM=sudo` muestra quién usó sudo, desde qué carpeta y para qué comando.

---

### sudoers
**Categoría:** sudo
**Definición:** Las reglas de sudo: el archivo `/etc/sudoers` y los que incluye de `/etc/sudoers.d/`. Cada regla dice quién (usuario o `%grupo`), en qué máquina, como quién y qué comandos. En Ubuntu, `%sudo ALL=(ALL:ALL) ALL` da todo al grupo `sudo`.
**En el curso:** Las reglas propias van en un archivo aparte de `/etc/sudoers.d/` con permisos `0440`.

---

## U

### UID
**Categoría:** Usuarios
**Definición:** Identificador numérico de un usuario. 0 es root; en Ubuntu, del 1 al 999 son usuarios de sistema y del 1000 en adelante, personas. Los archivos guardan el UID, no el nombre: si se borra el usuario, `ls -l` muestra el número suelto.
**En el curso:** `api-lab` recibe un UID debajo de 1000 por `--system`.

---

### umask
**Categoría:** Permisos
**Definición:** Máscara de los permisos que se **quitan** al crear un archivo o directorio. Se parte de `666` para archivos y `777` para directorios: con `022` nacen `644` y `755`; con `027`, `640` y `750`. Se hereda del proceso padre; en una sesión la fija `pam_umask` a partir de `/etc/login.defs`.
**En el curso:** Las personas en Ubuntu tienen `0002` y root `0022`; con `022`, el grupo pierde la escritura en la carpeta del equipo.

---

### Usuario de servicio
**Categoría:** Usuarios
**Definición:** Cuenta sin persona detrás que existe solo para correr un programa: UID de sistema, sin carpeta personal, sin shell de entrada y sin contraseña. Le da al servicio una identidad propia con permisos limitados a lo suyo.
**En el curso:** `api-lab`, creado con `useradd --system --no-create-home --shell /usr/sbin/nologin`, corre la API desde la sesión 4.

---

## V

### visudo
**Categoría:** sudo
**Definición:** Editor seguro de las reglas de sudo: valida la sintaxis antes de guardar y se niega a dejar un archivo roto. Con `-c` solo revisa y con `-f` trabaja sobre un archivo concreto, como uno de `/etc/sudoers.d/`.
**En el curso:** `visudo -cf /tmp/90-api-lab` responde `parsed OK` antes de instalar la regla, y señala la columna exacta de un error de sintaxis.
