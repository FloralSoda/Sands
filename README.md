# Sands

Welcome to Sands, a 2.5D isometric dungeon crawling sandbox primarily built to experiment with new styles of music and art,
as well as exploring the way different generative technologies can influence gameplay. This project is also my (FloralSoda's) first delve into using Vulkan to render.

While the intent is to make a fun game, much of the resources and code within is a product of experimentation and
exploration of techniques new to us. As such, it won't be the cleanest code nor will it be a shining example of how to tackle these problems.

## Usage

### Building yourself

This project uses Rust and Vulkan, and requires CMake and Python to build. Please ensure those dependencies are installed on your build system.
Ninja is required to build for Windows.
Modding the game and contributing new assets can be done with the window provided by `sands toolbox`

## Gameplay

### Save Data

Changes to the backend of gameplay are unlikely to be breaking, and if they are breaking we'll do our best to write converters so that you don't lose your data.

### Resources

Resources may come and go during the initial phases of development, but as we begin to understand how the game should interact and with which resources,
things will seal in place. Saves made at this current point in time may very well end up being pulled apart as features come and go. Be prepared to overhaul and repair your worlds as needed.

## Contribution

Feel free to make issues and pull requests, just please remember this is a play project at heart, so things will be changing around internally all the time as we learn new things
and discover new techniques we want to try.
