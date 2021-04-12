# Digispark-CPU-Meter
USB-HID-Device CPU meter indicator

<image src="./img/IMG_1914.png" width="30%"/>

NotePC<br>
<image src="./img/IMG_1936.png" width="30%"/> 

RaspberryPi<br>
<image src="./img/IMG_1958.png" width="30%"/> 

# Directories & Files
+DigiCPUMeter ... Arduino Program Files<br>
+CPUMeter ... Windows Program Files<br>
+CPUMeter.RP ... RaspberryPi Program Files<br>
+img ... Markdown image files<br>
-LICENSE ... License text<br>
-README.md ... this file<br>

## Circuit diagram
<image src="./img/circuit.png" width="50%"/> 

# Windows

## build
* VisualStudio 2019 (CPUMeter.exe) 
* ArduinoID 1.8.12 (DigiCPUMeter.ino)

comment out :DigiCPUMeter.ino
```cpp 
//#define _RASBERRYPI // to use RaspberryPi
```

## execute

```  CPUMeter.exe ```

# RaspberryPi

## build
Enable line :DigiCPUMeter.ino

```cpp
#define _RASBERRYPI // to use RaspberryPi
```

## execute

### required
[node-hid](https://www.npmjs.com/package/node-hid)
[usb-detection](https://www.npmjs.com/package/usb-detection) 
[dstat](https://qiita.com/ryuichi1208/items/387fa1cba44690c3db9b) 


```$ sudo node CPUMeter.js > /dev/null &```

# Special Thanks!!! & License
* https://qiita.com/takeru@github/items/42873e1a7e0aef830eea
* http://milkandlait.blogspot.com/2017/09/digisparkgcc-usb-hid.html 
* http://www.technoblogy.com/show?2EA7
* https://github.com/obdev/v-usb
* https://www.zer7.com/software/hidsharp
* https://wave.hatenablog.com/entry/2017/03/11/083000
* https://qiita.com/ryuichi1208/items/387fa1cba44690c3db9b

