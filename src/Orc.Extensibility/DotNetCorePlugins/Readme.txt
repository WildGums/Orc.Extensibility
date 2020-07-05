Note that this directory is heavily inspired (uses a simplified version) of DotNetCorePlugins. For
more information, please see the original repository:

https://github.com/natemcmaster/DotNetCorePlugins

The reason we don't use the library directly is that the current implementation allows shared
usage between .NET and .NET Core and supports smooth migration / switching between the 
target platform without any API changes.