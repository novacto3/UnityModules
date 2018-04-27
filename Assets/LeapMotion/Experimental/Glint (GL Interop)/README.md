# Glint - GL Interop (Experimental Module)

**This modules supports the OpenGL Graphics API only!**
You can change your graphics API settings in Edit->Project Settings->Player->Other Settings->(uncheck Auto Graphics API and add an OpenGL API to the top of the list).

Glint was built to get asynchronous GPU texture data readback. Now that newer versions of Unity have their own API for doing async GPU readback, this module will likely become stale over time.

Originally Glint supported Android. The latest DLL is Windows x64 only.

Check out the examples to see how the native DLL was built, and the repository for the native Glint library is MIT-licensed at https://github.com/leapmotion/Glint

This module is NOT under the same license as the Glint native DLL. This module is licensed under the terms of the Leap Motion Developer SDK agreement. (See the top-level Readme.)