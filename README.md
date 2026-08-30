# Chip8 Sharp

Port of the Chip Emulator from [Cooperative](https://github.com/Celizte/Cooperative), now in C#

## Download

You can download the latest release for your system [here](https://github.com/Celizte/Chip8-Sharp/releases/latest).

## Requirements

Install the .NET 10.0 SDK: https://dotnet.microsoft.com/en-us/download.

This project was made with Linux in mind, though since it's C# I am sure windows works perfectly as well.

## Build

``` bash
# First clone the repository
git clone https://github.com/Celizte/Chip8-Sharp.git
cd Chip8-Sharp

dotnet restore 
dotnet run --configuration Release
```

## Credits

This emulator uses [raylib](https://www.raylib.com/) and [Raylib-cs](https://github.com/raylib-cs/raylib-cs)

- raylib (c) 2013-2026 Ramon Santamaria (@raysan5)
- Raylib-cs (C) 2018-2025 raylib-cs
