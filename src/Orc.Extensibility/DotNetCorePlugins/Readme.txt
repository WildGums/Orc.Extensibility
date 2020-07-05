Note that this directory is heavily inspired (uses a simplified version) of DotNetCorePlugins. For
more information, please see the original repository:

https://github.com/natemcmaster/DotNetCorePlugins

The reason we don't use the library directly is that the current implementation allows shared
usage between .NET and .NET Core and supports smooth migration / switching between the 
target platform without any API changes.

**UPDATE**

After doing a Proof of Concept (PoC), we found out we can't use separate Assembly Load Context (ACL) because
WPF requires a single one. Therefore we have removed all files for now except PlatformInformation which we
use to determine the current platform runtime information.

This readme is kept in case we want to start using separate ACL in the future.