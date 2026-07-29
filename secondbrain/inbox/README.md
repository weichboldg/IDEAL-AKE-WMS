# Inbox (nur interactive-Modus)

Der Watcher legt hier fertige Auftraege ab, wenn er im interactive-Modus laeuft.
Du fuehrst sie in deiner OFFENEN Claude-Session aus - mit vollem Kontext und
ohne Turn-Limit:

1. Der Watcher meldet im Log (und optional per Ton) einen neuen Auftrag.
2. In deiner offenen `claude`-Session tippst du:  @secondbrain/inbox/<datei>.md
3. Claude fuehrt den Auftrag aus, du siehst zu und kannst eingreifen.
4. Danach die Inbox-Datei loeschen oder nach inbox/erledigt/ verschieben.

Diese Dateien sind fluechtige Auftraege, keine Denk-Inhalte - sie werden vom
Watcher erzeugt und nach Ausfuehrung entfernt. Der Ordner ist von der
Watcher-Beobachtung ausgenommen (kein Ereignis-Echo) und via .gitignore aus
dem Repo ausgeschlossen. Die Auftrags-VORLAGEN dagegen liegen versioniert in
../prompts/.
