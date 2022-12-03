\ Copyright (c) 2022 Travis Bemann
\ 
\ Permission is hereby granted, free of charge, to any person obtaining a copy
\ of this software and associated documentation files (the "Software"), to deal
\ in the Software without restriction, including without limitation the rights
\ to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
\ copies of the Software, and to permit persons to whom the Software is
\ furnished to do so, subject to the following conditions:
\ 
\ The above copyright notice and this permission notice shall be included in
\ all copies or substantial portions of the Software.
\ 
\ THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
\ IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
\ FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
\ AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
\ LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
\ OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
\ SOFTWARE.

begin-module ssd1306-print
  
  oo import
  bitmap import
  ssd1306 import
  font import
  simple-font import
  
  begin-module ssd1306-print-internal
  
    128 constant my-width
    64 constant my-height
    
    7 constant my-char-width
    8 constant my-char-height
    
    my-width my-height bitmap-buf-size constant my-buf-size
    my-buf-size 4 align buffer: my-buf
    <ssd1306> class-size buffer: my-ssd1306
    
    my-width my-char-width / constant my-chars-width
    my-height my-char-height / constant my-chars-height
    
    my-chars-width my-chars-height * constant my-char-buf-size
    my-char-buf-size 4 align buffer: my-char-buf
    
    variable cursor-col
    variable cursor-row
    
    false value inited?

    : init-ssd1306-text ( -- )
      14 15 my-buf my-width my-height SSD1306_I2C_ADDR 1 <ssd1306> my-ssd1306 init-object
      my-char-buf my-char-buf-size $20 fill
      0 cursor-col !
      0 cursor-row !
      init-simple-font
      true to inited?
    ;
    
    : render-ssd1306-text ( -- )
      $00 0 my-width 0 my-height my-ssd1306 set-rect-const
      my-chars-height 0 ?do
        my-chars-width 0 ?do 
          my-char-buf my-chars-width j * + i + c@
          my-char-width i * my-char-height j * my-ssd1306 a-simple-font set-char
        loop
      loop
      my-ssd1306 update-display
    ;
    
    : scroll-ssd1306-text ( -- )
      my-char-buf my-chars-width + my-char-buf my-chars-width my-chars-height 1- * move
      my-char-buf my-chars-width my-chars-height 1- * + my-chars-width $20 fill
      cursor-row @ 1- 0 max cursor-row !
    ;
    
    : pre-advance-ssd1306-cursor ( -- )
      cursor-col @ my-chars-width = if
        0 cursor-col !
        1 cursor-row +!
      then
      cursor-row @ my-chars-height = if
        scroll-ssd1306-text
        0 cursor-col !
      then
    ;

    : advance-ssd1306-cursor ( -- )
      1 cursor-col +!
    ;
    
    : add-ssd1306-char { c -- }
      c $0A = if
        cursor-row @ 1+ my-chars-height min cursor-row !
      else
        c $0D = if
          0 cursor-col !
        else
          pre-advance-ssd1306-cursor
          c my-char-buf my-chars-width cursor-row @ * + cursor-col @ + c!
          advance-ssd1306-cursor
        then
      then
    ;
    
  end-module> import
  
  : clear-ssd1306 ( -- )
    inited? not if init-ssd1306-text then
    my-char-buf my-chars-width my-chars-height * $20 fill
    0 cursor-col !
    0 cursor-row !
    render-ssd1306-text
  ;
  
  : emit-ssd1306 { c -- }
    inited? not if init-ssd1306-text then
    c add-ssd1306-char
    render-ssd1306-text
  ;
  
  : type-ssd1306 { c-addr u -- }
    inited? not if init-ssd1306-text then
    u 0 ?do c-addr i + c@ add-ssd1306-char loop
    render-ssd1306-text
  ;
  
  : cr-ssd1306 ( -- )
    inited? not if init-ssd1306-text then
    cursor-row @ 1+ my-chars-height min cursor-row !
    0 cursor-col !
    render-ssd1306-text
  ;
  
  : goto-ssd1306 { col row -- }
    inited? not if init-ssd1306-text then
    col 0 max my-chars-width min cursor-col !
    row 0 max my-chars-height min cursor-row !
  ;
  
end-module