# State Reporting

For reference purposes, the following are examples of state messages published by an Adafruit Huzzah running Tasmota 9.3.0 in various configurations.  The JSON has been expanded to a more easily readable format (actual state messages don't include the line breaks and spaces), and some fields (MAC, IP, SSID, etc.) have been masked for privacy reasons. The Huzzah was configured with `MQTT Topic` set to `huzzah_1`.

## Single Relay with Toggle

**Tasmota Template:**  
`{"NAME":"Huzzah","GPIO":[17,0,56,0,255,255,0,0,255,255,255,255,255],"FLAG":0,"BASE":18}`

**State:**  
Topic: `tele/huzzah_1/state`, `retain=false`

```jsonc
{
    "Time": "2021-02-23T19:49:19",
    "Uptime": "0T01:05:10",
    "UptimeSec": 3910,
    "Heap": 28,
    "SleepMode": "Dynamic",
    "Sleep": 50,
    "LoadAvg": 19,
    "MqttCount": 1,
    "POWER": "OFF",
    "Wifi": {
        "AP": 1,
        "SSId": "xxxx",
        "BSSId": "xx:xx:xx:xx:xx:xx",
        "Channel": 6,
        "RSSI": 70,
        "Signal": -65,
        "LinkCount": 1,
        "Downtime": "0T00:00:04"
    }
}
```

## iFan03 Fan & Light Controller

**Tasmota Template:**  
`{"NAME":"Huzzah","GPIO":[32,0,320,0,3200,3232,0,0,512,256,225,226,227,0],"FLAG":0,"BASE":71}`

**State:**  
Topic: `tele/huzzah_1/state`, `retain=false`

```jsonc
{
    "Time": "2021-02-24T15:57:44",
    "Uptime": "0T10:15:10",
    "UptimeSec": 36910,
    "Heap": 27,
    "SleepMode": "Dynamic",
    "Sleep": 50,
    "LoadAvg": 19,
    "MqttCount": 1,
    "POWER1": "OFF",
    "FanSpeed": 0,
    "Wifi": {
        "AP": 1,
        "SSId": "xxxx",
        "BSSId": "xx:xx:xx:xx:xx:xx",
        "Channel": 6,
        "RSSI": 70,
        "Signal": -65,
        "LinkCount": 1,
        "Downtime": "0T00:00:04"
    }
}
```

## Single Channel Dimmer

**Tasmota Template:**  
`{"NAME":"Huzzah","GPIO":[34,0,33,0,576,322,0,0,321,416,320,96,256,0],"FLAG":0,"BASE":73}`

**State:**  
Topic: `tele/huzzah_1/state`, `retain=false`
```jsonc
{
    "Time": "2021-03-07T23:12:34",
    "Uptime": "0T00:05:09",
    "UptimeSec": 309,
    "Heap": 28,
    "SleepMode": "Dynamic",
    "Sleep": 10,
    "LoadAvg": 99,
    "MqttCount": 1,
    "POWER": "ON",
    "Dimmer": 7,
    "Fade": "OFF",
    "Speed": 1,
    "LedTable": "ON",
    "Wifi": {
        "AP": 1,
        "SSId": "xxxx",
        "BSSId": "xx:xx:xx:xx:xx:xx",
        "Channel": 11,
        "RSSI": 62,
        "Signal": -69,
        "LinkCount": 1,
        "Downtime": "0T00:00:03"
    }
}
```
