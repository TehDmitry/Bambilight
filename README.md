# Bambilight

A simple implementation to send the colors from a Windows-PC-Screen to a LED-Strip over Arduino.

Maybe some people would call it "Ambilight" ;)

The Arduino sketch is written in standard C and the Windows application in C#.

Requirements:
=============

- Arduino or compatible device

- Optional AC adapter if using too many LEDs. USB provides 500mA (USB2.0) or 1000mA (USB3.0)
  and each LED (in example "WS2812B") can pull up to 60mA per LED on full brightness.

- Windows PC with DirectX9 and hardware accelerated graphics

- FastLED compatible LED-Stripe (See: https://github.com/FastLED/FastLED/wiki/Chipset-reference)

How to use:
===========

!!! If you just want to use Bambilight without compiling the Windows application by yourself,
you only need the contents of the "Binary" folder and the bambilight.ino Arduino sketch. !!!

1. Customize the .ino sketch (NUM_LEDS, LED_DATA_PIN, LED_TYPE) and flash it to your arduino device.

2. Connect the arranged LED-Stripe to your arduino device and the arduino to your PC.

3. Start the Bambilight tool and configure your screen setup (maybe overlay mode helps).

Videos:
=======

Demo-Video: https://www.youtube.com/watch?v=isHl0YpEQ1A

Future Features:
================

- Some faster screen capturing. Depending on system the capturing takes about 30-40ms

- Serial speed calculations for max led setting

- Sending brightness value to arduino instead of fix value

- Dynamic control limit for min and max values

- Dynamic arrow positions in overlay


Known Bugs:
===========

- Wrong start number in overlay if having only 1 column and activated "Mirror X-Axis"


Third-Party:
============

- Using SlimDX Library in Version 4.0.13.44 (Copyright (c) 2007-2011 SlimDX Group)
- Using FastLED Library in Version 3.1 (Copyright (c) 2013 FastLED)
