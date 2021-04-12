// Digispark CPU Meter
// Copyright: (c) 2021 by nori-dev-akg
// https://github.com/nori-dev-akg/Digispark-CPU-Meter
//
// original source code
// Digispark HID Device
// http://milkandlait.blogspot.com/2017/09/digisparkgcc-usb-hid.html

#include "usbportability.h"
#include "usbdrv.h"
#include "oddebug.h"
#include <util/delay.h>

//#define nop() asm volatile("nop\n\t"::);

// USB 送受信中フラグ確認プロトタイプ
//uchar USBNOTBUSY(void);

// 送受信バッファ
uchar state = '\0';

int hidInit(void) {
    uchar i;
    DDRB  |= _BV(PB1); // 動作確認用 LED
  cli();
  usbInit();
    i=10; while(--i){ _delay_ms(100);   }
    usbDeviceDisconnect();
    i=25;   while(--i){ _delay_ms(10); }
  usbDeviceConnect();
    sei();
  /*
  while(1) {
        usbPoll(); // USB ポーリング
    }
   */
  return 0;
}

// USB 送受信
// static uchar dataBuffer[1];
uchar usbFunctionSetup(uchar data[8]) {
  usbRequest_t *rq = (usbRequest_t*)((void *)data);
  // クラスリクエストかの判定(bmRequestType の bitusbPoll5,6 == 01H)
  if((rq->bmRequestType & USBRQ_TYPE_MASK) == USBRQ_TYPE_CLASS){
    // GET_REPORT リクエスト(01H); デバイス→ホストの通信
        if(rq->bRequest == USBRQ_HID_GET_REPORT) return USB_NO_MSG; // usbFunctionRead() を呼ぶ
        // SET_REPORT リクエスト(09H); ホスト→デバイスの通信
        if(rq->bRequest == USBRQ_HID_SET_REPORT) return USB_NO_MSG; // usbFunctionWrite() を呼ぶ
    } // クラスリクエスト
    return 0;
}

// *********************************************************
// GET_REPORT リクエスト(01H); デバイス→ホストの通信
// *********************************************************
uchar usbFunctionRead(uchar *data, uchar len) {
    // 送信すべき文字がある場合、ホストに送信するバイト数(1)を返す
    data[0] = state+1; // バッファに送信データをセット
    return 1;
    // return 0; // 送信すべき文字がない場合 0 を返す
}

// Digispark.ino
void drawMeter(uint8_t cpu, uint8_t memory, uint8_t hdd, int16_t net);

// *********************************************************
// SET_REPORT リクエスト(09H); ホスト→デバイスの通信
// *********************************************************
uchar usbFunctionWrite(uchar *data, uchar len) {
    // len にホストから送られてきたバイト数(=5)が入る
    if (len>=5) {
        int16_t* pNet = (int16_t*)&data[3];
        drawMeter(data[0], data[1], data[2], *pNet);
        return len; // 受信したらlenを返す
    }
    return 0;
}

// レポートディスクリプタ
const PROGMEM char usbHidReportDescriptor[USB_CFG_HID_REPORT_DESCRIPTOR_LENGTH] = {   
    0x06, 0x00, 0xff,              // USAGE_PAGE (Generic Desktop)
    0x09, 0x01,                    // USAGE (Vendor Usage 1)
    0xa1, 0x01,                    // COLLECTION (Application)
    0x15, 0x00,                    //   LOGICAL_MINIMUM (0)
    0x26, 0xff, 0x00,              //   LOGICAL_MAXIMUM (255)
    0x75, 0x08,                    //   REPORT_SIZE (8bit)
    0x95, 0x05,                    //   REPORT_COUNT (5)
    0x09, 0x00,                    //   USAGE (Undefined)
    0xb2, 0x02, 0x01,              //   FEATURE (Data,Var,Abs,Buf)
    0xc0                           // END_COLLECTION
};
