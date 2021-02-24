# AutoDiscovery Protocol

For reference purposes, the following are examples of autodiscovery messages published by an Adafruit Huzzah running Tasmota 9.3.0 in various configurations.  The JSON has been expanded to a more easily readable format (actual discovery messages don't include the line breaks and spaces), and IP and MAC address have been replaced with `x.x.x.x` and `xxxxxxxxxxxx` respectively.


## Single Relay with Toggle

**Tasmota Template:**  
`{"NAME":"Huzzah","GPIO":[17,0,56,0,255,255,0,0,255,255,255,255,255],"FLAG":0,"BASE":18}`

**Autodiscovery:**  
Topic: `tasmota/discovery/xxxxxxxxxxxx/config`, `retain=True`

```jsonc
{
    "ip": "x.x.x.x",                                                // IP Address
    "dn": "Tasmota",                                                // Device Name 
    "fn": ["Tasmota", null, null, null, null, null, null, null],    // Friendly Names
    "hn": "tasmota-huzzah",                                         // Host Name
    "mac": "xxxxxxxxxxxx",                                          // Full MAC as Device ID
    "md": "Huzzah",                                                 // Module or Template Name
    "ty": 0,                                                        // Flag for TuyaMCU devices
    "if": 0,                                                        // Flag for Ifan devices
    "ofln": "Offline",                                              // Payload Offline
    "onln": "Online",                                               // Payload Online
    "state": ["OFF", "ON", "TOGGLE", "HOLD"],                       // State Text
    "sw": "9.3.0",                                                  // Software Version
    "t": "huzzah_1",                                                // Topic
    "ft": "%prefix%/%topic%/",                                      // Full Topic
    "tp": ["cmnd", "stat", "tele"],                                 // Topic Prefixes
    "rl": [1, 0, 0, 0, 0, 0, 0, 0],                                 // Inputs / Outputs
    "swc": [-1, -1, -1, -1, -1, -1, -1, -1],                        // Inputs / Outputs
    "swn": [null, null, null, null, null, null, null, null],        // Inputs / Outputs
    "btn": [0, 0, 0, 0, 0, 0, 0, 0],                                // Inputs / Outputs
    "so": {                                                         // SetOptions
        "4": 0,     // (MQTT) Switch between RESULT (0) or COMMAND (1)
        "11": 0,    // (Button) Swap (1) button single and double press functionality
        "13": 0,    // (Button) Support only single press (1) to speed up button press recognition
        "17": 0,    // (Light) Switch between decimal (1) or hexadecimal (0) output
        "20": 0,    // (Light) Control power in relation to Dimmer/Color/Ct changes (1)
        "30": 0,    // (HAss) enforce autodiscovery as light (1)
        "68": 0,    // (Light) Enable multi-channels PWM (1) instead of Color PWM (0)
        "73": 0,    // (Button) Detach buttons from relays (1) and enable MQTT action state for multipress
        "82": 0,    // (Alexa) Reduced CT range for Alexa (1)
        "114": 0,   // (Switch) Detach Switches from relays and enable MQTT action state for all the SwitchModes (1)
        "117": 0    // (Light) run fading at fixed duration instead of fixed slew rate
    },
    "lk": 1,                                                        // lighting (?)
    "lt_st": 0,                                                     // light subtype
    "sho": [0, 0, 0, 0],                                            // Shutter Options
    "ver": 1                                                        // Discovery Version
}
```

Topic: `tasmota/discovery/xxxxxxxxxxxx/sensors`, `retain=True`
```jsonc
{
    "sn": {
        "Time":"2021-02-23T18:44:24"
    },
    "ver": 1
}
```


**Tasmota Template:**  
`{"NAME":"Huzzah","GPIO":[32,0,320,0,3200,3232,0,0,512,256,225,226,227,0],"FLAG":0,"BASE":71}`

**Autodiscovery:**  
Topic: `tasmota/discovery/xxxxxxxxxxxx/config`, `retain=false`

```jsonc
{
    "ip": "x.x.x.x",
    "dn": "Huzzah 1",
    "fn": ["Relay 1", "Tasmota2", "Tasmota3", "Tasmota4", null, null, null, null],
    "hn": "tasmota-huzzah",
    "mac": "xxxxxxxxxxxx",
    "md": "Huzzah",
    "ty": 0,
    "if": 1,
    "ofln": "Offline",
    "onln": "Online",
    "state": ["OFF", "ON", "TOGGLE", "HOLD"],
    "sw": "9.3.0",
    "t": "huzzah_1",
    "ft": "%prefix%/%topic%/",
    "tp": ["cmnd", "stat", "tele"],
    "rl": [2, 0, 0, 0, 0, 0, 0, 0],
    "swc": [-1, -1, -1, -1, -1, -1, -1, -1],
    "swn": [null, null, null, null, null, null, null, null],
    "btn": [0, 0, 0, 0, 0, 0, 0, 0],
    "so": {
        "4": 0,
        "11": 0,
        "13": 0,
        "17": 0,
        "20": 0,
        "30": 1,
        "68": 0,
        "73": 0,
        "82": 0,
        "114": 0,
        "117": 0
    },
    "lk": 1,
    "lt_st": 0,
    "sho": [0, 0, 0, 0],
    "ver": 1
}
```

Topic: `tasmota/discovery/xxxxxxxxxxxx/sensors`, `retain=True`
```jsonc
{
    "sn": {
        "Time":"2021-02-23T18:44:24"
    },
    "ver": 1
}
```