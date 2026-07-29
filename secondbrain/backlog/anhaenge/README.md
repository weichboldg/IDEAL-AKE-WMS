# Anhaenge

Zusaetzliche Input-Dateien fuer Backlog-Anforderungen: Schnittstellendoku,
Screenshots, PDFs, Beispiel-Exporte.

Konvention: ein Unterordner PRO Backlog-Anforderung, benannt wie der Backlog-
Slug (ohne Datum reicht, wenn eindeutig):

```
anhaenge/
  2026-07-oseon-import/
    oseon-schnittstelle-v2.pdf
    zielmaske.png
  ideal-belegnummer/
    beispiel-beleg.pdf
```

In der Backlog-Notiz per Frontmatter `anhaenge:` referenzieren und im Text
verlinken (`[[...]]` fuer Doku, `![[...]]` fuer Bilder). Details + Beispiel:
siehe ../README.md, Abschnitt "Anhaenge".

WICHTIG: Anforderungen mit Anhaengen interaktiv spezifizieren (`claude`
starten, Anhaenge mitgeben) - der Hintergrund-Watcher liest Bilder/PDFs
unzuverlaessig und meldet solche Notizen darum als "interaktiv noetig".
