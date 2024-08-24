# mcc2

Mini (toy) C compiler based on Nora Sandler's book ["Writing a C Compiler"](https://nostarch.com/writing-c-compiler)

## About

mcc2 is a simple (not quite) C compiler for learning how C and compilers work.
It is based on this blog series by Nora Sandler: [Writing a C Compiler](https://norasandler.com/2017/11/29/Write-a-Compiler.html), more specifically the full [book](https://norasandler.com/book/).

The compiler takes a C source file, preprocesses it using gcc, then compiles it to x86_64 assembly and finally uses gcc again to assemble and link the executable. (I might explore creating object/executable files without gcc in the future, let's see.)

Also check out my first version based on the blog only: [mcc](https://github.com/rumkugel13/mcc)

## Goals

Besides having fun and learning how compilers and C work, these are some of the goals I plan on reaching:

- [ ] Implement every Chapter from the book and successfully run the [provided tests](https://github.com/nlsandler/writing-a-c-compiler-tests)
- [ ] Implement the additional exercises
- [ ] Add arm64 backend
- [ ] Add riscv64 backend
- [ ] Interpreter for TACKY

## Progress

- [X] Part I: The Basics
  - [X] 1: A Minimal Compiler
  - [X] 2: Unary Operators
  - [X] 3: Binary Operators
    - [ ] Extra: Bitwise Operators
  - [X] 4: Logical and Relational Operators
  - [X] 5: Local Variables
    - [ ] Extra: Compound Assignment Operators
  - [X] 6: if Statements and Conditional Expressions
    - [ ] Extra: Labeled Statements and goto
  - [X] 7: Compound Statements
  - [X] 8: Loops
    - [ ] Extra: switch Statements
  - [X] 9: Functions
  - [X] 10: File Scope Variable Declarations and Storage-Class Specifiers
- [X] Part II: Types beyond int
  - [X] 11: Long Integers
  - [X] 12: Unsigned Integers
  - [X] 13: Floating-Point Numbers
    - [ ] Extra: NaN
  - [X] 14: Pointers
  - [X] 15: Arrays and Pointer Arithmetic
  - [X] 16: Characters and Strings
  - [X] 17: Supporting Dynamic Memory Allocation
  - [X] 18: Structures
    - [ ] Extra: Unions
- [ ] Part III: Optimizations
  - [ ] 19: Optimizing TACKY Programs
  - [ ] 20: Register Allocation

## Bugs

Chapter 18:

There is one test (tests/chapter_18/valid/no_structure_parameters/libraries/initializers/nested_static_struct_initializers_client) which doesn't work correctly due to double type conversions. In the function "test_implicit_conversions" the check "converted.four_d != 9223372036854777856.0" fails due to converted.four_d not having the same double type value. "converted.four_d" was initialized with a "9223372036854776833ul", which should produce the same double value (9223372036854777856.0), but somehow doesn't. I don't know why this happens, especially when all the previous tests for double in chapter 13 do not fail, so there's that.