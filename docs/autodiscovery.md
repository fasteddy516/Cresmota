# AutoDiscovery Protocol

For reference purposes, the following are examples of autodiscovery messages published by an Adafruit Huzzah running Tasmota 9.3.0 in various configurations.  The JSON has been expanded to a more easily readable format (actual discovery messages don't include the line breaks and spaces), and some fields (MAC, IP, SSID, etc.) have been masked for privacy reasons. The Huzzah was configured with `MQTT Topic` set to `huzzah_1`.

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
    "ty": 0,                                                        // Flag for TuyaMCU devices (Module = 54-Tuya MCU or 57-SK03 Outdoor)
    "if": 0,                                                        // Flag for Ifan devices (Module = 44-Sonoff iFan02 or 71-Sonoff iFan03)
    "ofln": "Offline",                                              // Payload Offline
    "onln": "Online",                                               // Payload Online
    "state": ["OFF", "ON", "TOGGLE", "HOLD"],                       // State Text
    "sw": "9.3.0",                                                  // Software Version
    "t": "huzzah_1",                                                // Topic
    "ft": "%prefix%/%topic%/",                                      // Full Topic
    "tp": ["cmnd", "stat", "tele"],                                 // Topic Prefixes
    "rl": [1, 0, 0, 0, 0, 0, 0, 0],                                 // Relays (3=Shutter, 2=Light, 1=Basic, 0=None)
    "swc": [-1, -1, -1, -1, -1, -1, -1, -1],                        // Switches (-1=None, 0=Toggle, 1=Follow, 2=-Follow, 3=Pushbutton, 4=-Pushbutton, 5=PB_Hold, 6=-PB_Hold, 7=PB_Toggle, 8=Toggle_Multi, 9=Follow_Multi, 10=-Follow_Multi, 11=Pushhold_Multi, 12=-Pushhold_Multi, 13=Push_On, 14=-Push_On, 15=Push_Ignore) (Tied to SetOption114)
    "swn": [null, null, null, null, null, null, null, null],        // Switch Name? (Return of GetSwitchText(i).c_str()) (Tied to SetOption114)
    "btn": [0, 0, 0, 0, 0, 0, 0, 0],                                // Button (0=Disable, 1=Enable) (Tied to SetOption73)
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
    "lk": 1,                                                        // light RGB/CT link (1=true, 0=false) 
    "lt_st": 0,                                                     // light subtype (0=None, 1=Single, 2=ColdWarm, 3=RGB, 4=RGBW, 5=RGBCW)
    "sho": [0, 0, 0, 0],                                            // Shutter Options (Bit0=invert, Bit1=locked, Bit2=End stop time enabled, Bit3=webButtons Inverted)
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

## iFan03 Fan & Light Controller

**Tasmota Template:**  
`{"NAME":"Huzzah","GPIO":[32,0,320,0,3200,3232,0,0,512,256,225,226,227,0],"FLAG":0,"BASE":71}`

**Autodiscovery:**  
Topic: `tasmota/discovery/xxxxxxxxxxxx/config`, `retain=true`

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

## Single Channel Dimmer

**Tasmota Template:**  
`{"NAME":"Huzzah","GPIO":[34,0,33,0,576,322,0,0,321,416,320,96,256,0],"FLAG":0,"BASE":73}`

**Autodiscovery:**  
Topic: `tasmota/discovery/xxxxxxxxxxxx/config`, `retain=true`
```jsonc
{
    "ip": "***REMOVED***",
    "dn": "Huzzah 1",
    "fn": ["Relay 1", null, null, null, null, null, null, null],
    "hn": "tasmota-huzzah",
    "mac": "***REMOVED***",
    "md": "Huzzah",
    "ty": 0,
    "if": 0,
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
    "lt_st": 1,
    "sho": [0, 0, 0, 0],
    "ver": 1
}
```

Topic: `tasmota/discovery/xxxxxxxxxxxx/sensors`, `retain=True`
```jsonc
{
    "sn": {
        "Time": "2021-03-07T23:07:39"
    },
    "ver": 1
}
```