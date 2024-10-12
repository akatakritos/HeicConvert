# HEIC Converter

I was trying to help a family member move a bunch of photos off an iPhone.

Doing so created a bunch of `.heic` format image, which her windows
computer doesn't know how to handle.

There's a bunch of costly, ad-ridden, confusing apps out there that
can bulk convert HEIC to JPG. 

I wanted my own. It also gave me a place to play with Akka.net and Avalonia.

## Usage
Its real easy - press a button and pick a folder. It recursively scans
the directory looking for heic files and then converting them to JPGs.

It stores the converted images in the same folder as their sources, just
changing the file extension.

## Getting Started

1. Clone the repository
2. On mac, you'll need to install `libheif` with homebrew `brew install
   libheif`
3. I haven't tested Windows or Linux yet, but Avalonia supports them, and the
   package `LibHeif.Native` is installed and that should grab the DLL or 
   or .so files for your environment.
4. Build and run through visual studio or rider

There are extensions for VS and Rider to enable Avalonia xaml editing.

## Tech

Image conversions through
[LibHeif.Sharp](https://github.com/0xC0000054/libheif-sharp) and
[ImageSharp](https://github.com/SixLabors/ImageSharp).


