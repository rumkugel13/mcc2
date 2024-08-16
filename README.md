# mcc2

mini (toy) c compiler based on nora sandler's book

## About

Based on [this blog series by Nora Sandler: Write a Compiler](https://norasandler.com/2017/11/29/Write-a-Compiler.html), more specifically the full [book](https://norasandler.com/book/)

Also check out my first version based on the blog only: [mcc](https://github.com/rumkugel13/mcc)

The current goal is to output assembly for the following ISA's:

- x86_64 (or just x64)
- aarch64 (or arm64)
- riscv64

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
- [ ] Part II: Types beyond int
  - [X] 11: Long Integers
  - [X] 12: Unsigned Integers
  - [X] 13: Floating-Point Numbers
    - [ ] Extra: NaN
  - [ ] 14: Pointers
  - [ ] 15: Arrays and Pointer Arithmetic
  - [ ] 16: Characters and Strings
  - [ ] 17: Supporting Dynamic Memory Allocation
  - [ ] 18: Structures
    - [ ] Extra: Unions
- [ ] Part III: Optimizations
  - [ ] 19: Optimizing TACKY Programs
  - [ ] 20: Register Allocation
