# industrial-monitoring-platform
Plateforme de supervision industrielle développée en C# et .NET 8 avec WPF pour la visualisation temps réel de capteurs, la gestion d'alarmes et l'enregistrement de données.

## Current Architecture

CSV Playback → WPF Dashboard → SQLite Historian

Sensor data is replayed from a CSV file, visualized in real time, and persisted to a local SQLite database for future historical analysis.

## Features

* Real-time monitoring dashboard built with WPF
* Simulated industrial sensor playback from CSV
* Live temperature trending using LiveCharts
* Alarm and warning system with color-coded status indicators
* Historical readings displayed in a DataGrid
* SQLite-based historian for persistent sensor data logging
* Retrieval of recent sensor readings from the historian database

Pour que ça tourne:
dans le NuGet manager de Visual Studio, installer le package LiveChartsCore.SkiaSharpView.WPF ainsi que Microsoft.Data.Sqlite


## Capture d'écran du dashboard:

![Industrial Monitoring Dashboard](screenshots/dashboard.png)


