#make data into a list of rows with timestamp and the other things after on the same line as a csv
#write it all to a file

import csv
import random
from datetime import datetime, timedelta

rows = []

timestamp = datetime(2026, 6, 9, 8, 0, 0)

temperature = 72.0
pressure = 4.8
flow_rate = 120.0
tank_level = 65.0

for i in range(3600):  # 1 hour of data, 1 row per second
    temperature += random.uniform(-0.2, 0.2)
    pressure += random.uniform(-0.05, 0.05)
    flow_rate += random.uniform(-3, 3)
    tank_level += random.uniform(-0.05, 0.05)

    temperature = max(65, min(85, temperature))
    pressure = max(3, min(7, pressure))
    flow_rate = max(80, min(160, flow_rate))
    tank_level = max(20, min(90, tank_level))

    rows.append([
        timestamp.isoformat(),
        round(temperature, 2),
        round(pressure, 2),
        round(flow_rate, 2),
        round(tank_level, 2)
    ])

    timestamp += timedelta(seconds=1)

with open("../data/sensor_readings.csv", "w", newline="") as file:
    writer = csv.writer(file)
    writer.writerow([
        "Timestamp",
        "TemperatureC",
        "PressureBar",
        "FlowRateLMin",
        "TankLevelPercent"
    ])
    writer.writerows(rows)