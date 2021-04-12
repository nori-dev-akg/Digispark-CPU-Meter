const HID = require('node-hid');
const usbDetect = require('usb-detection');

HID.setDriverType('libusb');
var VENDOR_ID = 5824; // 0x16c0
var PRODUCT_ID = 1500; //0x05dc
var REPORT_ID = 0;
var REPORT_LENGTH = 5;

process.on('SIGINT', function() {
  //終了処理…
  usbDetect.stopMonitoring();
   process.exit();
});

usbDetect.startMonitoring();

usbDetect.find(VENDOR_ID, PRODUCT_ID, function(err, devices) {
  if(err) {
    console.log(devices, err);
  } else if(devices.length>0) {
    startHID();
  }
});

usbDetect.on('add:' + VENDOR_ID + ':' + PRODUCT_ID, function(device) {
  console.log('add', device);
  if(device) {
    startHID();
  }
});

usbDetect.on('remove:' + VENDOR_ID + ':' + PRODUCT_ID, function(device) {
  console.log('remove', device);
  if(device) {
    if(childProcess) {
      childProcess.kill();
      childProcess = undefined;
    }
  }
});

//usbDetect.on('add', function(device) { console.log('add', device); });
//usbDetect.on('remove:5824:1500', function(device) { console.log('remove', device); usbDetect.stopMonitoring();});

var device;
function startHID() {
  try {
      console.log("opening first device");
      device = new HID.HID( VENDOR_ID, PRODUCT_ID );
      startStat();
  } catch(err) {
    device = null;
    console.log(err);
    usbDetect.stopMonitoring();
    process.exit(1);
  }
}

function sendCommand() {
  var featureReport = new Array(REPORT_LENGTH + 1).fill(0);  // + 1 for reportId
  featureReport[0] = REPORT_ID;
  for (var i = 0; i < arguments.length; i++) {
      featureReport[i + 1] = arguments[i];
  }
  try {
    device.sendFeatureReport(featureReport);
  } catch(err) {
    console.log(err);
  }
};

const { spawn } = require('child_process');
var childProcess;
function startStat() {
  if(childProcess) {
    childProcess.kill();
  }
  childProcess = spawn('dstat', ['--nocolor','1'])
  childProcess.stdout.on('data', (chunk) => {
    let values = chunk.toString().replace(/\x1B|\[0;0m|/g, '').replace(/\|/g, ' ').split(/\s+|\|/);
    //console.log(values);
    //console.log(new Date(), values.length, values[1]);
    if(values.length == 15 && values[1] != null && isFinite(values[1])) {
      let cpu = 100 - values[3];
      let netr = values[8];
      let cr = netr.slice(-1); netr = netr.slice(0,-1); netr = (cr == 'k') ? netr * 1024 : ((cr == 'M') ? netr * 1024 * 1024 : netr * 1);
      let nets = values[9];
      let cs = nets.slice(-1); nets = nets.slice(0,-1); nets = (cs == 'k') ? nets * 1024 : ((cs == 'M') ? nets * 1024 * 1024 : nets * 1);
      let net = Math.round((netr + nets) / 1024); // KB
      net = (net >= 1000) ? (net / 1000) * (-1) : net; // 1000 ==>  -1 (MB)
      const buffer = new ArrayBuffer(2);
      const view = new DataView(buffer);
      view.setInt16(0, net);
      let net2 = view.getUint8(0);
      let net1 = view.getUint8(1);
      let so = values[11].slice(-1) != '0' ? 1 : 0;
      let temp = Math.round(getCPUTemperature());
      //console.log('netr=', netr, 'nets=', nets, 'net1=', net1, 'net2=', net2, 'net=', net);
      console.log(new Date(), 'cpu=', cpu, 'so=', so, 'temp=', temp, 'net=', net);
      sendCommand(cpu, so, temp, net1, net2);
    }
  })
}

const { execSync } = require('child_process');
var regex = /temp=([^'C]+)/;
function getCPUTemperature() {
  const gpuTempeturyCommand = '/opt/vc/bin/vcgencmd measure_temp';
  temp = execSync(gpuTempeturyCommand).toString();
  temp = regex.exec(temp.toString("utf8"))[1];
  return temp;
}
