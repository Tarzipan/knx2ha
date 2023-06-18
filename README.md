# KNX Device Config Generator

Dieses Tool generiert eine YAML-Konfigurationsdatei für KNX-Geräte basierend auf den KNX-Addressen in einer KNX-Projektdatei.

## Voraussetzungen

- .NET Framework 4.7.2 oder höher

## Verwendung

1. Stellen Sie sicher, dass Ihre KNX-Projektdatei im ETS-Projektformat (.knxproj) vorliegt.
2. Öffnen Sie eine Befehlszeile und navigieren Sie zum Verzeichnis, in dem sich das Programm befindet.
3. Führen Sie den Befehl `knx2ha.exe` mit dem Argument `-in project.knxproj` aus, um die Konfiguration zu generieren.
4. Die generierte YAML-Konfigurationsdatei wird unter dem Namen `output.yaml` im gleichen Verzeichnis erstellt.

## Optionen

- `-in input_file.knxproj`: Gibt den Namen der KNX-Projektdatei an (Standard: *.knxproj).
- `-out output_file.yaml`: Gibt den Namen der Ausgabedatei an (Standard: output.yaml).
- `-all`: Exportiert alle Geräte, auch solche, die nicht erkannt wurden.

## Geräteerkennung

Die Geräteerkennung basiert auf spezifischen Strings in den Gerätebeschreibungen, die durch #[DeviceType]! angegeben werden, z.B. #Light!. Zusätzlich wird versucht, die einzelnen KNX-Adressen anhand einer gleichen Bezeichnung zu erkennen. 
Im besten Fall ergibt sich ein Gerät mit mehreren KNX-Adressen, die unterschiedliche DPT haben. 
Beim Schreiben der YAML-Datei werden die KNX-Adressen aufsteigend sortiert, wobei die address zuerst und die state_address danach kommt.
## Gerätekategorien

Die folgenden Gerätekategorien werden unterstützt:

- BinarySensor
- Cover
- Light
- Sensor

## Beispiel

Die generierte Ausgabedatei `output.yaml` würde die folgende YAML-Konfiguration enthalten:

```yaml
- platform: knx
  name: Light
  address: 1/0/1
  state_address: 1/0/1

- platform: knx
  name: Light
  address: 1/0/2
  state_address: 1/0/2
