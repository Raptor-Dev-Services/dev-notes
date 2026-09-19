#!/usr/bin/env python3
"""Puerta de calidad de dev-notes. Correr desde la raiz del repo: python3 herramientas/verifica-docs.py

Comprueba cinco cosas y sale con codigo 1 si alguna falla:
  1. Enlaces internos que apuntan a un archivo que no existe.
  2. Titulo H1 con un numero distinto al del nombre del archivo.
  3. Dos documentos con el mismo prefijo numerico en una carpeta.
  4. Nombres de clientes o productos reales (el material va con placeholders).
  5. Guiones largos y flechas en la prosa (van en tablas, codigo y encabezados, no en el texto).

Para verla fallar: cambia un enlace a un archivo inexistente y vuelve a correrla.
"""
import os, re, sys, collections
from urllib.parse import unquote

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
os.chdir(RAIZ)
PROHIBIDOS = re.compile(r'socio ?fit|\bgtm\b|gtm[-_.]|ferreira', re.I)

md = sorted(os.path.relpath(os.path.join(dp, f))
            for dp, dn, fn in os.walk('.')
            if '.git' not in dp.split(os.sep) and 'referencias-csharp' not in dp
            for f in fn if f.endswith('.md'))
leer = lambda p: open(p, encoding='utf-8-sig').read()

def fuera_de_codigo(texto):
    """Las lineas de prosa: sin bloques de codigo, tablas, encabezados ni codigo en linea."""
    dentro = False
    for l in texto.split('\n'):
        if l.lstrip().startswith('```'):
            dentro = not dentro; continue
        s = l.strip()
        if dentro or not s or s[0] in '|#': continue
        yield ''.join(l.split('`')[0::2])

fallos = collections.defaultdict(list)
for p in md:
    t = leer(p)
    for m in re.finditer(r'\[[^\]]*\]\(([^)\s]+)\)', re.sub(r'```.*?```', '', t, flags=re.S)):
        destino = m.group(1)
        if destino.startswith(('http', 'mailto:', '#')): continue
        ruta = destino.split('#')[0]
        if ruta and not os.path.exists(os.path.normpath(os.path.join(os.path.dirname(p), unquote(ruta)))):
            fallos['enlaces rotos'].append(f'{p} -> {destino}')
    num = re.match(r'(\d+)-', os.path.basename(p))
    if num:
        dentro = False
        for l in t.split('\n'):
            if l.lstrip().startswith('```'): dentro = not dentro; continue
            if not dentro and l.startswith('# '):
                h = re.match(r'# (\d+)\s', l)
                if h and int(h.group(1)) != int(num.group(1)):
                    fallos['H1 con numero distinto al archivo'].append(f'{p}: {l.strip()[:50]}')
                break
    for l in t.split('\n'):
        if PROHIBIDOS.search(l):
            fallos['nombres reales en vez de placeholders'].append(f'{p}: {l.strip()[:70]}')
    for l in fuera_de_codigo(t):
        if '—' in l or '→' in l:
            fallos['guiones largos o flechas en prosa'].append(f'{p}: {l.strip()[:70]}')

for carpeta in {os.path.dirname(p) for p in md}:
    cuenta = collections.Counter(re.match(r'(\d+)-', os.path.basename(p)).group(1)
                                 for p in md if os.path.dirname(p) == carpeta and re.match(r'\d+-', os.path.basename(p)))
    for prefijo, veces in cuenta.items():
        if veces > 1: fallos['prefijo numerico duplicado'].append(f'{carpeta}: hay {veces} documentos {prefijo}-')

print(f'{len(md)} documentos revisados')
for nombre in ('enlaces rotos', 'H1 con numero distinto al archivo', 'prefijo numerico duplicado',
               'nombres reales en vez de placeholders', 'guiones largos o flechas en prosa'):
    items = fallos.get(nombre, [])
    print(f'  {"FALLA" if items else "  ok "}  {nombre}: {len(items)}')
    for x in items[:5]: print(f'          {x}')
    if len(items) > 5: print(f'          ... y {len(items)-5} mas')
sys.exit(1 if fallos else 0)
