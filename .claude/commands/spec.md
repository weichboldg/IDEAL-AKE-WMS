---
description: Backlog-Notiz in eine vollstaendige Spec ueberfuehren (Entwurf). Beruecksichtigt split/epic/anhaenge.
argument-hint: <pfad-zur-backlog-datei>
---
Neue Backlog-Datei verarbeiten: $1

0. Beruecksichtige das Frontmatter der Backlog-Datei:
   - split: true  -> in mehrere einzeln mergbare Teil-Specs zerlegen (spec-agent-Regeln, Teil-1/2/... + Uebersicht).
   - epic: true   -> EINE Spec mit epic: true und Etappen-Tabelle (ein Worktree, mehrere Etappen, ein Merge am Ende).
   - anhaenge:     -> die dort genannten Dateien mitlesen. Bild/PDF nur interaktiv verlaesslich; bei Unlesbarkeit als erste offene Rueckfrage vermerken, nicht raten.
1. Nutze den Subagenten task-scout: welche Backlog-Dateien haben noch keine Spec? (Idempotenz - nicht doppelt spezifizieren.)
2. Fuer jede unverarbeitete Datei: Nutze den Subagenten spec-agent -> vollstaendige Spec(s) nach secondbrain/specs/entwurf/ (Template secondbrain/_templates/spec.md, status: Entwurf, source_backlog als Wikilink, Slug endet auf -spec).
3. Committe NUR die neuen/geaenderten Dateien unter secondbrain/ mit Message "spec: <slug> (Entwurf)".

Verboten: Dateien nach specs/freigegeben verschieben, Anwendungscode aendern, mergen, main anfassen.
