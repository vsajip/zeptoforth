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

compile-to-flash

begin-module oo

  internal import

  begin-module oo-internal
    
    \ The method structure
    begin-structure method-header-size
      
      \ The previous method
      field: prev-method

      \ The method index
      field: method-index

      \ The method word
      field: method-word
      
    end-structure
    
    \ The class header structure
    begin-structure class-header-size

      \ The method table of a class
      field: class-methods
      
      \ The superclass of a class
      field: class-superclass

      \ The method list of a class
      field: class-method-list
      
      \ The size of the members of a class
      field: members-size
      
      \ The number of methods of a class
      field: method-count
      
    end-structure

    \ A method of a class
    : class-method ( index class -- addr )
      class-methods @ swap 1+ cells +
    ;

    \ Find a method index by name
    : method-by-name ( c-addr u class -- index|-1 )
      class-method-list @ begin ?dup while
	dup method-word @ word-name 2over rot count
	equal-case-strings? if
	  nip nip method-index @ exit
	then
	prev-method @
      repeat
      2drop -1
    ;
    
    \ Get a method list length
    : method-list-len ( list -- len )
      0 swap begin ?dup while swap 1+ swap prev-method @ repeat
    ;
    
    \ Base new method
    : new ( object -- ) drop ;

    \ Base destroy method
    : destroy ( object -- ) drop ;

  end-module> import

  \ Method not found in class exception
  : x-method-not-in-class ( -- ) ." method not in class" cr ;
  
  \ Method not implemented exception
  : x-method-not-implemented ( -- ) ." method not implemented" cr ;

  \ Abstract method
  : abstract-method ( -- ) ['] x-method-not-implemented ?raise ;
  
  \ The object class's method list
  create new-method 0 , 0 , s" new" find ,
  create destroy-method new-method , 1 , s" destroy" find ,

  \ The object class's method table
  create object-method-table cell allot ' new , ' destroy ,
    
  \ Define the object class
  create <object> object-method-table , here cell - , destroy-method , 0 , 2 ,
  here 5 cells - object-method-table current!
  
  \ This is the entry point to the new method
  : new ( object -- ) dup @ 4 + @ execute ;

  \ This is the entry point to the destroy method
  : destroy ( object -- ) dup @ 8 + @ execute ;

  \ Begin the declaration of a class
  : begin-class
    ( super-class "name" -- class-header member-offset list method-count )
    <builds here cell allot over , over members-size @
    3 cells allot 2 pick class-method-list @ 3 roll method-count @
    does>
  ;

  \ Finish the declaration of a class
  : end-class ( class-header member-offset list method-count -- )
    2swap over members-size current! ( list method-count class-header )
    2dup method-count current!  ( list method-count class-header )
    swap 1+ cells cell align,
    flash-block-align, here swap allot flash-block-align, ( list class-header methods )
    2dup swap class-methods current! ( list class-header methods )
    over swap current! ( list class-header )
    class-method-list current!
  ;

  \ Begin the implementation of a class
  : begin-implement ( class -- class-header )
    compiling-to-flash? not if
      dup class-methods @ cell+ over class-method-list @ method-index @ 1+ cells
      0 fill
    then
  ;

  \ End the implementation of a class
  : end-implement ( class-header -- )
    dup class-method-list @
    begin ?dup while
      dup method-index @ >r
      r@ 2 pick class-method @ dup 0= compiling-to-flash? not and swap -1 = or if
	r@ 2 pick class-superclass @ class-method @
	r@ 3 pick class-method current!
      then
      rdrop prev-method @
    repeat
    drop
  ;

  \ Declare a member of a class
  : member ( member-offset list method-count size "name" -- member-offset list method-count )
    swap >r : 2 pick 65536 u< thumb-2 or if inlined then
    rot dup cell+ lit, postpone + postpone ; + swap r>
  ;
  
  \ Declare a method of a class
  : method ( list method-count "name" -- list method-count )
    :
    dup cells 65536 u< thumb-2 or if inlined then ( list method-count )
    postpone dup ( list method-count )
    postpone @ dup 1+ cells lit, ( list method-count )
    postpone + postpone @ postpone inline-execute ( list method-count )
    postpone ; ( list method-count )
    cell align, here method-header-size allot ( list method-count list' )
    rot over prev-method current! ( method-count list' )
    2dup method-index current! ( method-count list' )
    latest over method-word current! swap 1+ flash-block-align, ( list' method-count' )
  ;

  \ Define a method of a class
  : define ( class-header xt "name" -- class-header )
    token
    dup 0= triggers x-token-expected
    3 pick method-by-name dup -1 <> averts x-method-not-in-class
    2 pick class-method current!
  ;

  \ Get the size of an object of a class
  : class-size ( class -- bytes ) members-size @ cell+ ;

  \ Get the class of an object
  : object-class ( object -- class ) @ @ ;

  \ Initialize an instance of a class
  : init-object ( ? class addr -- ) tuck swap @ swap ! new ;

  \ Early-bind a method call
  : -> ( ? class "name" -- ? )
    [immediate]
    token
    dup 0= triggers x-token-expected
    2 pick method-by-name dup -1 <> averts x-method-not-in-class
    state @ if
      swap class-method lit, postpone @ postpone inline-execute
    else
      swap class-method @ execute
    then
  ;
  
  \ Allot an object and initialize it
  : with-object ( ? class xt -- )
    over class-size [:
      swap >r dup >r init-object
      r> r> over >r execute
      r> destroy
    ;] with-aligned-allot
  ;
  
end-module

compile-to-ram